#!/usr/bin/env python3
"""Read-only, dependency-free inspection of native format-5 Duty Recorder files.

Outputs omit raw actor IDs, names, recorder identity and packet contents. Opaque
payloads remain accessible by offset through Replay.payload; no opcode schema is
assumed. --segments writes filtered metadata, not a playable or converted replay.
Format provenance and recording-specific interpretations live in FRU-ROADMAP.md.
"""
import argparse
import collections
import datetime
import hashlib
import json
import math
import mmap
from pathlib import Path
import struct
import sys
from dataclasses import dataclass, field

HEADER_SIZE = 0x68
CHAPTER_COUNT = 64
CHAPTER_SIZE = 12
DATA_START = HEADER_SIZE + 4 + CHAPTER_COUNT * CHAPTER_SIZE
SEGMENT = struct.Struct('<HHII')


class ReplayError(ValueError):
    pass


@dataclass(frozen=True)
class Segment:
    offset: int
    opcode: int
    length: int
    ms: int
    actor: int = field(repr=False)

    @property
    def payload_offset(self):
        return DATA_START + self.offset + SEGMENT.size


class Replay:
    def __init__(self, path, expected_build=None):
        self.file = open(path, 'rb')
        self.data = None
        try:
            self.file.seek(0, 2)
            if self.file.tell() < DATA_START:
                raise ReplayError('truncated header/chapter table')
            self.data = mmap.mmap(self.file.fileno(), 0, access=mmap.ACCESS_READ)
            self._read_header(expected_build)
        except Exception:
            self.close()
            raise

    def _read_header(self, expected_build):
        b = self.data
        if b[:12] != b'FFXIVREPLAY\0':
            raise ReplayError('invalid replay magic')
        version, os_type, build, timestamp, total, displayed = struct.unpack_from('<hhIIII', b, 12)
        if version != 5:
            raise ReplayError(f'unsupported format version {version}; no offsets assumed')
        if expected_build is not None and build != expected_build:
            raise ReplayError('game build differs from requested build')
        if displayed > total:
            raise ReplayError('displayed duration exceeds total duration')
        length = struct.unpack_from('<i', b, 0x48)[0]
        if length < 0 or length != len(b) - DATA_START:
            raise ReplayError('declared segment area does not match physical file size')
        count = struct.unpack_from('<i', b, HEADER_SIZE)[0]
        if not 0 <= count <= CHAPTER_COUNT:
            raise ReplayError('chapter count outside fixed table')
        self.length = length
        self.header = dict(format_version=version, operating_system_type=os_type,
                           game_build=build, recorded_utc=datetime.datetime.fromtimestamp(
                               timestamp, datetime.UTC).isoformat(), total_ms=total,
                           displayed_ms=displayed, content_id=struct.unpack_from('<H', b, 0x20)[0],
                           data_bytes=length)
        self.chapters = []
        previous_ms = previous_offset = 0
        for i in range(count):
            kind, offset, ms = struct.unpack_from('<iII', b, HEADER_SIZE + 4 + i * CHAPTER_SIZE)
            if offset >= length or ms > total or offset < previous_offset or ms < previous_ms:
                raise ReplayError(f'invalid chapter bounds/order at chapter {i}')
            self.chapters.append(dict(index=i, type=kind, offset=offset, ms=ms))
            previous_ms, previous_offset = ms, offset

    def segments(self):
        offset = 0
        while offset < self.length:
            if self.length - offset < SEGMENT.size:
                raise ReplayError(f'truncated segment header at data offset {offset}')
            op, size, ms, actor = SEGMENT.unpack_from(self.data, DATA_START + offset)
            if offset + SEGMENT.size + size > self.length:
                raise ReplayError(f'truncated segment payload at data offset {offset}')
            if ms > self.header['total_ms']:
                raise ReplayError(f'segment timestamp exceeds duration at data offset {offset}')
            yield Segment(offset, op, size, ms, actor)
            offset += SEGMENT.size + size

    def payload(self, segment):
        if (segment.offset < 0 or segment.length < 0
                or segment.offset + SEGMENT.size + segment.length > self.length):
            raise ReplayError('payload outside file boundaries')
        return self.data[segment.payload_offset:segment.payload_offset + segment.length]

    def inspect(self, from_ms=0, to_ms=None, opcodes=None, output=None):
        if to_ms is None:
            to_ms = self.header['total_ms']
        if from_ms < 0 or to_ms < from_ms:
            raise ReplayError('invalid time interval')
        freq = collections.Counter()
        selected = collections.Counter()
        lengths = collections.defaultdict(collections.Counter)
        chapters = {c['offset']: c for c in self.chapters}
        seen_chapters = set()
        actors = {}
        previous = None
        backwards = 0
        first_ms = last_ms = None
        for s in self.segments():
            freq[s.opcode] += 1
            lengths[s.opcode][s.length] += 1
            if previous is not None and s.ms < previous:
                backwards += 1
            previous = s.ms
            if first_ms is None:
                first_ms = s.ms
            last_ms = s.ms
            if s.offset in chapters:
                chapter = chapters[s.offset]
                if chapter['ms'] != s.ms:
                    raise ReplayError(f'chapter {chapter["index"]} timestamp does not match its segment')
                seen_chapters.add(s.offset)
            # Assign aliases in whole-file order, so filtering cannot renumber them.
            if s.actor not in actors:
                actors[s.actor] = f'actor-{len(actors) + 1:04d}'
            if from_ms <= s.ms <= to_ms and (not opcodes or s.opcode in opcodes):
                selected[s.opcode] += 1
                if output is not None:
                    row = dict(data_offset=s.offset, payload_offset=s.payload_offset,
                               opcode=f'0x{s.opcode:04X}', ms=s.ms,
                               actor=actors[s.actor], payload_bytes=s.length,
                               payload_sha256=hashlib.sha256(self.payload(s)).hexdigest())
                    output.write(json.dumps(row) + '\n')
        if seen_chapters != set(chapters):
            raise ReplayError('chapter offset does not point to a segment boundary')
        return dict(header=self.header, chapters=self.chapters, segment_count=sum(freq.values()),
                    distinct_opcodes=len(freq), first_segment_ms=first_ms, last_segment_ms=last_ms,
                    backwards_timestamps=backwards,
                    opcodes={f'0x{op:04X}': dict(count=count, payload_lengths=dict(lengths[op]))
                             for op, count in sorted(freq.items())},
                    selected_interval_ms=[from_ms, to_ms], selected_segment_count=sum(selected.values()),
                    selected_opcodes={f'0x{op:04X}': count for op, count in sorted(selected.items())})

    def close(self):
        if self.data is not None:
            self.data.close()
            self.data = None
        self.file.close()

    def __enter__(self):
        return self

    def __exit__(self, *_):
        self.close()


def seconds(value):
    result = float(value)
    if not math.isfinite(result) or result < 0:
        raise argparse.ArgumentTypeError('time must be finite and nonnegative')
    return round(result * 1000)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('replay', type=Path)
    parser.add_argument('--expect-build', type=int)
    parser.add_argument('--from-seconds', type=seconds, default=0)
    parser.add_argument('--to-seconds', type=seconds)
    parser.add_argument('--opcode', action='append', type=lambda s: int(s, 0))
    parser.add_argument('--segments', type=Path, help='new JSONL file; actor references anonymized, payloads opaque')
    args = parser.parse_args()
    try:
        with Replay(args.replay, args.expect_build) as replay:
            if args.segments:
                # Exclusive creation prevents accidentally overwriting the recording or a report.
                with args.segments.open('x', encoding='utf-8') as output:
                    result = replay.inspect(args.from_seconds, args.to_seconds, args.opcode, output)
            else:
                result = replay.inspect(args.from_seconds, args.to_seconds, args.opcode)
        print(json.dumps(result, indent=2))
        return 0
    except (ReplayError, OSError) as e:
        print(f'inspection failed: {e if isinstance(e, ReplayError) else type(e).__name__}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
