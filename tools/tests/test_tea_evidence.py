import json
from pathlib import Path
import struct
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from tea_evidence import align_casts, decode_waymarks, inspect_log, inspect_replay
from replay_inspector import DATA_START, SEGMENT, ReplayError


def line(kind, seconds, *fields):
    return '|'.join([kind, f'2026-11-08T12:00:{seconds:06.3f}+07:00', *map(str, fields), 'hash'])


def spawn(actor, name, job=0):
    return line('03', 0, actor, name, f'{job:02X}', 50, '', '', '', 9211, 11335,
                10000, 10000, 10000, 10000, '', '', 100, 100, 0, 0)


def cast(action, seconds=2):
    return line('20', seconds, '40000001', 'Living Liquid', f'{action:X}', 'spell',
                '10000001', 'Private Player', 3.7, 100, 85, 0, 0)


def replay_bytes(build, cast_type=1):
    data = bytearray(32)
    struct.pack_into('<HBBIf', data, 0, 18470, cast_type, 0, 18470, 3.7)
    record = SEGMENT.pack(127, 32, 10, 0x1000ABCD) + data
    header = bytearray(DATA_START)
    header[:12] = b'FFXIVREPLAY\0'
    struct.pack_into('<hhIIII', header, 12, 5, 3, build, 0, 100, 90)
    struct.pack_into('<H', header, 0x20, 694)
    struct.pack_into('<i', header, 0x48, len(record))
    return header + record


class TeaEvidenceTests(unittest.TestCase):
    def inspect_lines(self, rows):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'fixture.log'
            text = '\n'.join([line('01', 0, '377', 'TEA'),
                              spawn('40000001', 'Living Liquid'),
                              spawn('10000001', 'Private Player', 24),
                              line('260', 1, 1, 1), *rows, line('260', 20, 0, 0)]) + '\n'
            path.write_text(text)
            result = inspect_log(path)
            self.assertEqual(path.read_text(), text)
            return result

    def test_export_omits_private_names_actor_ids_and_payload(self):
        report, anchors = self.inspect_lines([cast(18470),
            line('26', 3, '85E', 'Compressed Water', 29, '40000001', 'Living Liquid',
                 '10000001', 'Private Player', '00')])
        encoded = json.dumps(report)
        for secret in ('Private Player', '10000001', '40000001'):
            self.assertNotIn(secret, encoded)
        self.assertEqual(report['events'][1]['target_job'], 24)
        self.assertEqual(anchors[0]['action'], 18470)
        self.assertEqual(report['npc_ids']['Living Liquid'], [dict(name_id=9211, base_id=11335)])

    def test_number_motion_carries_sparse_pose_updates_without_interpolation(self):
        report, _ = self.inspect_lines([
            line('27', 2, '10000001', 'Private Player', 'E0000000', '0', '004F'),
            line('261', 3, 'Change', '10000001', 'PosX', '111', 'PosY', '99', 'Heading', '1'),
            line('261', 4, 'Change', '10000001', 'PosX', '112'),
        ])
        samples = report['numbered_motion']
        self.assertEqual(len(samples), 2)
        self.assertEqual(samples[1], dict(pull=1, t=3.0, number=1, x=112, z=99, heading=1))

    def test_absent_ground_target_is_not_reported_as_position(self):
        report, _ = self.inspect_lines([
            line('264', 3, '40000001', '4830', '00001', 1, '-0.015', '-0.015', '-0.015', 0, 'E0000000'),
            line('264', 4, '40000001', '4830', '00002', 1, '90.074', '109.880', '-0.015', 0, 'E0000000'),
        ])
        self.assertIsNone(report['events'][0]['ground_pose'])
        self.assertEqual(report['events'][1]['ground_pose'][:2], [90.074, 109.88])

    def test_clock_alignment_requires_multiple_distinct_action_anchors(self):
        native = [dict(action=18470, ms=10)] * 4
        logged = [dict(action=18470, epoch=100.010, pull=1, t=2)]
        self.assertIsNone(align_casts(native, logged))

    def test_clock_alignment_selects_correct_session_and_reports_jitter(self):
        native = [dict(action=a, ms=ms) for a, ms in ((18470, 1000), (18477, 2000), (18522, 3000))]
        logged = [dict(action=a, epoch=epoch, pull=4, t=t)
                  for a, epoch, t in ((18470, 101.01, 14.7), (18477, 102.0, 39.1), (18522, 102.99, 281.7))]
        logged.append(dict(action=18470, epoch=301.01, pull=12, t=14.7))
        result = align_casts(native, logged)
        self.assertEqual(result['pulls'], {4: 3})
        self.assertEqual(result['residual_ms'], [-10, 10])

    def test_replay_unknown_build_remains_opaque(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'future.dat'
            original = replay_bytes(42)
            path.write_bytes(original)
            report = inspect_replay(path, [dict(action=18470)])
            self.assertEqual(report['native_cast_schema'], 'unsupported-build')
            self.assertEqual(report['casts'], [])
            self.assertEqual(path.read_bytes(), original)

    def test_known_build_rejects_unexpected_native_cast_layout(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'invalid.dat'
            path.write_bytes(replay_bytes(14155597, cast_type=2))
            with self.assertRaises(ReplayError):
                inspect_replay(path, [dict(action=18470)])

    def test_waymarks_preserve_native_xyz_order_and_activation_mask(self):
        payload = bytearray(104)
        payload[0] = 0x11
        struct.pack_into('<8i', payload, 4, 99977, 0, 0, 0, 93056, 0, 0, 0)
        struct.pack_into('<8i', payload, 36, 123, 0, 0, 0, 0, 0, 0, 0)
        struct.pack_into('<8i', payload, 68, 94090, 0, 0, 0, 83222, 0, 0, 0)
        marks = decode_waymarks(payload)
        self.assertEqual(marks[0], dict(name='A', active=True, x=99.977, y=.123, z=94.09))
        self.assertEqual(marks[4], dict(name='1', active=True, x=93.056, y=0, z=83.222))
        self.assertFalse(marks[1]['active'])

    def test_waymark_schema_mismatch_does_not_silently_decode(self):
        with self.assertRaises(ReplayError):
            decode_waymarks(bytes(100))
        payload = bytearray(104)
        payload[1] = 1
        with self.assertRaises(ReplayError):
            decode_waymarks(payload)


if __name__ == '__main__':
    unittest.main()
