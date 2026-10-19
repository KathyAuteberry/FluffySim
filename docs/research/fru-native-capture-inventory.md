# FRU P4 Crystallize Time / P5 native capture inventory

Inventory date: 2026-10-20 (Asia/Tokyo). Source logs were read only. This document contains aggregate counts and relative timings, without player names, actor IDs, packet bodies or copied captures.

## Method and scope

Parsed all **32** `.log` files in the accessible local IINACT archive, approximately **3.8 GiB**, covering filename dates 2026-09-19 through 2026-10-19. Parsing used `tools/parser.py`'s `iter_records`, not action-name string searches. Only actual record types `20` (cast), `21` and `22` (action effects), with the hexadecimal action-ID field converted to an integer, count as mechanic evidence. The current territory was tracked from `01` ChangeZone records; combat state from field 3 of `260`; wipe/reset context from `33` director records.

The search set covered CT casts 40240/40298, its characteristic dragon/puddle/wave/knockback actions, the P4 Akh Morn actions, and all P5 Fulgent, exaline, Akh Morn, Paradise, wings, towers, Polarizing, Box and Paradise Lost actions from the current scenario constants. P5 Box/enrage IDs 40326–40328 were included explicitly. A textual occurrence of a name or number in chat, a dictionary, a position float or an unrelated payload was not counted.

Fourteen files contain territory-1238 records, totaling **3,652,927 records of all types**. Five of those contain no cast/effect records at all; they therefore establish local arena/activity context, not mechanic timing.

| Filename date | FRU records, all types | Cast records, all actions | CT entry casts | P5 casts/effects |
|---|---:|---:|---:|---:|
| 2026-09-23 | 317396 | 2997 | 0 | 0 |
| 2026-09-27 | 395962 | 3837 | 0 | 0 |
| 2026-09-30 | 325719 | 3323 | 0 | 0 |
| 2026-10-04 | 446981 | 4488 | 0 | 0 |
| 2026-10-07 | 485864 | 5037 | 0 | 0 |
| 2026-10-09 | 20721 | 0 | 0 | 0 |
| 2026-10-10 | 10587 | 0 | 0 | 0 |
| 2026-10-11 | 440701 | 4657 | 0 | 0 |
| 2026-10-12 | 18953 | 0 | 0 | 0 |
| 2026-10-14 | 377519 | 3927 | 2 | 0 |
| 2026-10-15 | 6323 | 0 | 0 | 0 |
| 2026-10-16 | 852 | 0 | 0 | 0 |
| 2026-10-17 | 331250 | 2865 | 2 | 0 |
| 2026-10-18 | 474099 | 4913 | 0 | 0 |

Dates identify archive filenames, not guaranteed encounter dates for every record in a file. Files spanning session boundaries were not treated as a single pull.

## What was actually observed

**No P5 cast or action-effect record was found**, in FRU or another tracked territory. Local logs therefore do not directly validate any P5 timing, native line/tower effect, targeting rule, resistance interaction or phase-loop gap.

Two incomplete CT entry pairs occur in the `Network_30303` logs dated October 14 and October 17:

| Item | October 14 | October 17 |
|---|---:|---:|
| Usurper CT cast 40240 | 1 record, 9.700 s | 1 record, 9.700 s |
| Oracle CT cast 40298 | 1 record, 9.700 s | 1 record, 9.700 s |
| Difference between paired starts | 0.000 s | 0.000 s |
| Wipe director 0x40000005 relative to CT starts | -3.028 s | -2.718 s |
| Game combat flag at CT starts | 0 | 0 |
| Reset/fadeout-complete director 0x40000006 | +7.183 s | +7.493 s |
| CT action effects completing the cast | 0 | 0 |

These are **interrupted casts in wipe cleanup**, not successful CT mechanic sequences. Their displayed duration and simultaneous entry are directly observed in the log stream; they do not validate debuff application, hourglass progression, dragons, Return, Tidal Light or late CT timing. Do not shift the complete mechanic timeline to match these truncated boundaries.

The remaining matching records belong to **preceding P4 content**, not evidence of completing CT:

| Action | Cast records | Effect records | Interpretation |
|---|---:|---:|---|
| Edge of Oblivion 40174 | 6 | 45 | Pre-CT P4 damage across October 11/14/17/18 |
| Usurper Akh Morn 40247 | 2 | 2 | Pre-CT paired cast, October 14/17 |
| Usurper Akh Morn hit 40248 | 0 | 9 | Repeated prior damage records; not nine independent casts |
| Oracle Akh Morn 40302 | 2 | 2 | Pre-CT paired cast, October 14/17 |
| Oracle Akh Morn hit 40303 | 0 | 33 | Multiple target/hit records; not thirty-three independent casts |

