import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import fru_apocalypse_evidence as evidence

class ApocalypseEvidenceTests(unittest.TestCase):
    def cast(self, action=40269, seconds=0):
        return ['20', f'2026-10-07T21:00:{seconds:02d}.0000000+09:00', 'PRIVATE_ID', 'PRIVATE_NAME', f'{action:X}', 'action', 'PRIVATE_TARGET', 'PRIVATE_NAME', '2']
    def inspect(self, records):
        with patch.object(evidence.logs, 'iter_records', return_value=records), patch.object(evidence.logs, 'find_pulls', return_value=[]):
            return evidence.inspect('unused')
    def test_partial_sequence_exports_no_player_information(self):
        result=self.inspect([self.cast(), self.cast(40296,13)])
        self.assertEqual(len(result),1)
        self.assertEqual(result[0]['casts'][1]['t'],13)
        self.assertNotIn('PRIVATE',json.dumps(result))
        self.assertEqual(dict(result[0]['effects']),{})
    def test_effect_receive_duplicates_are_collapsed(self):
        effect=self.cast(40297,31);effect[0]='22'
        result=self.inspect([self.cast(),effect,effect])
        self.assertEqual(result[0]['effects'][40297],[31])
    def test_event_object_coordinates_follow_act_axis_mapping(self):
        actor=['261',self.cast(seconds=13)[1],'Add','PRIVATE_ID','Type','7','BNpcID','1EB0FF','PosX','100','PosY','114','PosZ','0','Heading','1.5']
        result=self.inspect([self.cast(),actor])
        self.assertEqual(result[0]['lights'][0]['position'],[100,0,114])
        self.assertNotIn('PRIVATE',json.dumps(result))
    def test_malformed_records_and_outside_window_are_skipped(self):
        late=self.cast(40297,10);late[1]=late[1].replace('21:00:10','21:01:06')
        result=self.inspect([['20','bad'],self.cast(),['22','bad'],late])
        self.assertEqual(len(result),1)
        self.assertEqual(len(result[0]['casts']),1)
