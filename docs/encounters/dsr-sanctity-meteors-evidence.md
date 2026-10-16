# DSR Sanctity of the Ward, Part 2: evidence and reconstruction

## Source and coverage

Source: `Network_30303_20261016.log`; SHA-256 `90c8ab281a203eba73b221a6f76a899ab1f4dd66f7f6d103cbeab3d6a739a4e8`. Raw log stays outside the repository. Original physical line numbers and timestamps in the accompanying [typed event inventory](dsr-sanctity-meteors-events.json) are the evidence references. Player names, original player actor IDs and home worlds are omitted; player aliases are stable across attempts, while NPC spawn aliases are local to each attempt. Job IDs and NPC base/name IDs, where known, are retained. No alias exposes the original identity.

T=0 is the first Meteor Prey headmarker (285 / 0x011D), rather than the earlier Sanctity cast. The file contains 15 Sanctity starts, 12 Meteor executions and two **complete mechanic executions with failures** (both tower waves, fourteen comet releases, knockback). It contains no demonstrated successful Meteor solution. Three Sanctity starts never reach the Meteor marker. The ten other Meteor executions have incomplete resolution coverage; do not use them as full-timeline authorities.

| Meteor attempt | Marker timestamp (+09:00) | Marker line | First tower effects | Comet effects | Second tower effects | Full execution |
|---|---|---:|---:|---:|---:|---|
| 1 | 2026-10-16T22:29:53.446000+09:00 | 308750 | 8 | 0 | 0 | No |
| 2 | 2026-10-16T22:40:14.212000+09:00 | 337767 | 8 | 8 | 0 | No |
| 3 | 2026-10-16T22:43:17.945000+09:00 | 349393 | 8 | 14 | 0 | No |
| 4 | 2026-10-16T23:12:18.367000+09:00 | 400997 | 8 | 8 | 0 | No |
| 5 | 2026-10-16T23:19:09.981000+09:00 | 427298 | 8 | 8 | 0 | No |
| 6 | 2026-10-16T23:35:43.266000+09:00 | 487529 | 8 | 8 | 0 | No |
| 7 | 2026-10-16T23:41:52.976000+09:00 | 510310 | 8 | 8 | 0 | No |
| 8 | 2026-10-16T23:45:52.747000+09:00 | 522291 | 8 | 14 | 0 | No |
| 9 | 2026-10-16T23:52:16.905000+09:00 | 534881 | 8 | 14 | 8 | Yes, failed |
| 10 | 2026-10-16T23:55:19.454000+09:00 | 547581 | 8 | 8 | 0 | No |
| 11 | 2026-10-17T00:10:25.799000+09:00 | 605802 | 8 | 14 | 8 | Yes, failed |
| 12 | 2026-10-17T00:13:24.513000+09:00 | 618512 | 8 | 10 | 0 | No |

Attempt 9 starts 23:52:16.905; attempt 11 starts 00:10:25.799 on October 17. Each records eight deaths within the analysis window. Some wipe-cleanup events overlap later effects. The final attempt leaves territory 968 before completing; the parser ends its window at that transition. Each window includes two seconds before the marker and at most 33 seconds after it. Largest whole-record gaps in the two full windows are 1.054s and 0.883s respectively. These gaps are not proof of packet loss; neither absence of an event nor dense logging proves rendering coverage. No millisecond-accurate snapshot claim is made.

## Observed receive timeline

Intervals below span all usable attempts. Multi-target effects are counted once per source and server sequence, retaining individual target rows in the JSON. Cast positions and impact-source positions are separate evidence; helpers can move between them.

| Action | Record | Earliest T | Median T | Latest T |
|---|---|---:|---:|---:|
| 29563 | 20 | 0.044 | 0.045 | 0.051 |
| 29563 | effect | 9.201 | 9.223 | 9.238 |
| 29564 | 20 | 0.044 | 0.045 | 0.051 |
| 29564 | effect | 12.007 | 12.026 | 12.043 |
| 25576 | 20 | 0.088 | 0.089 | 0.092 |
| 25576 | effect | 12.051 | 12.073 | 12.087 |
| 28590 | 20 | 0.132 | 0.134 | 0.139 |
| 28590 | effect | 7.090 | 7.112 | 7.133 |
| 28591 | 20 | 0.132 | 0.134 | 0.139 |
| 28591 | effect | 7.592 | 7.618 | 7.634 |
| 28592 | 20 | 0.132 | 0.134 | 0.139 |
| 28592 | effect | 7.592 | 7.618 | 7.634 |
| 25574 | 20 | 0.176 | 0.178 | 0.179 |
| 25574 | effect | 7.134 | 7.157 | 7.177 |
| 25575 | effect | 7.759 | 7.788 | 7.827 |
| 28650 | 20 | 14.352 | 14.389 | 14.412 |
| 28650 | effect | 22.532 | 22.545 | 22.582 |
| 28651 | 20 | 14.352 | 14.389 | 14.412 |
| 28651 | effect | 25.318 | 25.331 | 25.345 |
| 25308 | 20 | 18.419 | 18.438 | 18.448 |
| 25308 | effect | 22.398 | 22.406 | 22.412 |

