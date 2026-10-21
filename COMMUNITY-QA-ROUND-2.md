# Community QA round 2 — development investigation

Baseline: published 3.0.1.0 (`master@origin`, change `xorxqy`, commit `f0f74b`). The Paradise Regained and Wroth Flames fixes are ancestors and were preserved. This batch is unpublished and requires native playtesting; it does not prepare 3.0.2.0.

## Darklit overhead Water

The Mana Darklit entry applied only status 2461 (Spell-in-Waiting: Dark Water III). Its sheet row has icon 215306 but **VFX 0**. A local retail Darklit capture shows both Water recipients gaining 2461 for 19 seconds and hidden companion **2458 for 14 seconds**, together at the Darklit resolution (4.987 seconds after the observed cast start). The capture ends in a wipe, so it does not establish the later successful resolution clock.

Pinned Status data and installed game data agree: 2458 has no icon, references StatusLoopVFX 118, and that row references VFX 742, `m0581stlp_b0h`. The installed asset `vfx/common/eff/m0581stlp_b0h.avfx` exists. The upstream Apocalypse and Darklit implementations also apply this companion status. This is a native status-loop effect, not a network head marker or the generic stack lockon.

The Mana drill now applies both statuses to the same two recipients at its existing 7.083-second assignment event. The companion expires at about 21.083, immediately before the existing 21.097 stack markers. Water itself still resolves at 26.1. Removed the previous generic stack lockon 62 at 2.01 seconds, which preceded assignment and was not the missing status effect. Normal status publication sends both the native status list and GainEffect for human and bot actors; expiry sends LoseEffect. Added 2458 to the scenario cleanup list. No direct VFX pointer ownership, new helper actors or marker requests were introduced.

Regressions cover human and bot recipients, delayed initialization, native-presented status slots, absence of the early lockon, Pause/Resume, companion expiry while the Water debuff remains, Water resolution, active-status Leave and repeated entry. These establish the native inputs and lifecycle, not rendered appearance. If native status-loop rendering still fails, capture the actual recipient's status manager and VFX state before considering a separate local visual.

Discord wording: **“Darklit now applies the missing native overhead Water effect to both Water holders, alongside their party-list debuff. The later stacks and mechanic timings are unchanged. Awaiting native playtest.”**

## Crystallize Time: no incorrect death reproduced

No encounter correction was made. The bounded new matrix uses seed 9, Mana (index 1), all six human non-tank seats, all four Tidal corners, and both Require Tank Mitigation settings (48 cases). The human follows AI until 50 seconds, then genuinely takes over: subsequent AI input and its mitigation exemption are disabled. UserActions and the global mitigation check are enabled. A six-second Arm's Length/Surecast status is applied at 50.6; no personal damage-reduction ability is supplied. Every case survives through 61 seconds, after the second wing's damage and knockback.

The recording is sampled at 44.8 and compared with the settled Rewind at 54.0. Rewind begins at 52.8; inspecting its coordinates immediately at 53.0 would incorrectly classify a carry still in progress as a bad restore. The first wing snapshots about 53.9, releases about 54.2 and applies damage about 55.7; the second snapshots about 58.5, releases about 58.8 and applies damage about 60.3. Exact events are frame-quantized. The first anti-knockback expires at 56.6, before the second snapshot and forced movement. Separate negative controls for every non-tank seat omit or expire immunity and fail by leaving the arena.

Example NE, human D1, seed 9:

| Wing | Native damage delivery order (all eight hit) | First four gaining Magic Vulnerability Up 2941 | Knockback |
|---|---|---|---|
| First | ST, D4, D3, D2, D1, H2, H1, MT | ST, D4, D3, D2 | Suppressed by active immunity |
| Second | MT, ST, H1, H2, D1, D2, D3, D4 | MT, ST, H1, H2 | Present for all eight |

Both tanks return to the existing outer-edge positions: MT (-12, -14.5), ST (12, -14.5) in this orientation. The human D1 returns to its actual recorded near-center position, approximately (-0.0214, -1.6388), and finishes the second push near Z=18.3612. ST carries first-wing vulnerability into the second; no first-front-four non-tank re-enters the second front four. The first tank hit uses the existing qualitative 80,000 flytext amount; ordinary hits use 40,000. These are not an HP/gear damage simulation.

