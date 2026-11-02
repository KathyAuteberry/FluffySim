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
The original port inherited AsuMech's unverified weather 45, which selected the
P2 arena in the client. P7 now uses environment 47; the resource evidence and
correction are recorded below. No P7 clear was found in the local ACT logs during
the initial port to independently verify the remaining timing/damage assumptions.
Existing P2/P3/P5 environment policies and packet-filter/entry safety behavior are
preserved.

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

## P7 arena correction — 2026-11-02

The first native playtest showed P2 scenery. The port's weather 45 was an
unverified inherited assumption, not evidence of P7's environment. A 2D map
Director update to map 765 does not select the P7 scene geometry.

Read-only extraction from installed game build `2026.10.16.0000.0000` establishes:

- Territory 968's LVB selects `genv_r1fz.envb`. Its ENVS weather table includes
  47 at file offset `0x8c`; 48 is absent. Weather 45 is a separate profile.
- The central P7 ground is BG object 8609070 in layer 60878, model
  `bg/ffxiv/roc_r1/fld/r1fz/bgparts/r1fz_a0_grd04.mdl`, at approximately (100,0,100).
  Its object-set resource `obset_r1fz_bg_01.obsb` disables it for 45 and enables it
  for 47. P2 ground object 8600331 in layer 61049, `r1fz_d0_grd01.mdl`, has the
  opposite selection in `obset_r1fz_bg_04.obsb`.
- The four native collider objects also distinguish the two selections:

| Layer | Object | Weather 45 | Weather 47 |
|---|---|---|---|
| 65362 | 8626397 | off | on |
| 5210 | 9279002 | on | on |
| 5212 | 9279003 | off | off |
| 5644 | 9303685 | on | off |

The [physis ENVS parser](https://docs.rs/physis/latest/src/physis/envs.rs.html)
defines the weather-section and relative-offset structure. Each relevant OBSB
has a type-29 element at `0x1b4` for 45 and `0x20c` for 47; its flag changes
between `0x00000032` and `0x00010032`. Interpreting bit 16 as activation is
corroborated by all four colliders in the existing recorded P2/P3 fixtures. This
is derived shipped-resource evidence, not a newly observed P7 runtime snapshot.
The parser's comment about only version 6 loading is not applicable to these
installed version-4 OBSBs and is not used as evidence.

Resource SHA-256 identities:

| Resource | SHA-256 |
|---|---|
| `r1fz.lvb` | `28c131788dcc3a5174db53404623bf9498f38a581e5db310230c0b847d845976` |
| `bg.lgb` | `2166bafd37798f9f3639d9756797ea196e21b240c318ec4de3776ec9ffff7541` |
| `genv_r1fz.envb` | `30ce56e14099f2bfc6e60df586d819ffa130b2e483290f5916bce89cfdeca8fa` |
| `obset_r1fz_bg_01.obsb` | `6c750d48fcdcc195c6e32ad3cc40d204463c497dcab74fa288d91e8416c773cc` |
| `obset_r1fz_bg_04.obsb`, `col_03.obsb` | `41cc910a510b7f6237cef6cb06e2e65fcf8b91928bcd4dd78e8028750def7084` |
| `obset_r1fz_col_01.obsb` | `2b7c24206d11c7f560bf196371bc26b680fdb9faf29b12a5fd87d649b097fdf3` |
| `obset_r1fz_col_02.obsb` | `bdeed0361a8869e21100e12255b0471f6c26c15e836be79f3af1064f97a92deb` |
| `obset_r1fz_col_04.obsb` | `9965042ad40824058deaa8861e080fe093a02bcbb6c4c97043ad9e8414b7a7b9` |

Lumina 7 parser identity was inspected with REA Evidence
`ev_060ae8fe7133695b9ab6d502997730bcff5ba76fea7c4490f21eb1e94d716373`,
DLL SHA-256 `43506bbccdff3499ad37c6c5120eb581148efc1cb70dc9b6f93ec1e8bbb0f9b6`.
The actual LGB/object-set extraction used that installed Lumina parser and a
read-only ENVS decoder. No game resources or SDK binaries are included here.

The correction changes P7's phase selector to 47 and admits it to the existing
ready-layout weather finalization in both inn and offline modes. Completion
requires the new floor on, P2 floor off, and all four colliders loaded and in
47's selected states. This is a critical-object verifier, not an invented
whole-scene fingerprint. It never writes layer activity, repeatedly enforces
weather, or changes the packet filter or entry guard.

Regression coverage rejects P2 geometry even after the rendered selector becomes
47, rejects stale/missing colliders and either incorrect floor, verifies one
native transition per layout generation, restart/Leave cleanup, and checks that
an actual P7 scenario run never requests weather 45. Native confirmation of the
corrected presentation in both inn and offline modes is still required.

Correction validation: Debug plugin/test builds pass; 284 focused P7 and DSR
arena/environment checks pass. Safety passes 318 checks with four environment
skips on each modeled backend, against ordinary and isolated optimized TRACE
Debug binaries. No Release output, live game state, release metadata or bookmarks
were modified during the correction.
