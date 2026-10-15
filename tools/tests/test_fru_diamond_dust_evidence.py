import json
from pathlib import Path
from types import SimpleNamespace
import sys
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import fru_diamond_dust_evidence as evidence


class DiamondDustEvidenceTests(unittest.TestCase):
    def inspect(self, records):
        start = evidence.log_parser.parse_timestamp("2026-10-07T21:16:13.0000000+09:00")
        pull = SimpleNamespace(index=4, start=start, end=None)
        with patch.object(evidence.log_parser, "find_pulls", return_value=[pull]), patch.object(evidence.log_parser, "iter_records", return_value=records):
            return evidence.inspect_log("unused")

    def cast(self):
        return ["20", "2026-10-07T21:16:13.0000000+09:00", "PRIVATE_ACTOR", "PRIVATE_NAME", "9CF4", "Mirror Image", "PRIVATE_TARGET", "PRIVATE_TARGET_NAME", "2.7", "100", "105", "0", "0"]

    def test_exports_no_actor_identifiers_or_names(self):
        count, windows, skipped = self.inspect([self.cast()])
        self.assertEqual(count, 1)
        self.assertEqual(skipped, 0)
        self.assertEqual(windows[4]["casts"][0]["id"], 40180)
        self.assertNotIn("PRIVATE", json.dumps(windows))

    def test_truncated_records_are_counted_and_skipped(self):
        count, windows, skipped = self.inspect([self.cast(), ["20", "2026-10-07T21:16:14.0000000+09:00"]])
        self.assertEqual(skipped, 1)
        self.assertEqual(len(windows[4]["casts"]), 1)

    def test_later_unrelated_actions_are_outside_window(self):
        later = self.cast()
        later[1] = "2026-10-07T21:17:14.0000000+09:00"
        later[4] = "9DCE"
        count, windows, skipped = self.inspect([self.cast(), later])
        self.assertEqual(len(windows[4]["casts"]), 1)
