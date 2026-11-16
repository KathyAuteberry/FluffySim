# Cast-time override audit (2026-11-16)

Every `CastSecondsOverride` in the custom families (`Scenarios/Dsr`, `Scenarios/Dmu`,
`Scenarios/Fru/FallOfFaith`), checked against the Action sheet and the owner's retail ACT logs.
No upstream family uses the override. TEA has none.

## Engine semantics (`Core/EnemyActions/EnemyActionHandler.Start`)

| Case | Bar | Snapshot (`Resolve`) | ActionEffect (animation, flytext confirmation) |
|---|---|---|---|
| No override, sheet cast `c > 0` | `c − 0.3` | `c − 0.3` | `c` |
| No override, sheet instant | none | 0 | 0 |
| `CastSecondsOverride = x > 0` | `x` | `x` | `x + 0.3` |
| `CastSecondsOverride = 0` | none | 0 | 0.3 |

So `x = c − 0.3` is a no-op, and `0` on a sheet-instant action delays only the ActionEffect by 0.3 s.
Starting a second action on a caster does not cancel the first: a "visual" sheet cast followed by an
override-0 "carrier" of the same action sends two ActionEffects (`Release` always calls
`NativeActionEffect`, even with no effects).

## Sources

- Sheet: pinned datamining `Action.csv` (commit `d9582a62…`), column `Cast100ms`.
- Retail: every log in `Logy/` and the evidence snapshot `FluffySim-evidence/logs/` (snapshot only
  for files no longer in `Logy/`). Inn/Roost segments excluded by territory.
  - DSR (968): `Network_30303_20261016.log` (snapshot), `Network_30304_20261023.log`,
    `Network_30304_20261024.log`. These reach P1, the P1 intermission (Planar Prison/Pure of Heart)
    and P2. **No DSR P3–P7 action appears in any log**, so P4, P5 Wrath, P5 Death-only actions,
    P6 and P7 have no retail evidence. P5 Death actions shared with P2 (Thordan leap, attack, Ancient
    Quaga, Heavy Impact rings, Lightning Storm, Dragon's Gaze/Glory, Holy Chain, Faith Unmoving,
    Heavensflame) are measured from P2/P1, not from P5.
  - DMU (1363): `Network_30305_20261111.log`, `…1112.log`, `…1113.log`.
  - FRU (1238): 27 logs, 236 complete Fall of Faith sequences.
- Measurement: `20` StartsCasting (duration field), `21/22` ability rows coalesced per release
  (same source + action within 0.15 s), `23` cancels. ACT times are receive times (tens of ms jitter).
  Note that ACT's `20` duration is already the bar (sheet − 0.3): every sheet-cast action measured
  shows `20` duration = sheet − 0.3 and the `22` at ≈ sheet (−0.01…−0.05 s).

## Classification

- **(a) REDUNDANT** — override = sheet − 0.3. Removed in this audit.
- **(b) INSTANT-ON-INSTANT** — override 0, sheet 0: only effect is the ActionEffect 0.3 s after the
  snapshot.
- **(c) SUPPRESSED CAST** — override 0, sheet > 0. Subtype **(c-carrier)**: the bar is already shown by
  a separate sheet-driven cast of the same (or a visual) action, and the override-0 action only
  carries the effects at the end of that bar.
- **(d) GENUINE** — the in-game cast differs from the sheet, or a computed remaining time.

Counts: 29 override lines. (a) 3 lines (5 call sites); (b) 18 lines; (c) 3 lines; (d) 1 line;
4 helper lines are mixed (b)+(c-carrier) depending on the action passed.

## Table

Times in sim columns are from the scenario's own timeline. "Retail" columns: `n` = releases or casts,
range across all of them.

### (a) Redundant — removed

| Site (before) | Action | Sheet | Override | Retail | Change |
|---|---|---|---|---|---|
| `Dsr/P1Knights/DsrP1KnightsScenario.cs:231` | 0x62D0 Holiest Hallowing | 4.0 | 3.7 | `20` dur 3.70 (n=14; no release logged, the cast is interrupted) | override removed |
| `Dsr/P4Eyes/DsrP4EyesScenario.cs:497` `CastSelf` ×3 callers | 0x68BE Hatebound | 3.0 | 2.7 | no retail evidence (P4 never reached) | `CastSelf` removed, callers use `Cast(id)` |
| same helper | 0x68C3 Mirage Dive (wind-up) | 3.0 | 2.7 | no retail evidence | same |
| same helper | 0x68BD Steep in Rage (visual) | 6.0 | 5.7 | no retail evidence | same |
| `Dsr/P5Death/DsrP5DeathScenario.cs:120` | 0x63C6 Ancient Quaga | 6.0 | 5.7 | `20` dur 5.70 (n=125), hit 5.95–6.00 (n=88), P2 | override removed |

