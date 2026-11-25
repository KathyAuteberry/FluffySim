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


START_SCHEMA = [
    (7, 'start', [('tracer', STR), ('host', STR), ('framework', STR), ('game', STR), ('platform', STR),
                  ('capture_session', U64)]),
    (8, 'written', [('format', U32), ('writer', STR), ('clock_frequency', U32), ('history', GROUP, [('note', STR)])]),
]

SCHEMA = [
    (6, 'pause', [('by_player', U8)]),
    (256, 'action', [
        ('server_time', U64), ('caster', U32), ('action_id', U32), ('facing', F32),
        ('targets', GROUP, [('id', U32), ('effects', GROUP, [('amount', U32)])]),
    ]),
    (291, 'party', [('members', GROUP, [('actor', U32), ('name', STR)]), ('leader', U8)]),
    (296, 'zone_init', [('server_time', U64), ('server', U16), ('territory', U16), ('instance', U16), ('content', U16)]),
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


def build(records, metadata=None, corrupt_crc=False, fmt=2, arrivals=None):
    """records: list of (kind_id, clock, values); kind 1 rows carry raw metadata bytes.
    Format 3 also writes one (bundle, repeats) arrival per record (default (0, 0))."""
    kinds = SCHEMA + (START_SCHEMA if fmt >= 3 else [])
    header = (rtrace.MAGIC3 + bytes([fmt]) if fmt >= 3 else rtrace.MAGIC) + uvar(len(kinds))
    for kid, kname, fields in kinds:
        header += uvar(kid) + name(kname) + bytes([len(fields)]) + fields_bytes(fields)
    if metadata is not None:
        records = [(1, records[0][1] if records else 0, metadata)] + records
    data = uvar(len(records)) + b''.join(uvar(k) for k, _, _ in records)
    prev = 0
    for _, clock, _ in records:
        data += zig(clock - prev)
        prev = clock
    if fmt >= 3:
        arrivals = arrivals or [(0, 0)] * len(records)
        last = 0
        for bundle, _ in arrivals:
            data += uvar(0) if not bundle else uvar(((bundle - last) << 1 ^ (bundle - last) >> 63) + 1)
            last = bundle or last
        data += b''.join(uvar(repeats) for _, repeats in arrivals)
    schema = {kid: fields for kid, _, fields in kinds}
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

    def sample3(self):
        records = [
            (7, 5000, {'tracer': '0.4.0.0', 'host': 'dalamud', 'framework': 'Dalamud 15.0.4.1',
                       'game': '2026.11.03.0000.0000', 'platform': 'linux', 'capture_session': 77}),
            (8, 5000, {'format': 3, 'writer': 'raidtrace-tracer 0.4.0.0', 'clock_frequency': 1000, 'history': []}),
            (291, 1000, {'members': [{'actor': 0x10000001, 'name': 'Someone Real'}], 'leader': 0}),
            (296, 5000, {'server_time': 1_795_000_000_000, 'server': 31, 'territory': 1363, 'instance': 19,
                         'content': 1094}),
            (256, 5300, {'server_time': 1_795_000_000_300, 'caster': 0x40000005, 'action_id': 40315,
                         'facing': 0.25, 'targets': []}),
            (6, 5300, {'by_player': 1}),
            (256, 5400, {'server_time': 1_795_000_000_400, 'caster': 0x40000005, 'action_id': 40316,
                         'facing': 0.5, 'targets': []}),
            (4095, 8000, {'server_time': 1_795_000_003_000, 'entity': 0x40000005, 'opcode': 7, 'payload': b''}),
        ]
        arrivals = [(0, 0), (0, 0), (0, 0), (0, 0), (45229, 0), (0, 0), (45229, 2), (45231, 0)]
        return self.write(build(records, fmt=3, arrivals=arrivals))

    def test_format3_header_and_arrival_columns(self):
        trace = rtrace.Trace(self.sample3())
        self.assertEqual(trace.format, 3)
        recs = list(trace.records())
        self.assertEqual([r.name for r in recs],
                         ['start', 'written', 'party', 'zone_init', 'action', 'pause', 'action', 'packet'])
        self.assertEqual([r.bundle for r in recs], [0, 0, 0, 0, 45229, 0, 45229, 45231])
        self.assertEqual([r.repeats for r in recs], [0, 0, 0, 0, 0, 0, 2, 0])
        self.assertEqual(recs[6].values['action_id'], 40316)
        self.assertEqual(recs[7].values['opcode'], 7)

    def test_format3_metadata_and_summary_come_from_start_and_zone_init(self):
        trace = rtrace.Trace(self.sample3())
        self.assertEqual(trace.metadata['plugin'], '0.4.0.0')
        self.assertEqual(trace.clock_hz, 1000)
        summary = rtrace.summary(trace)
        self.assertEqual(summary['warnings'], [])
        self.assertEqual(summary['format'], 3)
        self.assertEqual(summary['metadata']['writer'], 'raidtrace-tracer 0.4.0.0')
        self.assertEqual(summary['run'], {'server': 31, 'territory': 1363, 'instance': 19, 'content': 1094})
        self.assertEqual(summary['duration_s'], 3.0)

    def test_format3_cli_emits_arrival_and_redacts(self):
        path = self.sample3()
        buf = io.StringIO()
        with redirect_stdout(buf):
            self.assertEqual(rtrace.main([path, '--kind', 'action', '--kind', 'party']), 0)
        rows = [json.loads(line) for line in buf.getvalue().splitlines()]
        self.assertEqual(rows[0]['members'][0]['name'], '<redacted>')
        self.assertNotIn('bundle', rows[0])
        self.assertEqual((rows[1]['bundle'], rows[1].get('repeats')), (45229, None))
        self.assertEqual((rows[2]['bundle'], rows[2]['repeats'], rows[2]['clock_s']), (45229, 2, 5.4))

    def test_format2_has_no_arrival_columns(self):
        recs = list(rtrace.Trace(self.sample()).records())
        self.assertTrue(all(r.bundle == 0 and r.repeats == 0 for r in recs))
        self.assertEqual(rtrace.Trace(self.sample()).format, 2)

    def test_unknown_format_is_rejected(self):
        with self.assertRaises(rtrace.TraceError):
            rtrace.parse_header(rtrace.MAGIC3 + bytes([9]) + uvar(0))

    def test_corrupt_block_is_rejected(self):
        path = self.write(build([(6, 1, {'by_player': 0})], corrupt_crc=True))
        with self.assertRaises(rtrace.TraceError):
            list(rtrace.Trace(path).records())

    def test_non_trace_file_is_rejected(self):
        with self.assertRaises(rtrace.TraceError):
            rtrace.parse_schema(b'FFXIVREPLAY')


if __name__ == '__main__':
    unittest.main()
