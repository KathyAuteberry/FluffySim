# TEA recording evidence

The supplied package was found in `/home/kath/Stažené`. The addendum also says
`~/Stahování`; that directory does not exist on this machine. No home-wide search
was needed. Originals were opened read-only and were not modified. The immediate
download folder contains unrelated FRU, image and repository archive files; these
were excluded from TEA analysis.

## Complete relevant local inventory

| Identifier | File relative to `~/Stažené` | Bytes | Actual coverage |
|---|---|---:|---|
| TEA-Z01 | `drive-download-20261109T090200Z-1-001.zip` | 26,952,640 | Exactly the three files below; every member SHA-256 matches its extracted counterpart |
| TEA-L01 | `Logs/Network_30304_20261108.log` | 118,565,613 | 12 TEA pulls, two clears; P1–P4 |
| TEA-R01 | `Duty Recording/the Epic of Alexander (Ultimate) 2026.11.08 22.57.49.dat` | 15,995,936 | Pulls 1–4 including first clear |
| TEA-R02 | `Duty Recording/the Epic of Alexander (Ultimate) 2026.11.09 00.02.22.dat` | 11,489,428 | Pull 12 including second clear |

The ZIP timestamps are 2026-11-08 23:46 for both DAT members and 23:59 for the log;
these are archive metadata, not encounter clocks. Recording filenames are also
not reliable start timestamps. See the native headers and measured alignment below.
The ZIP has no additional videos, subtitles or strategy documents.

The log is an ACT FFXIV network log, plugin 3.0.3.4. Its timestamps explicitly use
UTC+07:00. The TEA territory is 887 (`0x377`). Pull detection uses the existing
`tools/parser.py` game-combat flag, with director completion distinguishing clears.
Pull 4 begins 2026-11-08 22:41:33.661+07:00 and lasts 962.038 seconds; pull 12 begins
23:42:43.757+07:00 and lasts 967.102 seconds. Durations include the entire encounter,
not just P1. The complete pull table and source fingerprints are in
[evidence-summary.json](evidence-summary.json).

## What was actually decoded

Both DATs validate as native `FFXIVREPLAY` format 5, OS type 3, game build
14155597, content ID 694. Every segment and chapter boundary validates against
the actual file size. R01 contains 168,133 segments / 49 opcodes over 1,558.924
seconds; R02 contains 119,476 segments / 48 opcodes over 1,219.840 seconds. Neither
has backwards segment timestamps. R01 has nine chapters; R02 has three. Generic
chapter type numbers alone are not phase labels.

The existing read-only `replay_inspector.py` works on these files. ARealmRecorded
source is available locally, but its presence does not establish current in-game
playback compatibility. No game playback or native visual verification was performed.

For this exact build, opcode 127 with a 32-byte payload validates as ActorCast:
the 16-bit action matches its 32-bit duplicate, action type is 1, and cast duration
is a finite nonnegative float. These checks passed across 966 total cast records
in R01 and 546 in R02. Filtering to TEA NPC actions observed in the log retains
185 and 133 records respectively. The committed tool deliberately leaves other
game builds opaque. It does not infer an opcode map from an extension.

ActorCast byte 3 is named `BaseCastTime100ms` in local BossMod `ServerIPC.cs`.
The older FRU audit calls this byte an omen delay. Neither interpretation proves
TEA visible omen lead time; TEA values observed here are zero. Cast receipt,
target selection, animation start, visible omen and damage remain different events.

`replay_export_duty_init.py` independently extracted matching zone-in state from
both DATs: entrance EObj 2000182, layout `0x7C1907`, wall layout `0x7C190B`,
position (100, 0, 115), rotation approximately zero; map slots `[0,1,4,1,4,4,4,4]`.
There are no RSV/RSF records. The extractor output is usable native initialization
evidence, not proof that all arena layers render correctly in the inn.

## Source-clock correlation

| Replay | Native header UTC start | Measured replay-zero UTC | Unambiguous NPC cast matches | Receive-time residual range |
|---|---|---|---:|---:|
| R01 | 2026-11-08 15:31:50 | 2026-11-08 15:31:49.949 | 83 across pulls 1–4 | −19 to +13 ms |
| R02 | 2026-11-08 16:42:02 | 2026-11-08 16:42:01.644 | 64 in pull 12 | −24 to +12 ms |

The alignment votes on action-matched offsets and retains only unambiguous
matches. Residuals describe agreement between two receive clocks. They do not
establish server execution accuracy to milliseconds. Simultaneous helper casts
are ambiguous and are excluded from the match count. Native cast durations agree
with ACT (e.g. Cascade 3.7, Hand of Pain 2.7); integer Action-sheet cast lengths
must not silently replace those received remaining durations.

