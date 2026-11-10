"""Read-only TEA evidence inventory; exports no player names, IDs or packet payloads.

Known-build native casts are decoded only after the complete replay framing has
validated. ACT timings are receive times. Numbered motion samples are observations,
not interpolated routes or proof of a regional strategy.
"""
import argparse
import collections
from datetime import datetime, UTC
import hashlib
import json
import math
from pathlib import Path
import statistics
import struct

import parser as logs
from replay_inspector import Replay, ReplayError

TERRITORY = 887
NATIVE_BUILD = 14155597
ACTORS = frozenset({
    'Living Liquid', 'Liquid Hand', 'Liquid Rage', 'Jagd Doll', 'Embolus',
    'Cruise Chaser', 'Brute Justice', 'Steam Chakram', 'Gelid Gaol',
    'Plasma Shield', 'Alexander Prime', 'Shanoa', 'Plasmasphere',
    'True Heart', 'Judgment Crystal', 'Alexander', 'Perfect Alexander',
})
STATUS_IDS = frozenset({
    695, 706, 783, 1025, 1035, 1121, 1122, 1123, 1124, 1138,
    2137, 2138, 2139, 2140, 2142, 2143, 2144, 2145, 2146, 2147,
    2152, 2153, 2154, 2155, 2222, 2223, 2224, 2225,
    3055, 3056, 3057, 3058,
})


def fingerprint(path):
    digest = hashlib.sha256()
    with open(path, 'rb') as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b''):
            digest.update(chunk)
    return dict(bytes=Path(path).stat().st_size, sha256=digest.hexdigest())


def position(values):
    if len(values) != 4 or any(v == '' for v in values):
        return None
    result = [float(v) for v in values]
    return result if all(math.isfinite(v) for v in result) else None


def inspect_log(path):
    pulls = logs.find_pulls(path, TERRITORY)
    actors = {}
    jobs = {}
    npc_ids = collections.defaultdict(set)
    counts = collections.Counter()
    events = []
    numbered = {}
    poses = {}
    motion = []
    cast_anchors = []
    zone = None
    for record in logs.iter_records(path):
        try:
            kind = record[0]
            if kind == '01':
                zone = int(record[2], 16)
                actors.clear()
                jobs.clear()
            if zone != TERRITORY:
                continue
            if kind == '03':
                if record[2].startswith('1'):
                    jobs[record[2]] = int(record[4], 16)
                if record[2].startswith('4') and record[3] in ACTORS:
                    actors[record[2]] = record[3]
                    npc_ids[record[3]].add((int(record[9]), int(record[10])))
                else:
                    actors.pop(record[2], None)
            stamp = logs.parse_timestamp(record[1])
            pull = next((p for p in pulls if p.start <= stamp
                         and (p.end is None or stamp <= p.end)), None)
            if pull is None:
                continue
            t = round((stamp - pull.start).total_seconds(), 3)
            counts[kind] += 1
            row = dict(pull=pull.index, t=t, kind=kind)
            if kind in ('20', '21', '22') and record[2] in actors:
                action = int(record[4], 16)
                row.update(action=action, actor=actors[record[2]])
                if kind == '20':
                    row.update(duration=float(record[8]), source_pose=position(record[9:13]))
                    cast_anchors.append(dict(action=action, epoch=stamp.timestamp(), pull=pull.index, t=t))
                else:
                    row.update(target_pose=position(record[30:34]), source_pose=position(record[40:44]))
                    if record[6] in jobs:
                        row['target_job'] = jobs[record[6]]
                    number = numbered.get((pull.index, record[6]))
                    if number is not None:
                        row['number'] = number
            elif kind in ('26', '30') and int(record[2], 16) in STATUS_IDS:
                row.update(status=int(record[2], 16), duration=float(record[4]), stacks=int(record[9], 16))
                if record[7] in jobs:
                    row['target_job'] = jobs[record[7]]
            elif kind == '27':
                marker = int(record[6], 16)
                row.update(marker=marker)
                if 79 <= marker <= 86 and t < 100:
                    numbered[(pull.index, record[2])] = marker - 78
                    row['number'] = marker - 78
            elif kind == '35':
                row.update(tether=int(record[8], 16))
            elif kind == '264' and 18000 <= int(record[3], 16) <= 19500:
                ground = position(record[6:10])
                row.update(action=int(record[3], 16), ground_pose=ground)
                if ground and all(abs(v + .015) < .001 for v in ground[:3]):
                    row['ground_pose'] = None
            elif kind == '261' and (pull.index, record[3]) in numbered and t < 100:
                key = (pull.index, record[3])
                pose = poses.setdefault(key, {})
                changes = dict(zip(record[4:-1:2], record[5:-1:2]))
                for field in ('PosX', 'PosY', 'PosZ', 'Heading'):
                    if field in changes:
                        pose[field] = float(changes[field])
                if 'PosX' in pose and 'PosY' in pose and any(k in changes for k in ('PosX', 'PosY', 'Heading')):
                    sample = dict(pull=pull.index, t=t, number=numbered[key], x=pose['PosX'], z=pose['PosY'])
                    if 'Heading' in pose:
                        sample['heading'] = pose['Heading']
                    motion.append(sample)
                continue
            else:
                continue
            events.append(row)
        except (ValueError, IndexError):
            continue
    unique = {json.dumps(row, sort_keys=True): row for row in events}
    return dict(file=fingerprint(path), territory=TERRITORY,
                pulls=[dict(index=p.index, start=p.start.isoformat(), duration_s=round(p.duration_s, 3), outcome=p.outcome)
                       for p in pulls],
                npc_ids={name: [dict(name_id=n, base_id=b) for n, b in sorted(ids)]
                         for name, ids in sorted(npc_ids.items())},
                record_counts=dict(sorted(counts.items())), events=list(unique.values()),
                numbered_motion=motion), cast_anchors


