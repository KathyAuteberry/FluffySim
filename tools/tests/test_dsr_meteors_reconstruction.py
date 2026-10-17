from datetime import timedelta
import json
import math
from pathlib import Path
import sys
import unittest
import tempfile
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import dsr_meteors_reconstruction as evidence

FIXTURE=Path(__file__).parent/'fixtures/dsr_meteors_complete_failed.log'

class ReconstructionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.result=evidence.analyze(FIXTURE)
        cls.pull=cls.result['sequences'][0]
        cls.events=cls.pull['events']

    def effects(self,action):
        return [e for e in self.events if e.get('action')==action and e['kind'] in ('21','22') and e['release']]

    def test_complete_failed_execution_does_not_become_successful_clear(self):
        self.assertEqual(self.result['meteor_attempts'],1)
        self.assertTrue(self.pull['complete_execution'])
        self.assertEqual(len(self.effects(25577)),14)
        self.assertEqual(len(self.effects(29564)),8)
        self.assertEqual(len(self.effects(28651)),8)
        self.assertTrue(any(e['kind']=='25' for e in self.events))

    def test_observed_sequence_and_intervals(self):
        fire=self.effects(28591)[0]['t']; ice=self.effects(25575)[0]['t']
        first=self.effects(29564)[0]['t']; comet=self.effects(25577)[0]['t']
        knockback=self.effects(25308)[0]['t']; second=self.effects(28651)[0]['t']
        self.assertLess(fire,ice);self.assertLess(ice,first);self.assertLess(first,comet)
        self.assertLess(comet,knockback);self.assertLess(knockback,second)
        self.assertAlmostEqual(comet-first,1.431,places=3)
        for a,b in zip(self.effects(25577)[::2],self.effects(25577)[2::2]):
            self.assertAlmostEqual(b['t']-a['t'],1.43,delta=.01)
        self.assertEqual(len([e for e in self.events if e.get('marker')==285]),2)
        self.assertTrue({562,2903,2945,2946,1209}.issubset({e['status'] for e in self.events if 'status' in e}))

    def test_observed_geometry_and_coordinate_axes(self):
        self.assertEqual(evidence.position(['108.49','91.51','2']),[8.49,2.,-8.49])
        for e in self.effects(29564):
            radius=math.hypot(e['position'][0],e['position'][2])
            self.assertTrue(abs(radius-18)<.02 or abs(radius-6)<.02)
        for e in self.effects(28651):
            self.assertAlmostEqual(math.hypot(e['position'][0],e['position'][2]),18,delta=.02)
        for e in self.effects(28591):
            self.assertEqual({abs(e['position'][0]),abs(e['position'][2])},{8.49})

    def test_ice_is_at_pairs_fire_is_fixed_and_presence_is_not_activation(self):
        fire=[e for e in self.events if e.get('object')==0x1EB686 and 'position'in e]
        ice=[e for e in self.events if e.get('object')==0x1EB682 and 'position'in e]
        self.assertEqual(len(fire),4);self.assertEqual(len(ice),4)
        self.assertTrue(all(abs(abs(e['position'][0])-8.4853)<.001 for e in fire))
        self.assertTrue(all(min(abs(e['position'][0]),abs(e['position'][2]))<1 for e in ice))
        self.assertNotIn('activation',json.dumps(fire+ice))
        self.assertTrue(all('line'in e and 'timestamp'in e and e['confidence']=='observed' for e in self.events))

    def inspect_records(self,records):
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'fixture.log';path.write_text('\n'.join('|'.join(r) for r in records)+'\n')
            return evidence.analyze(path)

    def test_failed_tower_punishment_is_retained_as_a_separate_later_action(self):
        records=list(evidence.logs.iter_records(FIXTURE))
        marker=next(r for r in records if r[0]=='27')
        failure=next(r.copy() for r in records if r[0] in ('21','22') and int(r[4],16)==29564)
        failure[1]=(evidence.logs.parse_timestamp(marker[1])+timedelta(seconds=14.117)).isoformat()
        failure[4]='63E0';failure[5]='Eternal Conviction';failure[44]='0000FFFF'
        records.append(failure)
        records.sort(key=lambda r:evidence.logs.parse_timestamp(r[1]))
        result=self.inspect_records(records)
        failures=[e for e in result['sequences'][0]['events'] if e.get('action')==25568]
        self.assertEqual(len(failures),1)
        self.assertAlmostEqual(failures[0]['t'],14.117,places=3)
        self.assertTrue(failures[0]['release'])

    def test_zone_exit_ends_coverage_and_does_not_import_inn_events(self):
        records=list(evidence.logs.iter_records(FIXTURE))
        first_marker=next(i for i,r in enumerate(records) if r[0]=='27')
        end=['01',records[first_marker][1],'B1','Inn','fixture']
        result=self.inspect_records(records[:first_marker+1]+[end]+records[first_marker+1:])
        self.assertFalse(result['sequences'][0]['complete_execution'])
        self.assertEqual(result['sequences'][0]['release_counts'],{})

    def test_multi_target_duplicate_does_not_inflate_source_release_count(self):
        records=list(evidence.logs.iter_records(FIXTURE))
        index=next(i for i,r in enumerate(records) if r[0] in ('21','22') and r[4]=='63E9')
        records.insert(index+1,records[index].copy())
        result=self.inspect_records(records)
        self.assertEqual(result['sequences'][0]['release_counts'][25577],14)

    def test_missing_second_wave_is_incomplete_even_with_fourteen_comets(self):
        records=[r for r in evidence.logs.iter_records(FIXTURE) if not(r[0] in ('21','22') and r[4]=='6FEB')]
        result=self.inspect_records(records)
        self.assertEqual(result['sequences'][0]['release_counts'][25577],14)
        self.assertFalse(result['sequences'][0]['complete_execution'])

if __name__=='__main__':unittest.main()
