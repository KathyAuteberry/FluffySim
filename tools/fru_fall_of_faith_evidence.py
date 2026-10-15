"""Sanitized Fall of Faith inventory; never writes to the source log."""
import argparse
import bisect
import collections
import json
from pathlib import Path
import parser as logs

ACTIONS = {40137, 40140, 40138, 40141, 40142, 40143, 40156, 40171, 40172}
STATUSES = {1051, 2304, 2941, 4372}


def inspect(path):
    starts = []
    for r in logs.iter_records(path):
        try:
            if r[0] == '20' and int(r[4], 16) in (40137, 40140):
                stamp = logs.parse_timestamp(r[1])
                if not starts or (stamp - starts[-1]).total_seconds() > 30:
                    starts.append(stamp)
        except (ValueError, IndexError):
            continue
    output = [dict(casts=[], effects=collections.defaultdict(list), tethers=[], statuses=[], markers=[], actors=[], cones=[]) for _ in starts]
    identities = {}
    positions = {}
    hit_targets = collections.defaultdict(set)
    for r in logs.iter_records(path):
        try:
            kind = r[0]
            stamp = logs.parse_timestamp(r[1])
            if kind == '03':
                identities[r[2]] = (int(r[10] or 0), int(r[9] or 0))
                positions[r[2]] = [float(r[17])-100, float(r[18])-100, float(r[20])]
            if kind == '261':
                fields = dict(zip(r[4::2], r[5::2]))
                old = positions.get(r[3], [0, 0, 0])
                positions[r[3]] = [float(fields.get('PosX', old[0]+100))-100,
                                   float(fields.get('PosY', old[1]+100))-100, float(fields.get('Heading', old[2]))]
            index = bisect.bisect_right(starts, stamp) - 1
            if index + 1 < len(starts) and 0 < (starts[index + 1] - stamp).total_seconds() < .15:
                index += 1
            if index < 0:
                continue
            t = round((stamp-starts[index]).total_seconds(), 3)
            if not -.15 <= t <= 27:
                continue
            window = output[index]
            if kind == '20' and int(r[4], 16) in ACTIONS:
                window['casts'].append(dict(t=t, action=int(r[4],16), duration=float(r[8]), npc=identities.get(r[2]), position=positions.get(r[2])))
            if kind in ('21', '22') and int(r[4], 16) in (40143, 40156) and len(r) > 6:
                hit_targets[(index, int(r[4],16), r[2], t)].add(r[6])
            if kind == '264' and int(r[3],16) in (40143,40156):
                window['cones'].append(dict(t=t, action=int(r[3],16), npc=identities.get(r[2]),
                    origin=positions.get(r[2], [])[:2] or None,
                    packet_position=[float(v) for v in r[6:9]] if all(r[6:9]) else None,
                    heading=float(r[9]) if r[9] else None, data_flag=int(r[5]),
                    hits=len(hit_targets.get((index,int(r[3],16),r[2],t),set()))))
            if kind in ('21', '22') and int(r[4], 16) in ACTIONS:
                action = int(r[4], 16)
                if t not in window['effects'][action]:
                    window['effects'][action].append(t)
            if kind == '35' and int(r[8], 16) in (249, 287):
                window['tethers'].append(dict(t=t, channeling=int(r[8],16), npc=identities.get(r[2]), position=positions.get(r[2])))
            if kind in ('26', '30') and int(r[2],16) in STATUSES:
                window['statuses'].append(dict(t=t, event=kind, status=int(r[2],16), duration=float(r[4]), param=int(r[9],16)))
            if kind == '27':
                window['markers'].append(dict(t=t, marker=int(r[6],16)))
            if kind == '03' and identities[r[2]][0] in (17819,17820):
                window['actors'].append(dict(t=t, npc=identities[r[2]], position=positions[r[2]]))
        except (ValueError, IndexError):
            continue
    for window in output:
        window['pattern'] = ''.join('F' if c['action']==40137 else 'L' for c in window['casts'] if c['action'] in (40137,40140))
        hits = sorted(set(window['effects'].get(40142,[]) + window['effects'].get(40156,[])))
        window['primary_hits'] = hits
        window['complete'] = len(hits) == 4
    return dict(starts=len(output), complete=sum(w['complete'] for w in output), sequences=output)


if __name__ == '__main__':
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument('log', type=Path)
    cli.add_argument('--output', type=Path, required=True)
    args = cli.parse_args()
    if args.log.resolve() == args.output.resolve():
        cli.error('Output must not overwrite the recording')
    result = inspect(args.log)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(f"Fall of Faith: {result['starts']} starts, {result['complete']} complete sequences")
