import importlib.util
from pathlib import Path
import sys
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import fru_fall_of_faith_evidence as evidence


def cast(seconds, action):
    return ['20', f'2026-10-14T12:00:{seconds:06.3f}+09:00', '40000001', 'Private actor name', f'{action:X}', '', '', '', '8.700']


def effect(seconds, action):
    return ['21', f'2026-10-14T12:00:{seconds:06.3f}+09:00', '40000001', 'Private actor name', f'{action:X}']


class FaithEvidenceTests(unittest.TestCase):
    def test_complete_and_partial_are_counted_without_identity_output(self):
        records = [cast(0,40140), cast(3.1,40137), cast(5.6,40140), cast(8.1,40137),
                   effect(13.1,40142), effect(16.13,40156), effect(18.62,40142), effect(21.15,40156),
                   cast(40,40137), effect(53.1,40156)]
        with patch.object(evidence.logs, 'iter_records', side_effect=lambda _: iter(records)):
            result = evidence.inspect(Path('unused'))
        self.assertEqual(result['starts'],2)
        self.assertEqual(result['complete'],1)
        self.assertEqual(result['sequences'][0]['pattern'],'LFLF')
        self.assertNotIn('Private actor name',str(result))
        self.assertNotIn('40000001',str(result))

    def test_other_p1_bound_of_faith_is_not_inventoried(self):
        with patch.object(evidence.logs, 'iter_records', side_effect=lambda _: iter([cast(0,40165), effect(12,40167)])):
            self.assertEqual(evidence.inspect(Path('unused'))['starts'],0)

    def test_duplicate_ability_targets_are_one_resolution(self):
        records = [cast(0,40137)] + [effect(t,40156) for t in (13.1,13.1,16.13,18.62,21.15)]
        with patch.object(evidence.logs, 'iter_records', side_effect=lambda _: iter(records)):
            result=evidence.inspect(Path('unused'))
        self.assertEqual(result['complete'],1)
        self.assertEqual(len(result['sequences'][0]['primary_hits']),4)