For each complete execution, comet effects occur as seven paired releases, approximately 1.43 seconds apart. This verifies fourteen drops, not fourteen separate timing waves. Spawn-to-effect intervals for 114 observed comet actors range 0.519–0.921s (median 0.7195s). This is actor-observation latency plus encounter behavior, **not a direct measurement of the server targeting snapshot**. The simulator now models a rounded 0.7s visible spawn lead; target capture at that spawn is inferred.

Prey status 562 is applied to the two marker recipients with 23s duration. Early removals accompany deaths and cannot replace its successful expiration. Hiemal Storm applies Ice Resistance Down II (2903 / 0xB57) for about 2.96s. Frostbite (2946 / 0xB82) appears by T+10.087 and Burns (2945 / 0xB81) by T+9.489. These are **upper bounds on hazard activation**, not its exact onset. Deaths, Damage Down, Paralysis, Sprint and anti-knockback statuses are retained separately from successful mechanics.

## Positions and axes

ACT X is game X (east); ACT Y is game Z (south); ACT Z is game Y (height). Normalize `(ACT X, ACT Y, ACT Z)` to `(X-100, Z, Y-100)` for this arena. The native arena origin is `(100,0,100)`. No fitted rotation is applied. Recorded headings remain native radians; the strategy helper Direction(0) points north (-Z), whereas native heading 0 faces +Z.

- **Observed:** first-wave tower caster coordinates lie on radius 18 (outer) or radius 6 (inner). Outer positions include `(±9, ±15.59)`, `(±15.59, ±9)` and cardinal 18; inner positions are `(±4.24, ±4.24)`. CastExtra coordinates corroborate the same locations within packet quantization.
- **Observed:** second-wave tower casters occupy all eight cardinal/intercardinal positions on radius 18. Intercardinals are `(±12.73, ±12.73)`.
- **Observed:** Fire impact sources and Type-7 objects 0x1EB686 repeat at `(±8.4853, ±8.4853)` (radius 12). Initial helper cast-source positions can differ and are not used as final Fire centers.
- **Observed:** Ice Type-7 objects 0x1EB682 appear at the moving pairs, near the four cardinal baits. These are player-selected positions, not fixed cardinal coordinates. Target position rows and subsequent object positions differ slightly as players move.
- **Observed:** Thordan last moves to approximately `(0,-19.02)` before the complete reference marker (ActorMove line 605574); the old north-23 placement was unsupported and is corrected. His recorded height is about 0.20; the practice arena retains its existing floor-height convention. Faith Unmoving source is center `(0,0)`. Hermenost, Noudenet, Charibert and Haumeric cast from south/east/north/west at radius 6 in the complete reference execution.
- **Derived:** ideal outer radius 18, inner radius 6 and Fire radius 12 use the symmetric relationships behind the rounded recorded coordinates. The state generates the full geometry; it does not replay a failed human route.
- **Unverified:** exact randomized pattern weighting/correlations, meteor separation/falloff threshold, damage activation frames and server capture lead. Radius-3 towers, radius-7 puddles and 16-yalm knockback are corroborated by the existing Action-data/BossMod implementation rather than measured from player movement in this log.

## Strategy cross-check