Bot party reductions 3985, 2711 and 1934 cover the sequence. `WingsDamage` has no mitigation threshold on a correctly placed non-tank: a carried Magic Vulnerability Up on a front-four non-tank remains lethal. Require Tank Mitigation changes the tanks' approximate threshold/forced mitigation validation only; it does not impose a DPS/healer defensive requirement. The existing unit and integration regressions also retain tank-first ordering, wrong-puddle/uncleansed Wyrmfang failures, vulnerable non-tank failure, tank cooldown checks, required first immunity and second push. There is no blanket immunity added.

The final focused TRX records per-seat positions/statuses at 53.6, 54.0, 58.2, 58.5 and 60.5, both native wing packets' recipients/status gains/knockback entries, and failures. These diagnostic artifacts remain in `/tmp`, not source control.

For Adodo: capture role/job, selected strat, seed, corner/second-wave direction, slow-hourglass pattern, starting debuff, Require Tank Mitigation and global mitigation options, UserActions setting, actual anti-knockback press time, Return recording position, and the exact death action/reason. Use the existing damage-debug snapshot and diagnostic run log around both wing snapshots. In particular distinguish arena fall, double-vulnerability/front-order errors, uncleaned Wyrmfang and a mitigation failure. A native movement discrepancy remains possible; this bounded result does not establish all seeds or client behavior.

## Wrath post-trio tankbuster: no incorrect swap reproduced

No tank mechanics correction was justified. Extended existing tank-swap tests to survive through Wrath's **88.1-second completion**, rather than stopping just after the third cleave. Repeated entry reconstructs the holder and Provoke window for both human MT and human ST. All four tank jobs retain the same conventional strategy.

Observed seed-0 snapshots (same targets for both human tank roles):

| Time | Action | Actual target / sole recipient | Target statuses |
|---:|---|---|---|
| 75.55 | Heavenly Heel 25543 | MT | Rampart 1191, then Slashing Resistance Down 3130 |
| 78.95 | Ascalon's Might 25541 | ST | Rampart 1191, no Slashing Resistance Down |
| 80.59 | Ascalon's Might 25541 | ST | Rampart 1191, no Slashing Resistance Down |
| 82.25 | Ascalon's Might 25541 | ST | Rampart 1191, no Slashing Resistance Down |

The initial holder is MT. Human MT mitigates Heel and steps out; bot ST applies Rampart and locally Provokes immediately after the Heel hit. Human ST must target Thordan and locally Provoke after the chat prompt, then mitigate the three cleaves. Bot MT does not reclaim. Deliberate missing Provoke leaves vulnerable MT as the real target and fails explicitly; deliberate human MT reclaim after the first cleave fails the next; absent/expired human mitigation remains a distinct legitimate failure. The standalone sequence constructs a fresh world/holder; no prior-phase vulnerability is present before Heel.

The existing 20% human tank mitigation budget is explicitly approximate, and forced checking depends on UserActions being enabled. It is not an exact Elemental HP/healing model. That existing tank responsibility was retained, including the existing chat guidance. Failure to press mitigation with UserActions enabled can explain a post-trio tank death; this is not evidence that a valid swap fails. Native Provoke execution and the player's actual position still require testing.

## Twisting Dive in the native Enemy List

Wrath's trio Vedrfolnir already had the correct battle-NPC/name, native actor cast and action 27531 (Twisting Dive). Its helper `Spawn` defaulted to **EnemyListMode.Never**, hiding it from the normal list even though its cast existed. The trio dragon now requests **Always**. It remains untargetable. The opening decorative dragon, other knights and hidden helpers retain their existing policies.

The existing EnemyList service sends local Hater-list updates, including the dragon's real simulation entity ID, independently of targetability. It resends every second and therefore tolerates delayed native registration. The dragon's existing ActorCast raises a 5.7-second bar from 38.98 (the six-second sheet action minus the established 0.3-second release offset); the existing dive snapshots about 44.68 and releases about 44.98. Twister positions still snapshot at 44.97. No attack scheduling, trajectory, target selection or damage geometry changed. Native English sheet name is singular “Twisting Dive”; no substitute cast action or custom overlay was added.

