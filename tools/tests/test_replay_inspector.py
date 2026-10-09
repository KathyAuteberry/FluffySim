import io
import json
from pathlib import Path
import struct
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from replay_inspector import DATA_START, Replay, ReplayError, SEGMENT, Segment


def fixture(records=((0xF123, 10, 0x12345678, b'opaque'),), chapter=None):
    payload = b''.join(SEGMENT.pack(op, len(data), ms, actor) + data for op, ms, actor, data in records)
    header = bytearray(DATA_START)
    header[:12] = b'FFXIVREPLAY\0'
    struct.pack_into('<hhIIII', header, 12, 5, 3, 42, 0, 100, 90)
    struct.pack_into('<i', header, 0x48, len(payload))
    if chapter is not None:
        struct.pack_into('<i', header, 0x68, 1)
        struct.pack_into('<iII', header, 0x6C, *chapter)
    return header + payload


class ReplayInspectorTests(unittest.TestCase):
    def inspect_bytes(self, blob, **kwargs):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'fixture.dat'
            path.write_bytes(blob)
            with Replay(path) as replay:
                return replay.inspect(**kwargs)

    def rejected(self, blob):
        with self.assertRaises(ReplayError):
            self.inspect_bytes(blob)

    def test_unknown_packet_preserved_opaque(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'fixture.dat'
            original = fixture()
            path.write_bytes(original)
            with Replay(path, expected_build=42) as replay:
                segment = next(replay.segments())
                self.assertEqual(replay.payload(segment), b'opaque')
                self.assertNotIn('12345678', repr(segment))
                self.assertEqual(replay.inspect()['opcodes']['0xF123']['count'], 1)
            self.assertEqual(path.read_bytes(), original)

    def test_payload_accessor_rejects_invalid_bounds(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'fixture.dat'; path.write_bytes(fixture())
            with Replay(path) as replay:
                for offset, length in [(-1, 1), (0, -1), (0, 65535)]:
                    with self.assertRaises(ReplayError):
                        replay.payload(Segment(offset, 1, length, 10, 1))

    def test_filter_does_not_change_actor_aliases(self):
        blob = fixture(((1, 10, 0x12345678, b'a'), (2, 20, 0x99999999, b'b'),
                        (1, 30, 0x12345678, b'c')), chapter=(2, 0, 10))
        out = io.StringIO()
        result = self.inspect_bytes(blob, from_ms=20, to_ms=30, opcodes=[1], output=out)
        row = json.loads(out.getvalue())
        self.assertEqual(result['segment_count'], 3)
        self.assertEqual(result['selected_segment_count'], 1)
        self.assertEqual(row['actor'], 'actor-0001')
        self.assertNotIn('12345678', out.getvalue())
        self.assertNotIn('payload', row)
        self.assertEqual(row['ms'], 30)

    def test_truncated_prefix(self):
        self.rejected(b'FFXIVREPLAY')

    def test_invalid_magic(self):
        blob = fixture(); blob[0] = 0
        self.rejected(blob)

    def test_unknown_format(self):
        blob = fixture(); struct.pack_into('<h', blob, 12, 6)
        self.rejected(blob)

    def test_declared_length_mismatch(self):
        self.rejected(fixture()[:-1])

    def test_negative_declared_length(self):
        blob = fixture(); struct.pack_into('<i', blob, 0x48, -1)
        self.rejected(blob)

    def test_truncated_segment_header(self):
        blob = fixture()[:DATA_START] + b'\0' * 5
        struct.pack_into('<i', blob, 0x48, 5)
        self.rejected(blob)

    def test_truncated_segment_payload(self):
        blob = fixture(); struct.pack_into('<H', blob, DATA_START + 2, 65535)
        self.rejected(blob)

    def test_chapter_count_bounds(self):
        blob = fixture(); struct.pack_into('<i', blob, 0x68, 65)
        self.rejected(blob)

    def test_chapter_requires_segment_boundary(self):
        self.rejected(fixture(chapter=(2, 1, 10)))

    def test_chapter_requires_matching_time(self):
        self.rejected(fixture(chapter=(2, 0, 11)))

    def test_segment_time_bounds(self):
        self.rejected(fixture(((1, 101, 1, b'a'),)))

    def test_backwards_time_kept_and_reported(self):
        result = self.inspect_bytes(fixture(((1, 20, 1, b'a'), (2, 10, 1, b'b'))))
        self.assertEqual(result['backwards_timestamps'], 1)
        self.assertEqual(result['segment_count'], 2)

    def test_requested_build_must_match(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'fixture.dat'; path.write_bytes(fixture())
            with self.assertRaises(ReplayError):
                Replay(path, expected_build=43)

    def test_invalid_interval(self):
        with self.assertRaises(ReplayError):
            self.inspect_bytes(fixture(), from_ms=30, to_ms=20)


if __name__ == '__main__':
    unittest.main()
