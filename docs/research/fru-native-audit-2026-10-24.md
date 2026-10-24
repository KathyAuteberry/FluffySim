# FRU native evidence audit — October 24–25, 2026 JST

## Outcome and limits

Tonight's archive substantially improves coverage: four Crystallize Time sequences and one P5 entry are present. The compared cast/map schedules are close to the current implementation. No production timing was changed. Receive-time offsets of tens of milliseconds, and the single P5 sample, do not justify adjusting snapshots or a global phase offset.

This is a receive-timeline audit, not a rendered-video review. Cast packets and MapEffects establish client inputs; they do not establish the first rendered frame, exact server position sampling, target-lock time, animation onset or safe movement deadline. Existing comments were cross-checked against executable schedules rather than accepted as evidence. No standalone video was found in the specified capture directories; no ACT database or independent server clock was available. Binary `Replaye/*.log` recordings are available but are not ACT text and were not decoded with the ACT parser.

## Sources and exact coverage

Primary text input: `Logy/Network_30304_20261024.log`, territory 1238. Dates below are embedded timestamps with +09:00 offsets, not filesystem modification times. Pull numbers are the FRU-only `tools/parser.py` in-game-combat segmentation, not ACT encounter ordinals. The long natural encounter progression, wipe/reset transitions, matched Duty Recorder sessions and player action streams support retail-duty provenance; territory alone would not do so. Playback cannot be categorically excluded by text records alone.

| Pull | Start JST | Combat duration (s) | Deepest audited anchor |
|---|---|---:|---|
| 1 | Oct 24 20:34:24.135 | 62.823 | P1 |
| 2 | Oct 24 20:36:24.654 | 586.808 | P3 Ultimate Relativity |
| 3 | Oct 24 20:46:42.940 | 587.224 | P3 Ultimate Relativity |
| 4 | Oct 24 20:57:06.751 | 207.910 | P2 Diamond Dust |
| 5 | Oct 24 21:01:21.129 | 602.129 | P3 Ultimate Relativity |
| 6 | Oct 25 00:25:31.697 | 153.006 | P1 |
| 7 | Oct 25 00:28:48.979 | 484.631 | P3 Ultimate Relativity |
| 8 | Oct 25 00:37:47.116 | 922.272 | P5 Paradise / Polarizing cast |
| 9 | Oct 25 00:54:12.924 | 785.993 | P4 Crystallize Time |
| 10 | Oct 25 01:28:00.632 | 765.727 | P4 Crystallize Time |
| 11 | Oct 25 01:41:29.561 | 115.885 | P1 |
| 12 | Oct 25 01:43:55.759 | 754.318 | P4 Crystallize Time |

Counts: nine Diamond Dust sequences, eight Light Rampant starts, eight Ultimate Relativity starts (seven reach Shell Crusher), four Darklit starts, four simultaneous Oracle/Usurper CT starts. P1 and Apocalypse events exist, but their detailed schedules were not independently validated by this focused audit.

Read-only format-5 inspection (`tools/replay_inspector.py`) of the three matching native files:

| File suffix (JST) | Header start JST | Duration ms | Segments | Chapters |
|---|---|---:|---:|---:|
| `2026.10.24 21.11.48.dat` | Oct 24 20:33:30 | 2297608 | 215972 | 11 |
| `2026.10.25 01.07.36.dat` | Oct 25 00:23:54 | 2620877 | 224381 | 10 |
| `2026.10.25 01.57.34.dat` | Oct 25 01:27:13 | 1819749 | 172009 | 7 |

All report content ID 1006, build 14155597, valid segment/chapter boundaries and zero backwards segment timestamps. The filenames describe recording end, not recording start. ActorCast payloads were decoded only after validating the build-specific schema described below; other opcode payloads remain opaque. Native replay existence is corroboration of coverage, not a second independent timing sample. Older captures, October 23 overnight sessions, and `FRU-Roost` simulation recordings were excluded. Original inputs were never modified or committed; no actor IDs, player names or raw packets are included here.

