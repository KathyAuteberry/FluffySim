#!/usr/bin/env python3
"""Extract sanitized Diamond Dust evidence from read-only ACT/IINACT logs.

Times are receive timestamps relative to Mirror Image, not damage snapshots.
Only game IDs, NPC coordinates, jobs and pull ordinals are exported.
"""
import argparse
import collections
import json
from pathlib import Path

import parser as log_parser

ACTIONS = {
    40180, 40197, 40202, 40203, 40206, 40199, 40198, 40200, 40201,
    40207, 40208, 40209, 40185, 40184, 40193, 40194, 40195, 40196,
    40210, 40211,
}


def empty_window():
    return {
        "casts": [], "effects": collections.defaultdict(list), "markers": [],
        "statuses": [], "maps": [], "objects": [], "actors": [], "deaths": [],
    }


def inspect_log(path):
    pulls = log_parser.find_pulls(path, 1238)
    windows, jobs, npcs, positions, objects, last_cast = {}, {}, {}, {}, {}, {}
    active = None
    skipped = 0
    for record in log_parser.iter_records(path):
        try:
            timestamp = log_parser.parse_timestamp(record[1])
            kind = record[0]
            fields = {}
            if kind == "03":
                positions[record[2]] = [float(record[17]) - 100, float(record[18]) - 100, float(record[20])]
                if record[2].startswith("1"):
                    jobs[record[2]] = int(record[4], 16)
                else:
                    npcs[record[2]] = [int(record[10] or 0), int(record[9] or 0)]
            if kind == "261":
                fields = dict(zip(record[4:-1:2], record[5:-1:2]))
                old = positions.get(record[3], [0, 0, 0])
                positions[record[3]] = [
                    float(fields.get("PosX", old[0] + 100)) - 100,
                    float(fields.get("PosY", old[1] + 100)) - 100,
                    float(fields.get("Heading", old[2])),
                ]
            if kind == "20" and int(record[4], 16) == 40180:
                pull = next((p for p in pulls if p.start <= timestamp and (p.end is None or timestamp <= p.end)), None)
                if pull is not None:
                    active = (pull.index, timestamp)
                    windows[pull.index] = empty_window()
                    objects.clear()
                    last_cast.clear()
            if active is None:
                continue
            pull, start = active
            time = round((timestamp - start).total_seconds(), 3)
            if not 0 <= time <= 60:
                continue
            window = windows[pull]
            if kind == "20" and int(record[4], 16) in ACTIONS:
                cast = {
                    "t": time, "id": int(record[4], 16), "duration": float(record[8]),
                    "pos": [float(record[9]) - 100, float(record[10]) - 100, float(record[12])],
                    "npc": npcs.get(record[2]),
                }
                window["casts"].append(cast)
                last_cast[record[2]] = cast
            if kind == "263" and record[2] in last_cast and int(record[3], 16) == last_cast[record[2]]["id"]:
                last_cast[record[2]]["target_pos"] = [float(record[4]) - 100, float(record[5]) - 100, float(record[7])]
            if kind in ("21", "22") and int(record[4], 16) in ACTIONS:
                effects = window["effects"][int(record[4], 16)]
                if time not in effects:
                    effects.append(time)
            if kind == "27" and int(record[6], 16) in (345, 525):
                window["markers"].append({"t": time, "id": int(record[6], 16), "job": jobs.get(record[2])})
            if kind == "26" and int(record[2], 16) in (911, 3077, 3078):
                window["statuses"].append({
                    "t": time, "id": int(record[2], 16), "duration": float(record[4]),
                    "param": int(record[9], 16), "job": jobs.get(record[7]),
                })
            if kind == "257":
                window["maps"].append({"t": time, "flags": int(record[3], 16), "index": int(record[4], 16)})
            if kind == "03" and npcs.get(record[2], [0])[0] in (17823, 17824):
                window["actors"].append({"t": time, "npc": npcs[record[2]], "pos": positions[record[2]]})
            if kind == "261" and record[2] == "Add" and fields.get("Type") == "7":
                object_id = int(fields.get("BNpcID", "0"), 16)
                window["objects"].append({"t": time, "eobj": object_id, "pos": positions[record[3]]})
                objects[record[3]] = (object_id, time)
            if kind == "261" and record[2] == "Remove" and record[3] in objects:
                object_id, born = objects.pop(record[3])
                window["objects"].append({"t": time, "eobj": object_id, "remove_age": round(time - born, 3)})
            if kind == "25":
                window["deaths"].append({"t": time, "job": jobs.get(record[2])})
        except (ValueError, IndexError, OverflowError):
            skipped += 1
    return len(pulls), windows, skipped


def main():
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument("logs", nargs="+", type=Path)
    cli.add_argument("--output", required=True, type=Path)
    args = cli.parse_args()
    if args.output.resolve() in {p.resolve() for p in args.logs}:
        cli.error("output must not overwrite a source log")
    sources, recordings = [], []
    for path in args.logs:
        count, windows, skipped = inspect_log(str(path))
        sources.append({
            "source": path.name, "fru_pulls": count,
            "diamond_dust_windows": len(windows), "malformed_records_skipped": skipped,
        })
        for pull, evidence in windows.items():
            recordings.append({"source": path.name, "pull": pull, **evidence})
    args.output.write_text(json.dumps({"sources": sources, "recordings": recordings}, indent=2) + "\n")
    print(json.dumps({"sources": sources, "complete_windows": sum(40211 in r["effects"] for r in recordings)}))


if __name__ == "__main__":
    main()
