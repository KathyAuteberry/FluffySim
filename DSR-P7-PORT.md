# Elemental P7 Dragon-king Thordan port — 2026-11-02

Adapted from [AsuMech](https://github.com/Asunnya/AsuMech), P7 implementation
`180e9b454c55` by Asunnya (`develop-asunnya@outlook.com`), inspected in the refreshed
checkout at release commit `98d630172edb25becbf427979bf8d634cbcfa570`.
The upstream source and attribution remain available; this is an adapted feature,
not a merge of unrelated AsuMech changes.

Strategy reference: [Tuufless Elemental P7](https://ffxiv.tuufless.com/elemental/dsr/07_dragonking_thordan/)
and the matching Elemental DSR macro in the local Tuufless checkout.

## Implemented practice

- Alternative End transition, Dragon-king actor, phase BGM, sword glow and all eight
  Flames/Ice reads across three Edge cycles.
- Static south exaflare dodge, with all 512 heading combinations checked geometrically.
- All three Akh Morns use Elemental 3-3-2: H1/D1/D3 front-left, H2/D2/D4
  front-right, MT/ST rear. AsuMech's later 6-1-1 movements and bot invulns are removed.
- Trinity nearest baits rotate D1/D2, D3/D4, H1/H2. Actual tank ownership follows
  local Provoke; bots Provoke after each pair, and a human incoming tank is prompted.
  A missed swap leaves resistance stacks on the actual recipient and fails naturally.
- Both Gigaflare sequences, then the third Akh Morn and Morn Afah's Edge cast.
  First enrage tower assignments are H1 left, H2 right, MT rear.
- Random, all-Flames and all-Ice practice settings. Deterministic test overrides
  retain per-mechanic elements, headings and Gigaflare orders.

The source's first 24 seconds are trimmed; all timeline literals here are relative
to the playable start. Main boss casts and helper cast bars read Action sheet
cast times. Flames and tower radii also read the sheet. Native spawns use
FluffySim's typed spawn configuration, without importing AsuMech's raw spawn
packet builder or game-data interface changes. BNpc 12616 has ModelChara 2969
and no NpcEquip row in the pinned real sheet, so no new gear override is required.
Helpers are staged before acting and retained for effect completion. Arena
Director updates run through instance events for host and peers. Existing
Tuufless waymarks are preserved.

## Deliberate scope and remaining uncertainty

Practice ends immediately before the first Morn Afah towers resolve, matching the
source's practice boundary. Boss HP, the DPS race and successive lethal enrage
towers are not simulated. The new entry remains marked incomplete until native
presentation is verified, consistently with the existing imported DSR phases.

Gigaflare's 18y lethal approximation, a third same-element Trinity being lethal,
and solo/empty tower failure costs remain source-derived practice approximations.
The P7 weather remains the source's unverified Thordan-arena weather 45.
No P7 clear was found in the available local ACT logs to independently verify
those details. Existing P2/P3/P5 environment policies and all packet-filter/entry
safety behavior are unchanged.

## Client verification

Check both inn and offline entry, including entry directly after another DSR phase:

1. Alternative End transitions into the circular P7 arena; the wall, boss model,
   scale, target ring, targetability, cast bars, BGM and Tuufless waymarks appear.
2. Flames/Ice sword glows and all three exaflare headings are visible before
   resolving. Verify the static south dodge and the first Trinity during exaflares.
3. Every Akh Morn shows two red and one blue tower, with 5/6/7 hits. All three
   use the same 3-3-2 groups. Check stepping in after Flames for healing range.
4. Both Gigaflare sequences show the correct order and allow the opposite-side
   dodge with either sword element.
5. Play each tank with local Provoke, confirm the displayed target changes,
   and deliberately miss a swap to confirm the resistance failure.
6. Check seven bots, Easy/Hard, hidden party, spectator, God Mode, pause/resume,
   Stop/restart and Leave during the transition and each major mechanic.
7. Check host/peer visuals, casts, statuses and cleanup; headless lifecycle tests
   do not prove native multiplayer rendering.

Only Debug artifacts are built during development. No release version, tag,
master bookmark, published asset or plogons metadata is changed.

## Automated validation

- Ordinary Debug plugin build passes. Isolated optimized Debug artifacts use
  `bin/fast-Debug` / `obj/fast-tests-Debug`; optimized TRACE safety input is separate.
- Final full plugin run: 8,790 passed, including all 249 dedicated P7 checks and
  catalog-wide multiplayer/spectator cases. Its only failure was the spectator
  catalog's old expected count (28); that assertion was updated to 29, rebuilt
  and passed in a focused rerun. No production behavior changed afterward.
- Relay: 67 passed. Python tooling: 76 passed.
- Safety: 318 passed and four environment-dependent skips on each modeled hook
  backend, against both ordinary and optimized TRACE Debug plugin binaries.
- The SDK's source-control query is disabled for these local builds because its
  Git reader does not support this checkout's `relativeworktrees` extension.
  No repository configuration was changed to work around that build-tool issue.
