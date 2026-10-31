# Pandora mechanic evidence and implementation register

Baseline: FluffySim `5d6b648b6ea6b1e4d0003494e00e2a363f4691d1`. Shared FRU source: AnoMech `6aec4a5940df56ebc4f70a2fdddeceb31c7848b8`, identical in inspected AsuMech `d3f4f3e07a9af49d66a2637ef3f329c76fd174d9`. Existing author attribution and AGPLv3 remain. These mechanics were already present in FluffySim's imported scenario tree; this effort extends/adapts them rather than claiming a new wholesale port.

## Evidence language

**Observed** means installed native sheet/asset data or an actual native capture. **Corroborated** means independent sources agree on the specified feature. **Inferred** means inherited simulator behavior or alignment not independently measured. **Unknown** means not established. An installed action row establishes its ID/shape/cast duration, not its runtime snapshot or rendered effect. The historical [local capture inventory](fru-native-capture-inventory.md) found no P5 cast/effect records and only interrupted CT entry casts. The later [clear audit](fru-clear-audit-2026-10-31.md) supplies complete P5 cast/event coverage, confirms the shorter second Paradise cycle, and documents a fix for prematurely cleared exalines. Its measured event timings supersede the inferred timing qualifications below where explicitly compared; rendered onset and exact snapshots remain unverified.

References:

- [Tuufless Mana Pandora](https://ffxiv.tuufless.com/mana/fru/05_pandora/): primary strategy, light parties, tower allocation, four-round order and phase repetition.
- Local `awgil/ffxiv_bossmod/BossMod.Ultimate/Dawntrail/Ultimate/FRU/{FRUEnums,FRUStates,P5FulgentBlade,P5AkhMorn,P5ParadiseRegained,P5PolarizingStrikes}.cs`: independent geometry, IDs and ordering. Source implementation is not a packet capture.
- Local `FFXIV-CombatReborn/BossmodReborn`, `WCGH/FRU-Sim`, `PunishXIV/Splatoon`: secondary comparison resources; no unsupported preset is treated as native evidence.
- Pinned [AnoMech FRU source](https://github.com/anomek/AnoMech/tree/6aec4a5940df56ebc4f70a2fdddeceb31c7848b8/AnoMech/Scenarios/Fru).
- [Arena evidence and readiness](fru-pandora-arena.md), [nine-scenario comparison](asumech-comparison.md), [manual checklist](fru-native-playtest.md).

## Mechanic-relative register

Times below are local to the isolated source drill, before any continuous-practice offset. They are executable simulator times, **not observed packet timestamps**.

| Event/time | Native IDs / actor and targets / geometry | Presentation | Evidence/confidence | Remaining verification |
| --- | --- | --- | --- | --- |
| P5 entry before T0 | Territory1238; Pandora BNpcBase17839, Name13561, layout0xA55C06, center facing north; weather108, platform slot47; BGM20099 | Native phase readiness, map effect, boss packet spawn | Installed territory/BGM rows observed; platform/weather runtime inferred from shared source | Both entry modes, actual rendered layers/weather/platform/collision/targeting |
| Fulgent1.5 | 40306; Pandora raidwide, six seams in three paired groups | Native boss cast, native event objects with beat state changes | Shared upstream, BMR/guide order corroborated; absolute alignment inferred | Native seam glow/arm/fire order and model visibility |
| Seam casts11.55 /15.6 /19.55 | 40307 light,40118 dark; helper rect5×80; paired perpendicular/diagonal strips | Native cast omens and action effects | BMR corroborates IDs/shape, seven-second first cast; glow delays inherited | Real glow-to-hit interval and omen disappearance |
| Exalines18.6+4g+2.05h | 40308/40309; each next strip advances5y, clipped after leaving arena | Helpers move, instant native hits | BMR corroborates5y/~2s; exact2.05 and geometry transforms inherited/inferred | All rotated/mirrored safe pockets, simultaneous light/dark hit effects |
| Tank autos10.65 onward | 40114 visual,40115 helper per MT/ST | Native swing then hit; top-two seats | Guide corroborates top-two aggro; inherited helper timing inferred | Native animation delay; dynamic threat is simplified to role seats |
| Akh Morn26.35 cast /34.05 target /34.45 hit | 40310 cast,40311/40312 helper stacks; two4-player stacks, magic vuln punishes overlap | Native cast and paired native effects | Guide/BMR corroborate side split; picks remain seeded random per side per guide; exact offsets inherited | Multi-hit animation, actual side boundary and random target selection |
| Paradise1.3 cast | 40319; Pandora | Native cast/model state/battle talk | Upstream and BMR corroborate family; offsets inferred | Model wing state and voice/text timing |
| Towers6+3.5i show /15.7+3.5i hit | Map slots51–53, radius3, distance7, three120° positions; exactly2 occupants | Native map effect towers/occupancy/base/expiration | BMR corroborates slots, geometry, order and2occupants | Appearance/expiration plus damage-offset fidelity |
| Wings8.3 cast /15.7,19.4 cleaves /16.5,20.2 busters | Native light/dark actions in shared constants; ±60° rotation,120° halfangle; nearest/farthest tether | Native model state, tethers, cleaves and target busters | BMR corroborates geometry; Mana guide corroborates tanks/tower grouping; offsets inherited | Tank swap display/aggro semantics and invulnerability practice; source uses scheduled tank facing rather than a full threat table |
| Polarizing30.35 cast | 40316; boss | Native cast and helper line telegraphs | Guide/BMR corroborate four rounds, T→M→R→H | Exact live bait snapshot |
| Lines36.9+4.6r /echo+2.6 | 40317/40318 bait,40119/40120 echoes;100×6 lines | Two independently aimed native helper lines, fixed-direction echo | BMR corroborates IDs/shape/echo; offsets inherited | Native snapshot and effect persistence |
| Resistance on front bait | Light4164 /Dark3323;15.95s; nearest hit only | Native status, same-color hit lethal, immediate magic vuln punishes overlapping lines | Guide corroborates only front bait switches; upstream effect rules already enforce this | Exact native duration and recipient ordering at ties |
| Box1.0 cast /sheet cast end | 40326; Pandora100y raidwide; requires active tank LB3 statuses196/863/864/1931 | Existing local LB actor controls/statuses/gauge, native boss cast/effect | BMR corroborates12s cast and tank LB requirement; eight-second LB status from existing JobActions | Live LB cast/animation timing and press latency; flytext60000/999999 is training representation, not measured damage |

## Mana adaptation and failure rules

Fulgent light parties remain MT/H1/D1/D3 and ST/H2/D2/D4. The upstream parity grouping already matches Mana; it is now labelled explicitly. Full exaline training retains authentic native seam/event-object and cast presentation. Every exaline collision is conservatively lethal in this inherited practice model; it does not model surviving a single color through heavy mitigation and then applying a resistance debuff. This is a known damage-model simplification, not proof of retail damage values.

Paradise uses the first tower as relative south. Healers take it. After the first cleave the two melee go together to the cleaved far tower, and the two ranged together to the near tower. This replaces the inherited melee+ranged pairs. First-tower orientation, second-tower side and wing order can all be forced or randomized. Mechanic scheduling never branches on region.

Polarizing remains T→M→R→H. Only previously front-baited players switch sides, and both four-person groups dodge their fixed residual lines. Bait selection now takes the nearest player independently on each boss side, corroborated by both BossMod implementations; a missing side falls back to the nearest player and the two overlapping beams fail through existing vulnerability rules (this fallback is inferred). Existing effects already enforce stack size, color resistance, overlap vulnerability and lethal echoes; those checks were preserved. Separate entry starts the same attack/AI timeline at a shifted origin rather than maintaining another implementation.

Pandora's Box uses the real local Limit Break route. A human tank presses their own button; a bot tank presses when the human is a non-tank. No fabricated server-bound action is sent. The LB gauge is restored through existing party teardown. The requirement checks each recipient's active LB3 status at snapshot, independently of optional generic mitigation practice. Early, late, lower-level, interrupted or out-of-range protection cannot satisfy it merely by spending the gauge. Bot LB no longer silently does nothing when optional mitigation checking is disabled. Debug-bot-controlled player seats use the same local status resolution for headless testing.

The inherited tank-threat model schedules MT/ST facing and tank targeting; it is not a general aggro/swap simulator. This remains a limitation for native Provoke/Shirk practice. No new real-server or native-memory mechanism was added to paper over it.

## Validation

See the delivery results for executed test counts. Dedicated coverage includes all3×2×2 tower configurations ×8player seats ×Easy/Hard, all Fulgent pocket rotations/mirrors, isolated Polarizing seats, Box protection and early/late failures, native P5 phase initialization and queued cast teardown. Passing headless damage tests does not establish rendered native fidelity. Public release requires the linked in-game checklist.