### (b) Instant-on-instant (all sheet 0)

| Site | Actions | Retail | Recommendation |
|---|---|---|---|
| `Dsr/P5Death/DsrP5DeathScenario.cs:78` | 0x63C4 Thordan leap (unnamed) | no `20`, 530 releases (P2) | remove override (owner decision) |
| `Dsr/P5Wrath/DsrP5WrathScenario.cs:142` `Instant` | 0x63BB attack, 0x6B8A Spiral Pierce, 0x72A2 Skyward Leap, 0x63CB Ascalon's Mercy Revealed, 0x6B91 Liquid Heaven, 0x6B90 Chain Lightning | 0x63BB: no `20`, 353 releases (P2). Others: no retail evidence | remove override (owner decision) |
| `Dsr/P4Eyes/DsrP4EyesScenario.cs:500` `PlayEffect` | 0x68C5 Soul of Friendship, 0x68C6 Soul of Devotion, 0x717A (Eyes drift, unnamed) | no retail evidence | owner decision |
| `Dsr/P7DragonKing/DsrP7DragonKingAi.cs:97` (not in my edit set) | 7533 Provoke (bot) | no `20`, 1858 releases across 29 logs | remove override (owner decision; `*Ai.cs` owned elsewhere) |
| `Dmu/DmuActions.cs:39` `RevoltingRuinSecond` | 0xC4E1 Revolting Ruin III (2nd hit) | no `20`; 136 releases. 8.15–8.54 s (med 8.19) after the 0xC403 `20`; sim schedules it at +8.20 | remove override: sim ActionEffect lands at +8.50 |
| `Dmu/DmuActions.cs:74` | 0xBAA2 Flagrant Fire III (spread) | no `20`; 277 releases | remove override |
| `Dmu/DmuActions.cs:83` | 0xBAA3 Flagrant Fire III (stack) | no `20`; 138 | remove override |
| `Dmu/DmuActions.cs:91` | 0xBAA9 Pulse Wave | no `20`; 326 | remove override |
| `Dmu/DmuActions.cs:99` | 0xBAA8 Wave Cannon | no `20`; 324 | remove override |
| `Dmu/DmuActions.cs:115` | 0xBAA7 Double-trouble Trap (burst) | no `20`; 248 | remove override |
| `Dmu/DmuActions.cs:125` | 0xBAAC Gravitas | no `20`; 439 | remove override |
| `Dmu/DmuActions.cs:133` | 0xBAB0 Vitrophyre | no `20`; 379 | remove override |
| `Dmu/DmuActions.cs:142` (+`IntemperateWill` via `with`) | 0xBAB1 Gravitational Wave, 0xBAB2 Intemperate Will | no `20`; 38 / 55 | remove override |
| `Dmu/DmuActions.cs:153` | 0xBAAF Gravity III | no `20`; 232 | remove override |
| `Dmu/DmuActions.cs:165` | 0xBABA Tele-trouncing (drop) | no `20`; 400 | remove override |
| `Dmu/DmuActions.cs:172` | 0xBAB5 Indulgent Will | no `20`; 96 | remove override |
| `Dmu/DmuActions.cs:179` | 0xBAB6 Idyllic Will | no `20`; 93 | remove override |
| `Dmu/DmuActions.cs:187` `Gaze` | 0xBAB4 Indolent Will, 0xBAB3 Ave Maria | no `20`; 8 / 17 | remove override |

The DMU timeline literals are retail hit times (`docs/encounters/dmu/p1-evidence.md`, e.g. Pulse Wave
34.91, Wave Cannon 42.27, RR III hit 2 at +8.20), so every DMU (b) site snapshots on time but shows
its ActionEffect 0.3 s after retail.

### Mixed helpers: per action

`Dsr/P1Knights/DsrP1KnightsScenario.cs:140` `Instant`:

| Action | Sheet | Class | Retail | Sim |
|---|---|---|---|---|
| 0x62E3 Shockwave, 0x62D1 Holy Shield Bash, 0x62D3 Holy Bladedance, 0x62D7 / 0x63EE Hyperdimensional Slash, 0x62CE Shining Blade, 0x62D5 Execution, 0x62DF Heavensflame, 0x6319 Brightwing | 0 | (b) | no `20` for any; releases 21 / 271 / 1290 / 52 / 13 / 24 / 6 / 48 / 24. Shockwave starts 11.27 s after Pure of Heart's `20` in all 3 pulls; sim 124.11 − 112.84 = 11.27 | ActionEffect +0.3 s |
| 0x62DD Heavensblaze | 5.0 | (c-carrier) | one `20` (4.70) + one hit 4.95–5.00 from the same caster (n=8) | visual cast 19.29, carrier 24.28 (+4.99): snapshot on time, two ActionEffects (+5.00 visual, +5.29 carrier) |
| 0x62DA / 0x62DB Empty/Full Dimension | 5.0 | (c-carrier) | `20` 4.70, hit 4.96–4.99 same caster (n=10 / 1) | visual on Grinnaux 14.26 / 95.21, helper carrier 19.25 / 100.18 (+4.99/+4.97), ActionEffect +5.29 |
| 0x62DC Faith Unmoving | 4.0 | (c-carrier) | `20` 3.70, hit 3.96–4.00 same caster (n=32) | visual 51.86 / 78.02, helper carrier 55.83 / 81.98 (+3.97/+3.96), ActionEffect +4.27 |
| 0x62E4 Pure of Heart | 35.5 | (c-carrier) | `20` 35.20, hit 35.46–35.48 same caster (n=3) | visual 112.84, carrier 148.31 (+35.47) on the same caster: ActionEffects at +35.50 and +35.77 |

`Dsr/P5Death/DsrP5DeathScenario.cs:131` `Instant`:

| Action | Sheet | Class | Retail |
|---|---|---|---|
| 0x63D7–0x63DA Heavy Impact rings, 0x63BB attack, 0x63CD Lightning Storm, 0x63D1 Dragon's Gaze, 0x63D2 Dragon's Glory, 0x62E0 Holy Chain, 0x62DF Heavensflame | 0 | (b) | no `20`; 139–140 per ring, 353, 1120, 59, 59, 4, 48 (P1/P2) |
| 0x6B94 Deathstorm, 0x6B8C Twister | 0 | (b) | no retail evidence |
| 0x62DC Faith Unmoving | 4.0 | (c-carrier) | as above (P1/P2). Sim: visual 33.26, carrier 37.23 (+3.97) on the same knight → two ActionEffects |

`Dsr/P6DoubleDragons/DsrP6DoubleDragonsScenario.cs:207` `Instant` (no retail evidence for any P6 action):

| Action | Sheet | Class | Sim pairing |
|---|---|---|---|
| 0x6D2A / 0x6D22 Wyrmclaw, 0x6D36 / 0x6D37 breaths, 0x6D3B / 0x6D3C, 0x6D39 / 0x6D3A / 0x6D3D, 0x6D44 / 0x6D42 Akh Afah, 0x6D28 / 0x6D25 Hallowed Wings, 0x6D29 Hallowed Plume, 0x6D31 Mortal Vow, 0x6D47 Akh Morn, 0x742B / 0x742C | 0 | (b) | — |
| 0x6D3E / 0x6D3F Cauterize (helper carrier, also `CleanseDives`) | 5.0 | (c-carrier) | visual 49.53 / 74.46 / 149.70, carrier +5.00 / +4.99 / +5.00 |
| 0x6D2C Hot Wing / 0x6D2E Hot Tail (helpers) | 6.5 | (c) | visual is a different action (0x6D2B / 0x6D2D on Nidhogg) at 85.78 / 116.05; helper hits at +6.48 / +6.47 |
| 0x6D46 Akh Morn first hit (helper) | 8.0 | (c-carrier) | visual 0x6D46 on Nidhogg 71.38, first hit 79.36 (+7.98) |

`Dsr/P4Eyes/DsrP4EyesScenario.cs:504` `Hit` (no retail evidence):

| Action | Sheet | Class |
|---|---|---|
| 0x68C5 / 0x68C6 Souls, 0x68BA Resentment, 0x68BF / 0x68C0 Flare Star, 0x68C1 / 0x68C2 Flare Nova, 0x68C4 Mirage Dive | 0 | (b) |
| 0x68BD Steep in Rage | 6.0 | (c-carrier): visual 79.10, carrier 85.05 (+5.95) on the same Left Eye → two ActionEffects |

### (c) Suppressed casts

