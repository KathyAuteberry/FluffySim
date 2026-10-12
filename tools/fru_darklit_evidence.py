"""Sanitized local FRU P4 inventory. Original logs remain read-only."""
import argparse
import collections
import json
from pathlib import Path
import parser as logs

ACTIONS = {40239, 40301, 40187, 40213, 40190, 40288, 40289, 40227, 40228, 40271, 40283, 40284, 40285, 40215, 40216}
STATUSES = {2253, 2255, 2257, 2461}
NPCS = {17833, 17834, 17835, 17841}

def inspect(path):
    pulls = logs.find_pulls(path, 1238)
    sequences = []
    current = None
    materializations = 0
    for r in logs.iter_records(path):
        try:
            stamp = logs.parse_timestamp(r[1])
            kind = r[0]
            if kind == '20' and int(r[4], 16) == 40246:
                materializations += 1
            if kind == '20' and int(r[4], 16) == 40239:
                pull = next((p for p in pulls if p.start <= stamp <= p.end), None)
                current = dict(pull=pull.index if pull else None, start=stamp, casts=[], effects=collections.defaultdict(list), statuses=[], tethers=[], markers=[], map_effects=[])
                sequences.append(current)
            if current is None:
                continue
            t = round((stamp - current['start']).total_seconds(), 3)
            if not 0 <= t <= 40:
                continue
            if kind == '20' and int(r[4], 16) in ACTIONS:
                current['casts'].append(dict(t=t, id=int(r[4], 16), duration=float(r[8])))
            if kind in ('21', '22') and int(r[4], 16) in ACTIONS:
                effects = current['effects'][int(r[4], 16)]
                if t not in effects:
                    effects.append(t)
            if kind in ('26', '30') and int(r[2], 16) in STATUSES:
                current['statuses'].append(dict(t=t, kind=kind, id=int(r[2], 16), duration=float(r[4]), stacks=int(r[9], 16)))
            if kind == '35':
                current['tethers'].append(dict(t=t, id=int(r[8], 16)))
            if kind == '27':
                current['markers'].append(dict(t=t, id=int(r[6], 16)))
            if kind == '257':
                current['map_effects'].append(dict(t=t, index=int(r[4], 16), flags=int(r[3], 16)))
        except (ValueError, IndexError):
            continue
    for sequence in sequences:
        sequence.pop('start')
    return dict(fru_pulls=len(pulls), materializations=materializations, darklit_starts=len(sequences), sequences=sequences)

if __name__ == '__main__':
    cli = argparse.ArgumentParser()
    cli.add_argument('log', type=Path)
    cli.add_argument('--output', type=Path, required=True)
    args = cli.parse_args()
    result = inspect(args.log)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(f"FRU pulls: {result['fru_pulls']}; P4: {result['materializations']}; Darklit: {result['darklit_starts']}")
