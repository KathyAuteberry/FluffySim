# Community QA after FluffySim 3.0.0.0

This records the investigation above published `v3.0.0.0`. The fixes are absent from that release and are incorporated into the subsequent 3.0.1.0 preparation. Test results and the Discord replies below describe the investigation before publication.

## Paradise Regained

Adodo's inversion report was correct. The first tower already established relative South, H1/H2 already soaked it, and spawn order/rotation were correct. The post-first-wing DPS positions assigned the opposite physical tower to each DPS group. Equal two-person occupancy let the old survival tests pass despite the wrong roles.

The repair exchanges only the melee/ranged destinations after the first wing. H1/H2 retain tower 1; D1/D2 cross into the far tower that was just cleaved; D3/D4 take the near tower. With the first tower treated as South:

| First wing | D1/D2 | D3/D4 |
| --- | --- | --- |
| Light | Relative NW | Relative NE |
| Dark | Relative NE | Relative NW |

The second tower is assigned to whichever group owns its physical position. It is not always melee or always ranged. All three first-tower orientations rotate this assignment. The same scenario implementation runs in both Paradise stages of continuous P5. Spawn coordinates, tower radius, damage, wing geometry and timing are unchanged. Settings now explain the Mana assignment; `FruP5ParadiseRegainedAi.AssignedTower(state, role)` exposes the human's tower identity for diagnostics.