[Tuufless: Meteors](https://ffxiv.tuufless.com/elemental/dsr/02_thordan/#part-2-meteors) was consulted during this reconstruction. Preserve its role swaps, Meteor-role outside priority, communication, clockwise routes and inner knockback alignment. The 810 training patterns and 54 forced-cursed patterns are retained. Sampling remains a training distribution; these twelve pulls do not establish game RNG probabilities.

Ice targeting a pair is independent of the fixed Fire geometry. The current simulator selects one member of each pair in the Meteor role category and places Ice at that member, requiring two-person stack occupancy. The guide establishes pair stacking; splash-hit target rows do not independently prove which role category the server selected as primary Ice targets. That category coupling remains an explicit unresolved targeting detail. The repository exposes one Elemental AI for this scenario; it did not contain a separately selectable LPDU Unity AI to preserve. No new strategy is claimed.

## Simulator changes and remaining estimates

The old scenario placed its Prey markers at scenario T+1.5; the rebuilt scenario anchors them at T=0. Therefore the old/new absolute constants below also reflect a clock-origin correction. Compare event intervals to separate clock alignment from actual mechanic errors: the old first-tower-to-first-comet gap was 3.4s, while the log establishes approximately 1.4s. The old damage sequence put Ice before Fire; the log establishes Fire before Ice. The approximately 13.3s between tower resolutions and 10.4s from first towers to knockback were already close.

| Old baseline | Replacement | Evidence class |
|---|---|---|
| First towers .7–12.4 | Cast from .05; fallback persists through effect at 12.05 (snapshot 11.75) | Observed cast/effect alignment, sheet snapshot model |
| Second towers 15.1–25.8 | Cast from 14.4; fallback persists through 25.4 (snapshot 25.1) | Observed cast/effect alignment, sheet snapshot model |
| Fire visual 5.5, impact 9 | Impact 7.63; object presentation 8.15 | Impact observed; rounded actor-arrival presentation |
| Ice visual/impact 8.5 | Storm effect 7.79; object presentation 8.3 | Impact observed; rounded actor-arrival presentation |
| First comet 16.1 | First effect 13.45, then six intervals of 1.43; spawn lead .7 | Effects observed; capture at spawn inferred |
| Knockback 23.1 | Cast 18.44, effect/motion 22.44 | Observed, rounded to sheet-compatible schedule |
| Outer 17.5, inner diagonal 3.6 | Radius 18 / radius 6 | Observed and derived symmetry |
| Fire diagonal 7.3 | Diagonal 8.4853 (radius 12) | Observed |
| +3s activation, +14s expiry for both hazards | Fire danger from presentation; Ice T+9.9; both safe at knockback snapshot T+22.14 | **Inferred practice model; exact retail onset/cutoff unknown** |

The scenario’s T+28 successful cleanup remains a practice outro, not an observed successful encounter ending; both full recorded executions fail.

The Ice +3s rule was contradicted by observed Frostbite before that deadline. T+9.9 is a conservative practice estimate preceding the earliest recorded Frostbite, not an observed activation event. Fire danger from object presentation is likewise conservative; Burns only bounds onset. Owner observation establishes that both puddles are safely traversable for knockback and disappear shortly afterward. The chosen common safe cutoff is the simulator knockback snapshot (cast bar end), 0.3s before motion/effect. This is explicitly **inferred**, not a recovered retail frame. Do not treat passing bots as validation of these estimates.

Bot routes now leave pairs via radial waypoints: inner players pass through center; outside players move radially outward before entering tower sectors. This prevents straight-line Fire clipping. Meteor routes use radius 19.8 so their straight movement segments stay outside the radius-7 fixed Fire. Sprint begins after first tower snapshot, before the earlier first comet spawn. Later drops steer after each previous spawn rather than waiting for its effect. Outside non-Meteor players hold the outer rim until the common hazard cutoff before entering final towers. Inner players align at radius 2 for a 16-yalm push into radius-18 towers. Easy/Hard retain the same deadlines and assignments; no patterns are filtered to pass.

## Native presentation audit

The initial reconstruction retained the corrected local projected fallback. The native-tower follow-up below supersedes that rendering choice: normal rendering now instantiates the action’s native Conviction omen AVFX directly. The log verifies 9020 / NameId 3640 tower helpers casting 29564 and 28651, not a reproducible persistent tower EObj. All Type-7 additions are inventoried, not just known puddles: 0x1E850B and 0x1EA1A1 reappear at arena entrance positions during resets; final non-arena objects belong to the territory exit and are excluded. There is no MapEffect (257) inside the Meteor windows. None of this proves towers have no native renderable; it means this capture does not identify its binding.

Existing native inspection paths reviewed: `tools/replay_inspector.py`, `tools/replay_export_rsv_rsf.py`, `ArenaSnapshot`, `OfflineSnapshot`, and the DSR arena fixture. Prior DSR investigation in `DSR-ROADMAP.md` documents inert action/omen-only tower attempts. FRU Darklit has verified map-effect indices 0x2A–0x2D on its own arena; those bindings cannot be transplanted into DSR. No DSR replay/capture establishing persistent tower spawn state was available in this source. No speculative memory flags, asset injection or server traffic were added.

Fire/Ice EObj row IDs and positions are observed, but Type-7 snapshot combat fields contain obviously invalid HP/MP/cast values. Only identity/type/position and presence are used. Tower helpers now use observed NameId 3640; Ice/Fire use the observed generic helper identities, and Storm applies the observed short Ice Resistance Down II. Actual EObj TimelineState/EventState/Arg2 cannot be recovered reliably from those bogus fields. The existing safe local EventObject spawn path is retained. In the full executions Fire object removals occur at 25.106/25.146; Ice at 25.146/25.503. Actor presence is **not** damage lifetime or proof of visible duration. A rounded T+25.1 local despawn caps existing presentation; exact visual fade and native state remain for client capture.

All local scheduler, hazard and object lifetimes use scenario time and freeze on Pause. Tower fallback spans the logical soak snapshot and release. Native one-shot casts, animations and EObj internal timelines can continue in real time while paused; frozen ownership does not freeze their animation. Resume may therefore show a finished native animation while the logical hazard remains. This pre-existing native limitation is not hidden or claimed fixed. Stop/Restart cancels world-owned events and despawns both pending and resolved comet actors, objects and fallback requests.

## Reproduce and inspect

```sh
python tools/dsr_meteors_reconstruction.py '/path/Network_30303_20261016.log' --output /tmp/dsr-meteors.json
python -m unittest discover -s tools/tests
```

The new analyzer reuses `tools/parser.py` and leaves the previous short `dsr_meteors_evidence.py` API intact. It inventories record kinds and territory transitions, aliases actors per attempt, deduplicates releases by source+sequence, keeps target rows, and extracts casts, CastExtra/AbilityExtra, positions/movement, NPC spawns/removals, EObj snapshots, relevant statuses, markers, tethers, deaths, map and director/ActorControl evidence. Unrecognized records are counted, not interpreted. Whole-record gaps do not imply packet loss. Tests use a small sanitized failed-execution fixture rather than committing the original log.

## In-game verification required

1. In inn and offline DSR P2, inspect radius-18 outer/second towers, radius-6 inner towers, correct cardinal axes and radius-12 Fire centers against arena notches. Confirm eight native Conviction towers per wave, including ground graphics and pillar height; the diagnostic overlay is off by default. Native appearance still needs client confirmation.
2. Observe fixed Fire and pair-baited Ice, their appearance relative to impacts, earliest hazardous contact and exact safe cutoff near knockback. Capture native EObj state transitions/fade; actor removal alone is insufficient.
3. Practice each Meteor category and all seats, including forced-cursed N/S patterns, Easy/Hard and visible/hidden bots. Verify seven drops per marked player, pre-impact comet appearance, clockwise clearance, first tower departure and final assignments. Check anti-knockback after drop five and inner knockback alignment.
4. Pause during both tower waves and during Fire/Ice. Logical windows must remain; inspect native animation tails separately. Resume, Stop mid-wave, restart after idle, repeat entry/exit and confirm no lingering objects, markers or callbacks.
5. Confirm native models, Thordan placement, arena/environment presentation, local cooldown controls and Discord Bridge offline entry/exit. Automated tests cannot validate rendering, exact visual fade or native hooks.

## Complete reference execution: chronological action releases

This compact table lists unique releases for attempt 11. The JSON also retains every target row, cast, actor observation, relevant status and position with its original timestamp and physical line.

| T | Timestamp (+09:00) | Action / name | Source alias | Position (local X,Y,Z) | Original line |
|---:|---|---|---|---|---:|
| 7.113 | 2026-10-17T00:10:32.9120000+09:00 | 28590 Heavens' Stake | actor-16 | [0.0, -0.0, -6.0] | 606099 |
| 7.158 | 2026-10-17T00:10:32.9570000+09:00 | 25574 Hiemal Storm | actor-17 | [-6.0, -0.0, 0.0] | 606103 |
| 7.605 | 2026-10-17T00:10:33.4040000+09:00 | 28591 Heavens' Stake | actor-14 | [-8.49, -0.0, -8.49] | 606109 |
| 7.605 | 2026-10-17T00:10:33.4040000+09:00 | 28591 Heavens' Stake | actor-13 | [-8.49, -0.0, 8.49] | 606112 |
| 7.605 | 2026-10-17T00:10:33.4040000+09:00 | 28591 Heavens' Stake | actor-12 | [8.49, -0.0, 8.49] | 606115 |
| 7.605 | 2026-10-17T00:10:33.4040000+09:00 | 28591 Heavens' Stake | actor-11 | [8.49, -0.0, -8.49] | 606118 |
| 7.605 | 2026-10-17T00:10:33.4040000+09:00 | 28592 Heavens' Stake | actor-10 | [0.0, -0.0, 0.0] | 606121 |
| 7.784 | 2026-10-17T00:10:33.5830000+09:00 | 25575 Hiemal Storm | actor-18 | [-12.73, -0.0, -12.73] | 606130 |
| 7.784 | 2026-10-17T00:10:33.5830000+09:00 | 25575 Hiemal Storm | actor-19 | [-12.73, -0.0, 12.73] | 606133 |
| 7.784 | 2026-10-17T00:10:33.5830000+09:00 | 25575 Hiemal Storm | actor-20 | [12.73, -0.0, 12.73] | 606136 |
| 7.784 | 2026-10-17T00:10:33.5830000+09:00 | 25575 Hiemal Storm | actor-21 | [12.73, -0.0, -12.73] | 606139 |
| 9.214 | 2026-10-17T00:10:35.0130000+09:00 | 29563 Conviction | actor-9 | [0.0, -0.0, 6.0] | 606291 |
| 12.034 | 2026-10-17T00:10:37.8330000+09:00 | 29564 Conviction | actor-7 | [-15.59, -0.0, -9.0] | 606446 |
| 12.034 | 2026-10-17T00:10:37.8330000+09:00 | 29564 Conviction | actor-6 | [-9.0, -0.0, 15.59] | 606450 |
| 12.034 | 2026-10-17T00:10:37.8330000+09:00 | 29564 Conviction | actor-5 | [9.0, -0.0, 15.59] | 606454 |
| 12.034 | 2026-10-17T00:10:37.8330000+09:00 | 29564 Conviction | actor-4 | [15.59, -0.0, 9.0] | 606458 |
| 12.034 | 2026-10-17T00:10:37.8330000+09:00 | 29564 Conviction | actor-3 | [4.24, -0.0, 4.24] | 606462 |
| 12.034 | 2026-10-17T00:10:37.8330000+09:00 | 29564 Conviction | actor-8 | [-9.0, -0.0, -15.59] | 606466 |
| 12.034 | 2026-10-17T00:10:37.8330000+09:00 | 29564 Conviction | actor-1 | [4.24, -0.0, -4.24] | 606470 |
| 12.034 | 2026-10-17T00:10:37.8330000+09:00 | 29564 Conviction | actor-2 | [-4.24, -0.0, 4.24] | 606474 |
| 12.078 | 2026-10-17T00:10:37.8770000+09:00 | 25576 Holy Comet | actor-15 | [6.0, -0.0, 0.0] | 606479 |
| 13.465 | 2026-10-17T00:10:39.2640000+09:00 | 25577 Holy Comet | actor-31 | [-8.45, -0.0, -16.87] | 606583 |
| 13.465 | 2026-10-17T00:10:39.2640000+09:00 | 25577 Holy Comet | actor-32 | [8.04, -0.0, 17.14] | 606588 |
| 14.895 | 2026-10-17T00:10:40.6940000+09:00 | 25577 Holy Comet | actor-33 | [-6.97, -0.0, -17.55] | 606700 |
| 14.896 | 2026-10-17T00:10:40.6950000+09:00 | 25577 Holy Comet | actor-34 | [3.04, -0.0, 19.02] | 606705 |
| 16.327 | 2026-10-17T00:10:42.1260000+09:00 | 25577 Holy Comet | actor-35 | [2.68, -0.0, -18.8] | 606770 |
| 16.327 | 2026-10-17T00:10:42.1260000+09:00 | 25577 Holy Comet | actor-36 | [1.69, -0.0, 19.11] | 606773 |
| 16.998 | 2026-10-17T00:10:42.7970000+09:00 | 25578 Holy Impact | actor-31 | [-8.45, -0.0, -16.87] | 606800 |
| 16.998 | 2026-10-17T00:10:42.7970000+09:00 | 25578 Holy Impact | actor-33 | [-6.97, -0.0, -17.55] | 606809 |
| 17.759 | 2026-10-17T00:10:43.5580000+09:00 | 25577 Holy Comet | actor-37 | [10.41, -0.0, -16.47] | 606863 |
| 17.759 | 2026-10-17T00:10:43.5580000+09:00 | 25577 Holy Comet | actor-38 | [-3.65, -0.0, 18.4] | 606867 |
| 18.428 | 2026-10-17T00:10:44.2270000+09:00 | 25578 Holy Impact | actor-34 | [3.04, -0.0, 19.02] | 606966 |
| 18.428 | 2026-10-17T00:10:44.2270000+09:00 | 25578 Holy Impact | actor-36 | [1.69, -0.0, 19.11] | 606970 |
| 19.187 | 2026-10-17T00:10:44.9860000+09:00 | 25577 Holy Comet | actor-40 | [16.05, -0.0, -10.97] | 607026 |
| 19.187 | 2026-10-17T00:10:44.9860000+09:00 | 25577 Holy Comet | actor-39 | [-9.58, -0.0, 16.96] | 607028 |
| 20.622 | 2026-10-17T00:10:46.4210000+09:00 | 25577 Holy Comet | actor-41 | [2.19, -0.0, -3.52] | 607074 |
| 20.622 | 2026-10-17T00:10:46.4210000+09:00 | 25577 Holy Comet | actor-42 | [2.19, -0.0, -3.52] | 607076 |
| 22.055 | 2026-10-17T00:10:47.8540000+09:00 | 25577 Holy Comet | actor-44 | [2.2, -0.0, -3.06] | 607109 |
| 22.055 | 2026-10-17T00:10:47.8540000+09:00 | 25577 Holy Comet | actor-43 | [2.2, -0.0, -3.06] | 607111 |
| 22.412 | 2026-10-17T00:10:48.2110000+09:00 | 25308 Faith Unmoving | actor-30 | [0.0, -0.0, 0.0] | 607123 |
| 22.546 | 2026-10-17T00:10:48.3450000+09:00 | 28650 Conviction | actor-9 | [0.0, -0.0, 6.0] | 607131 |
| 22.680 | 2026-10-17T00:10:48.4790000+09:00 | 25578 Holy Impact | actor-41 | [2.19, -0.0, -3.52] | 607136 |
| 22.680 | 2026-10-17T00:10:48.4790000+09:00 | 25578 Holy Impact | actor-42 | [2.19, -0.0, -3.52] | 607138 |
| 24.156 | 2026-10-17T00:10:49.9550000+09:00 | 25578 Holy Impact | actor-44 | [2.2, -0.0, -3.06] | 607167 |
| 24.156 | 2026-10-17T00:10:49.9550000+09:00 | 25578 Holy Impact | actor-43 | [2.2, -0.0, -3.06] | 607169 |
| 25.318 | 2026-10-17T00:10:51.1170000+09:00 | 28651 Conviction | actor-7 | [-12.73, -0.0, -12.73] | 607205 |
| 25.318 | 2026-10-17T00:10:51.1170000+09:00 | 28651 Conviction | actor-6 | [-18.0, -0.0, 0.0] | 607207 |
| 25.318 | 2026-10-17T00:10:51.1170000+09:00 | 28651 Conviction | actor-5 | [-12.73, -0.0, 12.73] | 607209 |
| 25.318 | 2026-10-17T00:10:51.1170000+09:00 | 28651 Conviction | actor-4 | [0.0, -0.0, 18.0] | 607211 |
| 25.318 | 2026-10-17T00:10:51.1170000+09:00 | 28651 Conviction | actor-3 | [12.73, -0.0, 12.73] | 607213 |
| 25.318 | 2026-10-17T00:10:51.1170000+09:00 | 28651 Conviction | actor-8 | [0.0, -0.0, -18.0] | 607215 |
| 25.318 | 2026-10-17T00:10:51.1170000+09:00 | 28651 Conviction | actor-1 | [12.73, -0.0, -12.73] | 607217 |
| 25.318 | 2026-10-17T00:10:51.1170000+09:00 | 28651 Conviction | actor-2 | [18.0, -0.0, 0.0] | 607220 |

## Validation before integration

The affected optimized **Debug** encounter/lifecycle run passed 2,375 tests, including
all Easy/Hard Meteor patterns and offline/inn lifecycle checks. The explicit hazard-cutoff
and pending-comet Stop/Restart follow-up passed six cases. Python tooling passed 75 tests;
relay passed 44. Safety passed 308 with three Linux-only native-hook skips for each of the
two hook models, using ordinary and isolated optimized TRACE Debug artifacts. The original
raw file was checked against the committed report/inventory/fixture: no original player
names or player actor IDs were found. Native rendering and hazard-frame accuracy still
require the client checklist above; passing tests do not establish those properties.

Final prepared-tree validation: the complete optimized Debug plugin suite passed **12,225**
tests, including other FRU/DSR scenarios. The complete Debug solution build succeeded with
zero errors (existing platform-analysis and NuGet vulnerability-feed warnings remain).
The freshly rebuilt 2.1.0.0 ordinary/optimized Debug safety artifacts passed both hook models
again: **308 passed, three platform skips each**. Release-note extraction for 2.1.0.0 succeeded.

## Native tower follow-up

Both Conviction actions (29564 and 28651), and Strength Conviction (25567), resolve through
installed Action data to Omen 90: `vfx/omen/eff/d1025_sp012_bunsan01s.avfx`.
The installed resource is 67,200 bytes, SHA-256
`5e71aac1977beb83be77ea00b337dff72cf348d347de762e64c2402650394589`.
It references the Rounds Diffusion Attack omen sound and native omen textures including
`d1025_elc_line01as.atex`, `uv_hex01bs.atex`, and `maholine1s.atex`.
The release timeline is separately verified as ActionTimeline 4470,
`mon_sp/gimmick/round_bunsansyouhani`, whose TMB references
`vfx/monster/d1025/eff/round_bunsanshouhani_t0s.avfx`. The release effect is not used as a
substitute for the persistent cast omen.

Code inspection found that `SimOmen` existed but had no production caller. Previous attempts
sent native ActorCast packets to the helper; they did not explicitly instantiate a world-owned
omen through this path. The follow-up exposes `SimWorld.SpawnOmen`, uses the Action row to
resolve the native resource and radius, and instantiates the real game AVFX through the existing
`IVfxFunctions` / `VfxObject` lifecycle. No EObj ID, map-effect index, asset replacement, new native
memory binding or server packet is invented. Native cast/impact dispatch remains intact.

Each tower owns a native handle at its verified world position, with radius-three X/Z scale and
native Y scale. Both waves explicitly release their handles at the existing action-effect times,
after their soak snapshots. Stop/Restart and world cleanup also release them, idempotently.
Normal UI rendering no longer draws projected tower geometry. The old overlay is available only
through an explicitly enabled diagnostic checkbox, with expected/native handle counts shown
separately. Failed native allocation does not silently turn it on or change tower mechanics.

This verifies the asset identity, action association and native instantiation path, **not a
client screenshot proving pillar appearance**. Actual depth, height, scale, sound, duplicate
omens from cast dispatch and long-pause behavior require a Debug in-client check. Logical
ownership remains frozen during Pause; the native particle animation can continue in real time.
If the client asset self-completes during a long pause, ownership alone cannot restart its
internal animation. Do not report that animation as frozen.

Read-only asset verification used installed Lumina GameData, Action/Omen/ActionTimeline rows,
and `GetFile<FileResource>`. Extracted game assets stayed in `/tmp` and are not committed.
Archived DSR recorder files were located; the existing strict inspector rejected the cached
copies' chapter timestamp consistency, so no new packet-schema claim was based on them.

Headless native-presentation tests check the exact sheet-derived path, scale, local-to-world
placement, both wave windows, ownership during Pause, cleanup exactly once, repeated delayed
starts, and native allocation failure. These tests cannot replace the client check above.

The existing native bridge was also cross-checked against the primary
[FFXIVClientStructs VfxObject API](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Graphics/Scene/VfxObject.cs).
Native-specific headless tests: nine passed. The initial ordinary/optimized Debug safety run
passed 308 checks with three platform skips. Final integrated validation is reported separately.

Final native-tower integrated validation: **12,234 plugin tests passed** in isolated optimized
Debug output; Python tooling passed 75. The complete Debug solution and ordinary/optimized
TRACE Debug plugin builds succeeded with zero errors. Both safety backends passed **308 checks
with three platform skips each** against the integrated 2.1.0.0 binaries. Native client rendering
remains the manual check, including pillar shape, radius, occlusion, sound, duplicates and long Pause.