## Native ActorCast presentation fields

A second pass decoded opcode 127 (`0x007F`) only for build 14155597, payload length 32. The established DSR mapping was independently checked in FRU: unsigned action ID at offset 0 equals the duplicate uint32 at offset 4; ActionType byte 2 is 1; float32 remaining cast at offset 8 agrees with tonight's ACT cast records. Byte 3 is the omen-delay field in tenths of a second, matching the production `ActorCastPacket` layout. No target/actor identity was exported. The correlated replay and ACT events are the same pulls, not additional samples.

| Native cast | Packet count across three files | Native cast seconds | Native omen byte / seconds | Current simulation omen seconds | Difference ms |
|---|---:|---:|---:|---:|---:|
| Diamond Dust 40197 | 9 | 4.700 | 0 / 0.0 | 0.0 | 0 |
| Light Rampant 40212 | 8 | 4.700 | 0 / 0.0 | 0.0 | 0 |
| Ultimate Relativity 40266 | 8 | 9.700 | 0 / 0.0 | 0.0 | 0 |
| Sinbound Meltdown 40291 | 62 | 3.700 | 0 / 0.0 | 0.0 | 0 |
| Darklit 40239 | 4 | 4.700 | 0 / 0.0 | 0.0 | 0 |
| Darklit Path of Light 40187 | 4 | 7.700 | 0 / 0.0 | 0.0 | 0 |
| Crystallize Time 40240 / 40298 | 4 + 4 | 9.700 | 0 / 0.0 | 0.0 | 0 |
| CT Maelstrom 40299 | 24 | 1.200 | 0 / 0.0 | 0.0 | 0 |
| CT Tidal Light 40251 / 40252 | 8 + 8 | 2.700 | 0 / 0.0 | 0.0 | 0 |
| Fulgent Blade 40306 | 1 | 5.700 | 0 / 0.0 | 0.0 | 0 |
| First Path of Darkness 40118 | 6 | 6.700 | 50 / 5.0 | 5.0 | 0 |
| First Path of Light 40307 | 6 | 6.700 | 50 / 5.0 | 5.0 | 0 |
| P5 Akh Morn 40310 | 1 | 7.700 | 0 / 0.0 | 0.0 | 0 |
| Paradise Regained 40319 | 1 | 3.700 | 0 / 0.0 | 0.0 | 0 |
| Wings Dark and Light 40313 | 1 | 6.600 | 0 / 0.0 | 0.0 | 0 |
| Polarizing Strikes 40316 | 1 | 6.200 | 0 / 0.0 | 0.0 | 0 |

Confidence is high for the identified packet fields, moderate for generalizing the single P5 pull. In particular the **5.0-second first-strip omen delay is directly corroborated in all twelve native strip packets** and should be retained. Zero delay is a field value, not proof that an action has a visible ground omen. Rendered onset still depends on resources and client processing.

The P5 recording contains Fulgent at segment time 1688505 ms (data offset 17721832) and Paradise at 1725618 ms (offset 18189716): separation 37113 ms, compared with ACT's 37098 ms and the simulation's 37150 ms. The 15 ms disagreement between correlated local capture clocks illustrates why ACT receive jitter must not be promoted to a mechanic correction.

## Normalized timing comparison

All times are seconds relative to the named cast-start receive anchor. Simulation times subtract that scenario's own anchor (Mirror Image 3.0; Light Rampant/UR 3.0; Darklit 2.1; CT 1.0; Fulgent 1.5; Paradise 1.3). Difference is **simulation minus received native event**, in milliseconds. Ranges represent observed pulls, not a statistical confidence interval. H means high confidence in the recorded event identification; M means limited sample or semantic mapping. Neither implies high confidence in a server snapshot or rendered frame.

