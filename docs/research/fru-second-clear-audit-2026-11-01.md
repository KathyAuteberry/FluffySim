# FRU second-clear comparison — 2026-11-01

Baseline: published FluffySim 3.2.2.0, commit `6be1b445804e8ddb5ce724e6de8a2e8b6f7f0b65`. Comparison: [first-clear audit](fru-clear-audit-2026-10-31.md). Dates and wall-clock times are JST. Source captures were read only; this report contains no player names, actor IDs or packet bodies.

## Clear and finish

The October 31 network archive contains 12 FRU combat intervals and one completion. The clear starts November 1 at **00:53:50.664** and combat ends at **01:12:25.760**, duration **18:35.096**. The completion director event arrives at 01:12:25.645, 115 ms before combat-end. Archive interval counts are not authenticated counts of live PF pulls: playback can also enter the log stream.

| Measurement | First clear, October 31 | Second clear, November 1 |
| --- | ---: | ---: |
| Combat interval | 18:42.254 | **18:35.096** |
| Player death records during the clear | 2, both in P5 | **0** |
| First Fulgent cast after combat start | 856.703 s | 857.005 s |
| Completion after first Fulgent | 265.497 s | **257.976 s** |
| Completion after enrage helper cast begins | 20.728 s | **13.001 s** |
| Nominal time left on received 21.2 s helper bar | 0.472 s | **8.199 s** |
| DPS LB3 before Ultimate Relativity | Dragonsong Dive | Chimatsuri |
| DPS LB3 in P5 | Dragonsong Dive | **No cast/effect record** |

The combat interval is **7.158 seconds shorter**; comparing completion-director boundaries instead gives 7.219 seconds. P5 completion advances by 7.521 seconds despite entering P5 0.302 seconds later. The earlier kill therefore does not come from materially faster earlier phase transitions or a skipped P5 mechanic. Both clears reach all three Fulgent/Akh Morn sequences, both Paradise/Polarizing cycles, Box and the enrage cast.

Chimatsuri starts at combat +444.280 and its effect arrives at +448.247; the required Dark Force effect arrives at +960.325. No later action categorized as Limit Break in the pinned Action sheet appears before completion. That establishes the absence of a recorded final DPS LB, not the state of the gauge or why nobody used it. The unused-LB finish and deathless run are consistent with stronger sustained damage, but these observations do not isolate composition, gear, buffs or individual performance.

The bar margins above compare received cast duration with a received completion event. They are not exact server-side DPS margins, and neither clear measures the interrupted lethal enrage effect.

## Native recordings

Four format-5 Duty Recorder files from the evening were checked with `Replay.inspect()` and the existing build-validated ActorCast decoder:

| Recording filename end time | Segments | Chapters | CT entry casts 40240 | P5 coverage |
| --- | ---: | ---: | ---: | --- |
| October 31 22:10:31 | 84304 | 7 | 0 | None |
| October 31 23:06:06 | 254229 | 11 | 2 | None |
| October 31 23:55:12 | 167946 | 5 | 2 | None |
| November 1 01:15:35 | 173574 | 5 | 1 | Complete clear through interrupted enrage |

All report content ID 1006 and build 14155597, valid segment/chapter boundaries and zero backwards timestamps. Native ActorCast decoding retains the opcode-127, 32-byte checks: action ID at offset 0 equals the duplicate at offset 4, ActionType byte 2 is 1, and remaining cast time is at offset 8. ACT and native records of this clear are correlated observations, not additional independent pulls.

## P5 against 3.2.2.0

Origin: first Fulgent cast received at 01:08:07.669. Simulation times subtract the isolated Fulgent's 1.5-second lead-in. Differences are simulation minus ACT receive time.

| Boss cast | ACT relative s | Native relative s | Simulation s | Difference s |
| --- | ---: | ---: | ---: | ---: |
| First Paradise | 37.113 | 37.117 | 37.150 | +0.037 |
| First Polarizing | 66.226 | 66.234 | 66.200 | -0.026 |
| Pandora's Box | 94.889 | 94.896 | 94.950 | +0.061 |
| Second Fulgent | 113.091 | 113.084 | 113.150 | +0.059 |
| Second Paradise | 154.278 | 154.278 | 154.500 | +0.222 |
| Second Polarizing | 178.124 | 178.119 | 178.200 | +0.076 |
| Third Fulgent | 203.682 | 203.691 | 203.750 | +0.068 |
| Both Paradise Lost casts | 244.975 | 244.961 | 245.100 | +0.125 |

The first-clear comparison ended with a +0.331-second enrage-start difference; this one gives +0.125. This variation argues against applying a global correction from one pull. The shorter second Paradise-to-Polarizing gap repeats: 23.846 seconds versus 29.113 in the first cycle, consistent with retaining the wrapper's second-cycle offset.