def align_casts(native, logged):
    histogram = collections.Counter()
    by_action = collections.defaultdict(list)
    for event in logged:
        by_action[event['action']].append(event)
    for event in native:
        for log in by_action[event['action']]:
            histogram[round(log['epoch'] - event['ms'] / 1000)] += 1
    if not histogram:
        return None
    peak = histogram.most_common(1)[0][0]
    matches = []
    for event in native:
        candidates = [(log, log['epoch'] - event['ms'] / 1000)
                      for log in by_action[event['action']]
                      if abs(log['epoch'] - event['ms'] / 1000 - peak) < .5]
        if len(candidates) == 1:
            log, offset = candidates[0]
            matches.append((event, log, offset))
    if len(matches) < 3 or len({e['action'] for e, _, _ in matches}) < 3:
        return None
    offset = statistics.median(o for _, _, o in matches)
    return dict(zero_utc=datetime.fromtimestamp(offset, UTC).isoformat(),
                matches=len(matches),
                residual_ms=[round((min(o for _, _, o in matches) - offset) * 1000, 3),
                             round((max(o for _, _, o in matches) - offset) * 1000, 3)],
                pulls=dict(sorted(collections.Counter(log['pull'] for _, log, _ in matches).items())),
                anchors=[dict(action=e['action'], replay_ms=e['ms'], pull=log['pull'], log_t=log['t'],
                              residual_ms=round((o - offset) * 1000, 3)) for e, log, o in matches])


def decode_waymarks(payload):
    if len(payload) != 104 or any(payload[1:4]) or any(payload[100:104]):
        raise ReplayError('known-build WaymarkPreset layout validation failed')
    x = struct.unpack_from('<8i', payload, 4)
    y = struct.unpack_from('<8i', payload, 36)
    z = struct.unpack_from('<8i', payload, 68)
    return [dict(name=name, active=bool(payload[0] & (1 << i)),
                 x=x[i] / 1000, y=y[i] / 1000, z=z[i] / 1000)
            for i, name in enumerate('ABCD1234')]


def inspect_replay(path, logged):
    with Replay(path) as replay:
        report = replay.inspect()
        casts = []
        waymarks = []
        if replay.header['game_build'] == NATIVE_BUILD:
            selected = {e['action'] for e in logged}
            for segment in replay.segments():
                if segment.opcode == 0x035E:
                    waymarks.append(dict(ms=segment.ms, marks=decode_waymarks(replay.payload(segment))))
                if segment.opcode != 127 or segment.length != 32:
                    continue
                payload = replay.payload(segment)
                action = struct.unpack_from('<H', payload)[0]
                if payload[2] != 1 or struct.unpack_from('<I', payload, 4)[0] != action:
                    raise ReplayError('known-build ActorCast layout validation failed')
                duration = struct.unpack_from('<f', payload, 8)[0]
                if not math.isfinite(duration) or duration < 0:
                    raise ReplayError('invalid native cast duration')
                if action in selected:
                    casts.append(dict(ms=segment.ms, action=action, duration=round(duration, 3),
                                      base_cast_time_100ms=payload[3]))
        return dict(file=fingerprint(path), framing=report, casts=casts, waymarks=waymarks,
                    native_cast_schema='validated-build-14155597' if replay.header['game_build'] == NATIVE_BUILD else 'unsupported-build',
                    alignment=align_casts(casts, logged))


def main():
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument('log', type=Path)
    cli.add_argument('--replay', type=Path, action='append', default=[])
    cli.add_argument('--output', type=Path, required=True)
    args = cli.parse_args()
    report, anchors = inspect_log(args.log)
    report['replays'] = [dict(identifier=f'TEA-R{i + 1:02d}', **inspect_replay(path, anchors))
                         for i, path in enumerate(args.replay)]
    with args.output.open('x', encoding='utf-8') as output:
        json.dump(report, output, indent=2)
        output.write('\n')
    print(f"{len(report['pulls'])} TEA pulls; {len(report['replays'])} validated replay inventories")


if __name__ == '__main__':
    main()
