#!/usr/bin/env python3
"""Read-only decoder for RaidTrace captures (.rtrace, written by the RaidtraceTracer plugin).

Layout, as observed in tracer 0.3.x files:
  "RAIDTRCE", varint kind count, then per kind: varint id, length-prefixed name, field count and
  fields (length-prefixed name, type byte; type 8 is a nested group followed by its own field count
  and fields). Kinds without a schema (e.g. 1 = tracer metadata JSON) are opaque.
  Blocks: "RTBK", u32 body length, u32 CRC-32 of the body, body = varint codec (2 = zstd), varint
  decompressed size, compressed bytes. A trailing "RTIX" index is not needed for reading.
  Decompressed block: varint row count, one varint kind per row, one zigzag-delta varint local
  clock per row, then per kind present: varint kind, varint rows, varint byte length and a
  column-major body. Columns: u8 = raw byte; u16/u32/u64 = zigzag varint deltas from the previous
  value in the column; f32 = the same applied to the IEEE-754 bit pattern; string/bytes = varint
  length + data; group = plain varint count per row, then the child columns over all children.
  A top-level key field (`actor` or `caster`) is written as the first column; the remaining
  columns follow schema order. `server_time` is epoch milliseconds (0 in tracer 0.3.0 captures);
  the per-row clock ticks at the metadata's clockFrequency (100 kHz).

Output contains other players' names and account/content IDs unless redacted (the default).
Never commit raw captures or unredacted output.
"""
from __future__ import annotations

import argparse
import collections
import json
import shutil
import struct
import subprocess
import sys
import zlib
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterator

MAGIC = b'RAIDTRCE'
BLOCK = b'RTBK'
INDEX = b'RTIX'
CODEC_ZSTD = 2
U8, U16, U32, U64, F32, STR, BYTES, GROUP = 0, 1, 2, 3, 4, 5, 6, 8
METADATA_KIND = 1
DEFAULT_CLOCK_HZ = 100_000
IDENTIFYING_FIELDS = frozenset({'name', 'account_id', 'content_id'})


class TraceError(ValueError):
    pass


@dataclass(frozen=True)
class Field:
    name: str
    type: int
    children: tuple['Field', ...] = ()


@dataclass(frozen=True)
class Kind:
    id: int
    name: str
    fields: tuple[Field, ...]


@dataclass
class Record:
    kind: int
    name: str
    clock: int
    values: dict = field(default_factory=dict)


def varint(buf: bytes, i: int) -> tuple[int, int]:
    result = shift = 0
    while True:
        if i >= len(buf):
            raise TraceError('truncated varint')
        byte = buf[i]
        i += 1
        result |= (byte & 0x7F) << shift
        shift += 7
        if byte < 0x80:
            return result, i


def unzigzag(value: int) -> int:
    return (value >> 1) ^ -(value & 1)


def _name(buf: bytes, i: int) -> tuple[str, int]:
    n = buf[i]
    return buf[i + 1:i + 1 + n].decode('utf-8'), i + 1 + n


def _fields(buf: bytes, i: int, count: int) -> tuple[tuple[Field, ...], int]:
    out = []
    for _ in range(count):
        name, i = _name(buf, i)
        kind = buf[i]
        i += 1
        if kind == GROUP:
            children, i = _fields(buf, i + 1, buf[i])
            out.append(Field(name, kind, children))
        else:
            out.append(Field(name, kind))
    return tuple(out), i


def parse_schema(buf: bytes) -> tuple[dict[int, Kind], int]:
    if not buf.startswith(MAGIC):
        raise TraceError('not a RaidTrace file')
    count, i = varint(buf, len(MAGIC))
    kinds = {}
    for _ in range(count):
        kid, i = varint(buf, i)
        name, i = _name(buf, i)
        fields, i = _fields(buf, i + 1, buf[i])
        kinds[kid] = Kind(kid, name, fields)
    return kinds, i


def _zstd(data: bytes) -> bytes:
    try:
        import zstandard  # type: ignore
        return zstandard.ZstdDecompressor().decompress(data, max_output_size=1 << 31)
    except ImportError:
        pass
    exe = shutil.which('zstd')
    if exe is None:
        raise TraceError('zstd is required: install the zstd CLI or the zstandard module')
    done = subprocess.run([exe, '-dcq'], input=data, capture_output=True, check=False)
    if done.returncode != 0:
        raise TraceError(f'zstd failed: {done.stderr.decode(errors="replace").strip()}')
    return done.stdout


