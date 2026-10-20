# FRU implementation and validation report

Latest native repair findings and validation: [Operation Unfuck FRU](fru-native-playtest.md#operation-unfuck-fru--native-retest-corrections-2026-10-20). Native retesting remains required.

Latest expansion: [FRU Completionist — edge Return, Diamond Dust and Mirror, Mirror](fru-completionist.md). This supersedes the earlier inward CT coordinates and hidden DD status.


## Native playtest corrections — 2026-10-20

This section supersedes the original delivery's standalone Akh Morn entry and menu defaults. Native retesting of these corrections is still required.

### Paradise: reproduced cause and correction

A human MT standing at relative North reproduces the reported deaths. With rotation 0 and seed 72, light-first kills H1/D1/D3 at 16.55; dark-first kills H2/D2/D4 at 16.47. Death action is the first Wings cleave (40314/40315), not tower failure. The previous all-bot MT stood diagonally NW/NE, turning the cone away from a formation that split DPS across both southern sides. That formation only worked with the bot-specific diagonal facing.

The Mana formation now keeps MT relative North, places all four DPS on the wing-dependent safe flank, and puts both healers on the safe side within the South tower. Dark-first ST takes a near position on the safe flank; light-first ST remains far SE. The second wing retains its mirrored tank/tower choreography. All positions rotate with the first tower defining relative South. Nearest/farthest tether searches now exclude that wing's cleave tank; other eligible players remain able to steal the bait and fail the mechanic. Damage, vulnerabilities, tower requirements and cleave geometry were not weakened.

The [Mana guide](https://ffxiv.tuufless.com/mana/fru/05_pandora/) diagrams and instructions define the strategy. The guide describes 225 degrees, while both inspected BossMod implementations and the inherited executable action use a 240-degree cone (120-degree half-angle, offset 60 degrees). This patch retains that independently corroborated geometry rather than shrinking it to fix positioning. The exact native angular boundary remains an evidence discrepancy to verify in game.

The human tank matrix covers MT/ST × three rotations × both wing orders × both tower orders × Easy/Hard: 48 cases. It checks survival through both wings, actual tank physical-vulnerability recipients, absence of physical vulnerability on non-tanks, and alternating ST/MT tether ownership. Existing all-role full-mechanic runs, wrong soaks, optional invulnerability, sequence, active-tower Restart and delayed offline entry tests remain in place.

### CT recording route

The [Mana guide Steps 21–27](https://ffxiv.tuufless.com/mana/fru/04_gaia_and_shiva/) distinguish a near-center Y recording formation from the later far intercardinal tank spreads. The recording arms remain relative (-3,-4.5) and (+3,-4.5), rotated 180 degrees for a South-origin second wave; these are simulator placements, not measurements from a scaled diagram. They retain front-four ordering and safe second-wing knockback landings. Step 25 still uses separate far spread positions.

Mana tanks now leave directly for their recording arms at 40.55, immediately after the second north/south strip's 40.5 snapshot, cancelling pending assignment-route moves. They no longer wait for the 41.1 party regroup. All attack and Return timings remain unchanged. Every role/corner/slow-pattern/difficulty matrix run now asserts both tank positions at the actual 44.8 Return snapshot and continues through Spirit Taker, both wings and final damage. Timing provenance remains inherited simulator scheduling plus pinned Action-sheet cast durations, not a newly observed native log.

### Selection and practice menu

Region derives separately from the strategy convention: Mana/Elemental strategies belong to JP. JP is listed first where supported; Mana strategies sort first inside their region and display their convention explicitly. Original strategy indices and multiplayer identities remain unchanged. Per-scenario strategy identities are serialized in configuration, preserving explicit NA choices through reopening and switching scenarios. Fresh and legacy configurations without this new field default to JP/Mana where available; scenarios offering only another region remain usable.

The redundant standalone P5 Akh Morn catalog entry, wrapper and unused isolated entry method were removed. Akh Morn remains in full Fulgent Blade and the continuous phase; its role/difficulty and wrong-light-party tests now exercise Fulgent Blade itself.

### Prioritized native retest

1. Paradise first tower South: play MT at North, then ST; test both wing orders. Confirm only the intended tank takes each cleave, both healers survive the South tower, DPS stay on the safe flank, and tethers alternate ST→MT. Repeat a rotated tower pattern and both tower orders. Check boss facing and native cone edges against the simulated damage.
2. CT North- and South-origin second Tidal patterns: watch tanks cross promptly after line two, inspect the **recorded** Y positions before spread, then verify Spirit Taker, first-wing immunity and second-wing landings in Easy/Hard.
3. Fresh/legacy settings show JP and Mana; explicitly choose NA, switch scenarios and reopen the window/plugin to confirm it persists. Confirm standalone Akh Morn is absent while Fulgent and sequence still finish with it.
4. Repeat Paradise Restart with towers/wings active and both new drills through inn/offline cold entry, hidden party, Pause/Resume and Leave. Native rendered effects and facing still need the owner's client check.

### Correction validation and Jujutsu

- Full optimized **Debug** unit/scenario regression: **14,488 passed**, 0 failed; catalog uses the checked-in 40 random seeds per strategy. `/tmp/fluffy-playtest-full.log`; `tests/AnoMech.Tests/TestResults/fru-playtest-full.trx`.
- Final focused CT/P5/configuration/lifecycle run: **579 passed**, including the final additional negative tether-bait cases and tether-recipient assertions. `/tmp/fluffy-playtest-focused-final.log`; `tests/AnoMech.Tests/TestResults/fru-playtest-focused.trx`.
- Relay: **44 passed**. Python tooling: **76 passed**. `/tmp/fluffy-playtest-relay.log`, `/tmp/fluffy-playtest-tooling.log`.
- Both firewall/hook-failure models: **308 passed, 3 Windows-only native-hook skips each**, against ordinary Debug plus isolated optimized TRACE Debug. `/tmp/fluffy-playtest-safety.log`, `/tmp/fluffy-playtest-safetyhook.log`. No safety allowlists or packet guards were weakened.
- Solution Debug build: success, 0 errors; two existing test warnings (CS8602/CS0067). Optimized TRACE Debug build: success, 0 warnings/errors. `/tmp/fluffy-playtest-build.log`, `/tmp/fluffy-playtest-safety-build.log`. No Release build, installed plugin write, version bump, publication or plogons change.

Anonymous correction change **suxsukpzsnrz** is incorporated in the existing shared integration **towxunptvwlp**, retaining original parents **uwrxzomlkwxz**, **towkrqovnplv** and **yuzulstmpsqq**. Anonymous **xowlqqopmrxu** preserves the former integration's seven-file validation/registration/report delta as the correction's baseline, avoiding a cycle while retaining a reviewable correction diff. Integration conflicts were resolved explicitly by hunk: only obsolete standalone Akh Morn registration/case and the old count were removed; old report/test content was preserved alongside additions. Final integration and correction trees compare equal. Existing feature bookmarks remain; no new bookmark was necessary.

Local master and master@origin remain **5d6b648b6ea6b1e4d0003494e00e2a363f4691d1**. Historical **plkqvn / 9810be4fa280ad3da2b74e214e1bd0953d2352ee** is unchanged. The operation checkpoint is `/tmp/fluffy-playtest-jj-checkpoint.log`; the preserved original integration commit is `ed7a5c7eb28d5a93c578a4a89f5a5cd64ada8a77`. No concurrent feature changes were discarded.


This is a playable implementation candidate with native-client verification outstanding. It is not a claim of complete retail fidelity or release readiness. The original nine custom drills, their Mana/Elemental strategies and the shelved Diamond Dust registration were preserved. No public version or release metadata was changed.

## A. AsuMech comparison

Inspected current AsuMech `d3f4f3e07a9af49d66a2637ef3f329c76fd174d9` and AnoMech `6aec4a5940df56ebc4f70a2fdddeceb31c7848b8`; public master tips were verified by read-only GitHub REST requests on 2026-10-20. Upstream is an actual ancestor of AsuMech. All seven relevant imported FRU source folders are byte-identical between those references. They already existed in FluffySim; they are not new AsuMech inventions.

The [comparison](asumech-comparison.md) covers all five original FRU and four DSR segments, including differing scenario boundaries, exact scheduler differences and native arena contracts. Existing measured FluffySim timelines and native P1–P4/DSR initialization were retained. No Asu-only timeline constant was promoted to an observation. A narrowly adapted helper-cast presentation fix queues ActorCast until the packet-spawned native actor arrives, preserving the logical deadline and omitting stale/cancelled casts. It does not delay damage or queue action effects with broken sequence linkage.

## B. Crystallize Time

The existing imported encounter now offers Mana/Nukemaru alongside its preserved NA strategy index. Mana uses H1→MT→ST→D1→D2→D3→D4→H2 red-pair priority, the central Y Return formation, relative role spreads, first-wing anti-knockback and ST-solo final Akh Morn. Morn Afah closes the drill. Existing hourglasses, deterministic red/blue assignments, dragons, puddles, debuff expiry failures, Tidal Light and rewind are reused. Hungry-dragon failure now uses corroborated Joyless Dragonsong40242.

CT now uses the verified main P4 phase infrastructure. Native anti-knockback statuses and the local six-second practice button are supported. Wing overlap uses the existing mitigation model to permit a heavily mitigated tank while retaining lethal vulnerable non-tank hits. The approximate 56% mitigation threshold is a training-model inference, not measured retail damage.

The [CT evidence register](fru-crystallize-time.md) records every stage and source/confidence. The [capture inventory](fru-native-capture-inventory.md) parsed 32 local logs (~3.8GiB): four interrupted opening cast records corroborate a 9.700-second bar, but no completed CT sequence and no P5 cast/effect records exist there. Remaining timings are inherited or independently corroborated, not newly measured.

## C. P5 Pandora

P5 uses a shared main-zone phase with one-shot readiness-gated native platform MapEffects, known earlier-layer suppression, the existing entrance barrier lifecycle, weather108 and BGM20099. Installed sheets confirm territory/BGM assets; actual rendered P5 scenery, weather and collision require a client check. Imported P5 entries use this same phase. [Arena evidence](fru-pandora-arena.md).

- Fulgent retains native seam event objects, cast-based exalines, all rotation/mirror configurations, tank autos and paired Akh Morn resolution. Its existing light-party parity already matches Mana and is labelled accordingly.
- Akh Morn remains the Fulgent Blade follow-up and part of the continuous sequence, using shared targeting/damage actions and Mana light parties; the redundant standalone entry was removed after playtesting.
- Paradise uses Mana healer-first towers, then melee together on the cleaved far side and ranged together on the near side. Tower rotation, second-tower side and wing order are forceable. Towers require exactly two occupants.
- Polarizing has an isolated entry and the bundled transition. Each side independently selects its nearest bait, with four T→M→R→H rounds, color resistance on front baits, side swaps, overlap vulnerability and fixed-direction lethal echoes.
- Pandora's Box has an isolated entry, native cast and existing simulation-only tank LB pathway. Each recipient must actually hold active LB3 protection at snapshot. Early, late, missing and lower-level LB fail. The local gauge is restored on teardown.
- Continuous practice uses one Pandora across three Fulgent/Akh Morn and two Paradise/Polarizing sequences plus Box. The second Paradise→Polarizing gap differs from the first. Stage helpers are cleaned between fragments. Optional terminal enrage really wipes; default practice stops before the lethal ending. No boss HP/DPS race or automatic boss kill is fabricated.
- [Optional tank invulnerability practice](fru-pandora-invulnerability.md) validates human tank timing without claiming invulnerability is mandatory in ordinary clears or inventing a universal mitigation percentage. It accepts native statuses or the existing ten-second practice button and preserves prior wrong-role/vulnerability failures.

[Full P5 mechanic register](fru-pandora-evidence.md).

Known scope limits: tank facing/target roles are scheduled, so Provoke/Shirk and a full threat table are not validated. Fulgent collisions retain the inherited lethal-hit model rather than simulating survival/resistance after a single exaline. Ordinary wing damage does not validate a measured mitigation budget. HP balancing, final melee-LB damage race, interstitial auto-attacks and exact retail boss movement between continuous-practice fragments are not modeled. The sequence resets boss pose at fragment boundaries. These limitations must not be described as experimentally verified fidelity.

## D. Validation and isolation

Final executed results and artifact paths are recorded below. All builds use Debug. Optimized scenario output/intermediates and optimized TRACE safety artifacts are isolated from ordinary Debug and all Release output. Tests do not run against installed binaries.

The new lifecycle suite exercises pause/status/position freezes, two same-seed restarts from active mechanics, idle-time resets, eight seats, offline cold readiness, delayed actors and Leave cleanup. Paradise clears tower wrappers on Run so old occupancy cannot leak into a restarted drill. Core queued-cast tests cover actor arrival, remaining omen duration, cancellation, expiry, release and despawn.

Firewall safety tests establish the modeled client/server invariants and architecture restrictions. They are not a live network capture and do not prove all native visual behavior. No packet-isolation mechanism was weakened or bypassed.

### Executed validation

| Check | Result | Local output |
| --- | --- | --- |
| Integrated solution, ordinary Debug | Passed; 0 errors, 2 pre-existing test warnings (CS8602/CS0067) | `/tmp/fluffy-debug-build.log` |
| Full scenario/core suite, default 40 random seeds per catalog strategy | 14,503 passed, 0 failed; 7m6s | `/tmp/fluffy-full-tests.log`, `tests/AnoMech.Tests/TestResults/fru-full.trx` |
| Extra wrong-color Polarizing resistance regression | 1 passed after the full-suite assembly was built; production unchanged | `/tmp/fluffy-polarizing-resistance.log` |
| Focused FRU/native/lifecycle matrix | 535 passed, 0 failed | `/tmp/fluffy-final-focused.log`, `tests/AnoMech.Tests/TestResults/fru-final-focused.trx` |
| CT randomized stress | 100 seeds for each of NA and Mana plus 142 CT unit/matrix cases;144 NUnit cases passed | `/tmp/fluffy-ct-stress.log` |
| Relay | 44 passed, 0 failed | `/tmp/fluffy-relay-tests.log` |
| Python tooling | 76 passed | `/tmp/fluffy-tooling-tests.log` |
| Ordinary + optimized TRACE Debug safety, default backend | 308 passed, 3 Windows-only native-hook cases skipped | `/tmp/fluffy-safety-tests.log` |
| Same artifacts, SafetyHook failure model | 308 passed, 3 Windows-only native-hook cases skipped | `/tmp/fluffy-safetyhook-tests.log` |

The initial resumed focused failures were fixed: the bait test now stops native simulated movement before asserting exact aim, and Paradise Run clears all old tower wrappers/aim state. The strengthened restart test resets twice from active mechanics and rejects any pre-spawn tower occupancy emissions. An initial safety run correctly rejected stale DLL timestamps after the Jujutsu split; fresh x64 ordinary Debug and isolated TRACE Debug binaries were rebuilt without bypassing the guard.

## E. Jujutsu checkpoint and integration

Initial baseline remains published `master@origin`/local `master` at `5d6b648b6ea6b1e4d0003494e00e2a363f4691d1`. Initial working copy was clean. Pre-split recoverable operation: `e5b1fbca9da9` (snapshot includes the restored restart fix, stationary bait test and research documents). Final feature IDs and integration ancestry are recorded below. Historical integrations, `plkqvn`, tags and released history are unchanged. Only FluffySim was modified; no plogons edit, push or publication occurred.

### Final feature topology

| Bookmark | Change ID | Scope / parent dependency |
| --- | --- | --- |
| `codex/asumech-comparison` | `uwrxzomlkwxz` | Nine-drill audit, capture inventory and pending-helper cast correction; published baseline |
| `codex/fru-crystallize-time` | `towkrqovnplv` | Mana CT, anti-knockback practice, ending and tests; published baseline |
| `codex/fru-pandora-arena` | `vlyutmzuokpm` | Shared native P5 initialization; published baseline |
| `codex/fru-pandora-fulgent` | `rrqtlmokrkuu` | Fulgent and isolated Akh Morn; arena parent |
| `codex/fru-pandora-paradise` | `nzkvrtrxqnvt` | Mana towers/wings, isolated Polarizing, independent side baits and restart cleanup; arena parent |
| `codex/fru-pandora-sequence` | `yuzulstmpsqq` | Local LB3, Box, shared-actor phase sequence and invulnerability UI; Fulgent + Paradise parents |
| `codex/fru-integration` | `towxunptvwlp` | Exact description `chore: integration commit`; audit + CT + sequence parents, with registry and cross-feature regression coverage |
| `codex/fru-mission-checkpoint` | `xuoxsm` | Preserved pre-split full tree, commit `c3d47f5593ec725592efa4ad951c7b77aed3e72d` |

All six feature tips were checked with `bookmarks(glob:"codex/*") & ancestors(<integration>)`, excluding the recovery-only checkpoint. The merged tree was byte-identical to the checkpoint before final reporting updates. No merge conflicts occurred. The historical `plkqvn` still resolves to `9810be4fa280ad3da2b74e214e1bd0953d2352ee` with six parents. Local `master` and `master@origin` still resolve to `5d6b648b6ea6b1e4d0003494e00e2a363f4691d1`. No release-preparation child was created.

## F. Native playtest gate

Use the [prioritized checklist](fru-native-playtest.md). First cold-enter P4/P5 from both inn and title-screen offline; inspect rendered arena/weather/platform, barrier collision, boss model and eight names. Then test CT dragons/puddles/Return/wings, Fulgent glows/line snapshots, Mana towers and both wing orders, all four Polarizing rounds and deliberately mistimed LB3. Finally pause, restart during active hazards, change phases, hide bots and Leave; confirm all native state is restored. Preserve captures locally for follow-up calibration without committing player identities or game binaries.
