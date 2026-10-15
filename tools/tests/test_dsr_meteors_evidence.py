import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import dsr_meteors_evidence as evidence

class MeteorEvidenceTests(unittest.TestCase):
    def cast(self,action=25569,seconds=0):
        return ['20',f'2026-10-10T21:00:{seconds:02d}.0000000+09:00','PRIVATE_ACTOR','PRIVATE_NAME',f'{action:X}','action','PRIVATE_TARGET','PRIVATE_TARGET_NAME','7']
    def inspect(self,records):
        with patch.object(evidence.logs,'iter_records',return_value=records):return evidence.inspect('unused')
    def test_partial_sanctity_does_not_claim_meteor_coverage(self):
        result=self.inspect([self.cast()])
        self.assertEqual(result['sanctity_starts'],1)
        self.assertEqual(result['meteor_starts'],0)
    def test_reports_actual_casts_and_deduplicates_effects_without_private_fields(self):
        effect=self.cast(28651,40);effect[0]='22'
        result=self.inspect([self.cast(),self.cast(25576,28),effect,effect])
        self.assertEqual(result['meteor_starts'],1)
        self.assertEqual(result['second_towers'],1)
        self.assertNotIn('PRIVATE',json.dumps(result))
        self.assertEqual(len(result['sequences'][0]['effects']),1)
    def test_malformed_and_unrelated_territories_are_skipped(self):
        self.assertEqual(self.inspect([['20','bad'],self.cast()])['sanctity_starts'],1)
        self.assertEqual(self.inspect([['01',self.cast()[1],'3D5'],self.cast()])['sanctity_starts'],0)
    def test_headmarker_and_zone_change_are_bounded(self):
        marker=['27',self.cast(seconds=20)[1],'PRIVATE','PRIVATE','0','0','011D']
        result=self.inspect([self.cast(),marker,['01',self.cast(seconds=21)[1],'3D5'],self.cast(25576,28)])
        self.assertEqual(result['sequences'][0]['markers'],[dict(t=20.0,id=285)])
        self.assertEqual(result['meteor_starts'],0)
