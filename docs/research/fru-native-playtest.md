# Native playtest gate: FRU additions and existing drills

Headless completion does not establish rendering, collision or safe native cleanup. Run the Debug plugin manually; do not replace installed Release output. Record the scenario, seed, player role, mode, bot difficulty and failed step. Capture snapshots/logs locally without committing identities or binaries.

## Priority 1: native entry and exit

- [ ] Cold enter P4 CT from an inn: correct P4 floor/scenery and rendered weather, no phase flicker, barrier visibly opens and has no remaining collision; no cast clock advances while loading.
- [ ] Cold enter P5 from an inn: Pandora's actual platform, floor and background; correct Pandora model, hitbox, facing, target ring, targetability, cast bar and music. A weather ID alone is insufficient.
- [ ] Repeat both from the title screen offline; verify the same final native state and eight distinct party names/HUD entries. Confirm first helpers and effects are visible after delayed loading.
- [ ] Enter P2, P3, P4, P5 in succession; repeat P5→P2→P5. Check old layers, actors, collision and music do not persist.
- [ ] Stop during a cast, Restart twice, Leave during loading and Leave during a mechanic. Check actors, tethers, statuses, local action state, HUD, held layers and collision fully clean up; return to ordinary gameplay safely.

## Priority 2: CT correctness

- [ ] Test both useful red/blue assignments and hourglass patterns, changing the human role between tank, healer and DPS. Confirm Mana/Nukemaru sorting and player-readable status names/durations.
- [ ] Six hourglasses appear with yellow/purple tethers and correct acceleration/slow behavior. Dragons travel in opposite directions and correct players catch them.
- [ ] Deliberately take a wrong catch, overlap a personal AoE and miss a cleanse. Each must produce its specific believable failure; bots must not rescue an invalid player action automatically.
- [ ] Confirm puddle appearance/arming/expiration, Wyrmclaw and Wyrmfang cleanses, Tidal Light waves and dodge deadlines.
- [ ] Capture Return positions, move away and verify restoration; check later Spirit Taker, Wings, knockback/immunity and ending Akh Morn whenever included by the selected drill.

## Priority 3: P5 correctness

- [ ] Fulgent: test clockwise/counterclockwise and line variants. Check all three paired sets, native telegraph order, cascading hit positions, safe pockets and tank autos; note exaline collisions use the inherited lethal-hit practice model. Deliberately stand in a line.
- [ ] Akh Morn: verify two four-player Mana groups and targeting, then deliberately miss a stack or overlap groups. Check the paired single-hit resolution and cleanup.
- [ ] Paradise: test every tower orientation and both wing orders. First tower establishes relative South; each tower requires two appropriate players. Check native towers expire at resolution and missed/wrong soaks fail.
- [ ] Wings: verify near/far tether selection, safe half, tank swap/buster timing and local invulnerability. Change the human tank role; bots must adapt.
- [ ] Polarizing: verify all four Mana rounds, light/dark baits, echo paths, swaps and status/resistance penalties. Deliberately cross groups and repeat an invalid color soak.
- [ ] Pandora's Box: compare correctly timed local tank LB3, early LB and absent LB. Confirm damage/failure behavior and simulation-only native effects; repeat after Restart.
- [ ] Continuous sequence, if implemented: verify actual repeat variants, transitions, Pandora's Box and final Paradise Lost/enrage. Check isolated drills remain available.

## Priority 4: regressions and controls

- [ ] Existing FRU: Fall of Faith, Light Rampant, Ultimate Relativity, Apocalypse and Darklit Dragonsong. Confirm Mana routes, native arena and familiar measured impact/visual timing.
- [ ] Existing DSR: Strength1, Strength2, Sanctity1 and Sanctity2/Meteors. Confirm Elemental routes, P2 scenery/weather, raised towers, meteor spacing and native knight first casts.
- [ ] Completed Mana Diamond Dust and Mirror, Mirror appear in normal P2; the duplicate upstream Diamond Dust remains hidden.
- [ ] Run representative CT/P5 seeds on Easy and Hard with visible bots, then hidden bots. All seven bots should complete their duties without immunity shortcuts.
- [ ] Pause immediately before snapshot/rewind/tower/line events; wait, resume and verify logical timing does not advance. Note any independently running native animation separately.
- [ ] Verify God Mode, local invulnerability and anti-knockback behavior; switch them off and reproduce a real mechanic failure.

