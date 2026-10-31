# FRU Pandora native arena

## Implementation

`AnoMech/Scenarios/Fru/FruZone.cs` owns P5 alongside the existing P1–P4 phases. The imported P5 phase is an alias of this phase, so both menu paths use the same zone, party spawn, entrance barrier, JP waymarks, arena radius and phase lifecycle. P1–P4 initializers are unchanged.

P5 requests all 54 native environment slots exactly once: slot 47 enters state/timeline `0x00010001`; every other slot receives `0x00040004`. The existing map controller waits for the FRU shared-group readiness gate, preserves request ordering, cancels queued work on unload and clears pending phase work on restart. There is no scheduled T+1 initialization callback or repeated whole-arena reset.

Client setup holds the existing known P1 and P2 scenery layers inactive, using the same suppression as P4. No guessed P5 layer keys, native memory manipulation or new packet pathway were added. Phase application keeps the existing weather commit per layout generation. This addresses initialization ordering; it does **not** establish that the rendered P5 platform is correct without a client check.

## Evidence register

| Item | Value | Evidence | Confidence / outstanding check |
|---|---|---|---|
| Territory and background | 1238; `ex3/01_nvt_n4/goe/n4gw/level/n4gw` | Installed TerritoryType sheet, read through Lumina | Observed sheet value; collision/rendering still needs client check |
| Arena show slot | 47, flags `0x00010001` | AsuMech `d3f4f3e07a9af49d66a2637ef3f329c76fd174d9`, `FruConstants.MapEffect.PandoraArena`, `FruZone.P5`; imported upstream implementation | Inferred from implementation; no direct P5 arena snapshot located |
| Other map slots | 0–53 except 47 hidden with `0x00040004` | Same source initializer | Inferred; verify no omitted persistent P5 object |
| Weather | 108 | Same source's `WeatherId.Pandora` | Inferred phase selection; weather-sheet existence or successful application is not proof of rendered scenery |
| BGM | 20099; `music/ex5/BGM_EX5_Ban_11.scd` | Installed BGM sheet row 20099 exists; same source selects it for Pandora | Observed asset mapping; phase track association is implementation evidence |
| P1/P2 layer suppression | `0x5F88`–`0x5F94`, `0x5F9F`, `0x5FC9` inactive | Existing FluffySim P3/P4 initializer and prior phase investigations | Retained established behavior; P5 native selection needs client validation |
| Entrance | EObj 2007457; layout `0xA58C08`; wall `0xA58C07` | Existing FluffySim FRU zone and verified entry lifecycle | Retained; validate P5 cold entry in both modes |
| Bounds | Radius 20, origin (100,0,100), spawn (0,0,16) relative | Existing FRU zone and inherited P5 mechanics | Corroborated implementation geometry; native edge/collision remains a client check |

Local arena-snapshot directories under both FluffySim and AnoMech plugin configurations were inspected. They contain P1–P4 and DSR captures, but no P5 capture. No P4 snapshot is presented as P5 evidence. The checked local AnoMech HEAD was `23842e787801ab1f47204b7c393ce3effc7e3935`; provenance for imported P5 source is covered by the main comparison report rather than attributed to that older local HEAD.

## Automated checks

`FruP5ArenaTests` checks immediate and delayed native readiness, all slot requests and ordering, no reapplication during subsequent ticks, phase alias identity, P1/P2 suppression ownership and cancellation on Leave. Existing `NativeEntryLifecycleTests` includes main-zone P5 catalog entries automatically and covers both entry modes, roles, visibility and difficulty. These are lifecycle checks against fakes, not proof of native visuals.

The first sandboxed Debug test invocation failed while evaluating the plugin project with no compiler diagnostics. The coordinating agent runs the final Debug validation with the working build environment; no Release output was built or changed for this work.

## Prioritized client verification

