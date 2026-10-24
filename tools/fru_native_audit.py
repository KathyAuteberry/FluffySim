#!/usr/bin/env python3
"""Read-only FRU cast/map receive-time audit; exports no actor or character identity."""
import argparse
import collections
import struct
import json
from pathlib import Path
import parser as logs
from replay_inspector import Replay

ANCHORS = {40180, 40197, 40212, 40266, 40239, 40240, 40306, 40319}


def inspect(path):
    pulls = logs.find_pulls(path, 1238)
    events = []
    for record in logs.iter_records(path):
        try:
            stamp = logs.parse_timestamp(record[1])
            pull = next((p for p in pulls if p.start <= stamp and (p.end is None or stamp <= p.end)), None)
            if pull is None:
                continue
            if record[0] == '20' and 40000 < int(record[4], 16) < 40500:
                events.append((pull.index, stamp, 'cast', int(record[4], 16), float(record[8])))
            elif record[0] == '257':
                events.append((pull.index, stamp, 'map', int(record[4], 16), int(record[3], 16)))
        except (ValueError, IndexError):
            continue
    windows = []
    for pull, start, kind, action, duration in events:
        if kind != 'cast' or action not in ANCHORS:
            continue
        rows = sorted(set((round((stamp-start).total_seconds(), 3), k, ident, value)
                          for p, stamp, k, ident, value in events
                          if p == pull and 0 <= (stamp-start).total_seconds() <= 90))
        windows.append(dict(pull=pull, anchor=action, start=start.isoformat(), events=rows))
    return dict(pulls=[dict(index=p.index, start=p.start.isoformat(), duration_s=round(p.duration_s, 3)) for p in pulls], windows=windows)


def inspect_native(path):
    selected = ANCHORS | {40118, 40307, 40310, 40313, 40316, 40298, 40291, 40187, 40251, 40252, 40299}
    counts = collections.Counter()
    anchors = []
    with Replay(path, expected_build=14155597) as replay:
        for segment in replay.segments():
            if segment.opcode != 127 or segment.length != 32:
                continue
            payload = replay.payload(segment)
            action = struct.unpack_from('<H', payload)[0]
            if action not in selected:
                continue
            duplicate = struct.unpack_from('<I', payload, 4)[0]
            if duplicate != action or payload[2] != 1:
                raise ValueError('ActorCast layout validation failed')
            counts[(action, payload[3], round(struct.unpack_from('<f', payload, 8)[0], 3))] += 1
            if action in (40306, 40319):
                anchors.append(dict(action=action, ms=segment.ms, data_offset=segment.offset))
        return dict(header=replay.header, casts=[dict(action=a, omen_delay_tenths=d, cast_seconds=t, count=n)
                    for (a, d, t), n in sorted(counts.items())], anchors=anchors)


if __name__ == '__main__':
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument('log', type=Path)
    cli.add_argument('--native-replay', type=Path, action='append', default=[])
    cli.add_argument('--output', type=Path, required=True)
    args = cli.parse_args()
    result = inspect(args.log)
    result['native_replays'] = [inspect_native(path) for path in args.native_replay]
    with args.output.open('x') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(f"{len(result['pulls'])} FRU pulls; {len(result['windows'])} anchored windows")