Do not mark an unsupported mechanic or unimplemented entry point as passing. Record it as missing and retain the release gate until resolved. Packet isolation and teardown tests supplement this checklist; native visuals alone cannot prove absence of server-bound traffic.

Latest CT/P2 native verification priorities: [FRU Completionist](fru-completionist.md#native-playtest-priorities).

## Operation Unfuck FRU — native retest corrections (2026-10-20)

### P2 startup contract

Both openings incorrectly required `HasDrawObject` on every registered source, including `InvisibleHelper` actors. Registration (`Exists`, packet completion and identity validation in `SimNpc`) and mesh readiness are different contracts. Invisible sources intentionally need no rendered model. The new shared readiness check retains registration/timeout failures and requires a draw object for visible actors, while accepting registered invisible helpers without one. Each failure logs its opening stage, missing condition, entity, BNpcBase/Name, visibility and layout ID. It does not assert mirror layout rendering: the mirror sources are invisible, and MapEffects own the native mirror visuals.

The fake previously always supplied draw objects. Dedicated regressions now remove helper draw objects and run both encounters through completion, and separately remove visible draw objects and require startup failure. Existing late-spawn/timeout/Leave tests remain. This reproduces the bad contract; it does **not** prove that this was the only condition failing on the user's client. Read the new per-actor diagnostics if startup still fails. Native map activation/rendering, slot availability and cold-loading still require client checks.

### Mirror geometry

[Tuufless Steps 12–15](https://ffxiv.tuufless.com/mana/fru/02_usurper_of_frost/) diagrams `mirror_mirror_02.jpg` and `mirror_mirror_03.jpg` were inspected. The first melee fan was incorrectly directed toward the blue mirror. It now fans beyond the boss, with tanks at the flanks and melee between them. The blue-mirror group retains its inward fan. The red-mirror fan now changes handedness according to the other red mirror's location, instead of applying identical role offsets to both sides. This reproduces the diagram's asymmetric support placements without changing cone widths, closest-four selection, donuts, snapshots or Banish damage. Existing Banish pairs remain MT/D1, ST/D2, H1/D3, H2/D4.

### CT mitigation

The [mitigation framework](https://ffxiv.tuufless.com/mana/fru/mitigation/) specifies both tank 90s cooldowns, both healer 120s cooldowns, D3 and both tank 40% cooldowns for Hallowed Wings. The simulator previously checked a human's own statuses but supplied none of the planned healer/ranged reductions. Mana/NA CT bots now contribute the preset White Mage's party Temperance (3985), Scholar's Desperate Measures (2711) and Bard's Troubadour (1934) at 48.6, after Spirit Taker, with 20/20/15-second durations and 30-yalm recipient range. Reductions are 10%/10%/15%; the level-98 Troubadour trait is confirmed by the [official Bard job guide](https://na.finalfantasyxiv.com/jobguide/bard/). A human occupying a contributor seat does not receive an automatic contribution from that seat. These are local status simulations; native ability animation/cooldown execution by bots is not implemented.

Shake It Off already supplies a 15%-HP barrier, not 15% damage reduction. Damnation is registered as 40% reduction for 15 seconds. Shake It Off consumes Damnation in the production action-clear table: order matters. Regression exercises the production clear/apply tables at 48.7, verifies remaining statuses and checks a vulnerable human tank at 55. Shake→Damnation survives with the three party reductions; Damnation→Shake loses Damnation and fails. This is not a native hotbar/cooldown-hook verification. The existing 56% vulnerable-tank requirement is **retained and inferred**, not a measured HP-damage model. Shield consumption still uses the existing whole-barrier abstraction; Shake's extra shield potency from consumed defensive buffs is not modeled. The party reductions and action-order findings explain reproducible failures without lowering the threshold. Native logs now include status IDs at the damage snapshot, before shield consumption, alongside effective mitigation and requirement.

Return coordinates, early tank crossing, Spirit Taker destinations and knockback geometry were not changed. H2 sorting was not reproduced as a defect: the seven possible Red Ice pairs all put H2 East using the existing priority.

### P4 presentation

Darklit already creates the visible Fragment actor and activates native MapEffect index 0x2E with state 0x00020001, but never spawned the two Visions. CT spawned both Visions, hid its Fragment with DisplayFlags 0x40003 and omitted that crystal MapEffect. CT now follows Darklit's visible Fragment recipe and crystal activation. Both use a shared Vision spawning recipe preserving CT's native appearances, layout IDs, levitation, model states, rotations and positions (-0.71,0,-16)/(0.71,0,-16). Darklit owns and despawns the additional actors with its existing actor list; CT owns them through its world. Tests assert all three identities and appearances; existing Stop/Restart/Leave tests cover disposal. Actual crystal/character layering, height, pose and first-entry rendering require native retest.

### Priority client checks

1. Cold inn and offline entry into DD/Mirror, then Restart and Leave. If rejected, save the specific new per-actor warning. Verify actual red/blue mirrors render; registered invisible sources alone are insufficient evidence.
2. Mirror first spreads: MT/ST on separate boss flanks, melee beyond boss; H/ranged beside blue. Check both rotated red arrangements, second spreads and both Banish patterns from MT/ST/healer/DPS seats. Confirm cones point as the guide, rather than merely observing no deaths.
3. Warrior CT: Shake then Damnation after Spirit Taker; inspect all party mitigation statuses at the wing snapshot. Repeat reversed button order to distinguish consumed Damnation. Preserve the now-verified edge Return and crossing behavior.
4. Both P4 drills: visible crystal **and** both women; Restart with active visuals and Leave without stale actors. Repeat offline.
5. DD: recorded opening, all Holy routes, actual player ice slides/gaze, both Twin orders, Ray. Headless completion does not validate native floor sliding or VFX.

### Final automated validation

- Complete routine optimized Debug suite: **7,835 passed, 0 failed, 2m17** (`/tmp/fluffy-unfuck-routine-final.log`, `fru-unfuck-routine-final.trx`). Includes all catalog drills, CT/P5, original FRU/DSR, recorded DD/Mirror fixtures and failure tests. The subsequently added delayed mirror-slot case is covered separately below.
- Final ordinary Debug contract/lifecycle/Warrior/P4/H2 run: **36 passed, 0 failed, 3s** (`/tmp/fluffy-unfuck-focused-final.log`). Mirror geometry matrix plus startup checks earlier: **195 passed, 6s**. Final routine also repeats the 192 mirror geometry cases.
- Relay **44 passed, 1s**; Python tooling **76 passed, 0.044s**. Solution Debug build: **0 errors**, two existing test warnings. Isolated optimized TRACE Debug build: **0 warnings/errors**. No Release output or installed plugin files were written.
- Firewall ordinary plus optimized TRACE Debug: **308 passed, 3 Windows-only skips, 17s** (`/tmp/fluffy-unfuck-safety-final.log`). Alternate hook failure model recorded separately in `/tmp/fluffy-unfuck-safetyhook-final.log`.
- Previous full stress baseline: **14,976 passed, 7m49**. Current routine workload executes 7,835 cases (plus the separately added slot-delay test), with catalog seed loops reduced from 40 to 4. This is intentionally a scope split, not a like-for-like speed benchmark. Exhaustive tests remain opt-in; see `tests/AnoMech.Tests/EXTENDED-TESTS.md`.
- The first routine repair run had one stale Darklit actor-inventory assertion expecting three visible actors. It now requires five (both bosses, Fragment, both Visions); the final run passes. An earlier broad ordinary Debug attempt was cancelled while running exhaustive coverage; it is not counted as a pass.

Anonymous repair change `wsrwyk` is a child of the completed P2 change `qkqvln`. It is an explicit parent of the existing `towxun` integration (`chore: integration commit`), alongside all five previous parents. The integrated tree equals the feature tree. Local/published master remain `5d6b648b6ea6b1e4d0003494e00e2a363f4691d1`; historical `plkqvn` remains `9810be4fa280ad3da2b74e214e1bd0953d2352ee`. No publication, release preparation, bookmarks, unrelated features or plogons changes were made.

The explicit Extended P2 hard/invisible loops also passed: 2 test cases execute 8 roles × 8 seeds for each P2 drill (128 full runs), 2s. The default settings filter combines with command-line filters; `extended.runsettings` is required to opt in and was verified by this run.

Last production adjustment: corrected the level-100 Bard contribution to 15% using the official job guide's level-98 trait. The 21 affected mitigation/startup/presentation tests passed afterward (`/tmp/fluffy-unfuck-mitigation-final.log`, 971ms). The 7,835-case routine run above precedes that numeric correction; its broad coverage was not repeated for this localized value change. Both final Debug artifacts were rebuilt afterward for the safety rerun.

## Final FRU polish — numbered cleansing and optional mitigation (2026-10-20)

The owner confirms native Diamond Dust and Mirror, Mirror now start and are playable. This pass preserves those fixes, the verified CT tank crossing/outer Return positions, P5 formations and JP/Mana defaults. No encounters were added.

### Wyrmfang 1–4

The [Mana guide's opening assignments and Step 20](https://ffxiv.tuufless.com/mana/fru/04_gaia_and_shiva/) and its `crystallize_time_07.jpg` diagram specify four blue-only numbers, ordered **1 East ice puddle, 2 SE Aero puddle, 3 SW Aero puddle, 4 West ice puddle**. These are absolute East/West, independent of the second Tidal orientation.

The per-run state shuffles 1–4 using the scenario RNG and assigns that permutation only to the four Wyrmfang holders. The roll follows the existing encounter rolls, preserving their seeded results. Neither role priority nor human/bot identity selects a number. Numbers appear with debuffs at 11.8 through the same local overlay `SoftMarkers` used by Ultimate Relativity; Attack4/icon 61204 completes its existing Attack1–3 support. No native party marking or network requests are used. The human's marker remains visible with simulated party hidden. Markers clear after the cleansing window at 41.1, and on world reset/Leave; active-marker Restart tests verify no accumulation.

Opening movement continues to follow each debuff's requirements. At 29.7, after the last spell resolves, blue cleansing routes are reassigned by number instead of debuff subtype. The existing hazard-aware route tables then wait for their own dragon catcher's puddle. They do not search for a nearest or alternate puddle. The same numbering remains consistent for the alternative AI strategy. Three bots never take over the missing human's assignment.

Tests check seeded four-number permutations, red exclusion, every human role, human numbers 1–4, actual cleanse coordinates sorted East-to-West, both Tidal orientations, hidden-party human marker visibility, active marker Restart and Leave. Reservation tests replay a valid human route while staying outside the assigned puddle, with **God Mode disabled**; all three other blue players cleanse, the human's puddle remains available, and contact cleanses it. Never collecting it produces the existing Wyrmfang expiry death. An incorrect early detour to another numbered puddle still dies to the existing Maelstrom hazard. Numbers are a strategy assignment, not a new native debuff: taking another puddle is not assigned an invented instant-kill rule.

### Require Tank Mitigation (default OFF)

The CT setting snapshots into each run and defaults to false. OFF sets only Hallowed Wings' approximate vulnerable-tank mitigation requirement to zero. It does not change attack areas, target selection, front order, non-tank failure, statuses, damage snapshots, knockback, anti-knockback or later mechanics. It is not God Mode.

ON uses the existing **UNVERIFIED 56% practice threshold** and the timed healer/ranged mitigation added in the preceding repair. It requests mitigation validation even if the global mitigation preference is off; local actions must be enabled. Existing diagnostics list active status IDs, effective mitigation and required mitigation at impact, before shields are consumed. An absent effect still requires action/cooldown inspection to distinguish a button that never applied from one that expired or was consumed. No newly measured native damage amount is claimed.

A scheduled-wing regression has the human tank use anti-knockback at the correct time but no personal mitigation: OFF survives both wings; ON reports insufficient mitigation. Existing missed-immunity and non-tank-leading failure cases remain. The now-verified tank recording/crossing coordinates and all mechanic times are unchanged.

### Local recast restoration

Strict CT Start/Restart requests only the current job's relevant defensives through `IZoneControl.ResetPracticeRecast`. The existing `PracticeRecastSession` now captures/tracks these allowed actions as well as its previous actions. Native guard, firewall, job identity, shared-group ownership checks, pause handling and exit restoration are unchanged. There is no periodic or mid-resolution reset. Leave restores the pre-session local cooldown record aged by elapsed wall time; no server cooldown is changed or server-bound action generated.

Pinned Action data: `d9582a624eaec69d45649366dee801c2f3a6e9b3`. Groups below are **sheet CooldownGroup values**; runtime code asks `ActionManager.GetRecastGroup` rather than assuming the sheet's indexing matches native storage.

| Job | 40% action / seconds / sheet group | Party action / seconds / sheet group | Other restored defensives |
|---|---|---|---|
| PLD | Guardian 36920 / 120 / 20 | Divine Veil 3540 / 90 / 15 | Bulwark 22 (90s, group16) |
| WAR | Damnation 36923 / 120 / 22 | Shake It Off 7388 / 90 / 15 | Thrill of Battle 40 (90s, group16) |
| DRK | Shadowed Vigil 36927 / 120 / 21 | Dark Missionary 16471 / 90 / 15 | Dark Mind 3634 (60s, group9) |
| GNB | Great Nebula 36935 / 120 / 22 | Heart of Light 16160 / 90 / 15 | Camouflage 16140 (90s, group16) |

Each job also restores Rampart 7531 (90s, group47) and Reprisal 7535 (60s, group45). These are explicit defensive actions, not a blanket action-manager reset. Eight additional recast-session cases exercise the 40% and party actions through capture/reset/elapsed-time restoration; per-job scenario tests verify the correct reset requests on initial Start and Restart and exclude other jobs' actions. Native hotbar recast behavior remains a client test.

### Mirror staging

The initial 6.05 waypoint used the old inward-facing melee fan, then corrected it at 12.06. It now uses the corrected first-spread formation immediately. The final 12.06 adjustment still follows the boss's actual stop position. MT's separate drag point remains necessary to reposition the boss; its later transition to the spread is retained. A subsequent native report for seed **592545838** exposed a separate red-mirror role swap: the same handedness sign was applied to supports and DPS, so changing the other mirror's side exchanged MT/ST destinations. Tank and healer outside-looking-in flanks are now fixed; only the inner DPS assignments use the other mirror to choose their handedness. The reported seed explicitly checks ST on the Dragoon flank rather than the Monk flank and then completes the encounter. A pure geometry test covers support flanks across all valid orientations, and all 192 damage/survival patterns pass. Cone geometry, targeting, timestamps and Banish were not changed.

### Native retest

1. Blue CT: read 1–4 (including 4), verify red players are unmarked; delay your own cleanse and confirm three bots leave your puddle available. Repeat North/South second Tidal and hidden party. Restart while numbers are visible, then Leave.
2. CT OFF: correctly execute Return and anti-knockback without tank mitigation; insufficient mitigation alone must not kill. ON: time the supported defensives, inspect active effects at impact, then Restart and check their hotbar recasts restore immediately. Repeat all four tank jobs where available; Leave must restore normal local recasts.
3. Mirror: confirm direct initial melee/ranged routes, necessary MT boss drag, both reflections and both Banish variants. Existing native P2 arena/playability fixes should remain intact.

No native verification of this new overlay/recast behavior is claimed from headless tests.

### Polish validation and integration

- Final bounded routine optimized **Debug**: **7,873 passed, 0 failed, 2m22** (`/tmp/fluffy-polish-delivery.log`, `fru-polish-delivery.trx`), including the reported Mirror seed and final strict trailing-tank correction. Existing Extended settings/categories were not changed.
- Focused CT/Mirror/recast/marker/lifecycle suite: **416 passed, 31s**. The final Mirror matrix and flank tests passed **194 cases in 6s**, and the screenshot-specific ST/Dragoon assertion passed (350ms). Additional scheduled-wing ON/OFF checks passed (761ms); strengthened eight reservation/expiry cases passed with God Mode disabled (2s). All these cases are included in the final routine run.
- Solution Debug build: **0 errors**, two existing test warnings. Isolated optimized TRACE Debug build: **0 warnings/errors**. No Release build or installed-output write.
- Final ordinary and alternate hook failure safety models: **308 passed, 3 Windows-only skips each, 18s/17s** (`/tmp/fluffy-polish-safety-final.log`, `/tmp/fluffy-polish-safetyhook-final.log`). Relay **44 passed, 1s**. Packet guards and native recast isolation gates were not weakened.
- Anonymous follow-up `zxmprt` is based on the preceding repair `wsrwyk`. It is incorporated into the existing unpublished `towxun` integration, preserving all six previous parents. No new bookmarks, master movements, publication, historical merge edits or plogons changes.

## Follow-up: cleansing route avoidance and ranged support flanks

The numbered cleanse destinations were correct, but a destination did not reserve the path to it. Straight-line movement could cross another player's live puddle, including a human's deliberately uncollected puddle during regrouping. CT now builds a personal obstacle field for each bot, retaining shared arena obstacles and adding every live puddle except the bot's actively assigned cleanse target. The original red catcher remains exempt from its own puddle, matching the encounter's existing consumption rule. The avoidance radius is 1.6y plus the existing navigator steering clearance, outside the unchanged 1y consumption radius. This is navigation clearance, not a change to damage or cleansing geometry.

The existing navigator handles detours; CT no longer reissues an identical puddle destination every frame, which would reset steering-side continuity. Personal fields clear with run state and revert to the shared field when no puddles remain. Manual player input is not constrained. Each consumption now logs catcher, consumer, assigned number, position and Wyrmfang state to identify accidental pickups during client retesting.

The seed 592545838 regression also checks H1/H2 on their fixed opposing outer flanks and D3/D4 inside. The support-flank geometry test already covers every valid mirror orientation: healer supports do not exchange sides with the mirror-dependent DPS adjustment. No further Mirror production change was necessary for this follow-up.

Focused validation: 376 cases passed in 32s, plus eight strengthened human-reservation cases in 2s. Coverage includes actual puddle consumers in all 128 role/orientation/difficulty cases, all four human blue numbers, three bot cleanses while the human waits, correct later human cleansing, preserved tank Return snapshot positions with a retained human puddle, active-puddle Restart/Leave, and personal-obstacle steering at 30/60/120Hz. These are headless results, not native rendering or movement verification.

Native retest priorities: hold the human's numbered puddle while other bots cleanse and regroup; confirm detours rather than accidental pickups in both Tidal orientations. Check the unchanged early tank crossing and outer-edge Return recording, restart with live puddles, and inspect consumption diagnostics if a wipe recurs. At red mirrors confirm H1/H2 remain on their fixed outer sides while D3/D4 adjust inside, including seed 592545838.

Final follow-up validation: routine optimized Debug **7,879 passed in 2m23**, with Extended coverage still opt-in (`/tmp/fluffy-puddle-routine.log`, `fru-puddle-routine.trx`). Solution Debug build passed in 4.70s (two existing test warnings); isolated optimized TRACE Debug build passed in 3.10s without warnings. Ordinary/alternate hook safety models each passed **308 tests**, with three Windows-only skips, in **18s/19s**; relay **44 passed in 1s**. Anonymous feature `lunpun` is incorporated into `towxun` with all seven preceding integration parents retained. Published/local master, historical `plkqvn`, release metadata and plogons were unchanged. No Release build or installed-output modification was performed.
