# FRU clear capture audit — 2026-10-31

Baseline: FluffySim `a65461` (`master@origin` before this work). All times and dates below are JST. Original logs and native Duty Recorder files were read only; no captures, player names, actor IDs or packet bodies are included in this report.

## Coverage and interpretation

The seven `Network_30304` archive files dated October 24–30 contain 105 territory-1238 combat intervals according to `tools/parser.py`, including one completion director event. These are recording intervals, not an authenticated count of live PF attempts: native playback can appear in ACT archives too. The files span midnight, so filename dates are not encounter dates.

The final evening's October 30 file contains nine intervals, eight without completion and one clear. The clear starts October 30 at 23:56:36.215 and its combat interval ends October 31 at 00:15:18.469, duration **1122.254 seconds (18:42.254)**. The completion director event precedes combat-end by 54 ms.

Three native recordings from that evening were inspected with the read-only format-5 parser:

| Recording filename end time | Segments | Chapters | Coverage relevant to this audit |
| --- | ---: | ---: | --- |
| October 30 22:58:21 | 104113 | 9 | Through P3; no P5 cast |
| October 30 23:34:00 | 112782 | 5 | One CT entry pair; no P5 cast |
| October 31 00:15:43 | 188690 | 7 | Two CT entry pairs; complete clear's P5 through enrage cast |

All three report content ID 1006, build 14155597, valid segment/chapter boundaries, and zero backwards segment timestamps. The clear recording contains three Fulgent casts, three Akh Morn casts, two Paradise casts and both Polarizing sequences. Native and ACT records describe the same encounters and are not independent samples.

ActorCast decoding uses the existing build-specific opcode-127, 32-byte layout: action at offset 0 equals its duplicate at offset 4, ActionType at byte 2 is 1, and remaining cast duration is the float at offset 8. Other native opcode payloads were not decoded. ACT action effects, status applications/removals and MapEffects provide the additional event timings below. Packet receive times do not establish first-visible frames or exact server snapshots.

## Confirmed defect: Fulgent cleanup erased late exalines

The first Paradise cast starts **37.095 seconds after Fulgent** in ACT (37.094 in the native recording), while a Path of Light effect still occurs at **+37.453**. The second Fulgent has late strip effects through +39.526. Starting the next boss mechanic does not imply that every prior strip is finished.

Before this change, `FruPandoraSequenceScenario.Begin` called `ClearStage`, despawning all non-retained children of the previous stage. The first transition runs at wrapper time 37.35; Fulgent's cast is at wrapper time 1.5, so cleanup happened at **Fulgent +35.85**. It therefore removed helpers before their already-scheduled late strip events could execute. The isolated Fulgent drill retained them.

A headless comparison with seeds 0 and 2 reproduced **48 rolling-strip effects in the isolated drill versus 44 in the continuous drill**, with four late effects missing. The paired tests also allow two frames for the wrapper's nested start rather than demanding identical float timestamps.

The sequence now retains the outgoing Fulgent children until its latest possible strip impact has finished. The deadline is derived from the last scheduled group/hit and the rolling action's damage delay, rather than a separate guessed duration. Other stage cleanup remains immediate. Tests check that the remaining effects match the isolated drill and that the outgoing helpers are gone by wrapper time 42. The first Paradise begins on its existing schedule.

This fixes native effect delivery and collision checks that were missing during the transition. In-client rendering of the overlap still needs checking.

## P5 timing comparison

Origin is the first Fulgent ActorCast receive time, October 31 00:10:52.918. Simulation times subtract its 1.5-second lead-in. Difference is simulation minus ACT receive time; these are event comparisons, not claimed snapshot corrections.

| Boss cast | ACT relative seconds | Native relative seconds | Simulation | Difference |
| --- | ---: | ---: | ---: | ---: |
| First Paradise | 37.095 | 37.094 | 37.150 | +0.055 |
| First Polarizing | 66.111 | 66.107 | 66.200 | +0.089 |
| Pandora's Box | 94.813 | 94.807 | 94.950 | +0.137 |
| Second Fulgent | 112.976 | 112.977 | 113.150 | +0.174 |
| Second Paradise | 154.200 | 154.198 | 154.500 | +0.300 |
| Second Polarizing | 177.926 | 177.929 | 178.200 | +0.274 |
| Third Fulgent | 203.515 | 203.530 | 203.750 | +0.235 |
| Paradise Lost, both casts | 244.769 | 244.766 | 245.100 | +0.331 |