## Phase coverage and timing anchors

Times below are seconds after the clear's game-combat flag. These are observed
receive timestamps, not proposed implementation values. Complete cast ledgers
for both clears are in the JSON companion.

| Event | Pull 4 | Evidence / limitation |
|---|---:|---|
| Cascade cast / effect | 14.716 / 18.683 | 18470; received cast 3.7 s, effect later than cast completion |
| Hand and three Liquid Rage spawns | 19.535 | Native NPC base IDs in companion |
| Dolls spawn | 32.737–32.836 | Four actors; spawn packets arrive across ~0.1 s |
| Puddle Protean cast / effect | 32.843 / 35.833 | 18869; received cast 2.7 s |
| Hand of Parting effect | 34.896 | 18476; castless in ACT |
| Invisible Protean effect | 37.929 | 18870; direction/lock cannot be inferred from damage alone |
| Hand of Pain cast / effect | 39.086 / 42.073 | 18477; received cast 2.7 s |
| Embolus spawns / first move | 41.178 / 46.529 | ACT spawn/move observations |
| Hand removed | 63.254 | Removal, not a recorded death packet |
| Limit Cut numbers start | 65.472 | Markers 79–86 map to numbers 1–8 |
| First Hawk Blaster ground effects | 67.518 | 18480; paired opposite circles |
| Alpha Sword 1/3/5/7 | 74.955 / 79.583 / 84.213 / 88.860 | 18484; target and source poses captured |
| Charges 2/4/6/8 | 76.425 / 81.049 / 85.685 / 90.334 | 19279; target and source poses captured |
| J Kick | 93.562 | 18516; transition damage |
| First P2 Whirlwind | 101.691 | 18882 |
| Judgment Nisi cast | 104.732 | 18494 |
| Temporal Stasis cast | 281.745 | 18522 |
| Inception Formation / Inception casts | 323.827 / 365.964 | 18543 / 18526 |
| Wormhole Formation cast | 415.267 | 18542 |
| Divine Judgment cast | 496.984 | 18552; 74.7 s received duration |
| Final Word cast | 654.966 | 18557 |
| Fate Projection / Calibration α | 703.700 / 711.832 | 18555 / 18556 |
| Fate Projection / Calibration β | 778.652 / 786.777 | 19219 / 19220 |
| Almighty Judgment casts | 875.675 / 920.952 | 18574; helper sequence 18575 / 18576 |
| Temporal Prison cast | 958.257 | 18583; boss dies before its full duration |

P1 is Doll Skip in both clears: only the opening Cascade/Protean/Hand of Pain
sequence occurs before Limit Cut. Dolls remain alive and emit Exhaust 18462
at roughly 39, 50 and 60 seconds. Across both clear logs their ACT movement
records preserve the same x/z coordinates; only headings change. This supports
stationary dolls with targeted attacks, not chase movement.

Hand of Pain is survived with a substantial boss/hand HP disparity. In pull 4,
the Hand reports 1,355,585/3,356,051 HP at resolution while Living Liquid's most
recent effect snapshot 1.293 s earlier is 518,258/3,356,051. This is not an exact
simultaneous HP comparison. Reported damage after mitigation/shields ranges
4,203–21,054 (pull 4) and 1,322–23,418 (pull 12). Both captures show substantial
party mitigation and shields; these numbers cannot establish unmitigated damage
or an HP-difference formula. Do not fit a damage rule from two mitigated samples.

## Recorded strategy and corroboration

The recordings were checked rather than presumed Mana. Evidence supports the
selected convention, while guide-defined responsibilities remain the authority:

* Limit Cut: all eight numbered actors move through the same clockwise pack,
  rather than splitting into opposite 1256 groups. Clear 4 odd-number Sword
  targets resolve E → S → SW → W; clear 12 is rotated 45 degrees earlier,
  NE → SE → S → SW. This matches the 1211 single-pack movement and illustrates
  why a fixed true-north route is wrong. Exact target poses and Hawk Blaster
  ground centers are retained in the JSON companion.
* P2 first/second Nisi pairs in clear 4 are MT–D1, ST–D2, H1–D3, H2–D4,
  using the capture's role positions. The third pass reaches SCH at 183.526
  and WHM at 186.792, before PLD at 189.210 and DRK at 191.488: healer-first
  is directly observed. Clear 12 starts Nisi on TH instead of DPS; healer
  donors still pass before tank donors. Initial Nisi ownership must be rolled.
