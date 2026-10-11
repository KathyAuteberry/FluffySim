# DSR implementation roadmap

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
| Sanctity Part 1: Sacred Severs, Elemental | Build/test validated; native presentation awaits manual validation | Full four-jump fragment, both rotations, role-partner swaps, double gaze and eighteen Bright Flares. Provisional jump damage model. |
| Sanctity Part 2: Meteors | Not researched | Not implemented. |
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