The shorter second Paradise-to-Polarizing interval is confirmed: **23.726 seconds**, versus **29.016** on the first cycle. The wrapper already applies a -5.35-second offset on the second cycle; it must not use two identical Paradise timelines.

Across the seven archive files, 19 Fulgent entries were found. Surviving first-cycle Fulgent-to-Paradise gaps are 37.095–37.245 seconds (nine entries), while second-cycle gaps are 41.224–41.349 (five entries). These reinforce the cycle distinction but are qualified recording counts, not independent authenticated live pulls.

The current sequence stays within about one third of a second by enrage on the clear. No broad retiming was made from this single complete recording. It would change practice cadence for much less benefit than fixing erased hazards.

The lethal enrage helper's received cast is 21.2 seconds, alongside a visual cast of 11.7. The simulation forced a 21.5-second helper bar, making it 0.3 seconds longer even before the wrapper's accumulated start difference. The pinned Action sheet has `Cast100ms=215` for 40328 and `120` for 40327: those are total times, from which the existing handler subtracts its 0.3-second effect offset to produce the received bar durations. The helper override bypassed that conversion. A regression reproduced the incorrect 21.5-second bar; removing the override restores a **21.2-second bar** and makes the simulated death occur at the already-declared `EnrageHitAt=268.1`, instead of 268.4.

The clear interrupts the enrage: it does **not** measure the lethal effect or damage delay. The retained 0.3-second damage delay remains a practice assumption. Completion is received 0.472 seconds before the helper's nominal received bar would expire; this is not an exact server-side DPS margin.

## Polarizing, towers and tank practice

On the first Paradise, line casts occur at +35.586, +40.193, +44.800 and +49.407. On the second they occur at +30.303, +34.914, +39.518 and +44.125. Intervals are **4.604–4.611 seconds**, consistent with the drill's 4.6-second spacing.

First line effects arrive about 0.491–0.493 seconds after the helper casts. Echo effects arrive **2.098–2.105 seconds after the initial effects**. The drill's `EchoAt = LinesAt + 2.6` is measured from the initial helper cast, not its effect; reducing it to 2.1 would introduce an error. The current half-second helper cast/effect interval accounts for that distinction.

Each sequence has four paired Light/Dark resistance applications with a logged duration of **16.00 seconds**. Observed removal is roughly 15.97–15.99 seconds after application. The drill's 15.95-second status is close; no duration change is justified by these receive timestamps. This supports keeping the bait-color history active across all four rounds. The log alone does not prove nearest-player selection under counterfactual positions.

First-cycle tower show times relative to Paradise are 4.829, 8.317 and 11.804; second-cycle times are 4.822, 8.307 and 11.792. Current simulation times are 4.7, 8.2 and 11.7 after subtracting the 1.3-second cast anchor. Appearance remains around 0.09–0.13 seconds early. Tower effects, by contrast, arrive at 14.396/17.926/21.411 and 14.383/17.914/21.401, close to simulated 14.4/17.9/21.4. Do not shift the entire tower mechanic to address appearance alone.

Both wing orders occur on the clear: Dark→Light first, Light→Dark second. Cleaves arrive about +14.30 and +18.10 after Paradise; targeted busters about +15.10 and +18.90. These establish ordering and receive-time spacing, not a universal invulnerability button time. The simulator still uses scheduled tank seats/facing rather than a full enmity/swap model.

Tank LB3 protection (status 864) reaches all eight recipients over 0.936 seconds and is logged as 8.00 seconds per recipient. Every recipient is protected when the Box effect arrives, 11.961 seconds after its cast starts. Protection reaches the last recipient 2.855 seconds before that effect. The existing recipient-by-recipient active-status check remains appropriate; spending LB gauge alone is insufficient. A zero-latency simultaneous application would erase a real delivery detail, but this one capture does not justify a general latency model.

## Earlier mechanics and practice priorities

The final evening's cast comparisons remain consistent with the earlier audit:

| Anchor → event | Receive range, seconds | Simulation | Entries |
| --- | ---: | ---: | ---: |
| Mirror Image → Diamond Dust | 5.125–5.163 | 5.100 | 6 |
| Light Rampant → first Burst | 19.338–19.376 | 19.330 | 5 |
| Light Rampant → second Burst | 22.365–22.392 | 22.360 | 5 |
| Light Rampant → House of Light | 37.108–37.215 | 37.120 | 5 |
| Ultimate Relativity → Speed | 14.114–14.137 | 14.130 | 4 |
| Ultimate Relativity → first Meltdown wave | 22.939–22.978 | 22.970 | 4 |
| Ultimate Relativity → second Meltdown wave | 32.957–33.005 | 33.030 | 4 |
| Ultimate Relativity → third Meltdown wave | 43.957–44.015 | 44.020 | 4 |
| Ultimate Relativity → Shell Crusher | 54.784–54.817 | 54.830 | 4 |
| Darklit → Path of Light | 8.135–8.149 | 8.133 | 3 |
| Darklit → Somber Dance | 22.315–22.350 | 22.292 | 3 |
| Crystallize Time → first Maelstrom wave | 20.455–20.505 | 20.500 | 3 |
| Crystallize Time → first Tidal cast | 28.350–28.367 | 28.300 | 3 |
| Crystallize Time → second Tidal cast | 34.720–34.765 | 34.700 | 3 |

Multiple helper casts within a wave are grouped; eight helpers are not eight independent mechanic samples. These comparisons do not validate every branch, status expiry or movement snapshot. They do not reveal a new large timing error in the reviewed earlier drills.

Of the eight final-evening non-clear intervals, two terminate during CT, one after the P3 Apocalypse/Darkest Dance sequence, and five earlier. The CT failures include paired Dark Aero effects before the first player deaths on one interval. This supports practicing the CT wind/Return/Tidal transition as a complete sequence, rather than only its opening assignments. It does not establish which player caused a wipe.

The clear has two player death records in P5, after the first and second Akh Morn stacks. Each victim's last recorded hostile ability is left-stack Akh Morn, about 2.46–2.50 seconds before the death record. That is a useful review point for stack recovery/mitigation, but not enough to distinguish positioning, prior damage, mitigation, healing or a later unlogged cause. No individual fault is assigned. The simulator's four-person geometry/vulnerability model is not a full HP/healing model, so it cannot reproduce all such failures.

## Validation and remaining work

- The new late-exaline regression failed before the fix (48 isolated versus 44 continuous effects for both selected seeds) and passes afterward. Cleanup is checked at wrapper time 42.
- The enrage regression failed with the explicit 21.5-second bar and passes using the sheet-derived 21.2-second bar; terminal deaths are checked against `EnrageHitAt` within three frames.
- Debug P5 suites: **276 tests passed**, including the new regressions, full-sequence terminal behavior, Akh Morn, tower/wing combinations, Polarizing and LB failure cases.
- Catalog P5 runs: **five tests passed with 32 random seeds each**, 160 scenario runs, including the continuous sequence with the remaining hazards active.
- Debug builds used `/home/kath/.xlcore/dalamud/Hooks/dev/` and `EnableSourceControlManagerQueries=false` because the installed MSBuild Git task rejects this repository's `relativeworktrees` extension. Test-host IPC required running outside the restricted sandbox. Existing NuGet vulnerability-feed warnings were retained. Release and installed outputs were untouched.

Cast/map reproduction: `python tools/fru_native_audit.py <October-30-network-log> --native-replay <clear.dat> --native-replay <23:34.dat> --native-replay <22:58.dat> --output <new-temporary-json>`. The current extractor yields nine pulls and 32 anchor windows. Additional effect/status comparisons use `tools/parser.py`'s record reader; native structural validation uses `Replay.inspect()` from `tools/replay_inspector.py`.

Next client checks: watch the first Fulgent→Paradise overlap after this fix; compare native tower first-visible frames; confirm the corrected helper enrage bar renders as expected; inspect CT Return/Tidal safe windows. None of these visual/snapshot claims are proved by successful headless runs. Release preparation, publication and bookmark movement are outside this audit.
