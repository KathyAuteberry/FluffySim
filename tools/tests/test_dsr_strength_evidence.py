import json
from pathlib import Path
from types import SimpleNamespace
import sys
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import dsr_strength_evidence as evidence


class StrengthEvidenceTests(unittest.TestCase):
    def cast(self, action=25555, time="2026-10-10T21:00:00.0000000+09:00"):
        return ["20", time, "PRIVATE_ACTOR", "PRIVATE_NAME", f"{action:X}", "action",
                "PRIVATE_TARGET", "PRIVATE_TARGET_NAME", "3.7", "100", "100", "0", "0"]

    def inspect(self, records):
        start = evidence.logs.parse_timestamp(self.cast()[1])
        pull = SimpleNamespace(index=1, start=start, end=None)
        with patch.object(evidence.logs, "find_pulls", return_value=[pull]), \
             patch.object(evidence.logs, "iter_records", return_value=records):
            return evidence.inspect("unused")

    def test_output_contains_no_player_names_or_actor_identifiers(self):
        result = self.inspect([self.cast()])
        self.assertEqual(result["territory"], 968)
        self.assertEqual(len(result["sequences"]), 1)
        self.assertNotIn("PRIVATE", json.dumps(result))

    def test_truncated_and_malformed_records_do_not_abort_inspection(self):
        result = self.inspect([self.cast(), ["20", self.cast()[1]], ["20", self.cast()[1], "id", "name", "invalid"]])
        self.assertEqual(len(result["sequences"][0]["casts"]), 1)

    def test_unrelated_actions_do_not_extend_the_fragment(self):
        result = self.inspect([self.cast(), self.cast(25558, "2026-10-10T21:00:30.0000000+09:00"), self.cast(25564)])
        self.assertEqual(len(result["sequences"][0]["casts"]), 1)
        self.assertFalse(result["sequences"][0]["complete"])