def inflate(body: bytes) -> bytes:
    codec, i = varint(body, 0)
    size, i = varint(body, i)
    if codec != CODEC_ZSTD:
        raise TraceError(f'unsupported block codec {codec}')
    data = _zstd(body[i:])
    if len(data) != size:
        raise TraceError(f'block inflated to {len(data)} bytes, header says {size}')
    return data


def iter_blocks(buf: bytes, start: int) -> Iterator[bytes]:
    i = start
    while i < len(buf):
        tag = buf[i:i + 4]
        if tag == INDEX:
            return
        if tag != BLOCK:
            raise TraceError(f'expected block at {i}, found {tag!r}')
        length, crc = struct.unpack_from('<II', buf, i + 4)
        body = buf[i + 12:i + 12 + length]
        if len(body) != length:
            raise TraceError(f'truncated block at {i}')
        if zlib.crc32(body) != crc:
            raise TraceError(f'CRC mismatch in block at {i}')
        yield inflate(body)
        i += 12 + length


def _column(buf: bytes, i: int, ftype: int, count: int) -> tuple[list, int]:
    values = []
    if ftype == U8:
        values = list(buf[i:i + count])
        if len(values) != count:
            raise TraceError('truncated u8 column')
        return values, i + count
    if ftype in (STR, BYTES):
        for _ in range(count):
            n, i = varint(buf, i)
            data = buf[i:i + n]
            i += n
            values.append(data.decode('utf-8', errors='replace') if ftype == STR else data)
        return values, i
    previous = 0
    for _ in range(count):
        delta, i = varint(buf, i)
        previous += unzigzag(delta)
        if ftype == F32:
            values.append(struct.unpack('<f', struct.pack('<I', previous & 0xFFFFFFFF))[0])
        else:
            values.append(previous)
    return values, i


KEY_FIELDS = ('actor', 'caster')


def _column_order(fields: tuple[Field, ...]) -> list[Field]:
    keys = [f for f in fields if f.name in KEY_FIELDS][:1]
    return keys + [f for f in fields if f not in keys]


def _rows(buf: bytes, i: int, fields: tuple[Field, ...], count: int, top: bool = False) -> tuple[list[dict], int]:
    rows = [{} for _ in range(count)]
    for f in (_column_order(fields) if top else fields):
        if f.type == GROUP:
            sizes = []
            for _ in range(count):
                n, i = varint(buf, i)
                sizes.append(n)
            children, i = _rows(buf, i, f.children, sum(sizes))
            at = 0
            for row, n in zip(rows, sizes):
                row[f.name] = children[at:at + n]
                at += n
        else:
            values, i = _column(buf, i, f.type, count)
            for row, value in zip(rows, values):
                row[f.name] = value
    return rows, i


def decode_block(data: bytes, kinds: dict[int, Kind]) -> Iterator[Record]:
    count, i = varint(data, 0)
    order = []
    for _ in range(count):
        kid, i = varint(data, i)
        order.append(kid)
    clocks, clock = [], 0
    for _ in range(count):
        delta, i = varint(data, i)
        clock += unzigzag(delta)
        clocks.append(clock)
    sections: dict[int, list[dict]] = {}
    while i < len(data):
        kid, i = varint(data, i)
        rows, i = varint(data, i)
        length, i = varint(data, i)
        body = data[i:i + length]
        i += length
        kind = kinds.get(kid)
        if kind is None:
            values, end = _column(body, 0, BYTES, rows)
            decoded = [{'payload': v} for v in values]
        else:
            decoded, end = _rows(body, 0, kind.fields, rows, top=True)
        if end != length:
            raise TraceError(f'kind {kid} section is {length} bytes, decoded {end}')
        sections[kid] = decoded
    expected = collections.Counter(order)
    for kid, n in expected.items():
        if len(sections.get(kid, ())) != n:
            raise TraceError(f'kind {kid}: {n} rows ordered, {len(sections.get(kid, ()))} decoded')
    cursor = collections.Counter()
    for kid, clk in zip(order, clocks):
        row = sections[kid][cursor[kid]]
        cursor[kid] += 1
        name = kinds[kid].name if kid in kinds else f'opaque_{kid}'
        yield Record(kid, name, clk, row)


