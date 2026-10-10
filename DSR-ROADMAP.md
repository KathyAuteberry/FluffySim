# DSR implementation roadmap

## Coverage and validation

| Fragment | Status | Scope |
|---|---|---|
| P2 arena | Manually validated in FFXIV | Territory 968 loads correctly and is traversable without an additional layout preset. |
| Strength of the Ward Part 1, Elemental | Build/test validated; native presentation awaits manual validation | Complete steps 1–5, including the game's fifth Heavy Impact hit. Registered in the normal Release sidebar. |
| Strength Part 2 | Not researched for implementation | No tethers, towers or Skyward Leap mechanics implemented. |
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
