import io
import json
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile
import unittest
import zlib
from contextlib import redirect_stdout

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import rtrace
from rtrace import BYTES, F32, GROUP, STR, U8, U16, U32, U64

HAVE_ZSTD = shutil.which('zstd') is not None


def uvar(n):
    out = bytearray()
    while True:
        b = n & 0x7F
        n >>= 7
        out.append(b | (0x80 if n else 0))
        if not n:
            return bytes(out)


def zig(n):
    return uvar((n << 1) ^ (n >> 63))


def name(s):
    data = s.encode()
    return bytes([len(data)]) + data


def fields_bytes(fields):
    out = b''
    for fname, ftype, *children in fields:
        out += name(fname) + bytes([ftype])
        if ftype == GROUP:
            out += bytes([len(children[0])]) + fields_bytes(children[0])
    return out


SCHEMA = [
    (6, 'pause', [('by_player', U8)]),
    (256, 'action', [
        ('server_time', U64), ('caster', U32), ('action_id', U32), ('facing', F32),
        ('targets', GROUP, [('id', U32), ('effects', GROUP, [('amount', U32)])]),
    ]),
    (291, 'party', [('members', GROUP, [('actor', U32), ('name', STR)]), ('leader', U8)]),
    (4095, 'packet', [('server_time', U64), ('entity', U32), ('opcode', U16), ('payload', BYTES)]),
]


def column(ftype, values):
    if ftype == U8:
        return bytes(values)
    if ftype in (STR, BYTES):
        return b''.join(uvar(len(v)) + (v.encode() if ftype == STR else v) for v in values)
    out, prev = b'', 0
    for v in values:
        cur = struct.unpack('<I', struct.pack('<f', v))[0] if ftype == F32 else v
        out += zig(cur - prev)
        prev = cur
    return out


def rows_bytes(fields, rows, top=False):
    if top:
        keys = [f for f in fields if f[0] in ('actor', 'caster')][:1]
        fields = keys + [f for f in fields if f not in keys]
        if keys:
            key = keys[0][0]
            return column(keys[0][1], [r[key] for r in rows]) + rows_bytes(
                fields[1:], sorted(rows, key=lambda r: r[key]))
    out = b''
    for fname, ftype, *children in fields:
        if ftype == GROUP:
            out += b''.join(uvar(len(r[fname])) for r in rows)
            out += rows_bytes(children[0], [c for r in rows for c in r[fname]])
        else:
            out += column(ftype, [r[fname] for r in rows])
    return out


def build(records, metadata=None, corrupt_crc=False):
    """records: list of (kind_id, clock, values); kind 1 rows carry raw metadata bytes."""
    header = rtrace.MAGIC + uvar(len(SCHEMA))
    for kid, kname, fields in SCHEMA:
        header += uvar(kid) + name(kname) + bytes([len(fields)]) + fields_bytes(fields)
    if metadata is not None:
        records = [(1, records[0][1] if records else 0, metadata)] + records
    data = uvar(len(records)) + b''.join(uvar(k) for k, _, _ in records)
    prev = 0
    for _, clock, _ in records:
        data += zig(clock - prev)
        prev = clock
    schema = {kid: fields for kid, _, fields in SCHEMA}
    for kid in dict.fromkeys(k for k, _, _ in records):
        rows = [v for k, _, v in records if k == kid]
        body = column(BYTES, rows) if kid not in schema else rows_bytes(schema[kid], rows, top=True)
        data += uvar(kid) + uvar(len(rows)) + uvar(len(body)) + body
    compressed = subprocess.run(['zstd', '-q', '-c'], input=data, capture_output=True, check=True).stdout
    body = uvar(rtrace.CODEC_ZSTD) + uvar(len(data)) + compressed
    crc = zlib.crc32(body) ^ (1 if corrupt_crc else 0)
    index = rtrace.INDEX + struct.pack('<I', 0)
    return header + rtrace.BLOCK + struct.pack('<II', len(body), crc) + body + index