| Mechanic / anchor | Event | Native receive range | Simulation | Difference ms | n / confidence |
|---|---|---:|---:|---:|---|
| P2 Mirror Image | Diamond Dust cast 40197 | 5.124–5.142 | 5.100 | -42…-24 | 9 / H |
| P2 Mirror Image | First Icicle cast 40198 | 12.730–12.802 | 12.780 | -22…+50 | 9 / H |
| Light Rampant | Outer tower MapEffect 09 show | 8.038–8.075 | 8.030 | -45…-8 | 8 / H |
| Light Rampant | Outer Bright Hunger effect 40213 | 16.144–16.189 | 16.160 | -29…+16 | 8 / M |
| Light Rampant | First Burst cast 40219 | 19.321–19.363 | 19.330 | -33…+9 | 8 / H |
| Light Rampant | Second Burst cast | 22.350–22.405 | 22.360 | -45…+10 | 8 / H |
| Light Rampant | House of Light cast 40189 | 37.101–37.167 | 37.120 | -47…+19 | 8 / H |
| Ultimate Relativity | Dark Blizzard-wait status 2462 application | 10.805–10.843 | 10.830 | -13…+25 | 8 / M |
| Ultimate Relativity | Speed cast 40293 | 14.107–14.145 | 14.130 | -15…+23 | 8 / H |
| Ultimate Relativity | First Meltdown cast 40291 | 22.930–22.986 | 22.970 | -16…+40 | 8 / H |
| Ultimate Relativity | Second Meltdown cast | 32.926–33.003 | 33.030 | +27…+104 | 8 / H |
| Ultimate Relativity | Third Meltdown cast | 43.940–43.996 | 44.020 | +24…+80 | 7 / H |
| Ultimate Relativity | Shell Crusher cast 40286 | 54.715–54.831 | 54.830 | -1…+115 | 7 / H |
| Darklit | Water status 2461 application | 4.963–4.979 | 4.983 | +4…+20 | 4 / M |
| Darklit | Tower MapEffect 2A show | 8.045–8.050 | 8.044 | -6…-1 | 4 / H |
| Darklit | Path of Light cast 40187 | 8.137–8.138 | 8.133 | -5…-4 | 4 / H |
| Darklit | Tower Bright Hunger effect 40213 | 16.144–16.149 | 16.149 | 0…+5 | 4 / M |
| Darklit | Somber Dance cast 40283 | 22.278–22.363 | 22.292 | -71…+14 | 4 / H |
| Crystallize Time | First Maelstrom cast 40299 | 20.452–20.507 | 20.500 | -7…+48 | 4 / H |
| Crystallize Time | First Tidal cast 40251 | 28.356–28.381 | 28.300 | -81…-56 | 4 / H |
| Crystallize Time | Second Tidal cast | 34.724–34.771 | 34.700 | -71…-24 | 4 / H |
| Crystallize Time | Quietus effect 40281 | 41.879–41.904 | 41.900 | -4…+21 | 4 / M |
| Fulgent Blade | First strip cast 40118/40307 | 10.097 | 10.050 | -47 | 1 / M |
| Fulgent Blade | Second strip cast | 14.123 | 14.100 | -23 | 1 / M |
| Fulgent Blade | Third strip cast | 18.102 | 18.050 | -52 | 1 / M |
| Fulgent Blade | Akh Morn cast 40310 | 24.807 | 24.850 | +43 | 1 / M |
| Fulgent Blade | Akh Morn party effect 40311/40312 | 32.852 | 32.950 | +98 | 1 / M |
| Fulgent Blade | Paradise Regained cast 40319 (continuous) | 37.098 | 37.150 | +52 | 1 / M |
| Paradise Regained | First tower show (slot 35 hex) | 4.827 | 4.700 | -127 | 1 / M |
| Paradise Regained | Second tower show (slot 33 hex) | 8.312 | 8.200 | -112 | 1 / M |
| Paradise Regained | Third tower show (slot 34 hex) | 11.797 | 11.700 | -97 | 1 / M |
| Paradise Regained | Wings Dark and Light cast 40313 | 7.149 | 7.000 | -149 | 1 / M |
| Paradise Regained | First tower effect 40320 | 14.391 | 14.400 | +9 | 1 / M |
| Paradise Regained | Second tower effect | 17.920 | 17.900 | -20 | 1 / M |
| Paradise Regained | Third tower effect | 21.405 | 21.400 | -5 | 1 / M |
| Paradise Regained | Polarizing Strikes cast 40316 | 29.176 | 29.050 | -126 | 1 / M |

