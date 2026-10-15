# DSR implementation roadmap

## Current upstream integration — 2026-10-15

See [integration decisions, exact source commits, recovery checkpoint and manual checks](UPSTREAM-INTEGRATION.md). The runtime now uses upstream InstanceSession/ScenarioSession, packet-handler spawning and per-object motion, combined with Evernow PRs 35, 38 and 50. FluffySim version remains 1.2.0.0. Historical implementation notes below describe earlier architectures; the integration document supersedes their old ZoneSession paths and teardown assumptions. Debug builds only; no installed Release output was changed.


## Development build workflow (current)

Use `dotnet build -c Debug` and run automated tests with `-c Debug`. Debug output
must remain in Debug-specific `bin` / `obj` trees (including `bin/x64/Debug`). **Never build Release during development,
or copy/publish/modify installed Release output.** Dalamud watches that output and
hot-reloads on changes, freezing the running FFXIV client. The user personally
compiles Release when ready to playtest. Earlier Release validation results below
are historical; older manual-build instructions are user-only, not agent actions.


## Coverage and validation

| Fragment | Status | Scope |
|---|---|---|
| P2 arena | Manually validated in FFXIV | Territory 968 loads correctly and is traversable without an additional layout preset. |
| Strength of the Ward Part 1, Elemental | Manually validated in FFXIV | Complete steps 1–5, including the game's fifth Heavy Impact hit. Registered in the normal Release sidebar. |
| Strength of the Ward Part 2, Elemental | Manually validated in FFXIV | Independent full steps 1–4, including crossed tethers, body-language tower communication, six towers and six Bladedance pulses. |
| Sanctity Part 1: Sacred Severs, Elemental | Manually validated in FFXIV (user report) | Full four-jump fragment, both rotations, role-partner swaps, double gaze and eighteen Bright Flares. Provisional jump damage model. |
| Sanctity Part 2: Meteors, Elemental | Experimental; full headless sequence implemented, native playtest pending | Standalone cardinal swaps, ice pairs, 810 outer configurations, both tower sets, seven clockwise drops per target and Faith Unmoving. Reference-derived timing and damage simplifications. |
| Other DSR fragments | Not researched | Outside this iteration. |

FRU Light Rampant and Ultimate Relativity remain registered and unchanged. Diamond Dust remains paused.

## Evidence