@unittest.skipUnless(HAVE_ZSTD, 'zstd CLI not available')
class RTraceTests(unittest.TestCase):
    def write(self, blob):
        tmp = tempfile.NamedTemporaryFile(suffix='.rtrace', delete=False)
        tmp.write(blob)
        tmp.close()
        self.addCleanup(Path(tmp.name).unlink)
        return tmp.name

    def sample(self, **kw):
        meta = json.dumps({'clockFrequency': 1000, 'game': 'test'}).encode()
        records = [
            (256, 1000, {'server_time': 1_794_000_000_000, 'caster': 0x10000001, 'action_id': 40314,
                         'facing': 1.0897, 'targets': [{'id': 0x10000002, 'effects': [{'amount': 48}, {'amount': 7}]}]}),
            (6, 1500, {'by_player': 255}),
            (256, 2500, {'server_time': 1_794_000_001_500, 'caster': 0x40000005, 'action_id': 40315,
                         'facing': -1.0745, 'targets': []}),
            (291, 2600, {'members': [{'actor': 0x10000001, 'name': 'Someone Real'}], 'leader': 0}),
            (4095, 2700, {'server_time': 1_794_000_001_700, 'entity': 0x40000005, 'opcode': 0x1A2, 'payload': b'\x01\x02'}),
        ]
        return self.write(build(records, metadata=meta, **kw))

    def test_schema_is_parsed_from_the_header(self):
        trace = rtrace.Trace(self.sample())
        self.assertEqual(trace.kinds[256].name, 'action')
        self.assertEqual([f.name for f in trace.kinds[256].fields[4].children], ['id', 'effects'])

    def test_rows_decode_in_recording_order_with_key_column_first(self):
        trace = rtrace.Trace(self.sample())
        recs = list(trace.records())
        self.assertEqual([r.name for r in recs], ['opaque_1', 'action', 'pause', 'action', 'party', 'packet'])
        first, second = recs[1].values, recs[3].values
        self.assertEqual(first['caster'], 0x10000001)
        self.assertEqual(first['server_time'], 1_794_000_000_000)
        self.assertAlmostEqual(first['facing'], 1.0897, places=5)
        self.assertEqual(first['targets'][0]['effects'][1]['amount'], 7)
        self.assertEqual(second['action_id'], 40315)
        self.assertAlmostEqual(second['facing'], -1.0745, places=5)
        self.assertEqual(second['targets'], [])
        self.assertEqual(recs[2].values['by_player'], 255)
        self.assertEqual(recs[5].values['payload'], b'\x01\x02')
        self.assertEqual(trace.clock_hz, 1000)

    def test_keyed_rows_realign_values_stored_in_key_order(self):
        records = [
            (256, 10, {'server_time': 3, 'caster': 0x40000009, 'action_id': 1, 'facing': 0.5,
                       'targets': [{'id': 7, 'effects': [{'amount': 70}]}]}),
            (256, 20, {'server_time': 1, 'caster': 0x10000002, 'action_id': 2, 'facing': -0.5, 'targets': []}),
            (256, 30, {'server_time': 2, 'caster': 0x40000009, 'action_id': 3, 'facing': 1.5,
                       'targets': [{'id': 8, 'effects': []}]}),
            (256, 40, {'server_time': 4, 'caster': 0x10000001, 'action_id': 4, 'facing': 2.5, 'targets': []}),
        ]
        recs = list(rtrace.Trace(self.write(build(records))).records())
        self.assertEqual([(r.clock, r.values['caster'], r.values['action_id']) for r in recs],
                         [(10, 0x40000009, 1), (20, 0x10000002, 2), (30, 0x40000009, 3), (40, 0x10000001, 4)])
        self.assertEqual(recs[0].values['targets'][0]['effects'][0]['amount'], 70)
        self.assertEqual(recs[2].values['targets'][0]['id'], 8)
        self.assertAlmostEqual(recs[3].values['facing'], 2.5, places=5)

    def test_summary_and_cli_redact_identity_by_default(self):
        path = self.sample()
        buf = io.StringIO()
        with redirect_stdout(buf):
            self.assertEqual(rtrace.main([path, '--kind', 'party']), 0)
        row = json.loads(buf.getvalue())
        self.assertEqual(row['members'][0]['name'], '<redacted>')
        summary = rtrace.summary(rtrace.Trace(path))
        self.assertEqual(summary['records']['action'], 2)
        self.assertEqual(summary['duration_s'], 1.7)
        self.assertTrue(any('unreliable' in w for w in summary['warnings']))

    def test_corrupt_block_is_rejected(self):
        path = self.write(build([(6, 1, {'by_player': 0})], corrupt_crc=True))
        with self.assertRaises(rtrace.TraceError):
            list(rtrace.Trace(path).records())

    def test_non_trace_file_is_rejected(self):
        with self.assertRaises(rtrace.TraceError):
            rtrace.parse_schema(b'FFXIVREPLAY')


if __name__ == '__main__':
    unittest.main()