Regressions cover native cast action/length/end, the same actor's ActionEffect release, enemy-list addition/removal, untargetability, 0/5/19-frame packet initialization, active-cast Leave and repeated entry. A 110-frame initialization experiment correctly hit the existing 20-frame dropped-spawn guard; it was not used to relax that guard. Fakes cannot establish actual Enemy List rendering.

Discord wording: **“Wrath's Vedrfolnir is now registered in the normal Enemy List, so its existing Twisting Dive cast can appear there while it remains untargetable. Dive and Twister timings are unchanged. Awaiting native playtest.”**

## Validation

Debug only. No installed output, Release build, version, tag, push, publishing or plogons change.

- Affected encounter/native-entry/Pause/Restart suite: **1,991 passed, 58 seconds**. This includes existing CT wave/seat/difficulty, Darklit, Wrath and tank-swap regressions. Stress/catalog sweeps were not added.
- Final focused human matrix/presentation/tank-swap diagnostics including active-effect Leave/reentry: **81 passed, 11 seconds**. Counts overlap with the preceding suite; six focused cases were added afterward.
- Ordinary Debug plugin built by the affected test build. Isolated optimized TRACE Debug plugin build: **0 errors, 6.65 seconds**, output `/tmp/fluffysim-qa-safety`, intermediates `obj/qa-safety-Debug` per project.
- Both ordinary and optimized Debug DLLs supplied to SafetyTests: **313 passed, 3 Windows-only skips**, **18 seconds** with each backend (default and `ANOMECH_HOOK_BACKEND=safetyhook`). Existing packet boundary, lifecycle, failure-model and IL contracts passed. No safety allowlist changes.
- Restore reported existing NU1900 vulnerability-feed connectivity warnings. The sandboxed test runner could not open its local socket; tests succeeded with permission to run that local runner. No automatic-approval rejection occurred.

## Native acceptance before release

Run in both inn practice and offline cold entry, with the normal Debug development setup:

1. Darklit: force human tethered Water and untethered Water in turn; confirm exactly both Water holders get the correct overhead effect at debuff assignment, party-list status remains correct, overhead transitions to the existing stack presentation around 21.1, and Water resolves around 26.1. Check the other six players, tethers and towers. Pause at 10 seconds, resume, then Restart/Leave while Water is active; check no stale effect after reentry.
2. CT: Mana Rewind North on each D1–D4/H1–H2 seat, both second-wave orientations and wing directions. Record correctly, press native Arm's Length/Surecast before stun, use no personal reduction, and survive both wings with normal options. Repeat non-tank runs with Require Tank Mitigation enabled. Confirm required second push still happens. Deliberately omit immunity, misrecord a position and miss a cleanse to retain failures. Preserve outer-edge tank recordings/crossings.
3. Wrath MT: mitigate Heel, leave the frontal after the hit, watch bot ST take all three cleaves, and continue until completion. Wrath ST: target Thordan, Provoke after the hit/prompt, mitigate all three, and confirm bot MT stays out. Deliberately miss Provoke and expire mitigation separately. Restart and repeat both roles.
4. Vedrfolnir: inspect the game's normal Enemy List during the trio, including cold/delayed model loads. Confirm one untargetable Vedrfolnir entry with the native Twisting Dive bar, correct name and finish before dive/Twisters; no helper/duplicate bar. Pause/Resume during the cast; Restart/Leave during it and confirm the list clears.

Headless success is not native rendering acceptance and these fixes are not live. New integration/release staging requires the owner's separate authorization; published 3.0.1.0 and the historical `plkqvn` integration are preserved.

## Local Jujutsu delivery

The existing clean owner-provided working change was directly above `master@origin` and was reused. Only the default workspace existed; no mutable integration generation was present. The published integration `qolpkm`, published master/tag `xorxqy`, and historical `plkqvn` were left unchanged.

- `kxkywx` (`36f6a6`): `fix(fru): restore native Darklit Water overhead status`.
- `zxqmxx` (`23a62f`): `fix(dsr): list Vedrfolnir during Wrath Twisting Dive`.
- `yowxqp`: `test(qa): audit human wing damage and Wrath tank completion`, including this report.

These are an anonymous local feature chain above the published baseline. No bookmarks were advanced and no replacement integration was created. There are no CT or tank-swap production changes in this batch.