Effect-record counts intentionally count ACT records. Multi-target actions produce multiple records and must not be interpreted as distinct snapshots without deduplicating timestamps/source/action/sequence.

## Native duty versus simulation/replay provenance

Territory 1238 alone is insufficient to prove retail encounter provenance: the local plugin can load that territory, and Duty Recorder playback can reproduce native duty events. The two CT pairs sit in long FRU combat streams with surrounding native wipe/reset director sequences and preceding Darklit/Akh Morn events. This supports interpreting them as native encounter or native-replay cleanup, rather than an inn-only practice timeline. They are not evidence of a live, successful CT pull.

The archive does not carry a trusted per-record label distinguishing live server input, Duty Recorder playback and every historical simulator build. Therefore the exact provenance remains qualified. The current plugin creates simulated native casts through local dispatcher calls, and actor-ID resemblance alone is not sufficient authentication. No incomplete source attribution has been promoted to a claim of server-observed P5 or full CT timing.

Existing arena snapshots under both plugin configuration names include FRU P1–P4 and DSR, but **no P5 snapshot**. An object being present in a P4 snapshot does not establish P5 activation or rendered state.

## BossMod Reborn versus original BossMod

Inspected local source revisions:

- BossMod Reborn: `343f7646ca8ae89055e6e9a61aaa5fe328f8e2f1`, `BossMod/Modules/Dawntrail/Ultimate/FRU/`.
- Original BossMod: `40b0abdd35e81517409bdc81446910f943a75a49`, `BossMod.Ultimate/Dawntrail/Ultimate/FRU/`.

Compared `P5FulgentBlade.cs`, `P5AkhMorn.cs`, `P5ParadiseRegained.cs` and `P5PolarizingStrikes.cs`. Their mechanics are overwhelmingly shared. BMR mainly changes collection APIs, drawing/distance helpers, class sealing, numeric types and event accessors. These forks must not be counted as two independent measurements of the same inherited constant.

| Mechanic | Shared semantic evidence | Meaningful difference / limit |
|---|---|---|
| Fulgent | 5-yalm line progression, ~2-second interval, matching cast IDs, rectangular geometry and six initial seams | BMR rewrites line lookup as a loop and sorts using a comparer; no new timing basis identified |
| Akh Morn | Radius 4, four-player stacks, boss-relative side split | Both display the nearest player on each side in predictive hints. This does not override Tuufless's random-target description or establish real selection from packets |
| Paradise towers | Slots 51/52/53, show state 0x00020001, radius 3, distance 7, exactly two soakers | Same inherited geometry/order model |
| Wings | Dark chooses nearest, Light farthest; alternating cleaves/baits | BMR cleave drawing range is 100 versus original's explicitly compromised 19; installed action data remains the simulation's authority for range |
| Polarizing | Width-6 lines (half-width 3), repeated echoes approximately 2 seconds after first effects, resistance-down exclusions | Same inherited semantics; BMR makes no newly evidenced resistance-duration/timing correction |

`FRUStates.cs` supplies the phase-loop gaps used by the continuous practice wrapper. It remains corroborating implementation evidence, not a local combat-log measurement.

## Splatoon cross-check

Inspected local revision `f43b92fa6c130aff00035240adb18901f904f557`, official script directory `SplatoonScripts/Duties/Dawntrail/The Futures Rewritten/`. Scripts are secondary client-observation implementations, not native packet captures. Full-tooling scripts were not imported.

- `P5 Paradise Regained.cs` reacts to cast 40319, wing casts 40233/40313, tower success 40320 and MapEffects with position 51/52/53 and `data1=1,data2=2` (packet flags 0x00020001). Its tower coordinates are (93.93782,96.5), (106.0622,96.5), (100,107), corroborating the simulation's origin-relative (-6.0622,-3.5), (+6.0622,-3.5), (0,+7). The first observed tower defines its relative assignment frame.
- `P5 Fulgent Blade Dodge.cs` identifies helpers with NameId 13561 casting 40118/40307 and starts from cast 40306. It contains CW/CCW dodge presets; those presets are strategy aids and do not prove every native line origin or snapshot time.
- `P4 Crystallize Time.cs` identifies hourglass base 17837, tracks red/blue statuses 3263/3264, delayed Return 4208 and Return 2452, and derives Tidal Light directions from cast 40251. This corroborates actor/status IDs and direction reads, but its runtime thresholds are not treated as measured phase-relative timings.

## Consequence for confidence

The implementation may label the CT entry bar duration as observed **with the wipe-context qualification**. Full CT ordering and P5 mechanics remain supported by game sheets, guide strategy, imported simulator code and the above client-tool implementations. P5 rendered arena, native effect activation and exact action timing require fresh real-client checks or a clean native recording. No log was fabricated, no raw capture was added to the repository, and absence of a capture was not replaced with an invented observation.
