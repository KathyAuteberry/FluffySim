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
- [ ] Diamond Dust stays hidden from the public menu.
- [ ] Run representative CT/P5 seeds on Easy and Hard with visible bots, then hidden bots. All seven bots should complete their duties without immunity shortcuts.
- [ ] Pause immediately before snapshot/rewind/tower/line events; wait, resume and verify logical timing does not advance. Note any independently running native animation separately.
- [ ] Verify God Mode, local invulnerability and anti-knockback behavior; switch them off and reproduce a real mechanic failure.

Do not mark an unsupported mechanic or unimplemented entry point as passing. Record it as missing and retain the release gate until resolved. Packet isolation and teardown tests supplement this checklist; native visuals alone cannot prove absence of server-bound traffic.
