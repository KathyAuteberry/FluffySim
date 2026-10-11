"""Local receive-timeline inventory; exports no player identities."""
import argparse,collections,json
from pathlib import Path
import parser as logs
ACTIONS={40269,40270,40272,40296,40297,40288,40289,40273,40274,40271,40181,40182,40183,40282}
def inspect(path):
    pulls=logs.find_pulls(path,1238); out=[]; current=None
    for r in logs.iter_records(path):
        try:
            stamp=logs.parse_timestamp(r[1]);kind=r[0]
            if kind=='20' and int(r[4],16)==40269:
                pull=next((p for p in pulls if p.start<=stamp<=p.end),None)
                current={'pull':pull.index if pull else 0,'start':stamp,'casts':[],'effects':collections.defaultdict(list),'statuses':[],'markers':[],'lights':[],'map_effects':[]};out.append(current)
            if current is None:continue
            t=round((stamp-current['start']).total_seconds(),3)
            if not 0<=t<=65:continue
            if kind=='20' and int(r[4],16) in ACTIONS:current['casts'].append({'t':t,'id':int(r[4],16),'duration':float(r[8])})
            if kind in ('21','22') and int(r[4],16) in ACTIONS:
                a=int(r[4],16)
                if t not in current['effects'][a]:current['effects'][a].append(t)
            if kind=='26' and int(r[2],16)==2461:current['statuses'].append({'t':t,'duration':float(r[4])})
            if kind=='261' and r[2]=='Add':
                fields=dict(zip(r[4::2],r[5::2]))
                if fields.get('Type')=='7' and int(fields.get('BNpcID','0'),16)==0x1EB0FF:
                    current['lights'].append({'t':t,'position':[float(fields.get(k,'0')) for k in ('PosX','PosZ','PosY')],'heading':float(fields.get('Heading','0'))})
            if kind=='27':current['markers'].append({'t':t,'id':r[6]})
            if kind=='257':current['map_effects'].append({'t':t,'index':r[4],'flags':r[3]})
        except (ValueError,IndexError):continue
    for w in out:w.pop('start')
    return out
if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('log',type=Path);ap.add_argument('--output',type=Path,required=True);a=ap.parse_args();out=inspect(a.log);a.output.write_text(json.dumps(out,indent=2)+'\n');print(f'{len(out)} Refrain sequences')
