"""Read-only Ultimate Relativity receive-timeline extraction; no player identifiers exported."""
import argparse
import collections
import json
from pathlib import Path
import parser as logs

ACTIONS = {40266,40293,40294,40295,40276,40277,40291,40235,40292,40279,40278,40274,40271,40286,40287}
STATUSES = {2454,2455,2456,2460,2461,2462,2464,2452,2970,4163}

def inspect(path):
    pulls=logs.find_pulls(path,1238)
    windows=[]; current=None; npcs={}
    for r in logs.iter_records(path):
        try:
            stamp=logs.parse_timestamp(r[1]);kind=r[0]
            if kind=='03' and not r[2].startswith('1'):
                npcs[r[2]]=(int(r[10] or 0),int(r[9] or 0))
            if kind=='20' and int(r[4],16)==40266:
                pull=next((p for p in pulls if p.start<=stamp<=p.end),None)
                current={'pull':pull.index if pull else 0,'start':stamp,'casts':[],'effects':collections.defaultdict(list),'statuses':[],'actors':set()};windows.append(current)
            if current is None:continue
            t=round((stamp-current['start']).total_seconds(),3)
            if not 0<=t<=65:continue
            if kind=='20' and int(r[4],16) in ACTIONS:
                current['casts'].append({'t':t,'id':int(r[4],16),'duration':float(r[8])})
            if kind in ('21','22') and int(r[4],16) in ACTIONS:
                a=int(r[4],16)
                if t not in current['effects'][a]:current['effects'][a].append(t)
            if kind=='26' and int(r[2],16) in STATUSES:
                current['statuses'].append({'t':t,'id':int(r[2],16),'duration':float(r[4]),'param':r[9]})
            if kind=='03' and r[2] in npcs and npcs[r[2]][0] in (17831,17832):current['actors'].add(npcs[r[2]])
        except (ValueError,IndexError):continue
    result=[]
    for w in windows:
        w.pop('start');w['actors']=sorted(w['actors']);w['complete']=40287 in w['effects'];result.append(w)
    return result

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('logs',nargs='+',type=Path);ap.add_argument('--output',type=Path,required=True);args=ap.parse_args()
    result=[{'recording':i+1,**w} for i,p in enumerate(args.logs) for w in inspect(p)]
    args.output.write_text(json.dumps(result,indent=2)+'\n');print(f'{len(result)} sequences, {sum(w["complete"] for w in result)} complete')