Reference: [Mana Pandora, steps 13–18](https://ffxiv.tuufless.com/mana/fru/05_pandora/). Reviewed every diagram in steps 13–18 directly, including both wing orders, the arrows, cleaved areas and relative boss facing.

## Wroth movement and playability

The previous route, expressed as unsigned corner-relative coordinates, was `(19.5,18.5) → (11,18.5) → (11.5,10) → (3,12.5) → center`. It turned inward after the second hit, then reversed part of its North/South travel. That was a zigzag, not the primary Elemental L dodge. Debuffs were random but had no visible Wroth automarkers; a human could not identify their exact spread slot or stack partner from the bot choreography.

Existing structured failure artifacts were inspected before editing: an older seed-31 run died in puddles while regrouping at approximately 96.5s; an older seed-3 run lost the party during the eye/puddle sequence. They predate the current baseline, whose 57 P6 tests passed. They establish historical failure modes, not proof of the cause of the maintainer's particular attempt. The baseline tests drive the human with the same AI as the bots and did not assert the required route or marker identities.

The repaired route is `(21,18.5) → (14,18.5) → (10.5,18.5) → (3,12.5) → center`, mirrored by safe East/West side and the corner opposite the second eyes. The first three drops stay on the horizontal leg. After hit three the party curves inward, drops hit four, then exits toward center before the third eyes. Holding a pure perpendicular fourth-drop leg too long was tested and rejected because it intersects the second Sable Price cross with the inherited timings.

All four stack hits, four persistent puddles, exposure-based bleed, three eye explosions, dive lanes, Hot Wing/Hot Tail shapes, debuff rolls and Mortal Vow offsets remain. Existing puddle grace allows prompt movement out of a fresh drop; lingering is still punished. Event offsets and inherited puddle activation/bleed timings remain provisional and explicitly UNVERIFIED. No AoE was reduced and no damage was disabled. The missing Fire Resistance Down check/application on Flames was added so overlapping white stacks also fail; the two-person stack requirement remains.

Reference: [Elemental Double Dragons, primary Wroth steps 1–8](https://ffxiv.tuufless.com/elemental/dsr/06_double_dragons/). Reviewed the primary route diagrams directly, including the second and third drops, inward turn, and both final Hot Wing/Hot Tail layouts.

## Local marker scheme

Uses the existing `SimWorld.SoftMarkers` renderer used by Ultimate Relativity. No native marking API, chat command or network-marker pathway was added or called. `DsrWrothMarkers` only computes the assignment of existing `Sign` values.

| Symbol | Debuff / meaning | Final destination |
| --- | --- | --- |
| Attack 1 | Purple, outermost spread | West, x=-20 |
| Attack 2 | Purple spread | West, x=-13.5 |
| Attack 3 | Purple spread | West, x=-7 |
| Attack 4 | Purple, innermost spread | West, x=-0.5 |
| Bind 1 | White stack | Inner East pair, x=7.5 |
| Bind 2 | No-debuff partner of Bind 1 | Inner East pair, x=7.5 |
| Ignore 1 | White stack | East-wall pair, x=17 |
| Ignore 2 | No-debuff partner of Ignore 1 | East-wall pair, x=17 |

This is the guide's **automarker** diagram: same-type pairs, Ignore symbols at the East wall. It is not the manual same-number scheme. The image places Attack 1 furthest West and Attack 4 closest to center. Role priority from center toward edge is MT, ST, H1, H2, D1, D2, D3, D4; the purple subset therefore receives Attack 4 through Attack 1 in that priority order. White and unmarked players are independently ordered by role priority; the earlier player of each subset forms the Bind pair, the later player the Ignore pair.

[Automarker diagram](https://ffxiv.tuufless.com/images/ultimates/dsr/06/wroth_flames_am_markers.jpg) was inspected directly and its symbols checked against `SoftMarkers.IconId`. All required symbols were already supported.

Encounter rolls remain in the encounter state. The marker allocator consumes those rolls without changing them or consuming RNG. Navigation selects final destinations by marker identity. Hot Wing uses the middle East/West strip; Hot Tail uses the opposite North/South side from the puddles. All eight receive local markers after debuffs at 70.09s; completion clears them at 93.37s, before the fast Vow pass and P6 continuation. Failure clears the set, and the shared world lifecycle clears it on Reset/Leave/teardown. Hidden bot presentation retains the existing renderer's behavior: bot markers can be hidden, while the human's remains visible.

Event diagnostics cover debuffs/symbols, dive lane and safe side, second-eye pattern, L-route corner, each drop and party positions, the inward turn, all eye resolutions, final destinations and actual stack recipients. Existing action/death diagnostics supply collision recipients and exact failure causes.

## Verification

Focused coverage includes:

- Existing Paradise matrix: 3 rotations × 2 second-tower orientations × 2 wing orders × 8 human seats × 2 difficulties. Now asserts actual occupancy and action-effect recipients, plus human assignment availability. Deliberate empty-tower and wrong-position failures remain.
- Continuous P5: every human seat, both Paradise rounds, actual tower occupancy; Easy/Hard represented.
- All 420 valid Wroth debuff configurations: eight unique symbols, exactly two mixed white/no-debuff pairs, deterministic allocation, no encounter mutation. A bounded state check verifies every human role receives each debuff category across seeds.
- 24 complete Wroth patterns: all three dive lanes, both safer North/South corners, both East/West second-eye patterns, Hot Wing/Tail, representative seats, both difficulties and visible/hidden bots. Checks eight local symbols, three horizontal drops before the inward turn, all-party stacking, four puddles, all three eyes, marker destinations, cleanup and the MT→ST Vow pass.
- Twelve independent human-input runs: tank, healer, melee and ranged, each purple/white/no-debuff. Human leaves AI control, moves at 6.5 yalms/s, reacts 150ms after hits and follows destinations with a 0.6-yalm offset on both axes. These clear through the Vow handoff without requiring exact bot positions.
- Wrong spread collision, overlapping white stacks, failure cleanup, Leave, repeated entry and an actual in-session Reset/Restart.

The complete ordinary Debug routine suite passed **7,188 tests in 9m 5s**, excluding opt-in Extended sweeps. After moving the new Mana assignment query from encounter state into AI (no change to destination behavior), the final affected-mechanic/shared-marker/lifecycle regression run passed **1,028 tests in 1m 33s**. Final ordinary/optimized artifact safety reruns passed **313 tests with 3 Windows-only skips in 18s per backend**. The final isolated optimized TRACE Debug build took **2.74s**, with zero warnings/errors.

Final focused Debug run: **245 passed in 18s**. The earlier combined existing P6/Pandora regressions passed **312 tests in 24s**. Relay Debug: **44 passed in 1s**. Packet-isolation/stale-artifact safety: **313 passed, 3 Windows-only skips in 18s**, repeated successfully with the SafetyHook failure model (**313 passed, 3 skips, 18s**). Safety inspected ordinary Debug and a separate optimized TRACE Debug artifact under `/tmp/fluffysim-community-safety`; the latter built in **3.03s** with zero warnings/errors. No Release build or installed output was used.

A complete successful structured reproduction is retained locally in `tests/AnoMech.Tests/TestResults/scenario-runs/DsrP6DoubleDragonsScenario-s0-11/` (ignored, not committed). It records the forced center dive lane, NE second eyes, SW start, random debuffs, all four drops, all three eyes, marker destinations and both exact stack memberships. Seed 11 with the test's pattern overrides completes at approximately 101s without deaths. Its `summary.txt` has the generic replay command; use the focused pattern test `(0,-1,1,true)` to retain those overrides.

## Native checks still required

In-game confirmation is still necessary for local marker texture/render visibility over the human and fake party, including hidden-party behavior, Pause, Restart and Leave; observing human movement/camera readability for all mirrored routes; and native capture validation of provisional Wroth timings and puddle exposure behavior. Headless success does not prove those client rendering/timing details. Ultimate Relativity and CT marker isolation are covered by shared-marker and scenario regressions, but should also receive a brief native smoke check.

## Discord replies

**Paradise:** “You were right — melee and ranged were inverted after the first wing. The local fix puts H1/H2 in the first tower, D1/D2 in the far tower that was just cleaved, and D3/D4 in the near tower, for either wing order. It isn't in published 3.0.0.0 yet.”

**Wroth:** “You were right — the bots turned after the second Akh Morn and zigzagged. The local fix keeps three drops horizontal, then curves inward for the fourth, with the Elemental L dodge and local-only automarkers: attacks West, Bind pair inner East, Ignore pair East wall. Deterministic clears include human movement with room for error; native checks remain. Not in published 3.0.0.0 yet.”

## Jujutsu integration for 3.0.1.0

With owner authorization, the original combined unpublished change was split into sibling feature changes from published `qyypuu` / `8b62c6` (`v3.0.0.0`): `zsklwo` contains the Paradise fix, and `soowvw` contains the Wroth fix. New integration change `qolpkm`, with description `chore: integration commit`, merges both feature tips and carries this shared investigation report. The merged implementation is identical to the previously validated combined fix. A separate child prepares 3.0.1.0. Historical `plkqvn`, the published 3.0.0.0 integration and all released tags remain intact.