The native enrage bars are again **21.2 seconds** for 40328 and **11.7 seconds** for 40327, corroborating 3.2.2.0's removal of the 21.5-second helper-bar override. All 36 initial exaline casts retain a 6.7-second remaining bar and byte-3 omen delay 50 (5.0 seconds). This validates the recorded presentation fields, not first-visible frames.

The first Fulgent's final recorded rolling-strip effect occurs at **+37.425**, **0.312 seconds after Paradise begins**. This independently supports retaining outgoing strip helpers during the transition. It is evidence for the 3.2.2.0 cleanup fix, not a native rendering test of that plugin build. The second and third Fulgent tails end at +39.491 and +35.395 respectively; the tail length varies with the strip pattern, so the implementation should retain its last-possible-impact deadline rather than copy one observed tail duration.

The new clear has **Light→Dark** wings on its first Paradise and **Dark→Light** on its second, reversing the first clear's cycle assignments. Its first tower is slot 51 in both cycles, whereas the first clear starts with slot 53. Across the two clears this adds different wing/cycle and tower-orientation coverage; it does not establish a rule relating the random choices across cycles.

New tower shows occur at +4.819, +8.300/8.301 and +11.783/11.784 after Paradise. Simulation shows them at +4.7, +8.2 and +11.7. The previously noted roughly 0.1-second appearance difference persists, while the tower effect times remain close to the simulation. No broad tower retiming is warranted without a rendered comparison.

## New CT status evidence

The clear's CT cast begins at combat +694.229, nearly identical to +694.238 on the first clear. The following are status transitions, not inferred first-visible marker frames or position snapshots. Simulation values subtract CT's 1.0-second cast lead-in.

| Status event | First clear, relative s | Second clear, relative s | Simulation, relative s |
| --- | ---: | ---: | ---: |
| Return waiting 4208 applies, 33 s | 10.806 | 10.811 | 10.800 |
| Waiting removed; Return 2452 applies, 7 s | 43.762 | 43.774 | 43.800 |
| Return removed; stun 4163 applies, 9 s | 50.782 | 50.784 | 50.800 |
| Stun removed | 59.796 | 59.799 | 59.800 |

Each row represents the corresponding eight-player transition within a clear, not eight independent timeline samples. The new clear agrees with the existing schedule within **26 ms** on these four events. In particular, it supports the nominal **seven-second Return-to-stun interval and nine-second stun**. It does not validate the exact position-recording instant, rewind movement, fragment collision or knockback snapshot. Keep those distinctions when using this evidence to assess `SaveReturns` and `Rewind`.

Five intervals that evening reach CT: four end without completion and the fifth is the clear. Three of the non-clears reach post-CT Morn Afah casts or later; one ends during the Wings section. Earlier deaths occur on some of those intervals. This gives more reason to review late CT, its mitigation/formation and P4's finish than to change the already matching Return/status timers. It does not identify a player or mechanic as the sole cause of a wipe. FluffySim does not model boss HP checks or the full party DPS/healing system.

## LB protection and retained limits

Dark Force protection again reaches all eight recipients over **0.936 seconds**, with an 8.00-second logged duration for each recipient. On this clear the last application precedes the Box effect by **1.294 seconds**, versus 2.855 seconds on the first clear. The later use still protects everyone when Box arrives; this further supports checking active protection per recipient rather than merely checking that the gauge was spent. It does not provide a universal button deadline or a general network-latency model.

No new combat-code change is justified by this comparison. The useful additions are a second complete native P5 sample, a deathless finish without a final DPS LB3, independent evidence for both 3.2.2 fixes, and explicit CT Return/stun status timings. Exact snapshot rules, target-selection counterfactuals, rendered onset and the final lethal enrage impact remain unverified.

## Reproduction

`python tools/fru_native_audit.py <October-31-network-log> --native-replay <November-1-01:15:35.dat> --native-replay <October-31-23:55:12.dat> --native-replay <October-31-23:06:06.dat> --native-replay <October-31-22:10:31.dat> --output <new-temporary-json>` yields **12 pulls and 52 anchor windows**. Use `Replay.inspect()` for full chapter/segment validation.

Clear comparison uses `tools/parser.py`'s `find_pulls(..., 1238)` completion classification and bounds all subsequent reads to the selected clear. Cast/action IDs are parsed from the hexadecimal action field of types 20/21/22; player death records require type 25 and a player-shaped target ID. Category-9 actions come from the pinned Action CSV. Status fields come from types 26/30 and completion from director code `0x40000003`. Multi-target effect timestamps are coalesced for timeline reporting and are not counted as independent attacks. Original inputs and installed outputs were unchanged; no new version or release was prepared.

Validation: all 17 `test_replay_inspector.py` tests passed; comparison arithmetic and local document links were checked. This change updates documentation only, so no plugin build or runtime regression suite was required.
