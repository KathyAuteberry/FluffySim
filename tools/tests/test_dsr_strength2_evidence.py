import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import dsr_strength2_evidence as evidence

class StrengthTwoEvidenceTests(unittest.TestCase):
    def cast(self, action=25555, second=0):
        return ['20',f'2026-10-10T21:00:{second:02d}.0000000+09:00','PRIVATE_ACTOR','PRIVATE_NAME',f'{action:X}','action','PRIVATE_TARGET','PRIVATE_TARGET_NAME','3.7']
    def inspect(self,records):
        with patch.object(evidence.logs,'iter_records',return_value=records):return evidence.inspect('unused')
    def test_sanitizes_private_fields(self):
        result=self.inspect([self.cast(),self.cast(25563,24)])
        self.assertNotIn('PRIVATE',json.dumps(result))
        self.assertEqual(result['sequences'][0]['casts'][0]['id'],25563)
    def test_malformed_lines_are_skipped(self):
        result=self.inspect([self.cast(),['20',self.cast()[1]],['35',self.cast(second=28)[1],'private']])
        self.assertEqual(len(result['sequences']),1)
        self.assertFalse(result['sequences'][0]['reached_end'])
    def test_part_one_and_later_mechanics_are_excluded(self):
        result=self.inspect([self.cast(),self.cast(25563,19),self.cast(25563,43),self.cast(25558,24)])
        self.assertEqual(result['sequences'][0]['casts'],[])