1. Cold-enter P5 from the inn and from title-screen offline mode. Confirm the actual Pandora platform/background, weather, music and collision, without P4 scenery or flicker. Save one native snapshot in each mode.
2. Confirm the entrance opens, the player moves, Pandora is visible/targetable with the correct model, and eight correctly named party frames appear.
3. Stop/Restart twice, then switch P5 → P1 → P2 → P4 → P5. Confirm scenery and entrance state converge without duplicate actors or leftover towers/exalines.
4. Check Fulgent native line visuals and Paradise tower activation against platform geometry, with bots visible and hidden. Check Pause/Resume and Leave for persistent native effects.
5. Repeat a delayed cold load and verify mechanic time starts after readiness. Report active rendered weather and layout state separately from the requested weather ID.

## Isolated Akh Morn and continuous sequence

`FruAkhMornScenario` shares the Fulgent encounter's actual stack actions and targeting, with the exalines omitted. Its cast starts at 1.5, targets lock at 9.2, and the two helper actions start at 9.6. These are inherited Fulgent relative times shifted by -24.85, not newly measured native timings. Mana light parties use the existing alternating-role grouping (MT/H1/D1/D3 versus ST/H2/D2/D4). Seeded random selection among each side's four candidates is retained: Tuufless specifies random targeting; BossMod's nearest-person display is not treated as proof of actual target selection.

`FruPandoraSequenceScenario` composes three Fulgent/Akh Morn segments, two Paradise/Polarizing segments and Pandora's Box using one persistent Pandora. Each segment owns its helpers and effects; completed segment children are removed at the next boundary, except that Fulgent helpers remain until their final possible strip impact. The boss, party, waymarks, arena and phase clock persist. Pandora's position/facing/model state resets at these practice boundaries; exact retail interstitial movement and additional auto-attacks between fragments are not reproduced.

| Boundary | Sequence time | Basis |
|---|---:|---|
| First Fulgent segment starts | 0.00 | Existing fragment |
| First Paradise segment starts | 37.35 | Akh Morn 34.45 + BossMod's 4.2 gap, minus fragment's 1.3 lead-in |
| Pandora's Box segment starts | 95.45 | First Polarizing final echo + BossMod's 5.8 gap, minus Box's 1.0 lead-in |
| Second Fulgent segment starts | 113.15 | Box's 12-second cast + BossMod's 6.2 gap, minus Fulgent's 1.5 lead-in |
| Second Paradise segment starts | 154.70 | Akh Morn + BossMod's 8.4 gap, minus Paradise's 1.3 lead-in |
| Second Polarizing cast | 179.70 | Second Paradise uses -5.35 offset, giving the BossMod 2.3 gap after its last tower |
| Third Fulgent segment starts | 203.75 | Second Polarizing + BossMod's 2.6 gap, minus Fulgent's 1.5 lead-in |
| Paradise Lost cast | 246.60 | Last Akh Morn + BossMod's 8.4 gap |
| Optional lethal Paradise Lost | 268.10 | BossMod 12 + 9.5; Action 40328 has sheet cast 21.5 and radius 100 |

Sequence boundaries were initially **corroborated/inferred** from the existing fragments and `BossMod/Modules/Dawntrail/Ultimate/FRU/FRUStates.cs`. The [October 31 clear audit](fru-clear-audit-2026-10-31.md) compares these boundaries with native recordings and documents the remaining receive-time differences. The terminal helper uses Action 40328's 21.5-second sheet total, producing the observed 21.2-second bar through the engine's default sheet-to-bar subtraction. With the retained 0.3-second damage delay, terminal deaths occur at sequence time 268.1; the interrupted clear does not validate that damage delay.

The sequence setting **Resolve terminal enrage** defaults off: practice stops after the final cast without claiming the boss was defeated. Enabling it resolves genuine lethal damage with the enrage action. Boss HP, final damage optimization and the melee LB DPS race are not modeled. This is an explicit practice boundary, not invulnerability or an automatic boss kill. Automated tests separately require the default sequence to clear its mechanics and the enabled terminal action to wipe all eight characters.