- Strategy: [Tuufless Elemental P2](https://ffxiv.tuufless.com/elemental/dsr/02_thordan/), local `_elemental/dsr/02_thordan.en.md` and its five Part 1 diagrams. MT/H1/D1/D3 use the W-to-NE safe sector; ST/H2/D2/D4 use the opposite sector. Healers occupy the front of their triangles.
- Local October 10 IINACT recording: **65 territory-968 pulls, 50 Strength sequences, 48 initial resolutions, 46 complete Part 1 resolutions**. `tools/dsr_strength_evidence.py` produces sanitized NPC/position/action timelines. The test fixture contains public game IDs, relative positions/times and pull ordinals; no player names or actor identifiers.
- BossMod `DSW2Enums.cs`, `P2StrengthOfTheWard1.cs`, `HeavyImpact.cs`, `DSW2States.cs`: independent identities, charge rectangles (52y by 16y), 5y spreads, five 6y-wide expanding rings, 15-degree Mercy half-angle. BossMod is BSD-3-Clause; no source or assets were copied.
- Splatoon P2 preset independently identifies territory 968, NPC names 3632/3636/etc., Mercy actions 25544/25545 and Heavy Impact cast 25558. Overlay cone sizes are drawing choices; the mechanic uses BossMod's 15-degree half-angle.
- Local native DSR replay (session recorded October 11 at 00:16; game build 14006063): repeated 656-byte NpcSpawn records verify demihuman equipment and weapon identities. Typed CommonSpawnData fields map to these records. ActorControl PlayActionTimeline verifies 7737 (hide/warp), 7747 (arrival/preparation), 4573 (Guerrique departure). ActionEffect headers verify native release locks and caster-target presentation.
- Existing pinned game sheets verify BNpcBase, ModelChara and Action rows. Cast times and AoE shapes are read from those sheets, not duplicated.

### Verified registry

| Actor | BNpcBase | BNpcName | ModelChara |
|---|---:|---:|---:|
| King Thordan | 12604 | 3632 | 3269 |
| Ser Vellguine | 12633 | 3636 | 3268 |
| Ser Paulecrain | 12634 | 3637 | 3268 |
| Ser Ignasse | 12635 | 3638 | 3268 |
| Ser Guerrique | 12637 | 3641 | 3268 |
| Ser Adelphel | 12601 | 3634 | 3268 |
| Ser Janlenoux | 12632 | 3635 | 3268 |
| Ser Grinnaux | 12602 | 3639 | 3268 |
| Ser Hermenost | 12636 | 3640 | 3268 |

`DsrActors` includes the recorded head/body/leg equipment, mainhand weapons and PLD shields. Thordan's recorded model attributes are 16 and animation state is 1. Optional native appearance fields leave existing FRU configurations intact. Meshless helper 9020 supplies the native attack effects.

| Action | ID | Receive time from Strength cast start (representative median) |
|---|---:|---:|
| Strength of the Ward | 25555 | Cast 0; release about 3.97s |
| Lightning Storm visual | 25548 | Cast 10.64s; release 16.33s |
| Spiral Thrust | 25556 | Cast 10.64s; effect 16.62s |
| Heavy Impact visual | 25557 | Cast 10.64s; effect 14.92s |
| Heavy Impact hits 1–5 | 25558–25562 | 16.62, 18.54, 20.41, 22.28, 24.15s |
| Lightning Storm spreads | 25549 | 16.81s |
| Ascalon's Mercy visual | 25544 | Cast 18.71s; effect 21.69s |
| Ascalon's Mercy cones | 25545 | Cast 21.78s; effect 23.26s |

The scenario begins Strength at **3s**, adding actor-loading lead-in; finishes at **30s**, after the final ring's native animation lock. Tests compare emitted native effects with all 46 complete receive timelines within **0.18s**. Damage snapshots retain normal cast-bar-end semantics; they are not asserted to equal network receive timestamps.

### Observed variations and supported practice rolls

- Eight compass starting positions, three distinct diameter axes, one omitted safe axis. **25 unordered charge triples observed**; all **32 legal axis/opposite-side patterns** supported using the independent mechanic rule. Knight identity permutations use seeded RNG (all six possible permutations verified).
- Guerrique at radius 7 on four cardinals: 49 usable cast records. The guide mentions intercardinals, but none were established in this sample; intercardinal Heavy Impact is not invented or offered.
- Eight recorded additional-knight position sets. Actual Adelphel/Janlenoux positions are radius 10 and **120 degrees apart**, differing from the guide's illustrative 90-degree description. The implementation retains recorded positions; these actors perform no Part 2 mechanics.
- Deterministic overrides: safe axis, knight side bits, Heavy Impact cardinal and additional-knight pattern. Seed also reproduces knight identity permutation.

## Native arena comparison

Generic `/anomech snapshot <label>` accepts 1–64 ASCII letters, digits, hyphens or underscores in any established matching territory/layout. It retains schema 1 and all original fields, adds plugin version metadata, uses unique GUID filenames and refuses overwrite. It runs read-only on the framework thread, rejects loading/unavailable layouts and skips queued requests after plugin disposal. Output stays in the existing `arena-snapshots` configuration directory; chat prints its full path. No player names, private actor IDs or raw pointers are collected.

The submitted DSR replay/simulation snapshots have territory 968, filter 0, 61 layout layers, no festivals, identical filter definitions and nine identical map-effect bindings (eight in layer 2887, one in 2888). Differences:

- Real duty CFC 788 versus simulation 0, expected for client-side territory loading.
- Replay map slots 0–8 state 4 versus simulation 0. Two light children per slot in layer 2887 are disabled in replay and active in simulation.
- Layer scheduling flags differ; their raw values do not establish missing phase geometry.
- Replay forces WeatherManager weather 45; simulation WeatherManager retains 2 while the existing environment weather path supplies the phase weather. Transition progress also differs.
- Two shared-group collider flags differ: layer 60753 object 8870880 (a VFX shared group at Z=118) and layer 60878 object 8589204 (dust group at Y=-60). Both report **ColliderLoaded=false**; these flags do not demonstrate an active physical obstacle. Physics broadphase membership is not captured.

The user manually confirmed `/anomech arena-dsr-p2` loads perfectly. **No speculative map-state, layer or collider writes were added.** Stable visual/collision equivalence is manually accepted, not claimed byte-for-byte. Existing snapshot commands and the command-only actor-free diagnostic remain available; no diagnostic is in the sidebar.

The supplied Tuufless preset is the DSR default, preserving the exact coordinates including asymmetric thousandths. Zone origin is (100,0,100); A/B/C/D are radius 13, and numbered markers use the supplied 109.192/90.807 coordinates. These are local simulation waymarks, not server-side commands.

## Implemented behavior and presentation limits

Full Part 1 includes visible authenticated Thordan/knight spawning, recorded warp/preparation timelines, native casts and action effects, three charges, all-player Lightning Storm, Guerrique and all five Heavy Impact rings, four subsequent knight appearances, eight snapshotted Mercy cones, completion and owned-object cleanup. Bots wait until 11.5s, after the recorded knight-arrival cue, before selecting their triangles. They use ordinary 6y/s movement, take the same hits as the player and enter resolved rings before gathering near center. There are no immunity or teleport shortcuts.

Triangle distances are a constraint-safe realization of the guide diagram (tank rear, healer front, DPS lateral), not a claim that diagram pixels or one recorded party define exact mandatory coordinates. Tank radius 19.5, healer 13.5, DPS radial 18.55/lateral 5.65. The choreography validates against charge geometry, spread separation and every supported Heavy cardinal. Groups bait at radius 2, then turn clockwise to dodge.

Remaining manual checks:

- Native knight models, armor, shields, arrival/charge root-motion animations and disappearance.
- Heavy Impact circle/expanding ring effects, including the helper's native omen.
- Lightning Storm's native visual and individual impacts. **No Lightning head-marker ID was established**; recorded marker 330 appears later for Part 2 and is deliberately excluded. No invented marker or VFX path is used.
- Mercy cast/cleave presentation and whether the current native tell is sufficiently readable.
- No BG music ID was established; P2 uses BGM 0. Part 2 boss relocation/skyward markers are omitted.
- Lethal practice failures model failed dodges/spread overlap, not a recreation of every mitigation-dependent game damage amount.

## Validation and manual test

Release validation: **`dotnet build -c Release` succeeds with zero errors and 3,541 warnings** (146 more than the prior 3,395-warning build; the added call sites produce the existing CA1416 platform-warning family, not new nullable warnings). **1,711 plugin tests, 41 relay tests and 23 Python tooling tests pass**, with no skips. The prior 1,532/41/20 passing baseline is preserved. `git diff --check` passes. Tests include 128 complete charge-axis/side/Heavy-cardinal combinations, all eight additional patterns, all six identity permutations, deterministic rolls, Elemental groups/triangle separation, native equipment, actual native-effect timing, human/bot failures, all five rings, cancellation with delayed packet spawns and reused-scenario execution. Existing FRU regression suites remain intact.

1. From the repository root run `dotnet build -c Release` (Linux SDK setup: `DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev` if not already configured). Manually reload AnoMech; no automatic reload is configured.
2. In an inn, select **Dragonsong's Reprise (Ultimate) → P2 King Thordan → Strength of the Ward — Part 1 (Elemental)**. Select **Elemental triangles** and default **Tuufless P2 onwards** waymarks.
3. First use safe sectors **N/S**, Heavy Impact **North**, knight sides **0**, additional knights **0**, and a known party role. Verify visible Thordan, four first-wave knights and their tells, spread in your triangle, move into resolved rings, bait near center and dodge Mercy. Confirm the fifth ring and completion at 30s.
4. Repeat with safe sectors **NE/SW**, Heavy Impact **West**, knight sides **7**, additional knights **7**. Then test random seeds.
5. Leave during knight appearance, re-enter, complete, reset and repeat. Check no duplicate models, effects or old casts remain. Recheck FRU Light Rampant/Ultimate Relativity if native equipment presentation behaves unexpectedly.
6. For further environment comparisons: `/anomech snapshot dsr-p2-replay` in paused Duty Recorder; `/anomech arena-dsr-p2`, then `/anomech snapshot dsr-p2-sim` in the actor-free diagnostic; `/anomech leave` afterward.

## Strength of the Ward Part 2 — independent implementation

### Evidence and scope

The October 10 recording contains **50 Strength sequences; 46 reach Part 2 blue markers, Skyward Leap, Holy Shield Bash, Dragon's Rage, Holy Bladedance and all six Conviction tower resolutions**. The other four are incomplete attempts, not evidence of missing mechanics. **41 of the 46 produce Eternal Conviction**, the unsoaked-tower failure; five lack that failure packet, which does not establish that every player survived. Each complete sequence contains six tower casts. `tools/dsr_strength2_evidence.py` and the separate Part 2 receive-timeline fixture retain public NPC/action IDs, positions, relative times and ordinal numbers only; player identities and raw packets remain local.

Sources: the complete Part 2 steps and diagrams in [Tuufless Elemental P2](https://ffxiv.tuufless.com/elemental/dsr/02_thordan/); BossMod `P2StrengthOfTheWard2.cs`, `DSW2Enums.cs` and the P2 state machine; the original IINACT recording; the previously decoded native DSR recording; pinned Action/Omen/Status sheets. No reference source or assets were copied. BossMod's canonical Skyward Leap marker is independently checked against the recording: **330 / 0x014A**, three per complete sequence, with no observed offset in this session.

| Event | Game ID | Receive time from Strength cast | Scenario time |
|---|---:|---:|---:|
| Three blue markers | 330 | approximately 23.65 | 3.00 |
| Dimensional Collapse visual / eight helper casts | 25563 / 25564 | 23.734 | 3.08 |
| Conviction visual / six tower casts | 25566 / 25567 | 25.471 | 4.82 |
| Dragon's Rage visual cast | 25550 | 27.029 | 6.38 |
| Two passable knight tethers | 84 | approximately 27.96 | 7.31 |
| Dimensional Collapse impacts | 25564 | approximately 32.733 | 12.08 |
| Three Skyward Leaps | 25565 | approximately 32.77 | 12.12 |
| Holy Shield Bash | 25297 | approximately 33.13 | 12.48 |
| Five-person Dragon's Rage | 25551 | approximately 33.18 | 12.53 |
| Shield Bash secondary visual | 25579 | approximately 33.84 | 13.19 |
| Holy Bladedance visual | 25298 | approximately 35.22 | 14.57 |
| Six Bladedance hits per tank | 25299 | approximately 36.16–38.39 | 15.51–17.74 |
| Six Conviction tower resolutions | 25567 | approximately 36.48 | 15.82 |
| Unsoaked tower punishment | 25568 | approximately 38.58 | 17.93 |

The independent fragment starts with three seconds to inspect orientation before markers. Receive timestamps are normalized by subtracting 20.65 seconds; this is a practice lead-in, not replaying Part 1. Native effect timelines are compared with every complete recording using a **0.25-second tolerance**, covering observed asynchronous source/network receive differences (the three Leap effects span approximately 32.733–33.009). Damage snapshots are separately the existing Action-sheet cast-bar-end model; tests do not equate those with receive times.

Down for the Count **3165** is observed with approximately 5.95–6.00 seconds remaining after Bash. The scenario applies six seconds and the existing status movement/control locks; tanks stay immobile through Bladedance. Native ActionEffect release locks establish Leap 2.1s, Bash 1.6s, secondary Bash 1.1s, Blade visual 5.1s, individual Blade hits 0.5s, Rage visual 3.1s and tower hits 2.1s. Completion at 21 seconds leaves those animations and the stun time to finish.

### Assignments and Elemental movement

All **eight recorded central-knight patterns** are supported. Relative north is the direction opposite Adelphel plus Janlenoux, not absolute arena north. Pattern-to-reference-north: `0=SE, 1=E, 2=SW, 3=N, 4=NE, 5=NW, 6=S, 7=W`. The two knights' recorded positions and identities are reused from Part 1's pure position table; no execution state is shared and Part 1 need not run first. Thordan is placed on that north at radius 23. Grinnaux and Hermenost occupy the recorded corresponding spawn positions.

Exactly three of the six non-tanks receive blue markers: **all 20 trios and six W/E/S permutations** are supported. Blue W/E positions are at radius 20 and ±100 degrees from reference north; S is at radius 20. The other three non-tanks stack at reference north, radius 20. Two distinct unmarked players seed the west/east tethers and the remaining player is the Rage target. All six valid initial-target permutations and six tower-communication permutations are deterministic under the scenario RNG or settings overrides. The human receives the same valid role-dependent assignments as bots, without a privileged roll.

**MT takes the west-source tether east; ST takes the east-source tether west**, ending at ±18 degrees from north, radius 20. Live passable beams resolve pickups; no timed assignment directly transfers a tether to a tank. The existing pickup logic checked only outgoing VFX slots, allowing a player already holding an incoming beam to pick up the parallel beam during crossing. Added opt-in `End.Passable(..., exclusive: true)` checks live incoming/outgoing endpoints. Only Part 2 opts in; existing encounter handovers retain their prior behavior, with the entire regression suite passing.

The unmarked bots commit distinct NW/N/NE tower claims from the authoritative state. At 8.5 seconds they move to separate communication positions near the north edge, pause visibly after arriving (side positions reached around 9.5), and return at 10.7 for the stack. These are AI movement choices inside the observed tether-to-resolution window, not newly claimed game event timestamps. Side communication positions lie beyond the crossed tether endpoints; the middle player steps two yalms inward between the beams. All three choices involve visible movement, and none crosses a beam.

**Bots do not inspect the human's movement or change their claim.** When the human is unmarked, the other two bots communicate their fixed claims and the human takes the remaining tower. Resolution checks physical occupancy—exactly one living non-tank—not the human's identity against a forced slot. Contesting a bot creates a shared tower and an empty tower, triggering Eternal Conviction. Tests place the human on a bot's communication position and later contest its tower; the bot keeps both its communicated position and final tower.

The six tower positions are reference-relative N/NE/E/S/W/NW, radius 12, soak radius 3. Marked players move inward to W/E/S towers; unmarked players take NW/N/NE. After the simultaneous impacts, non-tanks move at 6y/s, with no teleports. Dimensional Collapse resolves before this movement and is not fabricated as a persistent hazard. Its eight recorded inner/outer positions and 9y effective radius are modeled independently of the Action sheet's smaller base range. Leap overlap, charge corridors, insufficient stacks, non-tanks in Bladedance and empty/shared/tank-occupied towers have failures; bots have no special immunity.

### Native presentation and remaining validation

The boss and all knight models, demihuman body/head/legs and main/offhand equipment reuse Part 1's replay-authenticated visible native spawning. Cast bars, blue lockon 330, channeling 84, charge/jump/Blade action animations and native impact effects use the existing native abstractions. No actor-readiness guard, new hook, memory offset, layout preset or server-side marker command was added.

Towers use the actual **25567 helper cast, native Omen 90 and its sheet animation**, with draw explicitly enabled for meshless helpers. The native sheet associates hit timeline 4471 and release timeline 4470; no unrelated tower event object or guessed AVFX is substituted. Replay inspection found no separate six-tower event-object spawning sequence. Collapse uses actual Omen 92 / release timeline 4712. Native helper spawn ModelScaleId is 0 in the inspected recording, matching the existing native default; no speculative scale write was introduced.

Outstanding manual checks:

- Native blue marker appearance/expiry, tether pickup/crossing and the six tower telegraphs must be observed in FFXIV. A passing logical state does not verify rendering.
- The three jump knights are staged in a valid prior-charge arrangement, rather than reconstructing all earlier dash root motion. Their precise inherited pre-jump poses and native jump travel remain unverified; the central orientation actors and their positions are verified.
- No separate Rage headmarker is present in the examined stream. The implementation uses Thordan's authentic visual cast and the target impact; it does not fabricate a stack icon. Verify that this is readable in practice.
- Secondary Shield Bash 25579 is presented natively; its unverified additional hit geometry is not invented. The verified primary charge and Blade cone enforce the positional failures.
- Damage uses AnoMech's established normal/tank-buster/lethal categories. Exact HP loss and a required mitigation percentage have not been invented from surviving recordings.
- Blue lockon effects use the existing self-completing native path to avoid double-freeing AVFX. Verify early cancellation/re-entry; the effect may finish its native lifetime rather than disappear synchronously on cancellation.

### Validation and manual procedure

Validation results are recorded below after the final Release run. Existing Part 1 and FRU scenario sources and arena initialization remain unchanged.

1. Build `dotnet build -c Release` with `DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev` as required locally. Manually reload outside a running simulation; no automatic reload is configured.
2. Select **Dragonsong's Reprise (Ultimate) → P2 King Thordan → Strength of the Ward — Part 2 (Elemental)**, immediately below Part 1. Choose Elemental crossed tethers and the existing Tuufless P2 onwards waymarks. The settings label native validation as provisional.
3. Start with central pattern **3** (reference north=N), blue trio **0**, blue order **0**, communication **2**, initial tether targets **0**. Trio 0 is H1/H2/D1; order 0 places them W/E/S. As **D2**, watch the other two unmarked bots (D3 and D4) indicate NW and NE, return for the five-person stack, then take the remaining N tower. The bots will not yield if you instead contest NW or NE.
4. Repeat as **MT** and **ST** to verify west→east and east→west interception respectively. Check the stun prevents movement during all Blade hits, while the stack and towers remain readable.
5. Set central pattern **0** (SE) or **5** (NW), keeping the other settings, and resolve the same assignments relative to Thordan. Then try random settings; all 20 trios and all eight orientations are supported.
6. As H1 in trio 0, check the west blue position slightly south of west, then move inward to the west tower. Vary blue order 0–5 to practice each position.
7. Leave during markers, during tethers and during towers; re-enter both Part 2 and Part 1 without restarting FFXIV. Check that no NPCs, casts, beams or tower effects remain. Report any native marker lifetime or jump-source presentation problem before calling native fidelity validated.

Final validation: **`dotnet build -c Release` succeeds: 0 errors / 3,743 warnings** (202 more than the prior full 3,541-warning build; added platform-annotated call sites produce CA1416, with no new nullable/compiler-warning category). **2,713 plugin tests, 41 relay tests and 26 Python tooling tests pass, zero skips**, preserving all earlier passing tests. Coverage includes 960 full orientation/trio/order runs, all 36 communication/tether permutations across eight orientations, both correct/contested human claims for all six non-tank roles, real crossed endpoints and stun locks, verified mistakes applied to humans and bots, native presentation configuration, all 46 receive timelines, delayed native-spawn cancellation and reuse of the same scenario instance. The standard catalog also runs the complete fragment under randomized seeds. `git diff --check` passes. Headless results do not establish native rendering fidelity.


## Practice settings and phase music (2026-10-11)

The owner has now manually validated both Strength fragments, including tank/DPS gameplay, crossed tethers and fixed bot tower communication. This QoL pass changes assignment selection and audio only; combat timing, AI, actors, waymarks and environment presets are preserved.

### Part 2 practice selection

**Scenario settings → Practice Assignment** offers Random, Skyward Leap — Any/West/East/South, and Unmarked — Tethers / Stack / Towers for every non-tank seat. It filters the existing 20 × 6 valid trio/order combinations (60 Any, 20 per direction, 60 Unmarked), then selects with scenario RNG. Tanks retain their MT/ST crossed-tether responsibilities; saved non-tank practice selections are ignored for tank seats. Spread directions remain Thordan-relative.

**Use Advanced / Exact Pattern Overrides** explicitly controls precedence. Advanced uses the original KnightPattern, LeapTrio, LeapOrder, TowerCommunication and TetherTargets fields unchanged and ignores Practice Assignment. Quick mode ignores all stored exact overrides without erasing them. Legacy settings with any exact override automatically use Advanced until the user explicitly chooses a mode. Empty legacy settings and Random use exactly the previous RNG draws. Readable blue-player, tower-claim and tether-target summaries accompany applicable exact overrides.

**Advanced / RNG Seed** is now available in Release under the normal setup UI, with reuse/copy controls. The other three scenarios already offer named safe sectors, pair/branch choices or role/assignment choices; their setting semantics remain unchanged.

Bots still choose fixed, distinct tower claims, visibly communicate and return for the stack. Forced Unmarked tests cover both reading the remaining tower correctly and contesting a bot: the bots never yield, and a contest fails.

### Native phase BGM

DSR P2 maps to **Heroes, BGM row 921**, the DSR encounter entry (`music/ex4/BGM_EX4_Boss02.scd`), rather than the original Singularity Reactor row 312 or Heavensward. Track naming is independently identified by [Orchestrion's track metadata](https://github.com/perchbirdd/OrchestrionPlugin/blob/main/Orchestrion/Data/xiv_bgm_en.csv). The row/path were read from this machine's installed BGM sheet (game version `2026.09.15.0000.0000`). No audio assets were extracted, downloaded or bundled.

PhaseMusic reuses IPhase.Bgm for sibling scenarios; incomplete and arena-only scenarios explicitly opt out. Native playback uses the existing FFXIVClientStructs BGMSystem.SetBGM/ResetBGM Content scene (3), with validated Scene fields from the installed assembly. No new signatures, hooks or offsets. The game supplies its normal music playback, looping, volume and mute behavior.

A small ownership lease reads the scene track and territory. Same-track starts are idempotent; a foreign occupied Content slot is left unchanged. External replacement or territory change ends ownership, with no periodic reassertion. Cleanup only resets a slot still holding AnoMech's expected track; normal current-zone music is selected by the engine, never by replaying a cached old track. Completion, death, cancellation/reset, leave, failed startup and disposal release the override. Existing unrelated mid-track scenarios retain their previous offset playback route.

**Settings (cog) → Enable Scenario BGM** replaces the negative checkbox. New configurations enable music. Existing saved `SuppressBgm` values remain authoritative and are not silently changed: owners with suppression previously on must enable music once. Disabling it stops the active override on the next framework tick; enabling it applies on the next scenario start.

Compatibility: other controllers using higher-priority scenes, inn orchestrions or song-replacement hooks can mask the track. AnoMech does not disable or fight them. Read-only identity checks cannot distinguish a second controller selecting exactly the same track in the same slot; no native API provides an ownership token. Actual audibility and engine restoration are **awaiting in-game validation**, not established by mocks.

### Validation / manual procedure

- Release plugin suite: **2,806 passed** (93 new tests; previous 2,713 preserved).
- Relay: **41 passed**. Python tooling: **26 passed**.
- Coverage includes every non-tank practice option, full practice runs, unchanged Random draw order, tank legality, legacy JSON/mode switching, fixed bot claims/contests, actual Game music starts and completion/cancellation/disposal/failed-init cleanup, disabled audio, external replacement and territory changes.
- `dotnet build -c Release`: successful, zero errors. Existing platform-analysis/obsolete-member warnings remain; the new headless tests also produce the repository's usual CA1416 platform warnings. No new native signature or compiler deprecation warning was introduced.

1. Build Release and manually reload AnoMech outside a simulation. In the cog settings, enable **Enable Scenario BGM**, and keep the game's BGM volume unmuted.
2. Select Part 2, choose your non-tank seat, leave **Use Advanced** off, and choose **Skyward Leap — West**. Start and verify your blue assignment appears west relative to Thordan. Repeat East/South/Any and a healer/ranged seat.
3. Choose **Unmarked**: read the other two bots' claims, stack, then take the remaining tower. Deliberately contest a bot once; it must keep its claim and the attempt must fail. MT/ST should see only their fixed tank responsibility text.
4. Enable Advanced and reopen exact overrides; verify previously stored numbers are intact, active, and no longer affected by Practice Assignment. Return to Quick without losing them. Test a fixed seed via the Release seed panel.
5. Start both DSR parts and listen for Heroes. Restart while active; there should be one continuous native track, without simultaneous copies. Verify music releases on completion, a death, Leave and early cancellation, and normal inn music returns after leaving.
6. Repeat FRU P2/P3 for Footsteps/The Extreme; toggle BGM off during a run, and test plugin unload outside/inside a simulation. If another BGM controller is active, verify AnoMech leaves its changed selection alone. No manual reload is triggered by the build.

## Sanctity of the Ward Part 1 — Sacred Severs

### Scope and evidence

Independent 26-second practice fragment, beginning at Sanctity's cast and ending after **four alternating Sacred Severs, six Shining Blade dashes and eighteen Bright Flares**. No Meteors, ice, meteor towers, Faith Unmoving or subsequent mechanics. Existing scenarios, arena presets, waymarks, BGM mapping and bot communication are unchanged. Registered after Strength Part 2 in the standard Release sidebar.

The October 10 log contains **four Sanctity attempts**, identified by their preceding Strength sequence ordinal:

| Sequence | Swords | Double gaze | Jumps | Orb explosions | Coverage |
|---|---:|---|---:|---:|---|
| 41 | 2 | Both | 4 | 18 | Complete; clockwise, Zephirin SE |
| 46 | 2 | Both | 4 | 18 | Complete; clockwise, Zephirin NE |
| 49 | 0 | No | 0 | 0 | Interrupted during Sanctity cast |
| 50 | 2 | Both | 3 | 12 | Partial; clockwise, Zephirin NE |

All recorded charge paths are clockwise. Counterclockwise paths are established by independent encounter implementations and mirrored strategy diagrams, not claimed as observed locally. Missing events in interrupted sequences are not mechanic omissions. `tools/dsr_sanctity_evidence.py` produces sanitized receive timelines and public NPC geometry; the test fixture retains only game IDs, relative times and sequence ordinals. Raw logs, replay payloads and player identifiers remain outside generated repository data.

Sources:

- [Tuufless Elemental Sanctity steps 1–4](https://ffxiv.tuufless.com/elemental/dsr/02_thordan/#sanctity-of-the-ward): DRK-relative groups, corresponding role-partner adjustments, double gaze, clockwise/counterclockwise movement and waiting for the third/fifth orb before moving. The six rotation diagrams were inspected for starting and stopping geometry.
- [BossMod Sanctity implementation](https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod.Ultimate/Endwalker/Ultimate/DSW2/P2SanctityOfTheWard1.cs): 6y four-player Severs, 9y Bright Flares, 6y-wide charge paths, three segments per knight and nine orbs per knight; independent timing and mirrored rotation geometry.
- [Cactbot DSR](https://github.com/quisquous/cactbot/blob/main/ui/raidboss/data/06-ew/ultimate/dragonsongs_reprise_ultimate.ts): sword marker identities and knight-position rotation detection.
- Native DSR replay: build **14006063**, format 5. Typed SpawnNpc packet fields establish Zephirin's name/model/equipment and Brightsphere model; typed action-effect headers establish animation locks. Installed game sheets validate Action and Lockon rows. No binary offsets, signatures or visual paths were invented.
- BossMod BSD-3-Clause and Cactbot Apache-2.0 licenses inspected. Behavior was implemented using AnoMech abstractions; no reference code or assets were copied.

### Verified registry and presentation

| Kind | ID / row | Use |
|---|---|---|
| NPC base / name / model | 12592 / 3633 / 3268 | Ser Zephirin; body/head/legs 65537, mainhand 12887197149, native spawn flags/animation 0 |
| NPC base / name / model | 13070 / 4385 / 3501 | Brightsphere; visible native spawn, no demihuman equipment |
| NPC base | 12601 / 12632 | Adelphel / Janlenoux; established equipped DSR PLD spawning |
| NPC base | 12604 | King Thordan; established visible DSR spawn/appearance |
| Action | 25569 | Sanctity; 4s sheet cast, 3.7s displayed bar |
| Action | 25552 | Dragon's Gaze cast |
| Actions | 25553 / 25554 | Thordan / Dragon Eye gaze effects |
| Action | 25570 | Shining Blade; 6y-wide finite charge segment |
| Action | 25571 | Sacred Sever; alternating sword targets, radius 6y |
| Action | 25295 | Bright Flare; 1s sheet cast, 0.7s bar, radius 9y, sheet omen |
| Lockon | 50 / 51 | One/two swords; installed names `m0244trg_a1t` / `m0244trg_a2t`; same IDs observed in local log |
| MapEffect | Eye direction slot 0–7, flags 0x00020001 / 0x00100001 / 0x00400020 | Authentic Dragon Eye appearance, gaze and clearing; direction clockwise from N |

The Eye is a native map object, not a guessed NPC model. Its effect source is an invisible helper at the verified 40y directional position. Thordan performs its own gaze effect; Zephirin performs native Sacred Sever actions, and the knights perform Shining Blade with existing native carry movement. Orbs use their real models and Bright Flare cast/omen/action animations. Orb actors survive through the verified 2.1-second post-release animation lock before disposal.

Sword effects use the existing **fire-and-forget actor-attached Lockon** facility, not party-marker commands. Their AVFX self-completes; unsafe destruction of potentially self-freed native VFX is deliberately avoided. Immediate cancellation fade and native attachment duration require manual verification. The passed lifecycle tests establish actor disposal and model consistency, not the game's native VFX lifetime.

### Assignments and choreography

All **56 ordered distinct sword-target pairs**, all four Zephirin intercardinals and both rotations are supported. Group 1 starts opposite Zephirin (MT/H1/D1/D3); group 2 starts on his side (ST/H2/D2/D4). Sword 1 belongs to group 1; Sword 2 belongs to group 2. An incorrect base-group sword assignment swaps that player with MT↔ST, H1↔H2, D1↔D2 or D3↔D4. Both swords on one role pair are handled consistently. Groups remain four players, with corresponding role partners separated. The human is subject to the same rolls and adjustments as bots.

Thordan supports eight compass positions at radius 25; Eye offsets are ±90°, ±135° and 180° at radius 40. Zephirin's recorded spawn radius is 15. Initial groups use radius 20, offset 15° for early movement or 11.7° for late movement. Final groups stop 33.3° from their reference diagonal, approximately three notches before the next cardinal shuriken. Bots move at 6y/s, maintain four-person groups, and face away from both actual gaze origins. Actual character facing is used for both human and bot gaze failures.

Clockwise SE/NW and counterclockwise NE/SW use the early route: wait for the third orb pair and move at 20.8s. The other patterns wait for the fifth pair and move at 22.05s. Movement is continuous, without party teleports, bot immunity or adaptation to human mistakes.

Simple practice options: **Random, Sword 1, Sword 2, Unmarked — stay, Unmarked — role-partner swap**. Each filters the existing valid sword pairs for the selected seat. Rotation can be Random/CW/CCW. Explicit Advanced mode enables exact Zephirin, Thordan, Eye and sword overrides; inactive settings remain stored. The standard scenario RNG seed setting remains available.

### Timing and failure model

Times are relative to Sanctity cast start:

| Event | Scenario | Complete recording receive range |
|---|---|---|
| Swords | 9.45 | 9.433–9.463 |
| Gaze cast | 12.62 | 12.599–12.634 |
| Both gaze effects | 17.74 | 17.720–17.777 |
| Four Severs | 17.55 / 19.33 / 21.11 / 22.90 | 17.542–17.554 / 19.320–19.338 / 21.102–21.122 / 22.883–22.909 |
| Three paired charges | 17.67 / 19.235 / 20.795 | 17.631–17.688 / 19.187–19.250 / 20.746–20.810 |
| First/final paired Flare release | 20.04 / 23.875 | 19.988–20.051 / 23.818–23.896 |
| Cleanup/outro | 26.0 | Allows final native animation lock to finish |

Tests compare native effect releases against both complete recordings with **0.18s receive-time tolerance**. Damage uses existing cast-bar snapshot semantics and is explicitly not equated with network receipt. The difference also preserves the wait-until-the-visible-explosion movement cue.

Gazes, charges and orb explosions are lethal practice failures under the same rules for humans and bots. Severs require at least four nearby soakers and use travel distance. **Exact Sever damage scaling is unresolved:** the displayed falloff `40 / distance` and 30y inadequate-distance cutoff are provisional practice approximations, not a reconstructed game damage formula. The implementation exposes this calculation separately and labels it in settings. Wrong grouping or a missed partner swap leaves a three-player jump and fails the group; bots never compensate by changing assignments.

A small world-owned cleanup callback clears this scenario's Eye slot on reset, cancellation and leave. It captures the map and slot only, never an actor pointer, and is consumed once. This prevents stale Eye state when restarting within the already loaded territory. Existing scenarios register no callbacks and retain their behavior.

### Validation and first manual test

Final Release/test results are appended below. Native visuals, jump animation/carry interaction, sword lifetime and Eye presentation remain **unverified in FFXIV**.

1. Run `dotnet build -c Release`; manually reload AnoMech. No automatic reload is enabled.
2. In an inn, select **Dragonsong's Reprise (Ultimate) → P2 King Thordan → Sanctity of the Ward — Part 1: Sacred Severs (Elemental)**, immediately below Strength Part 2. Choose the Elemental AI and Tuufless P2 onwards waymarks. Heroes uses the existing phase mapping.
3. First test: Advanced mode, Zephirin **SE**, **Clockwise**, Thordan **NW**, Eye **+90°**, Sword 1 **MT**, Sword 2 **ST**. With MT, read one sword, start opposite Zephirin, face away from both gaze sources, wait for the third orb pair, then rotate to the second safe position. Watch all four jumps and the final explosions before the clear.
4. Repeat SE **Counterclockwise**: mirrored route and fifth-pair waiting. Test NE with both directions to cover the opposite early/late timings.
5. For adjustments, seat H1 and choose Simple **Unmarked — role-partner swap**. Read the swords and move to the other group while bots hold their correct assignments. Also test Sword 1, Sword 2 and Unmarked — stay.
6. Deliberately face each gaze, stay with the wrong group, or move into an unexploded orb. Confirm failures; no bot should yield or become immune.
7. Leave during swords/gaze, restart with another Eye direction, then switch to Strength or FRU. Confirm no lingering Eye, duplicate actors, sword effects or music override. Native one-shot sword effects may finish naturally rather than synchronously disappearing; report their observed lifetime.

Do not proceed to Meteors as part of this scenario. After this playtest, resolve any native presentation gaps and refine jump damage only with additional evidence.

Final validation (Release, 2026-10-11):

- `dotnet build -c Release` with the established local `DALAMUD_HOME`: **0 errors, 3,957 warnings**, unchanged warning count from the prior build; no new scenario-source warnings. Existing platform/obsolete-member warnings and offline NuGet vulnerability-check warnings remain.
- Full plugin NUnit suite: **3,360 passed, 0 failed, 0 skipped** (previous 2,806 plus 553 focused Sanctity cases and its catalog regression case).
- Relay NUnit suite: **41 passed, 0 failed**.
- Python tooling: **30 passed** (previous 26 plus four sanitized Sanctity-inspector tests).
- Focused coverage includes 448 full sword/Zephirin/rotation combinations, 320 full gaze configurations within 40 cases, all eight seats and five practice categories, actual-facing failures for both gazes, human/bot charge and orb failures, wrong groups and unmarked partner adjustments, early-movement failure, normal-speed frame-by-frame movement, all native release counts/times against both complete recordings, cancellation and same-instance re-entry.
- `git diff --check` passed. No packages installed, reference files modified, automatic reloads triggered or version-control history changed.

### Display-name cleanup and development workflow

The sidebar and selected-scenario header now read **P2 Strength of the Ward (Part 1)**,
**P2 Strength of the Ward (Part 2)** and **P2 Sanctity of the Ward (Part 1)**.
Scenario names used for persistent identity, configuration keys, settings and catalog
ordering are unchanged. Development now uses Debug exclusively; Release is user-built.
Debug validation: build **0 errors / 3,961 warnings**, plugin **3,361 passed**,
relay **41 passed**, no skipped tests. The solution plugin output resolves to
`AnoMech/bin/x64/Debug`, separate from installed Release output. Only Debug build/test
commands were executed; no copying, publishing or Release build was performed.


## Sanctity Part 2 — experimental Meteors implementation (October 11)

Standalone **P2 Sanctity of the Ward (Part 2)** is directly below Sacred Severs in
normal scenario selection. It is explicitly provisional (`IsIncomplete`), but
runs the complete requested fragment through the second towers; it does not run
Meteors as a continuation of Part 1. Heroes, the arena and waymarks reuse P2.
No Ultimate End, Broad Swings or later mechanics are scheduled.

### Evidence and limits

- Local October 10 log: **four Sanctity starts, zero Meteor casts/markers or
  second-tower resolutions**. October 11 log: no Sanctity starts. Earlier two
  complete Sacred Severs sequences do not establish Meteor coverage. The new
  `tools/dsr_meteors_evidence.py` performs a sanitized, read-only inventory.
  No missing event is interpreted as a nonexistent mechanic.
- [Tuufless Elemental guide](https://ffxiv.tuufless.com/elemental/dsr/02_thordan/#sanctity-of-the-ward),
  including examples/FAQ, supplies strategy, swaps, priority, communication,
  cursed patterns and clockwise movement. The **current Elemental macro** uses
  N MT/D3, E H2/D2, S ST/D4, W H1/D1. All fifteen Meteor diagrams were
  inspected; different later role placements are the documented swaps, not
  alternate initial pairings. The cursed N/S tower adjustment matches the
  illustrated example.
- [BossMod P2SanctityOfTheWard2](https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod.Ultimate/Endwalker/Ultimate/DSW2/P2SanctityOfTheWard2.cs)
  independently establishes same-category Prey targets, 7-yalm ice/fire,
  15–30 donut, 3-yalm towers, 16-yalm knockback, approximate 5-yalm comet link
  threshold and cardinal/intercardinal final responsibilities. Its state machine
  supplies Storm → first towers 4.2s, first towers → knockback 10.4s,
  knockback → second towers 3s. Cactbot independently identifies Prey `0x011D`.
- [ffxiv-raid-sim](https://github.com/susy-bakaa/ffxiv-raid-sim) **actual source,
  `meteors.unity` scene, action assets and hazard prefabs were accessible**.
  Scene events execute then wait, rather than interpreting each `time` as an
  absolute timestamp. The executable repeat mechanic drops seven comets at
  **1.43s** intervals. Puddle prefabs activate at +3s and expire at +14s;
  tower generation specifies 8 total, 2–3 middle, at most one middle per sector.
  It uses LPDU choreography and GPL-3.0 with a Unity linking exception. No source
  code, prefabs or assets were copied into AnoMech; behavior was implemented
  independently in C# and Elemental assignments are separate.
- Installed game sheets validate the action casts/shapes, Lockon 285
  `r1fz_holymeteo_s12x`, NPC models/equipment and utility statuses. The local
  native replay (format 5, build 14006063) independently confirms Charibert,
  Haumeric and Noudenet name/model/mainhand/head/body and visible flags/state 0.
  Recorded spawn positions are used for those actors and Hermenost. Holy Comet's
  model is sheet-confirmed, **not observed in this replay**; it is unnamed in the
  simulation (name row 0), avoiding an invented NPC name mapping.

### IDs / presentation registry

| Purpose | Verified identifier | Evidence |
|---|---|---|
| Charibert | BNpcBase 12603, name 3642, model 3268; body/head 65540, legs 655361, mainhand 17180787689 | Installed BNpcBase/NpcEquip, replay |
| Haumeric | BNpcBase 12638, name 3643, model 3268; body/head 131076, legs 720897, mainhand 12887261985 | Sheets/replay |
| Noudenet | BNpcBase 12639, name 3644, model 3268; body/head 196612, legs 786433, mainhand 17181311777 | Sheets/replay |
| Holy Comet | BNpcBase 12591 (`0x312F`), model 629 | BossMod / installed sheets; visible rendering untested |
| Hiemal Storm | 25574 visual, 25575 radius-7 pair damage | BossMod / Action sheet |
| Heavens' Stake | 28590 visual, 28591 circles, 28592 donut | BossMod / Action sheet |
| First Conviction | 29563 visual 9.2s, 29564 tower 12s / radius 3 | BossMod / Action sheet |
| Holy Comet / failed link | 25576 visual 12s, 25577 drop, 25578 Holy Impact | BossMod / Action sheet |
| Faith Unmoving | 25308 / 4s, knockback 16 | BossMod / Action sheet |
| Second Conviction | 28650 visual 8.2s, 28651 tower 11s / radius 3 | BossMod / Action sheet |
| Ice / fire event objects | `0x1EB682` / `0x1EB686` | BossMod; default native timeline state unverified |
| Meteor Prey | status 562; Lockon 285 (`0x011D`) | BossMod / Cactbot / installed Lockon sheet |
| Anti-knockback | Arm's Length action 7548/status 1209; Surecast action 7559/status 160 | Installed sheets, established six-second buffs |

Native casts use actual actions/animations and the established meshless helper
path for towers, circle/donut telegraphs and impacts. Equipment is explicit;
there is no new readiness gate or guessed VFX. Event objects use the existing
spawn abstraction and default state; **their actual visibility is unverified**.
Lockon uses local native VFX, never party marking. One-shot native VFX are allowed
to complete through the existing safe path; early-cancellation visual tails need
manual inspection, not speculative native destruction.

### Assignments, geometry and choreography

- Twelve distinct same-category target pairs (six support, six DPS). Only the
  Meteor-role swaps; targets end N/S. For two E/W targets, W swaps N and E swaps S.
  All eight seats can choose Random, Meteor or No Meteor. Simple and Advanced
  overrides are independent; Advanced exposes target pair, outer pattern and
  inner bitmask. Cursed filtering is explicit, and contradictory settings block
  launch. RNG seed uses the existing scenario seed setting.
- All **810 outer patterns**: 324 with five outside, 486 with six; each quadrant
  has one or two of three positions. **54 forced N/S cursed patterns** are
  supported, not discarded. Center > CCW > CW is retained unless the N/S choice
  can avoid a cursed 120° pairing.
- Two/three distinct inner towers complete eight total. Supported inner masks
  cover the four intercardinals. Exact real correlations/weighting between
  inner and outer patterns remain unknown; default uniform outer sampling and
  compatible inner masks are a **training distribution, not measured game RNG**.
- Non-Meteor players reserve the second outer tower or the immediately-clockwise
  inner tower, then choose any remaining inner towers deterministically. Bots
  visibly indicate fixed choices at 3.7–5.9s and return for ice. They never read
  human movement or yield. Tower validation checks physical occupancy, not a
  hidden requirement that the human match a predetermined assignment.
- Outer radius 17.5, inner positions `(±3.6, ±3.6)` and fire positions
  `(±7.3, ±7.3)` are simulator geometry, not locally observed Meteor coordinates.
  Storm baits use BossMod's radius 10. Inside soakers use the inner edge of
  their tower (radius 2.4) to avoid surrounding voidzones.
- Meteor routes are an **Elemental practice construction**: clockwise at radius
  19.3, ending the last drop 18° before the opposite cardinal, then entering
  the final tower. Legitimate ten-second Sprint (7.8 y/s), started at 15.8s, supports the path; no teleport or
  immunity. Every planned comet remains >=5 apart, including cursed starts.
  Unmarked outside bots travel an outer arc instead of cutting through ice.
- Final towers are four cardinals **and** four intercardinals: marked players
  finish opposite, other Meteor-role players take E/W, non-Meteor role takes
  clockwise intercardinals. Inside players align at radius 1.5 for knockback 16;
  outside players must use anti-knockback after drop five. This corrects a
  literal reading of “eight intercardinal towers” inconsistent with the guide's
  actual responsibilities and BossMod.
- Anti-knockback utility buttons now grant their statuses even when optional
  combat-action simulation is disabled, like Sprint. Their recasts reset on
  scenario start via the same validated ActionManager interface already used
  for Sprint. Only the new scenario interprets those statuses for knockback;
  other encounters' mechanics are unchanged.

### Provisional timeline and failure model

Times below are scenario-relative **native action-effect releases**, not measured
receive times or asserted damage snapshots. Cast snapshots use the established
sheet cast time minus 0.3s. Absolute alignment, first-comet delay and actor
presentation need future recordings.

| Release | Seconds |
|---|---:|
| Hiemal Storm | 8.5 |
| Heavens' Stake | 9.0 |
| First towers | 12.7 |
| Comet drops | 16.10, 17.53, 18.96, 20.39, 21.82, 23.25, 24.68 |
| Faith Unmoving | 23.1 |
| Second towers | 26.1 |
| Successful outro / cleanup | 29.0 |

Failed pair stacks, missed/overfilled towers, active puddles, linked comets and
knockback beyond the arena all fail. Comet link threshold 5 is approximate in
BossMod (the Unity collider uses 4); needs real failure evidence. Puddle entry
is lethal for practice, rather than modeling unverified DoT/freeze amounts.
Comet impact uses normal damage in its sheet radius 20: **exact falloff/HP damage
is not implemented or claimed**. Ice/fire +3s activation and +14s expiration
are reference-simulator values, not locally observed timings. Native comet
spawn/snapshot lead is simplified to the scheduled drop; BossMod notes a possible
~0.5s actor-spawn lead/lag. These uncertainties are isolated in the new files.

### Validation and first playtest

Final Debug build: **0 errors, 3,960 warnings** (no warnings in new files).
Plugin: **4,236 passed**, including **874 Meteor tests**; relay: **41 passed**;
Python tooling: **34 passed**. The plugin suite includes the five existing Ultimate
scenarios and catalog regression coverage. Every outer pattern (including all 54
forced cursed cases) completes the full two-tower/fourteen-comet fragment with
all eight seats on AI. Geometry/assignment invariants also cover every compatible
inner mask and all twelve target pairs. Focused tests cover all seats/practice
categories, fixed body-language claims, native release scheduling, deliberate
human failures, utility buttons, no party marking, cancellation and repeated
use of the same scenario instance with delayed native spawns.

Native rendering, cooldown reset behavior, cancellation VFX tails, and provisional
movement/timing still need the first in-game playtest. These tests do not establish
rendering or exact encounter fidelity. Debug outputs were isolated from Release;
no Release build, copying, publishing, package installation or history changes
were performed.

1. **User only:** compile `dotnet build -c Release`, then manually reload. Agents
   must continue using Debug; no Release build/output writes were performed.
2. Select **Dragonsong's Reprise (Ultimate) → P2 King Thordan → P2 Sanctity of
   the Ward (Part 2)**; choose Elemental cardinal pairs and your normal seat.
3. First try Simple **No Meteor**, difficulty **Not forced cursed**. Read both
   markers, the role swap and bots' 3.7–5.9s tower body language. Stack at your
   cardinal for ice, then take the unclaimed tower. Inside players hug its inner
   rim and align for knockback; outside players press their actual anti-knockback
   button after drop five. Bots do not accommodate a contested tower.
4. Try **Meteor**, same difficulty. Take your N/S outside tower near the true N/S
   line, start Sprint shortly before the first drop (~15.8s), and pace clockwise
   movement along the wall (roughly 20–26° per drop, tighter on the short cursed
   side). Continuous full-speed Sprint is not the constructed practice route. Use
   anti-knockback after the fifth drop; finish in the opposite cardinal tower
   after drop seven. No automatic movement/anti-knockback is granted to humans.
5. Try **Meteor → Forced cursed**, then both support and DPS seats. Check that
   all seven drops are readable and leave enough space; record if the provisional
   timing or constructed route differs from actual footage.
6. Leave during ice or Meteor drops, re-enter, and finish twice. Inspect markers,
   visible/equipped knights, towers, puddle activation/expiration, comet models
   and impacts, knockback, actor cleanup and restoration of normal BGM.
7. In the next real DSR session collect through both tower resolutions: marker
   times/targets (anonymized), helper cast/effect coordinates, inner/outer pattern
   correlations, ice/fire event-object states/despawns, every comet spawn and
   drop, successful/colliding comet separation, Faith Unmoving and anti-knockback
   statuses. Keep recordings local. Prefer a native Duty Recorder capture too.

### Meteor tower visibility repair

The first manual playtest found both tower waves invisible. The simulation casts
29564/28651 from hidden generic 9020/3632 helpers; this supplies cast/effect packets
but has not established the native tower pillar/ground-effect spawn state.
Installed sheets previously verified radius 3 and animation-end timeline 4470;
these values alone are insufficient to reproduce the persistent tower display.
No speculative actor flags or VFX paths were added.

BossMod's `P2SanctityOfTheWard2Towers1/2` inherit `CastTowers`: action 29564
(first wave) / 28651 (second), radius 3, activation at cast finish. Position is
caster position for self-target casts, otherwise target position or cast location.
Its radar draws computed circles and removes them on cast finish, not native VFX.
Splatoon's `P2 Sanctity Of The Ward Second.cs` discovers BattleNpc DataId 9020 /
NameId 3640, groups their positions into inner/cardinal sectors, transitions after
25575 and 29564, and checks nearby actors casting 29564 within three yalms.
Its registered elements are overlays, not a native tower spawning recipe.
Neither implementation supplies an event-object ID or native tower VFX resource.
A read-only installed-sheet query confirms both actions reference Omen row 90,
`d1025_sp012_bunsan01s`, with no alternate omen. Existing ActorCast dispatch
already requests these actions, so merely adding the same action/omen again
does not establish the missing native pillar. VFXEditor asset editing/injection
was not used; a verified persistent native spawn recipe remains unknown.
The Unity action assets establish mechanical towers, not FFXIV-native assets.

Both waves now have an explicit **local projected ground overlay**: radius-three
yellow rings with vertical edge lines, sourced exclusively from authoritative
scenario positions. No assignment hints or occupancy colors are added. This is
an intentional practice visualization, not an authentic native tower claim.
Native casts/impact effects remain unchanged; the overlay is the sole persistent
render path. Visibility windows are .7–12.4 and 15.1–25.8 seconds (existing cast
snapshot deadlines); teardown clears requests. Framework rendering uses the
existing softmarker UI callback, active-scenario guard and scenario origin.
Optional settings checkbox opens diagnostics with active count, world positions,
radii, windows and rendering path. Projection is screen-space and cannot provide
native depth occlusion; out-of-screen edges are omitted.

Manual validation: user builds Release personally, reloads, selects Meteor Part 2,
tests No Meteor and Meteor plus forced cursed patterns. Confirm eight first-wave
rings including inner towers, removal at first resolution, eight second-wave
rings, accurate waymark-relative positions, and no lingering rings after leaving,
cancellation or re-entry. Authentic tower spawning remains a future presentation
investigation; the fallback should make human practice possible now.

Repair validation: Debug plugin build succeeds (0 errors; incremental build has
one existing NU1900 offline vulnerability-query warning). Full plugin suite:
4,236 passed; Meteor suite: 874 passed with additional render-position/window
assertions across all 810 patterns; cancellation/reuse: 3 passed with explicit
empty-render-request assertions. Relay: 41 passed. Python tooling: 34 passed.
`git diff --check` clean. No Release output was built or copied. In-game fallback
visibility is pending the user's manual test.

### Shared practice features and Meteor presentation — 2026-10-12

The common main scenario controls now persist Easy/Hard bot difficulty, simulated
party visibility and God Mode. See FRU-ROADMAP.md for the shared lifecycle and
visibility policy. Defaults preserve the previous Easy choreography and visuals.

Hard DSR windows: Strength1 triangle spreads finish before Lightning/Spiral;
Strength2 initial positioning finishes before tether pickup (late leap spreads
otherwise intercepted tank tethers in tests), towers use Conviction's actual
15.52s snapshot, not the 17.93s delayed failure effect; Sacred Severs positions
finish before the first jump/gazes, while the cleared-orb rotation remains
unchanged; Meteors delays pair/tower positioning but preserves fixed communication
and meteor-drop paths. Ice exits have a latest-departure constraint derived from
7-yalm hazard egress at original speed and the existing 11.5s activation. Body
language never reacts to human contests or changes assignment to accommodate them.

#### Ability/recast audit

Concrete source path: LocalPlayerInputHooks.UseActionDetour/UseActionLocationDetour
call the original native ActionManager method; after success, ActionExecuted
reaches UserActions.OnActionExecuted. Its SimActive gate applies local modeled
Arm's Length/Surecast statuses through AntiKnockbackHandler.Apply. This is not a
pure managed ability input path: the original game action system has run.

Former start reset: Game.RunScenarioInternal → Natives.UserActions.OnScenarioStart
→ AntiKnockbackHandler.OnScenarioStart → ActionManager.GetRecastGroup / detail,
writing IsActive=false and Elapsed=0 for 7548/7559. Those are real native **client**
recast records, not AnoMech cooldowns. They do not directly reset server-authoritative
cooldowns. No corresponding anti-knockback recast snapshot/restore exists on leave,
so the changed client availability could survive outside the filtered session.

ZoneSession.Enter arms the filters **before** territory loading and checks
FirewallArmed; failed enable is refused, despite EnableFirewall's own catches.
ZoneSession.Guard checks hook state, territory/login/teleport drift, and holds
filters through a verified inn revert; disabled hooks during a stay trip the guard.
SendPacketDetour permits only its signature-derived heartbeat opcode and holds
other packets through that hooked function, including its exception path.
These are real safeguards, not a fail-open territory-entry claim. They still do
not prove all native action transports/callers traverse this signature target or
eliminate a hook-loss race between checks; a firewall alone is not proof.

**Decision:** remove Arm's Length/Surecast native start resets. Keep ordinary
ability use unchanged; add no packet manipulation or native recast bypass.
Do not add tank recast resetting. The existing Sprint reset is a separate legacy
facility and was not expanded/changed by this scoped anti-knockback audit; this
report is not a zero-risk certification of the entire plugin/native action system.

#### Local practice anti-knockback

Sanctity Meteors explicitly enables the same managed PracticeAction timer used
by practice invulnerability. Button: **Practice Anti-Knockback (Local Only)**,
above Stop/Leave for every active Meteor player, regardless of job or level.
Ready / Active countdown / Used; one activation per run, fresh on restart, cleared
on completion/cancel/leave/death. It neither calls native actions nor applies a
native status/recast. Faith Unmoving accepts this private protection only for the
human; bots keep their existing legitimate simulated six-second statuses. Wrong
or missing timing still produces the ordinary knockback/failure outside God Mode.
Installed ActionTransient tooltips for Arm's Length7548 and Surecast7559 both
explicitly specify **6 seconds**; this supplies the practice duration.

#### Fire/ice visual ordering

Current code previously began both cast bars at 1.5s. Hiemal Storm25574 casts7s,
resolving ice at8.5s. Heavens' Stake28591 casts7.5s when started at1.5s, releasing
fire at9s, with persistent ice objects requested8.5s and fire9s. Thus early fire
omens existed but the persistent/native impact ordering could look reversed, with
only a half-second gap. No usable local Meteor impact recordings establish a
replacement appearance timestamp.

Fire event objects (the already used 0x1EB686) are now requested at **5.5s**;
ice (0x1EB682) remains8.5s. This is a clearly **UNVERIFIED visual lead**, providing
three seconds between object requests; actual asynchronous native attachment time
needs playtest. Fire's cast/effect resolution, damage, radius, activation12s and
expiration23s remain unchanged. Ice impact, stack drops, activation11.5s and
expiration22.5s remain unchanged. Towers, meteor cadence, movement assignments and
knockback timing are unchanged. Earlier fire visuals are a preview, not an earlier
hazard; they expire at the same23s. Tower fallback rendering was untouched.

Validation: full Debug regressions **9,248 passed**, exhaustive added Hard/invisible
patterns **1,962 passed**, final shared controls/ability/God Mode checks **30 passed**;
relay **41**, tooling **43**. Debug build zero errors, Release files untouched.
The exhaustive tests include all810 outer Meteor patterns; neither damage rules
nor assignment assertions were weakened to accommodate lateness. All eight player
seats clear each scenario in the shared Hard/invisible matrix. God Mode feedback
for persistent hazards is rate-limited while assessments and timelines continue.

Manual: personally build Release/reload. Start Meteors with visible bots and
observe fire ground preview before ice impact/puddles; verify the visual gap and
same later activation/expiration. Treat the lead as provisional pending recordings.
Choose a Meteor assignment, click Practice Anti-Knockback (Local Only) shortly after
the fifth drop (21.82s, before Faith Unmoving23.1s), and continue clockwise normally.
Its six-second timer should protect without an unlocked/ready real ability; no
real buff icon is applied by this control. Repeat without clicking or click before
17.1s so it expires: normal knockback/failure should occur. Inner tower players
still intentionally take knockback; using the virtual action there can cause their
legitimate tower failure. Restart must show Ready. The real Arm's Length/Surecast
cooldown should no longer be reset by AnoMech. Try Easy/Hard and visible/invisible
independently, keeping bodies visible when learning tower communication. God Mode
allows blind/error practice while still explaining failed mechanics.

## FluffySim 1.0 final Hard coverage audit (2026-10-12)

All four DSR scenario AI files were traced. Strength 1 triangles, Strength 2 opening/towers, Sacred Severs initial groups, and Meteor pair/ice-return/tower preparation use the shared distance/speed/deadline scheduler. Tether crossing, committed tower body-language communication, Heavy Impact/Mercy dodges, Sacred Severs orb rotation and Meteor arcs retain their safe/communicative timing. Direct bypasses and guarded prompt requests are listed in HARD-DIFFICULTY-AUDIT.md. No DSR strategy, assignment, damage, geometry, presentation or audio changes were needed.

New per-scenario comparisons run identical Easy/Hard seed/seat assignments through completion and observe actual actor positions before delayed departures. Existing exhaustive Darklit/Meteor Hard checks and invisibility/God Mode/local practice actions remain regression coverage. See INSTALLATION.md and RELEASING.md for FluffySim DLL/config migration and owner-operated packaging. Debug-only development remains mandatory; original source/project identities stay AnoMech internally.

Release audit validation: 11,221 full Debug plugin tests passed; final 14 focused tests passed, including expanded all-seat Easy/Hard comparisons (overlapping counts, not additive). Across all eight FRU/DSR scenarios the comparison executes 256 matched pairs with identical seed/seat and verifies unchanged assignment state and real later movement with successful full resolution. Relay 41 and tooling 50 passed. Final Debug solution build has 0 errors / 3,965 warnings, matching the prior warning baseline. Packaging verified with Debug input and temporary output; no Release files or running plugin registration were modified.

## Shared Pause / Resume (2026-10-15)

Local practice now has common Pause/Resume controls beside Stop, a frozen timestamp and an explicit
PAUSED indicator. All nine FRU/DSR scenarios share the same suspended runtime and reused local input
hooks. Pause applies no fake-death/HP/status change, makes no action/cooldown/packet operation and
bypasses the existing auto-attack cancellation branch. Existing death/Fetters restrictions survive
Resume. Virtual actions and simulation statuses remain frozen; all party slots and encounter actors
are captured with exact duration/elapsed/stacks/source/instance metadata. Resume retains the same
status objects, avoiding refresh/cleansing. Native animation/status displays may continue; pose drift
is corrected through the existing per-actor transform interface without cancelling movement plans.
Multiplayer Pause is disabled until peer synchronization is implemented. See
[PAUSE-INVESTIGATION.md](PAUSE-INVESTIGATION.md) for call paths, safety boundaries, cleanup, validation
and the native keyboard/mouse/controller playtest checklist. Existing mechanic assignments/timings
and BGM are unchanged; Debug development only.

### Pause release validation — 1.1.0.0

Owner reports successful native Pause playtesting. Debug regression: 11,291 plugin tests passed; final focused checks after cleanup refinements: 97 passed. Relay: 41 passed; tooling: 58 passed. Debug build: zero errors, 3,965 warnings. The 26,880 Fall of Faith assignment/element combinations passed the prior exhaustive sweep. These counts are separate runs, not additive coverage totals. Native cosmetic timers may continue while authoritative statuses remain frozen.

## Native hotbar defensive practice and filter audit — 2026-10-15

Supersedes the earlier conservative native-reset decision. See [PACKET-FILTER-AUDIT.md](PACKET-FILTER-AUDIT.md) for actual source paths, the owner's Holmgang/Cactbot evidence, scoped confidence and residual transport risks. Outgoing traffic through the inherited hook is heartbeat-only; unknown requests fail closed. No queued packet bytes are retained or replayed. The unload exception to native inn readiness and settling has been removed; pending actions and disposal failures cannot bypass verification. An unsafe unload now stops the client, so Leave and wait for restoration before rebuilding/reloading.

Native hotbar tank defenses and Arm's Length/Surecast resolve independently of the optional combat module. Their owned client recasts reset only for relevant practice scenarios and restore the pre-entry baseline with real elapsed time on Leave, including shared-group ownership. Sprint uses the same snapshot/restore rather than its old unowned reset. No server cooldown is reset or ability automatically pressed. Main virtual buttons are suppressed when native practice is ready; unavailable gates retain safe fallback controls. Low-level Meteor players can use `/fluffysim antikb` (explicit, six seconds, once per run) without a skill unlock. Native cooldown/exit behavior still requires owner testing. All nine mechanics, Pause status ownership, arenas and BGM remain unchanged.

### Validation results

- Full Debug plugin regression: **13,009 passed**, zero failures, including the complete 1,680 × 16 Fall of Faith sweep.
- Final isolated Debug focused checks after the restore/Pause refinements: **75 passed**, zero failures; includes all nine paused complete scenarios, cooldown ownership/shared groups, repeated native-timer pause restoration, missed/timed tank defenses and cleanup exceptions. These are overlapping runs, not additive test totals.
- Relay: **41 passed**. Tooling: **65 passed**.
- Debug build: **zero errors, 3,965 existing warnings**; no increase in the warning baseline. Diff whitespace checks passed.
- Debug ZIP packaging was checked in temporary output; no public Release artifact was produced. Release output, installed plugins, release version and repository history were not modified.
- Native recast reset/restoration, action presentation and Wine hook behavior remain subject to the manual client checks in PACKET-FILTER-AUDIT.md. No automated live gameplay actions or network payload capture occurred.
