"""Read-only sanitized DSR Strength Part 2 receive-timeline extraction."""
import argparse
import collections
import json
from pathlib import Path
import parser as logs

ACTIONS = {25550,25551,25565,25297,25579,25298,25299,25563,25564,25566,25567,25568}

def inspect(path):
    windows = []; current = None; actors = {}; positions = {}
    for r in logs.iter_records(path):
        try:
            t = logs.parse_timestamp(r[1]); kind = r[0]
            if kind == '03':
                actors[r[2]] = (int(r[10] or 0), int(r[9] or 0))
                positions[r[2]] = [round(float(r[17])-100,3),round(float(r[18])-100,3),float(r[20])]
            if kind in ('270','271'):
                positions[r[2]] = [round(float(r[6])-100,3),round(float(r[7])-100,3),float(r[3])]
            if kind == '20' and int(r[4],16) == 25555:
                current = {'ordinal':len(windows)+1,'start':t,'casts':[], 'effects':collections.defaultdict(list), 'markers':[], 'tethers':[], 'stuns':[]}
                windows.append(current)
            if current is None: continue
            rel = round((t-current['start']).total_seconds(),3)
            if not 20 <= rel <= 42: continue
            if kind == '20' and int(r[4],16) in ACTIONS:
                current['casts'].append({'t':rel,'id':int(r[4],16),'bar':float(r[8]),'npc':actors.get(r[2]),'pos':positions.get(r[2])})
            if kind in ('21','22') and int(r[4],16) in ACTIONS:
                values = current['effects'][str(int(r[4],16))]
                if rel not in values: values.append(rel)
            if kind == '27' and int(r[6],16) == 330: current['markers'].append({'t':rel,'id':330})
            if kind == '35' and int(r[8],16) == 84: current['tethers'].append({'t':rel,'id':84,'npc':actors.get(r[2])})
            if kind == '26' and int(r[2],16) == 3165: current['stuns'].append({'t':rel,'id':3165,'duration':float(r[4])})
        except (ValueError,IndexError,OverflowError): continue
    for w in windows:
        del w['start']
        w['reached_end'] = all(str(a) in w['effects'] for a in (25565,25297,25551,25299,25567))
        w['tower_failure'] = '25568' in w['effects']
    return {'territory':968,'sequences':windows}

if __name__ == '__main__':
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument('log',type=Path);cli.add_argument('--output',type=Path,required=True)
    args=cli.parse_args()
    if args.log.resolve()==args.output.resolve():cli.error('output must not overwrite the source log')
    result=inspect(args.log);args.output.write_text(json.dumps(result,indent=2)+'\n')
    print(f'{len(result["sequences"])} sequences; {sum(w["reached_end"] for w in result["sequences"])} reached all resolutions; {sum(w["tower_failure"] for w in result["sequences"])} tower failures')
