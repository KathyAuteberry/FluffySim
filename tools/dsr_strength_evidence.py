"""Read-only sanitized DSR Strength Part 1 receive-timeline extraction."""
import argparse
import collections
import json
from pathlib import Path
import parser as logs

ACTIONS = {25555,25548,25549,25556,25557,25558,25559,25560,25561,25562,25544,25545}
NPCS = {12604,12632,12633,12634,12635,12637,12601,12602,12636,12638,12639}

def inspect(path):
    pulls = logs.find_pulls(path, 968)
    actors = {}; windows = []; current = None
    for r in logs.iter_records(path):
        try:
            t = logs.parse_timestamp(r[1]); kind = r[0]
            if kind == '03':
                actors[r[2]] = (int(r[10] or 0), int(r[9] or 0))
            if kind == '20' and int(r[4],16) == 25555:
                pull = next((p for p in pulls if p.start <= t and (p.end is None or t <= p.end)), None)
                current = {'pull':pull.index if pull else 0,'start':t,'casts':[], 'effects':collections.defaultdict(list), 'actors':[], 'maps':[], 'markers':[], 'moves':[]}
                windows.append(current)
            if current is None: continue
            rel = round((t-current['start']).total_seconds(),3)
            if not 0 <= rel <= 27: continue
            if kind == '20' and int(r[4],16) in ACTIONS:
                current['casts'].append({'t':rel,'id':int(r[4],16),'duration':float(r[8]),'npc':actors.get(r[2]),'pos':[float(r[9])-100,float(r[10])-100,float(r[12])]})
            if kind in ('21','22') and int(r[4],16) in ACTIONS:
                v = current['effects'][int(r[4],16)]
                if rel not in v:v.append(rel)
            if kind == '03' and actors[r[2]][0] in NPCS:
                current['actors'].append({'t':rel,'npc':actors[r[2]],'pos':[float(r[17])-100,float(r[18])-100,float(r[20])]})
            if kind in ('270','271') and actors.get(r[2],(0,))[0] in NPCS:
                current['moves'].append({'t':rel,'npc':actors[r[2]],'pos':[float(r[6])-100,float(r[7])-100,float(r[3])]})
            if kind == '257':current['maps'].append({'t':rel,'flags':int(r[3],16),'index':int(r[4],16)})
            if kind == '27':current['markers'].append({'t':rel,'id':int(r[6],16)})
        except (ValueError,IndexError,OverflowError):continue
    for w in windows:
        del w['start'];w['complete']=25562 in w['effects'] and 25545 in w['effects']
    return {'territory':968,'pulls':len(pulls),'sequences':windows}

if __name__ == '__main__':
    cli=argparse.ArgumentParser(description=__doc__);cli.add_argument('log',type=Path);cli.add_argument('--output',type=Path,required=True);a=cli.parse_args()
    if a.log.resolve()==a.output.resolve():cli.error('output must not overwrite the source log')
    result=inspect(a.log);a.output.write_text(json.dumps(result,indent=2)+'\n')
    print(f'{result["pulls"]} pulls; {len(result["sequences"])} sequences; {sum(w["complete"] for w in result["sequences"])} complete')