Cast durations directly received: Diamond Dust/Light Rampant/Darklit 4.700, UR/CT 9.700, Fulgent 5.700, first strips 6.700, P5 Akh Morn 7.700, Paradise 3.700 and Wings 6.600 seconds. These are packet remaining-cast fields; do not substitute them mechanically for Action-sheet total cast times. A typical effect arrives around 0.26–0.30 seconds after that field expires. This includes packet/presentation conventions and transport; it is not proof that every snapshot is late by that amount.

## P5 progression and transition interpretation

Pull 8 anchors: Darklit 00:48:23.438; CT 00:49:19.983; Fulgent 00:52:03.260; Paradise 00:52:40.358 JST. CT therefore starts 56.545 seconds after Darklit, and Fulgent starts 163.277 seconds after CT. These include inter-mechanic content and phase presentation; isolated scenario origins are intentionally different. They are not a reason to globally offset the continuous wrapper.

The group completed the initial Fulgent sequence and all three Paradise tower effects. `Unmitigated Explosion` 40321 appears at Paradise +19.974; Polarizing Strikes starts at +29.176. No later P5 mechanic cast, Pandora's Box, repeated cycle or enrage is established by this recording. The 90-second Fulgent window reaches the Polarizing cast but does not prove its full resolution. Tower appearance intervals are 3.485/3.485 seconds; effect intervals 3.529/3.485. The implementation uses 3.5 seconds. Single-sample appearance differences of 97–149 ms merit a fresh rendered check, not a guessed correction.

## What remains unvalidated

- Native omen first-visible frames, VFX onset, clone/model hiding and actor despawn: packet presence is insufficient. Play the relevant native .dat in the client and compare screen recordings frame by frame.
- Target selection/lock and position/facing snapshots: the log lacks the rejected counterfactual positions needed to infer these. Effect arrival is not a snapshot timestamp.
- Debuff expiry/removal and CT Return/Rewind exact safe windows: status application anchors were checked only for the rows above; no general expiry conclusion is made.
- Continuous phase-transition rendering/downtime and post-Polarizing P5: not validated. Four CT entries are materially stronger coverage than the older interrupted-wipe CT pairs documented in the previous inventory, but do not validate all CT presentation.
- The roughly 100 ms late UR second/third Meltdown and Shell cast schedules, and roughly 100–150 ms early Paradise presentation, are retained pending more precise native comparison. Receive jitter, per-action scheduling and the differing event semantics preclude declaring a bug from these differences alone.

## Reproduction and validation

`python tools/fru_native_audit.py <read-only ACT log> --output <new temporary JSON path>` produces sanitized pull/anchor/cast/map metadata, with exclusive output creation. Add repeatable `--native-replay <.dat>` arguments for the build-validated ActorCast summary. It requires no external dependencies. It exported 12 pulls and 44 anchor windows from tonight's text input. Effect/status rows were separately checked with the existing read-only parser; repeated multi-target effects were not counted as independent pulls or server snapshots. Millisecond receive precision does not imply millisecond measurement accuracy. Relative anchors remove constant recording-clock offsets; no cross-format clock equivalence was assumed.

Only this report and the read-only extractor changed. No game, SDK, installed output, release metadata, capture or encounter timing changed. Native client acceptance remains required for the unvalidated presentation items above.
