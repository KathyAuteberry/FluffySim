"""Sanitized, read-only Sacred Severs receive timeline and NPC geometry inventory."""
import argparse
import collections
import json
from pathlib import Path
import parser as logs

ACTIONS = {25569, 25552, 25553, 25554, 25570, 25571, 25295}

def inspect(path):
    windows = []; current = None; actors = {}; positions = {}; pull = 0
    for r in logs.iter_records(path):
        try:
            t = logs.parse_timestamp(r[1]); kind = r[0]
            if kind == '03':
                actors[r[2]] = (int(r[10] or 0), int(r[9] or 0))
                positions[r[2]] = [round(float(r[17])-100,3),round(float(r[18])-100,3),round(float(r[20]),4)]
            if kind == '04':
                actors.pop(r[2], None); positions.pop(r[2], None)
            if kind in ('270','271'):
                positions[r[2]] = [round(float(r[6])-100,3),round(float(r[7])-100,3),round(float(r[3]),4)]
            if kind == '20' and int(r[4],16) == 25555: pull += 1
            if kind == '20' and int(r[4],16) == 25569:
                current = {'pull':pull,'start':t,'casts':[], 'effects':[], 'spawns':[], 'markers':[], 'map':[]}
                windows.append(current)
            if current is None: continue
            rel = round((t-current['start']).total_seconds(),3)
            if not 0 <= rel <= 26: continue
            if kind == '03' and actors[r[2]][0] > 0:
                current['spawns'].append({'t':rel,'npc':actors[r[2]],'pos':positions[r[2]]})
            if kind == '20' and int(r[4],16) in ACTIONS:
                current['casts'].append({'t':rel,'id':int(r[4],16),'bar':float(r[8]),'npc':actors.get(r[2]),'pos':positions.get(r[2])})
            if kind in ('21','22') and int(r[4],16) in ACTIONS:
                e={'t':rel,'id':int(r[4],16),'npc':actors.get(r[2]),'pos':positions.get(r[2])}
                if e not in current['effects']: current['effects'].append(e)
            if kind == '27':
                latest = {v[0]: {'npc':v, 'pos':positions.get(k)} for k,v in actors.items() if v[0] in (12592,12601,12632,12604)}
                current['markers'].append({'t':rel,'id':int(r[6],16),'actors':list(latest.values())})
            if kind == '257': current['map'].append({'t':rel,'flags':int(r[3],16),'index':int(r[4],16)})
        except (ValueError,IndexError,OverflowError): continue
    for w in windows:
        del w['start']
        w['jumps'] = len({e['t'] for e in w['effects'] if e['id']==25571})
        w['flares'] = len([e for e in w['effects'] if e['id']==25295])
    return {'territory':968,'sequences':windows}

if __name__ == '__main__':
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument('log',type=Path);cli.add_argument('--output',type=Path,required=True)
    args=cli.parse_args()
    if args.log.resolve()==args.output.resolve(): cli.error('output must not overwrite the source log')
    result=inspect(args.log);args.output.write_text(json.dumps(result,indent=2)+'\n')
    for w in result['sequences']: print(f"Pull {w['pull']}: {len(w['markers'])} markers, {w['jumps']} jumps, {w['flares']} orb effects")
