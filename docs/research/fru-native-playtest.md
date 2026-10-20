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
