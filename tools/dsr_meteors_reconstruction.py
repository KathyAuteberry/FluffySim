"""Audit Sanctity Meteor coverage without treating failed pulls as successful mechanics.

Reuses the ACT/IINACT reader. Coordinates are ACT X,Y,Z (east,south,height),
normalized to game/simulator X,Y,Z at arena origin (100,0,100).
"""
import argparse
import collections
import hashlib
import json
import statistics
import re
from pathlib import Path
import parser as logs

ACTIONS = {25569,25574,25575,28590,28591,28592,29563,29564,25576,25577,25578,25308,28650,28651}
NPCS = {9020,12591,12602,12603,12604,12636,12638,12639}


def position(values):
    x, south, height = map(float, values[:3])
    return [round(x-100,4), round(height,4), round(south-100,4)]


def analyze(path):
    records = list(logs.iter_records(path))
    times = [logs.parse_timestamp(r[1]) for r in records]
    zones = []; zone = None; sanctities = []; anchors = []
    for line,r in enumerate(records,1):
        if r[0] == '01':
            zone = int(r[2],16); zones.append(dict(line=line,timestamp=r[1],territory=zone))
        if zone != 968: continue
        if r[0] == '20' and int(r[4],16) == 25569: sanctities.append(line)
        if r[0] == '27' and int(r[6],16) == 285:
            t = logs.parse_timestamp(r[1])
            if not anchors or (t-anchors[-1][1]).total_seconds()>60: anchors.append((line,t))
    sequences=[]; player_aliases={}
    for index,(anchor_line,start) in enumerate(anchors,1):
        aliases={}; npc={}; npc_names={}; jobs={}; eobjects={}; events=[]; seen=set(); window_times=[]
        def alias(actor):
            if actor in ('','E0000000','00000000'): return None
            if actor not in aliases:
                category='player' if actor.startswith('1') else 'actor'
                if category=='player':
                    if actor not in player_aliases:player_aliases[actor]=f'player-{len(player_aliases)+1}'
                    aliases[actor]=player_aliases[actor]
                else:aliases[actor]=f'actor-{sum(v.startswith(category) for v in aliases.values())+1}'
            return aliases[actor]
        for line,(r, timestamp) in enumerate(zip(records,times),1):
            kind=r[0]
            if kind=='03':
                npc[r[2]]=int(r[10] or 0)
                npc_names[r[2]]=int(r[9] or 0)
                if r[2].startswith('1'): jobs[r[2]]=int(r[4],16)
            t=(timestamp-start).total_seconds()
            if t < -2: continue
            if t > 33: break
            if kind=='01' and int(r[2],16)!=968:
                break
            window_times.append(timestamp)
            event=None
            if kind in ('20','21','22') and int(r[4],16) in ACTIONS:
                action=int(r[4],16)
                # Multi-target ability rows share source + server sequence, not timestamp.
                key=(r[2],r[44],action) if kind in ('21','22') else (line,)
                event=dict(action=action,name=r[5],source=alias(r[2]),target=alias(r[6]),npc=npc.get(r[2]))
                if kind=='20': event.update(position=position(r[9:12]),direction=float(r[12]),cast_seconds=float(r[8]))
                else:
                    event.update(position=position(r[40:43]),direction=float(r[43]),sequence=r[44])
                    if r[30]: event['target_position']=position(r[30:33])
                # Keep targets on effects, but count unique releases separately below.
                event['release']=key not in seen; seen.add(key)
            elif kind=='263' and int(r[3],16) in ACTIONS:
                event=dict(source=alias(r[2]),action=int(r[3],16),position=position(r[4:7]),direction=float(r[7]))
            elif kind in ('26','30') and int(r[2],16) in (562,2903,2945,2946,216,3166,160,1209,50):
                event=dict(status=int(r[2],16),name=r[3],duration=float(r[4]),source=alias(r[5]),target=alias(r[7]))
            elif kind=='27' and int(r[6],16)==285:
                event=dict(marker=285,target=alias(r[2]))
            elif kind=='261':
                fields=dict(zip(r[4:-1:2],r[5:-1:2]))
                if r[2]=='Add' and fields.get('Type')=='7':
                    eobjects[r[3]]=int(fields['BNpcID'],16)
                    event=dict(object=eobjects[r[3]],source=alias(r[3]),position=position([fields['PosX'],fields['PosY'],fields['PosZ']]))
                elif r[2]=='Remove' and r[3] in eobjects:
                    event=dict(object=eobjects[r[3]],source=alias(r[3]))
            elif kind in ('03','04') and npc.get(r[2]) in NPCS:
                event=dict(npc=npc[r[2]],source=alias(r[2]))
                if kind=='03':event.update(position=position(r[17:20]),direction=float(r[20]))
            elif kind=='39' and r[2].startswith('1'):
                event=dict(source=alias(r[2]))
                if all(r[10:14]):event.update(position=position(r[10:13]),direction=float(r[13]))
                else:event['missing_position']=True
            elif kind=='264' and int(r[3],16) in ACTIONS:
                event=dict(source=alias(r[2]),action=int(r[3],16),sequence=r[4],target=alias(r[10]),has_position=r[5])
                if r[5]=='1':event.update(position=position(r[6:9]),direction=float(r[9]))
            elif kind=='272' and npc.get(r[2]) in NPCS:
                event=dict(source=alias(r[2]),owner=alias(r[3]),parameters=r[4:-1])
            elif kind=='273' and (npc.get(r[2]) in NPCS or r[2] in eobjects):
                event=dict(source=alias(r[2]),parameters=r[3:-1])
            elif kind in ('270','271') and (npc.get(r[2]) in NPCS or r[2].startswith('1')):
                event=dict(npc=npc.get(r[2]),source=alias(r[2]),position=position(r[6:9]),direction=float(r[3]))
            elif kind=='25':event=dict(target=alias(r[2]),source=alias(r[4]))
            elif kind=='35':event=dict(source=alias(r[2]),target=alias(r[4]),tether=int(r[8],16))
            elif kind=='33':event=dict(parameters=r[3:-1])
            elif kind=='257':event=dict(parameters=r[2:-1])
            if event is not None:
                events.append(dict(line=line,timestamp=r[1],t=round(t,3),kind=kind,confidence='observed',**event))
        releases=[e for e in events if e.get('release') and e['kind'] in ('21','22')]
        counts=collections.Counter(e['action'] for e in releases)
        full=counts[29564]==8 and counts[25577]==14 and counts[28651]==8 and counts[25308]==1
        sequences.append(dict(attempt=index,anchor_line=anchor_line,start=start.isoformat(),complete_execution=full,
                              player_jobs={alias(actor):job for actor,job in jobs.items() if actor in aliases},
                              actors={aliases[actor]:dict(base_id=base,name_id=npc_names[actor]) for actor,base in npc.items() if base and actor in aliases},
                              last_record=window_times[-1].isoformat(),
                              largest_record_gap=max((b-a).total_seconds() for a,b in zip(window_times,window_times[1:])) if len(window_times)>1 else None,
                              release_counts=dict(counts),events=events))
    distributions={}; coordinates=collections.defaultdict(set)
    for sequence in sequences:
        first={}
        for event in sequence['events']:
            if event.get('release'):
                key=f"{event['kind']}:{event['action']}"
                first.setdefault(key,event['t'])
                if 'position' in event: coordinates[key].add(tuple(event['position']))
        for key,value in first.items():distributions.setdefault(key,[]).append(value)
    timing={key:dict(samples=len(values),minimum=min(values),median=statistics.median(values),maximum=max(values))
            for key,values in sorted(distributions.items())}
    return dict(source=Path(path).name,sha256=hashlib.sha256(Path(path).read_bytes()).hexdigest(),
                record_counts=dict(collections.Counter(r[0] for r in records)),territory_transitions=zones,
                first_event_timing=timing,coordinates={key:sorted(values) for key,values in sorted(coordinates.items())},
                sanctity_starts=len(sanctities),meteor_attempts=len(anchors),complete_executions=sum(s['complete_execution'] for s in sequences),sequences=sequences)


def main():
    cli=argparse.ArgumentParser(description=__doc__);cli.add_argument('log',type=Path);cli.add_argument('--output',required=True,type=Path)
    args=cli.parse_args()
    if args.log.resolve()==args.output.resolve():cli.error('output must not overwrite input')
    result=analyze(args.log)
    encoded=json.dumps(result,indent=2)
    encoded=re.sub(r'\{\n[^{}]*\}',lambda match:json.dumps(json.loads(match[0]),separators=(',',':')),encoded)
    args.output.write_text(encoded+'\n')
    print({k:v for k,v in result.items() if k in ('sanctity_starts','meteor_attempts','complete_executions')})

if __name__=='__main__':main()