class Trace:
    def __init__(self, path: Path | str):
        self.path = Path(path)
        self.data = self.path.read_bytes()
        self.kinds, self.body_start = parse_schema(self.data)
        self._metadata = None

    def records(self) -> Iterator[Record]:
        for block in iter_blocks(self.data, self.body_start):
            yield from decode_block(block, self.kinds)

    @property
    def metadata(self) -> dict:
        if self._metadata is None:
            self._metadata = {}
            for rec in self.records():
                if rec.kind == METADATA_KIND:
                    try:
                        self._metadata = json.loads(rec.values['payload'])
                    except (ValueError, KeyError):
                        pass
                    break
        return self._metadata

    @property
    def clock_hz(self) -> int:
        return int(self.metadata.get('clockFrequency', DEFAULT_CLOCK_HZ))


def redact(value, enabled: bool = True):
    if not enabled:
        return value
    if isinstance(value, dict):
        return {k: ('<redacted>' if k in IDENTIFYING_FIELDS and v not in (0, '', b'') else redact(v))
                for k, v in value.items()}
    if isinstance(value, list):
        return [redact(v) for v in value]
    return value


def _jsonable(value):
    if isinstance(value, bytes):
        return value.hex()
    if isinstance(value, dict):
        return {k: _jsonable(v) for k, v in value.items()}
    if isinstance(value, list):
        return [_jsonable(v) for v in value]
    return value


MIN_RELIABLE_PLUGIN = (0, 3, 1)


def _version(text: str) -> tuple[int, ...]:
    try:
        return tuple(int(p) for p in str(text).split('.'))
    except ValueError:
        return (0,)


def summary(trace: Trace) -> dict:
    counts = collections.Counter()
    first = last = start = None
    run = None
    for rec in trace.records():
        counts[rec.name] += 1
        first = rec.clock if first is None else min(first, rec.clock)
        last = rec.clock if last is None else max(last, rec.clock)
        if rec.name == 'recording' and start is None:
            start = rec.clock
            runs = rec.values.get('run') or []
            run = runs[0] if runs else None
    meta = {k: v for k, v in trace.metadata.items() if k in ('clockFrequency', 'dalamud', 'game', 'platform', 'plugin')}
    begin = start if start is not None else first
    warnings = []
    if _version(meta.get('plugin', '0')) < MIN_RELIABLE_PLUGIN:
        warnings.append(f"written by tracer {meta.get('plugin', '?')}; captures before 0.3.1 are unreliable "
                        "(missing actions, zero server_time) and raidtrace.com rejects them")
    return {
        'warnings': warnings,
        'file': trace.path.name,
        'metadata': meta,
        'run': run,
        'duration_s': None if begin is None else round((last - begin) / trace.clock_hz, 3),
        'records': dict(counts.most_common()),
        'kinds_in_schema': len(trace.kinds),
    }


def main(argv=None) -> int:
    cli = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    cli.add_argument('trace', type=Path)
    cli.add_argument('--kind', action='append', default=[], help='emit records of this kind as JSON lines (repeatable)')
    cli.add_argument('--limit', type=int, default=0, help='stop after this many emitted records')
    cli.add_argument('--schema', action='store_true', help='print the embedded schema')
    cli.add_argument('--no-redact', action='store_true', help='keep player names and account/content IDs')
    args = cli.parse_args(argv)
    try:
        trace = Trace(args.trace)
        if args.schema:
            def fmt(fields):
                return [{'name': f.name, 'type': f.type, **({'fields': fmt(f.children)} if f.children else {})} for f in fields]
            print(json.dumps({k.id: {'name': k.name, 'fields': fmt(k.fields)} for k in trace.kinds.values()}, indent=1))
            return 0
        if not args.kind:
            print(json.dumps(summary(trace), indent=1, ensure_ascii=False))
            return 0
        wanted = set(args.kind)
        hz = trace.clock_hz
        emitted = 0
        for rec in trace.records():
            if rec.name not in wanted:
                continue
            row = {'kind': rec.name, 'clock_s': round(rec.clock / hz, 5), **redact(rec.values, not args.no_redact)}
            print(json.dumps(_jsonable(row), ensure_ascii=False))
            emitted += 1
            if args.limit and emitted >= args.limit:
                break
    except TraceError as e:
        print(f'rtrace: {e}', file=sys.stderr)
        return 2
    except BrokenPipeError:
        pass
    return 0


if __name__ == '__main__':
    sys.exit(main())