| Site | Action | Sheet | Retail | Sim | Recommendation |
|---|---|---|---|---|---|
| `Dsr/P4Eyes/DsrP4RescueSequence.cs:137` | 25314 Spear of the Fury | 10.0 | `20` dur 9.70 starts 0.53–0.54 s **before** Pure of Heart's `20`; hit 9.97–9.98 s after its own `20` = +9.44 after Pure of Heart (n=3) | visual cast at +19.9 (0.1 s before Pure of Heart at +20.0), instant impact at +27.5 = **+7.5 after Pure of Heart, 1.94 s before retail**, then the visual's own ActionEffect at +29.9 | owner decision: drop the carrier and resolve on the visual cast, or move the impact to +29.9 |
| `Dsr/P4Eyes/DsrP4RescueSequence.cs:166` | bot heals 190 Physick / 24284 Diagnosis / 3594 Benefic / 120 Cure | 1.5 | not measured (player GCDs; sheet cast bars) | instant bot heal | keep: deliberate bot simplification |
| `Dsr/P4Eyes/DsrP4RescueSequence.cs:200` | 25316 Pure of Heart | 35.5 | `20` 35.20, hit 35.46–35.48 (n=3) | visual at +20.0 (ActionEffect +55.5), carrier at +55.5 (ActionEffect +55.8): (c-carrier), two ActionEffects | owner decision (same as P1) |

### (d) Genuine / computed

| Site | Action | Sheet | Retail | Sim | Recommendation |
|---|---|---|---|---|---|
| `Fru/FallOfFaith/FruFallOfFaithScenario.cs:104` | 40137 / 40140 Fall of Faith | 9.0 | `20` dur 8.70 (n=472, 27 logs); hit 8.94–9.02 (med 8.98) from the same tether source. Cast starts +3.10–3.16 / +5.56–5.70 / +8.02–8.23 after the first (236 sequences); sim reveals at +3.1 / +5.6 / +8.1 | bar `Reveal + 9 − elapsed` (9.0 when on time), ActionEffect `Reveal + 9.3` | owner decision: `Reveal[i] + 8.7 − elapsed` reproduces retail (bar 8.7, ActionEffect +9.0); keep the floor |

## Conflicts between code and retail

1. **ActionEffect 0.3 s late on every (b) site with retail data** (DSR P1/P2-shared actions, all
   DMU instants, Provoke). Retail has no `20` line and the release is the instant; the sim snapshots
   on time but sends the ActionEffect 0.3 s later. The engine's own doc says the override is "only for
   the rare case where the in-game cast time differs from the sheet's".
2. **Fall of Faith bar 0.3 s too long, ActionEffect 0.3 s late** (n=472). The mechanic itself
   (Fetters, charges, impacts) runs on its own timeline and is unaffected.
3. **Duplicate ActionEffects on (c-carrier) same-caster pairs**: Heavensblaze, P1 and P4 Pure of
   Heart, P5 Faith Unmoving, Spear of the Fury, Steep in Rage. Retail logs one release per cast for
   each (n = cast count). Whether the doubled animation is visible in game is not verified.
4. **Spear of the Fury (rescue sequence) impact 1.94 s early**: sim +7.5 vs retail +9.44 relative to
   Pure of Heart, plus the cast starts 0.1 s before Pure of Heart instead of 0.54 s.
5. Side finding, not an override: `DsrP1KnightsScenario.cs:100` starts Spear of the Fury
   (0x62E2) at 122.28 = Pure of Heart + 9.44, which is retail's **hit** time; retail starts the cast
   at Pure of Heart − 0.54. Reported only.

## Needs owner decision

1. Remove `CastSecondsOverride = 0` from all sheet-instant actions (18 (b) lines plus the (b) uses of
   the four mixed helpers), letting the sheet's instant path send the ActionEffect with the snapshot.
   Check first that no site relies on the 0.3 s gap (none sets `ResolveSnapshotOffset`; P5 Death's
   offsets are on non-override actions).
2. (c-carrier) pattern: either keep the carrier and drop the separate visual cast's ActionEffect, or
   put the effects on the visual cast itself (single release at sheet time, as retail).
3. Hot Wing / Hot Tail helpers (sheet 6.5): retail probably has the helpers cast too; no P6 log exists.
4. Fall of Faith formula (`+ 8.7` instead of `+ 9`).
5. Spear of the Fury timing in `DsrP4RescueSequence` and the P1 Spear start in `DsrP1KnightsScenario`.
6. Bot heals in the rescue sequence: keep instant (recommended) or give them their 1.5 s cast.

## Not verified

- DSR P3–P7 actions: no retail pull in any log (P4 Eyes, P5 Wrath/Death-only, P6, P7).
- Retail cast of 0x63C6 in P5 (measured in P2 only; same action ID).
- Visible effect in game of the doubled ActionEffect and of the 0.3 s delay.
- The three DSR intermission pulls give identical relative times to 0.02 s; consistent with a
  scripted sequence, but only n=3.