* Water resolves west, east, east; Lightning north, north, west. Clear 4 first
  water includes its healer owner plus the three non-Lightning DPS; second
  includes all DPS; third includes three DPS and ST. Third Lightning includes
  both healers. The captured locations agree with Mana macro directions, but
  the **third water stack disagrees with the precise macro mark**: the macro
  specifies inner B, while both recordings stack near outer 2. Clear 4's four
  target poses at 204.260 s are (113.76,99.90), (111.04,100.24), (111.99,103.53),
  (116.14,104.05); clear 12's at 205.151 s are (113.08,100.21), (113.76,96.85),
  (112.20,104.60), (117.63,98.59). B is actually at approximately (105.77,100.07),
  and 2 at (113.70,100.13). Implement the JP guide's B assignment; do not relabel
  an outer-east position as B to hide the discrepancy.
* ACT contains no waymark placement records here. Native WaymarkPreset data
  does contain all eight marks, decoded below. The unexpected offset of mark 1
  must be considered when comparing a qualitative cardinal waymark diagram.
* P3/P4 cast and effect coverage is complete, but this inventory does not
  certify every Wormhole number rule or Fate assignment as Mana. Those need
  guide/video and per-role analysis before implementation claims.

Current-status IDs differ from some legacy assumptions: Nisi α/β are 2222/2223,
γ/δ are 2137/2138; final required α/β are 2224/2225, γ/δ 2139/2140.
Compressed Water/Lightning are 2142/2143 and their resistance-down statuses
2144/2145. Status application/removal receive records can repeat; a refresh
must not be counted as a new pass.

## Recorded waymarks

Both native recordings contain the same placement, within 0.06 units. For this
build opcode `0x035E` has a 104-byte WaymarkPreset payload: an eight-bit mask at
offset 0, eight signed x coordinates at 4, eight y coordinates at 36, eight z
coordinates at 68, each divided by 1000. The reserved bytes validate as zero.
This matches the local BossMod `WaymarkPreset` coordinate layout plus four bytes
of trailing packet padding. The initial and terminal snapshots clear the mask;
the pre-pull snapshot activates all eight marks. R01 activates them at replay
13.673 s, R02 at 6.330 s.

| Mark | R01 x | R01 z | R02 x | R02 z |
|---|---:|---:|---:|---:|
| A | 99.971 | 94.070 | 99.977 | 94.090 |
| B | 105.765 | 100.072 | 105.766 | 100.073 |
| C | 100.013 | 105.949 | 99.961 | 106.000 |
| D | 94.084 | 100.011 | 94.085 | 100.011 |
| 1 | 93.055 | 83.222 | 93.056 | 83.222 |
| 2 | 113.699 | 100.127 | 113.700 | 100.128 |
| 3 | 100.011 | 113.742 | 100.011 | 113.743 |
| 4 | 86.274 | 99.995 | 86.274 | 99.995 |

All heights are zero. ABCD are approximately cardinal with radius 6; 234 have
radius approximately 13.7. Mark 1 is **not true north**: it is approximately
22.5 degrees west of north at radius 18.16. This is measured placement in both
sessions, not a claim that every Mana group uses precisely this preset. Where a
guide schematic suggests cardinal 1, the diagram and recordings differ; preserve
this distinction rather than inventing exact guide coordinates.

## Reproduction and limits

Run `python tools/tea_evidence.py <log> --replay <R01> --replay <R02> --output <new.json>`.
The output file must be new; exclusive creation prevents overwriting an input.
The tool exports approved NPC names and base/name IDs, action/status/marker IDs,
anonymous numbered poses, job IDs and encounter timestamps. It never exports
player names, player entity IDs, full packets or their contents. Its complete
local output is intentionally not committed; the small reviewed JSON companion
contains only the inventory and selected encounter observations.

ACT source poses can be stale, especially reused invisible helpers. Ground
effects come from AbilityExtra (264), whose x/z/y ordering differs from game
Vector3 ordering. The encoded all-`−0.015` position means no useful ground
target. ActorChange (261) PosY is the horizontal arena coordinate; PosZ is
height. Sparse numbered samples are not interpolated and are not proof of a
safe route between samples. Some status sources retain the boss ID and cannot
identify the player who passed them; recipient transitions and role observations
must be correlated.

Headless extraction cannot establish visible native jumps, cutscene animations,
omen fade-in, enemy-list attachment or overhead marker rendering. These remain
explicit in-game retest requirements. The packet safety harness and scenario
tests establish separate properties and cannot certify visual presentation.
