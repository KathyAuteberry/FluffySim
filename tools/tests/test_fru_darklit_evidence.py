import datetime
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import fru_darklit_evidence as evidence

class DarklitEvidenceTests(unittest.TestCase):
    def cast(self, action=40239, seconds=0):
        return ['20', f'2026-10-11T21:00:{seconds:02d}.0000000+09:00', 'PRIVATE_ID', 'PRIVATE_NAME', f'{action:X}', 'action', 'PRIVATE_TARGET', 'PRIVATE_NAME', '4.7']
    def inspect(self, rows):
        with patch.object(evidence.logs, 'iter_records', return_value=rows), patch.object(evidence.logs, 'find_pulls', return_value=[]):
            return evidence.inspect('unused')
    def test_partial_pull_does_not_claim_later_resolution(self):
        result=self.inspect([self.cast(),self.cast(40283,22)])
        self.assertEqual(result['darklit_starts'],1)
        self.assertEqual(result['sequences'][0]['casts'][1]['id'],40283)
        self.assertNotIn(40284,result['sequences'][0]['effects'])
    def test_exports_no_names_actor_ids_or_private_target_references(self):
        tether=['35',self.cast(seconds=5)[1],'PRIVATE_ID','PRIVATE_NAME','PRIVATE_TARGET','PRIVATE_NAME','0000','0000','006E','PRIVATE_PARAM']
        result=self.inspect([self.cast(),tether])
        self.assertEqual(result['sequences'][0]['tethers'],[dict(t=5,id=110)])
        self.assertNotIn('PRIVATE',json.dumps(result))
    def test_duplicate_effect_packets_are_deduplicated(self):
        effect=self.cast(40213,16);effect[0]='22'
        result=self.inspect([self.cast(),effect,effect])
        self.assertEqual(result['sequences'][0]['effects'][40213],[16])
    def test_malformed_and_outside_window_records_are_skipped(self):
        late=self.cast(40285,45)
        result=self.inspect([['20','bad'],self.cast(),late])
        self.assertEqual(len(result['sequences'][0]['casts']),1)
    def test_map_effects_keep_only_verified_numeric_fields(self):
        effect=['257',self.cast(seconds=8)[1],'PRIVATE_INSTANCE','00200010','2A']
        result=self.inspect([self.cast(),effect])
        self.assertEqual(result['sequences'][0]['map_effects'],[dict(t=8,index=42,flags=0x00200010)])
