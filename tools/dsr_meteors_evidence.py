"""Read-only, sanitized inventory of Sanctity starts and subsequent Meteor events."""
import argparse
import json
from pathlib import Path
import parser as logs

ACTIONS = {25574,25575,28590,28591,28592,29563,29564,25576,25577,25578,25308,28650,28651}

def inspect(path):
    windows = []
    current = None
    zone = None
    for record in logs.iter_records(path):
        try:
            kind = record[0]
            timestamp = logs.parse_timestamp(record[1])
            if kind == '01':
                zone = int(record[2], 16)
                current = None
            if kind == '20' and int(record[4],16) == 25569 and zone in (None,968):
                current = dict(start=timestamp,casts=[],effects=[],markers=[])
                windows.append(current)
            if current is None:
                continue
            elapsed = round((timestamp-current['start']).total_seconds(),3)
            if not 0 <= elapsed <= 90:
                continue
            if kind in ('20','21','22') and int(record[4],16) in ACTIONS:
                bucket = 'casts' if kind == '20' else 'effects'
                event = dict(t=elapsed,id=int(record[4],16))
                if event not in current[bucket]:
                    current[bucket].append(event)
            if kind == '27' and int(record[6],16) == 285:
                current['markers'].append(dict(t=elapsed,id=285))
        except (ValueError,IndexError,OverflowError):
            continue
    for window in windows:
        del window['start']
    return dict(territory=968,sanctity_starts=len(windows),meteor_starts=sum(any(e['id']==25576 for e in w['casts']) for w in windows),
                second_towers=sum(any(e['id']==28651 for e in w['effects']) for w in windows),sequences=windows)

if __name__ == '__main__':
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument('log',type=Path)
    args = cli.parse_args()
    print(json.dumps(inspect(args.log),indent=2))
