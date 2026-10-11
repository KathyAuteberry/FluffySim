# FRU implementation roadmap

## Development build workflow (current)

Use `dotnet build -c Debug` and run automated tests with `-c Debug`. Debug output
must remain in Debug-specific `bin` / `obj` trees (including `bin/x64/Debug`). **Never build Release during development,
or copy/publish/modify installed Release output.** Dalamud watches that output and
hot-reloads on changes, freezing the running FFXIV client. The user personally
compiles Release when ready to playtest. Earlier Release validation results below
are historical; older manual-build instructions are user-only, not agent actions.


Updated 2026-10-10. **P2 Light Rampant (Mana) and the P2 arena are manually
validated in FFXIV**, including traversable collision, both visible orb orders,
explosions, tethers, Hammer grace period and bot resolution. This validation is
for the supported Mana formation, not every outstanding native presentation
question. Startup flicker has not been separately confirmed eliminated.

**Diamond Dust: Paused — incomplete and currently failing at startup.**
Removed from the normal catalog; source, overrides and tests remain intact.
Light Rampant remains selectable and unchanged. **P3 Ultimate Relativity (Mana)
is implemented through Shell Crusher, registered and Release build/test
validated; native mechanic presentation awaits FFXIV validation.** The P3
arena appearance was manually confirmed; its working preset is preserved.

## Architecture

`ScenarioCatalog` registers fresh `IScenario` instances. Each scenario belongs to
an `IPhase`, which belongs to an `IZone` supplying territory, world origin,
party level/item level and waymarks. `Game.RunScenarioInternal` creates the party
and runs zone, phase and scenario setup. Native interaction in simulation code
must use `Core/Native/Interfaces` through `Natives` and object proxies.

`Plugin.OnFrameworkUpdate` calls `Game.Tick`: the scheduler runs at
`EventTimeScale`, while world objects, movements, casts and statuses use real
time. Use absolute scheduler times and validate initially at scale 1.
Spawn actors, event objects, omens and tethers through `SimWorld`; child objects
own their lifetime. `EnemyAction` obtains cast duration and geometry from the
Action sheet and adds damage, stacking, status or knockback effects.
Tether distance/death failure checks belong to the scenario; `SimTether` renders
and expires the tether. `*State` owns deterministic rolls from `world.Rng`;
`*Ai` owns strategy assignments and bot movements. Existing
`ScenarioCatalogTests.SunnyDay` runs every registered strategy with a configurable seed count;
`ScenarioRun` uses fake native services and real pinned game sheets.

No separate simulation engine is needed. The working FRU P2 arena is reused.
New Diamond Dust native telegraphs and actor animations require in-game checking.

## Coverage

Statuses used here: Not researched; Research complete; Implemented but
unverified; Build/test validated; Manually validated in FFXIV. Inventory or partial
research does not advance a mechanic to Research complete.

| FRU-Sim selectable scenario | Reference sequence | AnoMech | Status / evidence |
| --- | --- | --- | --- |
| P2 Light Rampant | `scenes/p2/sequences/light_ramp_seq.gd`, `p2_lr_main.tscn` | Mana ring formation | Manually validated in FFXIV for the supported formation |
| P3 Ultimate Relativity | `scenes/p3/sequences/ult_relativity_seq.gd`, `p3_ur_main.tscn` | Full Mana sequence through Shell Crusher | Build/test validated; 607 focused tests, native mechanic presentation pending |
| P3 Sextuple Apocalypse | `scenes/p3/sequences/apoc_seq.gd` | Missing | Not researched to completion; Mana rotations and water swaps inspected; casts in 4, 6, 13, 16 |
| P4 Darklit Dragonsong | `scenes/p4/sequences/darklit_seq.gd` | Missing | Not researched; reference offers NA/EU/JP/Mana/MUR variants |
| P4 Crystallize Time | `scenes/p4/sequences/crystal_time_seq.gd` | Missing | Not researched; reference offers NA/MUR variants |
| P5 Fulgent Blade | `scenes/p5/sequences/p5_fb_seq.gd` | Missing | Not researched |
| P5 Paradise Regained | `scenes/p5/sequences/p5_pr_seq.gd` | Missing | Not researched; rotation, tower orientation and light/dark order rolls identified |
| P5 Polarizing Strikes | `scenes/p5/sequences/p5_ps_seq.gd` | Missing | Not researched to completion; bait/echo sequence inspected |

The P1 scene is a template, not a supported mechanic. The legacy `p5_seq.gd`
is not one of the three selectable P5 sequences in `p5_main.tscn`; do not count
it as a ninth module. P4/P5 casts were not found by the signature scan in this
recording. Missing events do not establish that those mechanics do not exist.

## Local recording coverage

Source: local `Network_30301_20261007.log`, territory 1238. No raw player names,
instance IDs, raw log lines or player positions are included here.

The parser finds 16 pulls. Light Rampant cast starts occur in pulls
4, 6, 8, 9, 12, 13, 14, 15, 16. Pulls 4, 6, 8, 9, 13, 15, 16 cover its
full sequence through House of Light. Pull 12 stops about 19.33 seconds after
its Light Rampant cast start and records Refulgent Fate; it is useful for failure
research. Pull 14 ends about 2.96 seconds into the cast and cannot establish
mechanic behavior. Pulls 4, 6, 13, 16 are the best initial P3 sources because
they record both Ultimate Relativity and Apocalypse. Their durations are
10:02, 9:48, 10:01 and 10:04 respectively; cast presence alone does not prove
every P3 submechanic was recorded successfully.

Light Rampant cast starts, seconds from pull start:

| Pull | LR | Ultimate Relativity | Apocalypse |
| --- | ---: | ---: | ---: |
| 4 | 289.378 | 451.46 | 546.48 |
| 6 | 285.553 | 447.93 | 542.86 |
| 13 | 287.637 | 449.93 | 544.90 |
| 16 | 288.387 | 450.66 | 545.88 |

For pull 4, relative to the **start of Light Rampant casting**, observed events:

| Event | Seconds |
| --- | --- |
| LR action effect | 4.957 |
| Initial chain/weight/lightsteeped statuses | 5.805 |
| Luminous Hammer drops | 12.851, 14.415, 15.976, 17.542, 19.101 |
| Chains become Curse of Everlasting Light | 15.799 |
| Six outer Bright Hunger towers resolve | 16.155 |
| Burst casts begin, first/second groups | 19.323 / 22.357–22.358 |
| Powerful Light stacks resolve | 21.866 |
| Burst groups resolve | 24.319 / 27.351 |
| Banish III cast begins | 28.960 |
| Central Bright Hunger resolves | 30.832 |
| Banish III spread effect | 34.044 |
| House of Light cast / visual effect / cone effects | 37.122 / 42.116 / 43.007–43.009 |

These are received network effect times, **not measured damage snapshots**.
The seven complete LR windows agree on five puddle drops at roughly 1.56-second
intervals. Pulls 4/16 record spread Banish (40221 → 40223); pulls
6/8/9/13/15 record pair Banish (40220 → 40222).

## Initial registry and provenance

`L` = the recording above; `S` = local Splatoon FRU scripts;
`B` = BossMod `FRUEnums.cs` / `P2LightRampant.cs`;
`D` = pinned datamining sheets at
`d9582a624eaec69d45649366dee801c2f3a6e9b3`.
IDs are decimal unless prefixed with `0x`. BossMod OIDs are data IDs, not
instance IDs. NPC name rows below were cross-checked against the pinned sheets.

| Kind | Value | Meaning / evidence |
| --- | --- | --- |
| Territory | 1238 | FRU: L, S |
| Actor data ID | `0x459F` | P2 boss, BNpcBase 17823 / BNpcName 12809: L, B, D |
| Actor data ID | `0x233C` | Helper: B |
| Actor data ID | `0x45A2` (17826) | Holy Light orb / BNpcName 9318: L, B, D |
| Event-object row | `0x1EBC4F` | Persistent puddle: B; ten Type=7 additions in pull 4 LR window: L |
| Action | 40212 | Light Rampant, 5s, radius 40: L, S, B, D |
| Action | 40213 | Bright Hunger, instant radius 4, **Omen=0**: L, S, B, D |
| Action | 40214 | Missed/incorrect tower punishment: B, Mana guide, D; damage-down details not modeled |
| Action | 40215 | Refulgent Fate: L (pull 12), B, D |
| Action | 40216 | Lightsteep: B, D; not established by the inspected successful windows |
| Action | 40217 | Powerful Light, instant radius 5: L, B, D |
| Action | 40218 | Luminous Hammer, instant radius 6: L, B, D |
| Action | 40219 | Burst, 5s, radius 11: L, B, D |
| Actions | 40220/40221 | Banish pairs/spread visual casts: L, B, D |
| Actions | 40222/40223 | Banish pairs/spread effects, radius 5: L, B, D |
| Actions | 40189/40188 | House of Light visual/cone effects: L, B |
| Status | 4157 | Chains of Everlasting Light, observed 10s: L, B |
| Status | 4158 | Curse of Everlasting Light, observed 9s: L, B |
| Status | 4159 | Weight of Light, observed 16s: L, B |
| Status | 4372 | Mark of Mortality on underfilled Powerful Light: B, D |
| Status | 2257 | Lightsteeped, initial duration 36s, observed stack counts 1/2: L, B |
| Tethers | 110/111 | Initial chains / active curse; six links each: L, B |
| Head marker | 375 (`0x177`) | Two puddle targets: L, B |
| VFX path | `vfx/lockon/eff/target_ae_s7k1.avfx` | Puddle target tell: S `P2 Light Rampant JP.cs`; not independently observed as a path in L |
| Geometry | Tower centers N/S ±16, NW/NE/SW/SE approximately (±14, ±8) from (100,100) | S `P2 Light Rampant.cs`; B uses radius 16, six 60-degree offsets |
| Geometry | Active chain danger below 25 units | B uses squared distance <625; exact game break threshold not measured in L |

## Mana strategy and evidence gate

Use `_mana/fru/02_usurper_of_frost.en.md`, not `_static/fru` or the
simulator's default NA strategy. Mana orders tethered players from H1 clockwise
and assigns NW, S, NE, SW, N, SE towers. Puddle players move NE/SW and rotate
clockwise, then join NNE/SSW stacks. FRU-Sim has distinct Mana, Elemental,
NA and EU branches, so those coordinates/assignments are not interchangeable.

Read both `.gd` behavior and `.tscn` animation method tracks. In particular,
FRU-Sim declares `PUDDLE_COUNT=6` but its LR animation calls five explicit
drop methods. The actual five-drop sequence agrees with L, B and the Mana
guide. Its tower-soak movement is scheduled at 12.3 in the animation, although
the function comment says 12.75. Port executable timing, reconciled against L.
Simulator dimensions are not game units: tower centers around 37.7 in the sim
must not be copied into AnoMech's radius-20 arena or scaled by an assumed factor.

### Implemented sequence and supported randomization

`Scenarios/Fru/` adds the zone/P2 phase, verified IDs/actions, scenario, state,
Mana AI and settings. A three-second lead-in precedes LR cast start. The
absolute timeline includes initial chains/Weight/Lightsteeped, five Hammer
pairs and persistent puddles, six single-person outer towers, two four-person
Powerful Light stacks, two orb trios, the four-person central tower, both
Banish outcomes, House of Light and teardown at scenario time 49 seconds.
AI follows the Mana H1/H2/MT/ST/D4/D3/D2/D1 ordering, assigns NW/S/NE/SW/N/SE,
uses outward-first clockwise puddle drops and NNE/SSW camps, then assigns
central soaks to two-stack players and uses Mana clocks/pairs. Practice movement
coordinates are authored safe positions, not reconstructed player snapshots.
The two orb camps use different 20/40-degree offsets, depending on whether
that camp's central N/S orb is in the exploding trio.

All 28 puddle pairs, all 70 four-recipient extra-Lightsteeped combinations,
all six Weight edges, both orb orders, both Banish outcomes and both pair-target
role groups are seedable; settings can force puddle pairs, orb order and Banish.
Status refreshes use 36 seconds, matching the initial and reapplied player
Lightsteeped status packets (receive-time residuals may be slightly lower).
Initial stacks are one on each puddle player plus one on four distinct random
players, as in FRU-Sim's executable assignment code. The seven recordings support
total six and the observed one/two-stack distributions; they do **not** prove
that every modeled combination is sampled uniformly by the game.

Tower count mistakes and underfilled stacks invalidate success without an
invented lethal hit. Underfilled stacks apply verified Mark of Mortality.
`Game.RecordMechanicMistake` supplies the existing result tracking for these
nonlethal practice failures. Missed-tower Inescapable Illumination is visual;
its real damage-down penalty is not implemented. Overlapping Hammer/spreads/
House cones, unpaired Banish, Burst, five Lightsteeped stacks and invalid active
chains fail practice. Entry into an activated persistent puddle is an immediate **practice KO**;
the real Bleed tick damage is not simulated.

### New evidence and remaining limitations

- All seven complete windows contain six directed initial links forming a
  six-player cycle, excluding the two puddle targets. Weight targets are adjacent
  in that cycle. Local analysis used anonymous labels, jobs and latest 03/261
  coordinates; the parser's first-seen role labels are not real party assignments.
  The Mana guide's tether FAQ describes random start/nearest-unlinked selection
  and rare tangled layouts. Those general selection rules are not independently
  established by these recordings. **Only the ring formation is supported:**
  `BindChains` sorts positions angularly. Tangled topology and untangling practice
  remain missing; the state binding is isolated for later refinement.
- Pull 12's Refulgent Fate effect is at +16.848; first deaths follow at +17.918.
  Latest available endpoint positions give link lengths 26.51–35.33. These are
  stale receive-time samples and do not demonstrate the failure cutoff or cause.
  Active chain/dead-endpoint failures use BossMod/the guide; the isolated planar
  rule uses BossMod's 25y danger boundary. No speculative maximum-distance
  failure from the simulator's unrelated scale was introduced.
- Outer tower environment events: indices 09–0E, flag `0x00020001`, at +8.033;
  paired overlays 0F–14, flag `0x00010004`; resolution flag `0x00080004`
  at +16.065. Central indices 15/16 use those spawn flags at +22.713 and 15
  resolves at +30.743. These observed events are replayed through `Map.AddEffect`.
  Occupancy flags were also observed, but index-to-position/count mappings are
  unresolved and occupancy feedback is not replayed. **Native tower visibility
  and the phase-specific arena initialization still need in-game validation.**
- Boss timeline IDs `0x11E5` (+8.121) and `0x1E43` (+26.814), model hide/show
  (+10.163/+26.872), orb show (+16.566/+19.465) and hide (+26.024/+29.062)
  come from local actor-control/model events. Rendering these with existing
  timeline/model abstractions is unverified in game; no guessed VFX path was used.
- Puddle event objects appear about 0.83s after Hammer. First-object model state
  changes at age 9.794/10.143s support a roughly ten-second active/fading window;
  removal is age 12.857s. Gameplay uses FRU-Sim's ten-second danger window and
  visual lifetime 12.85s. The exact server Bleed cutoff remains unmeasured.
- House of Light uses BossMod's 30-degree half-angle, which its source itself
  labels TODO verify. The precise cone edge is explicitly UNVERIFIED.
- Weather/BGM and item-level sync are unspecified rather than assigned guessed
  IDs/values. Multiplayer is not enabled for this provisional scenario.

### Automated acceptance

221 focused LR cases pass: 112 full runs across all 28 pairs × both Banish ×
both orb orders; 70 full runs for extra-stack masks; all Weight edges with both
orb orders; both pair-target groups; deterministic assignments; stack progression;
cleanup and restarting the same instance; failure cases for short/dead chains,
persistent puddles, missed outer/central soaks, underfilled stacks, overload,
Burst, Banish pairs/spreads and House cones. Seven additional timeline cases
are included in the 221 and use only anonymized action/timestamp fixtures from
pulls 4/6/8/9/13/15/16. No player identifiers or names are stored in tests. “Complete” describes
recorded event coverage, not a death-free run: pull 4 includes a player death
at +26.81; its later one-stack House reapplication is not the sunny-day
stack progression. Surviving players have four stacks after House.

Timeline checks normalize to LR cast start and compare distinct action groups
and counts. Instant-action receive tolerance is 0.13s (observed variation plus
60Hz scheduling); Burst allows 0.36s because AnoMech snapshots at sheet cast
end minus its established 0.3s effect offset. This is **not** an assertion that
packet receipt equals an actual game damage snapshot.

Final full-suite/build results are recorded in the validation section below.

## Implementation order and acceptance

1. Validate provisional LR in game at time scale 1, starting with bot control:
   verify arena, tower/orb visibility, head markers, boss hide/show and cleanup;
   then play one puddle and one chain seat. Investigate occupancy mapping,
   tangled selection and precise failure edges without blocking normal ring practice.
2. P3 Apocalypse: observed data and an explicit Mana reference strategy;
   include water swaps, both rotation directions, spread, farthest bait and
   knockback. Then Ultimate Relativity (more status/rewind/hourglass work).
3. P4 Darklit, then Crystallize Time; obtain independent later-phase evidence
   instead of extrapolating missing progression recordings. Compare Mana
   strategies separately from simulator NA/MUR defaults.
4. P5 Fulgent Blade, Paradise Regained and Polarizing Strikes; validate public
   encounter implementations and actual cues, since these logs do not establish
   their behavior. Consider Polarizing Strikes as a smaller fallback once its
   boss cues, geometry and strategy are fully researched.
5. Inventory Dragonsong-Sim and prioritize Elemental DSR strategies afterward.

Headless success advances only to Build/test validated. Territory rendering,
native VFX, reading assignments and player movement need explicit in-game
validation before Manually validated in FFXIV.

## Tooling, parser and validation

On this machine .NET SDK 10.0.112/runtime 10.0.12 and Wine are present. Existing
Dalamud files resolve through `DALAMUD_HOME`; no Windows SDK installation or
system package installation was needed. Run from the AnoMech repository:

```sh
export DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev
dotnet build AnoMech/AnoMech.csproj -c Debug
dotnet test tests/AnoMech.Relay.Tests -c Debug
dotnet test tests/AnoMech.Tests -c Debug
```

The bare solution build failed without useful errors in the initial sandboxed
run. The explicit plugin build succeeded with 22 existing warnings and zero
errors. NuGet restore, public pinned-sheet downloads and MSBuild/test-host local
sockets required sandbox escalation. No repository configuration was changed.
Relay tests: 41 passed. Plugin headless baseline: 452 passed after disabling the
generated Windows-only assembly annotation for Linux test builds. NUnit had
discovered 454 cases internally but rejected the assembly as Windows-only;
two explicit replay cases are excluded from the normal run. A successful command
that says "No test is available" is not a passing test suite.

`tools/parser.py LOG 1238` lists pulls; append a pull index for a summary or
`PULL BEGIN END -x --x 100 --y 100` for NPC/event windows. The positional times
are relative to pull start, not phase or mechanic start. `--code` emits a
baseline scenario and `--dropped PATH` records events that were not emitted.
Pull boundaries use `260` in-game combat changes, with director/first-hostile-
ability fallback if 260 is absent. It reads actor adds/removes, casts/effects,
status/headmarker/tether events, actor move/set-position and additional actor,
cast, director and environment messages. Unsupported code mappings become
comments; filtering/coalescing and receive timestamps limit reconstruction.
Output is not a mechanic implementation: it lacks randomized assignments,
bot strategy, reliable snapshots and failure rules.

Generated baseline code includes raw-log comments. Its player-name redaction
does not establish that all private IDs/fields are removed. Keep such output
local and inspect/sanitize before retaining it in the repository. Research in
this document stores only encounter IDs, pull indices and relative timings.

Reference snapshots: FRU-Sim `2a77c857ce1bb6eb472a59a95c7544faba01c55a`;
Splatoon `f43b92fa6c130aff00035240adb18901f904f557`;
macros `a6c00b1ecea2c2b5889f531e10bb147b5eb5c919`.
Public BossMod tree snapshot `40b0abdd35e81517409bdc81446910f943a75a49`;
FRU files are under `BossMod.Ultimate/Dawntrail/Ultimate/FRU`, not
`BossMod/Modules/Dawntrail/Ultimate/FRU`.

License inspection: AnoMech/Splatoon declare AGPL v3; FRU-Sim source headers
declare GPL v3 but no root LICENSE file was found in this checkout;
Dragonsong-Sim and the macro repository have MIT licenses. No substantial
reference code or assets have been copied. Inspect the relevant file/asset
license and preserve required attribution before any future copying.

Public references: [BossMod LR behavior](https://github.com/awgil/ffxiv_bossmod/blob/40b0abdd35e81517409bdc81446910f943a75a49/BossMod.Ultimate/Dawntrail/Ultimate/FRU/P2LightRampant.cs),
[BossMod IDs](https://github.com/awgil/ffxiv_bossmod/blob/40b0abdd35e81517409bdc81446910f943a75a49/BossMod.Ultimate/Dawntrail/Ultimate/FRU/FRUEnums.cs).

Final iteration validation (2026-10-09): plugin build succeeded, zero errors.
The final incremental build retained NU1900 because NuGet vulnerability metadata
was unavailable; Linux test compilation also reports existing platform analyzer
warnings. Full plugin suite: **674 passed, 0 failed** (452 baseline + 221 focused
LR cases + one catalog entry). Relay suite: **41 passed, 0 failed**. No existing
assertions were weakened. The test runner now recognizes an exhausted timeline
with a recorded nonlethal mistake as failure rather than waiting for a timeout.

Smallest in-game check: load the built plugin through Dalamud in the normal
practice location, choose FRU → P2 → Light Rampant (provisional) → Mana, leave
time scale at 1 and enable bot control for a first observation run. Verify the
six outer and central towers render in the expected locations, both orb groups
appear/explode, and boss visibility/cleanup work before practicing a human seat.
Repeat with the other forced orb order and Banish outcome. A successful headless
run does not advance the scenario to Manually validated in FFXIV.

## P2 native arena and crash investigation (2026-10-09)

**Status: P2 environment initialization unresolved; native crash mitigation implemented,
not manually validated.** Light Rampant gameplay is unchanged.

The local game crash report identifies an access violation during
`ActorVfxProxy.Remove → SimVfx.Despawn → SimPartyNpc.Despawn → SimParty.Despawn → Game.ResetInternal`.
This establishes teardown as the crashing operation; it does not establish an
arena-load crash or identify the exact stale effect. Light Rampant's puddle
lockon was incorrectly retained as persistent with a timed native destruction.
Other scenarios use fire-and-forget lockons, and SimVfx documents that these
effects free themselves: destroying the stale native handle can crash. Changed
this lockon to `persistent: false`. SimVfx now detaches ownership before removal;
ActorVfxProxy clears its pointer before removal to prevent repeat destruction.
Neither guard can detect a pointer already freed by the engine. Added identity-free
begin/end diagnostics around native actor-VFX removal and world teardown.
The crash mitigation requires a new game test; a passing build cannot prove it.

The most likely arena problem is absent phase setup combined with the default
layout selection. FruZone.P2 has no environment client setup. Existing zone
loading uses transition filter key 0. The local native diagnostics show territory
1238 with three available filters (285948, 288312, 288313), and many active layout
layers before the first cast. That does not yet establish whether the geometry
was already mixed before the scenario's actor spawns. These filter IDs are
observations, **not verified phase selectors**.

Read-only installed game data resolves territory 1238 to
`ex3/01_nvt_n4/goe/n4gw/level/n4gw`. Its bg and planmap LGB layers include P2/P3
and later-phase groups, plus older shared Eden geometry. The inspected layers
have festival ID/phase 0; this offers no evidence for an FRU festival preset.
Layout shared groups are distinct from AnoMech's spawned NPC actors. Names alone
do not establish which layers can safely be suppressed or which colliders belong
to P2. No layer suppression, scene writes or guessed weather values were added.

Observed P1→P2 transitions in pulls 4/6/16 emit map index 0x17 flags 0x00020001;
P2→P3 emits index 0x18 with the same flags. P2 freeze/thaw uses index 0x17 flags
0x00200010 / 0x00400001. These are timeline invocations plus resting states,
not a complete P2 initialization array. The recording does not establish the
initial scene selection, weather, festival state or all resting map slots.
The director arguments 0x4EA7/0x4EA8 are not used as arena selectors.

External references inspected:
- [Hyperborea main](https://github.com/kawaii/Hyperborea/blob/c145b7c01643fc95fc371d7ca883650459d5ba71/Hyperborea/Hyperborea.cs)
  and [debug window](https://github.com/kawaii/Hyperborea/blob/c145b7c01643fc95fc371d7ca883650459d5ba71/Hyperborea/Gui/DebugWindow.cs):
  festivals, weather, scenes and map effects are separate systems. Its data lists
  FRU shared groups but provides no verified complete P2 preset. Debug scene
  pointer writes are not suitable evidence for production initialization.
- [Simulant](https://github.com/MnFeN/Simulant/tree/1dd1a4ad993423e39935a8027536c77327c8a025):
  phase presets use weather/BGM/map effects, but supported content has no FRU preset.
- [ffxiv-arena-images](https://github.com/kotarou3/ffxiv-arena-images/tree/868ca11e7b2bd85e36fc97dc0f3e2bcefd75101e):
  FRU images are extracted with unwanted phases manually hidden; no runtime preset.

AnoMech already supports phase client setup and map-slot resting-state arrays
(Dancing Mad) and verified map transitions (TOP). Reuse those interfaces once a
complete FRU P2 state is established. **No evidence-backed complete P2 native
initialization values were found, so the arena is not claimed fixed.**

A catalog entry `Arena diagnostic (P2 initialization unresolved)` exposes Solo
inspection: no bosses, bots, casts, tethers or mechanic VFX. It logs layout layers
and map-slot/live-effect descriptions at +1/+3 seconds and stays available until
Leave. It intentionally leaves the current environment unchanged. Focused tests
check that Solo inspection creates no mechanic actors/tethers or party NPCs and
that puddle lockons are not retained for later native destruction.

Manual validation, starting with the diagnostic rather than Light Rampant:
1. From an inn, select FRU P2 arena diagnostic, Solo. Capture the +1/+3s diagnostics.
2. Inspect geometry before starting Light Rampant; note any extraneous phase objects.
3. Check center, floor collision, movement and boundary. Stop if unsafe.
4. Only after arena state is resolved, start Light Rampant and inspect tower/marker visuals.
5. Leave/reset and check teardown begin/complete messages; retain local crash logs if it fails.
6. Re-enter the diagnostic and later Light Rampant without restarting FFXIV.

Next evidence needed: a read-only capture of the active layout/filter, scene,
weather and every map-slot resting state during real FRU P2 (after thaw, before
Light Rampant), compared with the arena-only diagnostic. This is needed to choose
an initialization preset safely; do not replay arbitrary packets.

Validation after these changes: Linux plugin build succeeds (0 errors, 21 existing
obsolete-API warnings); plugin NUnit **677/677 passed**, relay **41/41 passed**.
The previous 674 plugin tests remain passing, with three new regression/diagnostic
cases. `git diff --check` passes. Headless fakes do not validate native rendering,
collision, actual AVFX self-destruction or the crash mitigation in FFXIV.

### Coordinated Duty Recorder WebSocket experiment

A local OverlayPlugin WebSocket capture subscribed to LogLine, ImportedLogLines
and ChangeZone, confirmed incoming events before the user resumed playback, and
recorded the P1→P2 replay. Raw capture remains outside the repository under /tmp;
no player data is included here. The capture stopped normally after the user
paused, with 6,242 messages (6,238 LogLine and four ChangeZone).

Replay combat text contains P1 Burnished Glory followed by P2 Quadruple Slap,
Mirror Image and Diamond Dust. Mirror Image→Diamond Dust is 5.085 seconds in the
capture versus 5.135 seconds in original pull 4, supporting local sequence
alignment across that uninterrupted interval. Pauses and fast-forward prohibit
using overall capture wall time as encounter time. The replay was already near
the end of P1 when coordinated playback resumed; this was not a fresh full-pull
capture.

Crucially, no MapEffect (257), DirectorUpdate (33), or ImportedLogLines messages
arrived. Original pull 4 records index 0x17 / 0x00020001 at 161.621s, then index
0x00 effects around Diamond Dust. Those environment packets have no counterpart
in this WebSocket capture. The connection and event subscriptions worked; whether
Duty Recorder bypasses packet processing or these streams omit its playback
requires separate investigation. This experiment does not establish a P2 native
preset. Next useful evidence is a read-only live-client layout/scene/map-slot
snapshot while the replay is paused in P2, rather than another identical
WebSocket-only replay.

## Native Duty Recorder offline investigation

**Outcome B: transition inputs and map resting states recovered; full native
layout/scene and collision setup is not yet proven. No simulation/native-state
changes were made in this investigation.**

Inspected the recording named `Futures Rewritten (Ultimate) 2026.10.07
23.02.28.dat` in the local replay/autorenamed directory. Its header begins at
21:07:46 JST on October 7, not at the filename's time or pull 4. Territory 1238
and ContentFinderCondition 1006 agree with FRU. Format 5, Windows OS type 3,
game build 14006063, 6,880,731ms total duration. The 67,788,628-byte file contains
32 chapters, 531,944 segments and 47 distinct opcodes. Declared data length
67,787,752 exactly matches the physical segment area. Every chapter points to a
segment boundary with a matching timestamp; segment times are monotonic.

### Source and installed-version verification

The local A Realm Recorded checkout is commit
`679f25321d9459abc00e73fa2eade35f8b172043`. Installed assembly metadata reports
`1.0.3.0+679f25321d9459abc00e73fa2eade35f8b172043`, so this source matches the
installed release, rather than merely sharing its version number. Its Hypostasis
submodule is pinned to `c230760c7bbc85a6e37b8477012dcd92ab700dc7`.

- [Game.cs](https://github.com/UnknownX7/ARealmRecorded/blob/679f25321d9459abc00e73fa2eade35f8b172043/Game.cs)
  derives the replay directory from Framework.UserPathString, reads files into a
  native buffer and enumerates segments in its debug ReadPackets method.
- [ReplayManager.cs](https://github.com/UnknownX7/ARealmRecorded/blob/679f25321d9459abc00e73fa2eade35f8b172043/ReplayManager.cs)
  supplies segments from the loaded buffer to native playback. Its seeking logic
  can jump chapters and skip intervening sections; full offline traversal avoids
  treating skipped playback packets as absent from the recording.
- [ReplayPacketManager.cs](https://github.com/UnknownX7/ARealmRecorded/blob/679f25321d9459abc00e73fa2eade35f8b172043/ReplayPacketManager.cs)
  handles custom RSV/RSF opcodes, then leaves other packets to native playback.
- [FFXIVReplay.cs](https://github.com/UnknownX7/Hypostasis/blob/c230760c7bbc85a6e37b8477012dcd92ab700dc7/Game/Structures/FFXIVReplay.cs)
  defines the file structures. Installed DLL field offsets/sizes were checked
  independently with .NET metadata: header 0x68; chapters 0x304 (count and 64
  12-byte entries); data starts at 0x36C; segment is little-endian
  `ushort opcode, ushort length, uint milliseconds, uint objectID`, then payload.
  Chapter offsets are relative to the segment area. Opcode values are not ACT
  line-type numbers. No substantial reference code/assets were copied.

The standalone `tools/replay_inspector.py` is separate from the plugin, uses only
Python's standard library and opens recordings read-only. It validates version,
physical/declared lengths, chapters and segment boundaries; reports frequencies;
filters by time/opcode; and retains every unknown payload through its original
file offset and read-only payload accessor. JSONL output contains anonymized actor
references and payload lengths/hashes, never names, recorder IDs or raw payloads.
No player data was added to the repository. Public opcode names are not assumed
compatible simply because their numbers look familiar.

### Timeline alignment and packet candidates

All 672 single-map records in this replay match the original IINACT log's ordered
index/flags sequence. The original receive times are 874–1012ms later than replay
milliseconds added to the header's whole-second timestamp (median 942.5ms).
This is an observed clock/receive offset, not a damage snapshot calibration.

Pull 4's cast anchors, replay milliseconds:
Burnished Glory 658093; Quadruple Slap 678911/685559; Mirror Image 694578;
Diamond Dust 699716; Light Rampant 796386. Their corresponding log cast receive
offsets near the transition are 917–928ms. The P2 map event at 668630 maps to
21:18:55.550 JST, **161.621s after the parser's pull-4 start at 21:16:13.929**.
The owner's approximate 164–166s is not substituted for this packet alignment.
The 640–710s replay window was inspected in full (5,061 segments, 27 opcodes).

| Replay time (s) | Opcode / bytes | Verified interpretation | Evidence / limits |
| --- | --- | --- | --- |
| 476.128 (pre-pull reset) | 0x0117 / 136 | Bulk map resting-state refresh | Director ID at +0, flags byte at +4, 64 ushort states at +6; all slots 0–53 are 4, trailing 54–63 zero. Native consumer checks director/table readiness and updates states. |
| 668.630 | 0x0198 / 16 | MapEffect equivalent: index 0x17, state 1, timeline 2 | Director ID +0, state ushort +4, timeline ushort +6, index byte +8, padding afterward. Same payload as original 257 event; native playback calls the existing ContentDirector map-effect path. |
| 668.630 | 0x032F / 8 | Server-weather update: weather 35, flags 1, fade 0s | Byte +0 is weather; bit 0 at +1 supplies the SetNextWeather boolean; float +4 is fade. Static consumer resolves to WeatherManager.ServerWeather's virtual SetNextWeather. Splatoon independently identifies P2 using WeatherId 35. |
| 668.630 | 0x0204 / 40 | ActorControlSelf / DirectorUpdate: 0x80000001, arg1 0x4EA7 | Actor-control category 109 at +0; director ID +4; director-update code +8; arg1 +12. Common director consumer dispatches this to the BGM path. |
| 668.683 | 0x0204 / 40 | DirectorUpdate: 0x8000001E, arg1 0x315 (789) | Exact payload verified; native common handler sets a pending ushort and dirty bit, later consumed by another manager. Its role is unresolved; do not call it a scene/filter/festival ID. |
| 699.628 / 703.770 | 0x0198 / 16 | Diamond Dust index 0: 0x00020001 / 0x00080004 | Matches the original log and the cast at 699.716s. |
| 732.827 / 744.816 | 0x0198 / 16 | Index 0x17 freeze/thaw: 0x00200010 / 0x00400001 | Low/high halves are resting state/timeline; freeze leaves state 0x10, thaw returns to 1. |
| 873.756 / 922.590 | 0x0198 / 16 | Intermission ice index 0x18: 0x00020001 / 0x00080004 | Splatoon's `P2 Delete Intermission Ice` specifically handles this index to remove ice; actual collider binding still needs native inspection. |
| 950.097 | 0x032F and 0x0204 | P3 weather 105, director 0x8000001E arg1 0x316 (790) | Follows intermission; weather subsequently changes to 106 at 958.374s. Distinguish intermission start from the later established P3 state. |

Eleven P1→P2 transitions show the same map 0x17/state1/timeline2 and weather35
pair (pulls 2/4/6/7/8/9/12/13/14/15/16). Pull 6 repeats at replay 1749.383s;
its director arg789 follows 56ms later, versus 53ms in pull 4. All 17 bulk map
records carry the same state array; the first has flags5, subsequent resets
flags1. This refresh is absent from the previous WebSocket experiment and is
useful new baseline evidence.

Applying recorded low-half state changes over each bulk reset reconstructs the
map **resting-state vector**: immediately after the P2 switch, and again before
Light Rampant, slots 0–53 are 4 except **0x17 = 1**. In particular, intermission
ice slot **0x18 remains 4**. This is a verified logical state vector, not proof
that blindly writing it recreates every animation, geometry instance or collider.

### Native interpretation and remaining state

A read-only static inspection of the installed `ffxiv_dx11.exe` (game version
`2026.09.15.0000.0000`) followed the installed replay dispatch signature; nothing
was injected or called. BossMod's documented opcode-trace technique also mapped
775 dispatch entries without unhandled cases. Its public enum names leave 0x0198
and 0x032F unrecognized, so their meanings were established from packet/log
correlation and actual replay consumers instead of assigning stale enum names.
These opcode mappings are recording/build-specific.

Reproducibility anchors in the on-disk executable (RVAs, **not callable hooks**):
replay dispatch 0x84D600; single-map consumer 0xB844E0 → 0xB75240;
bulk-map consumer 0xB842C0 reads states at payload+6;
weather consumer 0xB4B700 → 0xE14440 reads byte weather/flags and float fade;
common director update 0xB73F10 dispatches 0x8000001E to 0xB74453, with deferred
consumers 0xB75360/0xB753F0 calling 0x854B10. That last path loads a row using
native sheet index 0x145; its manager/schema has not been verified. Native sheet
indices are not assumed identical to EXL row IDs. No production signatures,
offsets, writes or new hooks were introduced from these addresses.

No separate phase-specific InitZone occurs: only the recording's initial InitZone
(0x032B, 136 bytes) exists. Its known prefix confirms territory1238/content1006;
other fields remain opaque. No festival or explicit scene/layout packet has been
verified. Unknown packets remain preserved, so this is **not** evidence those
systems never change. The recording supplies transition commands, not snapshots
of the resulting active layout/filter, selected environment scene or colliders.

AnoMech's current weather abstraction is explicitly SKY-TINT ONLY; its existing
comment documents that prior WeatherManager writes leaked into the inn. Do not
reintroduce that path merely because the packet's weather value is now known.
Hyperborea's festival/debug-scene facilities do not establish FRU values either.
The unexplained director arg789, initial layout selection, native slot bindings
and weather lifecycle restoration still prevent claiming a fully safe P2 preset.

Smallest next step: while Duty Recorder remains paused in established P2, obtain
a **read-only** snapshot using supported typed client fields of GameMain's
current/transition territory filter keys and content condition; ActiveLayout's
selected filter and active layers; EnvManager/WeatherManager current, forced and
scene state; and ContentDirector map bindings/states (especially 0x17/0x18).
Compare with AnoMech's arena-only diagnostic. Do not probe speculative scene
pointer offsets. Resolve the deferred 0x8000001E consumer if the comparison shows
it affects the required environment.

Once that comparison establishes the prerequisites, the proposed correction is
small: FRU P2 client setup uses the existing map/native abstraction to restore the
verified 54-slot vector (state4 except 0x17=1), preserve 0x18=4, and initialize
only verified phase presentation. Any weather/scene change must restore its
previous state on Leave/restart. No raw packet replay is needed or authorized.

### Offline tool validation

`python3 -m unittest discover -s tools/tests -p 'test_replay_inspector.py' -v`:
**17/17 passed**. Covers malformed magic/version/lengths, truncated records,
chapter bounds/boundaries/times, time filtering, build mismatch, opaque payload
retention, actor anonymization, backwards-time reporting and unchanged input.
The real file passed full traversal and boundary validation. Plugin/relay suites
were not rerun: no production or .NET test code changed in this investigation.

Example, with raw input kept local and sanitized output outside the repository:

```bash
python3 tools/replay_inspector.py '<local FRU recording.dat>' \
  --expect-build 14006063 --from-seconds 640 --to-seconds 710 \
  --opcode 0x198 --segments /tmp/fru-native-map-window.jsonl
```

The example output path must be new: exclusive creation prevents overwriting
inputs or earlier reports. Omit --opcode to retain metadata for all unknown types.

## Read-only native P2 snapshot facility

`/anomech snapshot replay-p2` and `/anomech snapshot anomech-p2` now save
explicitly triggered JSON snapshots in AnoMech's plugin configuration directory,
under `arena-snapshots`. Chat reports the exact local filename. The command is
available during Duty Recorder without starting a scenario. No new hook, packet
injection, activation call, weather write or automatic sampling was added.
Framework-thread capture requires GameMain territory1238/loadstate2 and active
layout territory1238/initstate7. A queued capture is cancelled on plugin disposal.
Each file is created exclusively; labels are restricted to the four documented
values. Original recordings and reference repositories remain unchanged.

Captured fields:

- GameMain current/transition filter keys, content condition and festivals.
- Active layout resource identity, territory/content/filter/level, initialization,
  festival status and active/pending festival arrays, terrain-collider skip flag.
- Filter table keys and their territory/content selection; every loaded layer's
  group/festival/flags and instances, including nested SharedGroups to depth4.
- Instance type, layer/instance/sub IDs, activation flags; SharedGroup SGB path,
  position, prefab flags and selected timeline index. Traversal has count limits
  and explicitly marks children omitted at the depth limit.
- ContentDirector map slots with layout IDs, states, flags and resolved
  SharedGroup binding identity, including 0x17/0x18.
- EnvManager weather/progress, scene presence/weather table/location count and
  current environment/sound/ambient resource paths; WeatherManager current,
  override, selected slot and current/next/forced values for its slots.
- Typed read-only IsColliderLoaded/IsColliderActive queries on SharedGroup,
  BgPart and CollisionBox instances. Graphics activation, prefab collider flag
  and collider activation are separate observations; physics broadphase
  membership is not captured. No collider pointer is dereferenced.

No player names, player/actor IDs, recorder identifiers, arbitrary memory dumps
or pointer addresses are exported. Layout placement IDs and game asset paths are
not player identifiers. The selected environment scene index/forced selector is
unavailable in validated typed fields; it is explicitly marked unavailable.
The downstream manager for director argument789 is also unresolved and is not
read through invented offsets.

### Consumer findings relevant to the comparison

The previously established native chain remains: MapEffect enters
ContentDirector's map table/timeline path; server weather dispatches to
WeatherManager.ServerWeather.SetNextWeather; director0x8000001E sets pending
state consumed by 0xB753F0 → 0x854B10. Additional read-only static inspection
confirms the last function selects a subobject of its manager and loads the
requested row using native sheet index0x145. This still does not name that manager
or establish a layout/festival operation. None of these executable addresses is
used by the new diagnostic code.

[BossMod FRUStates](https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod.Ultimate/Dawntrail/Ultimate/FRU/FRUStates.cs)
uses actor death/destruction and boss targetability to detect phase boundaries.
Its [WorldStateGameSync](https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod/Framework/WorldStateGameSync.cs)
records single/bulk MapEffects and director updates; it does not furnish a native
FRU initialization preset. Actor phase detection must not be used as evidence of
layout selection.

The existing client-structure APIs model festival selection separately from
instance activation and collider activation:
[LayoutManager](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/LayoutEngine/LayoutManager.cs),
[ILayoutInstance](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/LayoutEngine/ILayoutInstance.cs),
[SharedGroupLayoutInstance](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/LayoutEngine/Group/SharedGroupLayoutInstance.cs).
Snapshot code compiles against the installed Dalamud ClientStructs assembly and
uses its typed fields/getters rather than copying offsets from web sources.
Hyperborea's SetActiveFestivals is a mutator and is intentionally not invoked.
Whether FRU's timelines activate graphics and collision together remains a
question for the paired snapshots and manual traversal; the separate getters
alone do not establish that relationship.

### Exact capture procedure

1. Load/reload the newly built development plugin using the existing Dalamud
   development-plugin workflow, outside an active AnoMech simulation. Build output
   is `AnoMech/bin/Debug/AnoMech.dll`; in Wine it is
   `Z:\home\kath\Projekty\anomek\AnoMech\AnoMech\bin\Debug\AnoMech.dll`.
2. Load the October7 FRU Duty Recorder recording. Let the correct P2 arena fully
   appear, then pause, preferably before Diamond Dust/Mirror Image so temporary
   mechanic objects do not complicate comparison. Run
   `/anomech snapshot replay-p2`. Wait for chat's saved-file confirmation.
3. Exit Duty Recorder normally and return to the inn. Open AnoMech, select
   Futures Rewritten (Ultimate) → P2 → Arena diagnostic (P2 initialization
   unresolved). Start Solo, wait at least3s and run
   `/anomech snapshot anomech-p2`. This diagnostic spawns no bots/bosses/casts
   or mechanic VFX. Wait for saved-file confirmation, then `/anomech leave`.
4. Tell the assistant both captures are saved (and give the displayed local
   paths if needed). Keep JSON local; no recording uploads are required.
   If a command reports a loading guard, wait for loading to finish and retry.
5. Optional useful controls, taken during replay without starting AnoMech:
   `/anomech snapshot replay-p1` in established P1 and
   `/anomech snapshot replay-ice` during P2-end central-ice intermission.

At initial diagnostic delivery, comparison was pending and no real-game snapshot
had been received. The following section completes that comparison using the
owner's subsequent captures.

Validation: plugin build succeeded with0 errors/21 existing warnings; full plugin
suite682 passed (previous677 plus5 new snapshot safety/file tests); relay41 passed.
New tests reject private/free-form/path labels before any native access and prove
an existing snapshot cannot be overwritten. Headless tests cannot validate native
pointers, read-only getter behavior, rendering or physics. Gameplay remains
unchanged at that stage; no arena initialization fix had yet been applied.

## Paired snapshots and provisional P2 presentation correction

The comparison above is now completed using the owner's local snapshots
`20261009-140052-3768457-replay-p2.json` and
`20261009-140130-6844836-anomech-p2.json`. Original JSON remains outside the
repository and was read without modification.

Both have the same44 layers, placement identities,54 map bindings, filter table,
zero festivals and scene weather-ID table. GameMain/Layout content condition is
1006 in replay versus0 in AnoMech; filter keys are0 in both. No evidence yet
establishes that writing content condition alone selects the missing presentation.
The initial load brought up the required assets; their activation differs.

| State | Replay P2 | AnoMech before correction |
| --- | --- | --- |
| Map slots0–53 | 4 except0x17=1 | All0 |
| Slot0x17 binding | Layout10865420, layer0x6053, `sgbg_n4gw_a2_gmc01.sgb` | Same binding |
| Slot0x18 binding | Layout10883630, layer0x6215, `sgvf_n4gw_b3561.sgb` | Same binding |
| Slot0x18 CollisionBox child/subID0x07000000 | Loaded, collider inactive | Loaded, collider active |
| Layers0x5F88–0x5F94 and0x5F9F | All814 top-level objects inactive | All814 active |
| Layer0x5FC9 (`n4gw_p2_1`) | All15 top-level objects active | All15 inactive |
| Weather | Env/WeatherManager35; selected slot forced | Env/WeatherManager2; selected slot not forced |

Slot0x18's SharedGroup root remains active in both captures even while its
CollisionBox switches off. Thus root graphics/activation and child collision are
not interchangeable. This directly identifies an inappropriate physical obstacle
in AnoMech, corroborating the earlier replay/Splatoon index0x18 interpretation.
Other nested phase groups also differ: layer0x606E's later-phase BgParts and
layer0x6082's visible objects are active in AnoMech and inactive in replay; these
are bound to the existing map table and covered by restoring resting states.

### Implemented correction and its limits

FRU's P2 phase initializes all54 slots to resting state4 through the existing
native MapEffect abstraction, then applies the observed index0x17/state1/timeline2
transition. For an already loaded group, timeline mask4 explicitly selects its
resting-state timeline. This mask is derived from the native state/timeline
contract, **not claimed to be a newly observed FRU packet**. Static inspection of
ApplyMapEffect at0xB75240 confirms that its timeline argument is passed to
PlayMapEffectTimeline and that failed playback falls back to the resting state.
No direct writes to map item flags or unknown fields are used.

The phase's peer-safe client setup also holds the14 measured background layers
inactive and P2 layer0x5FC9 active using the existing typed ILayoutInstance.SetActive
API. This is an explicitly measured presentation preset; it does not pretend to
decode the missing vanilla scene/layer-set selection command. Requests store
only territory/layer keys, resolve instances afresh each frame, require loaded
territory1238 and layoutinitstate7, and clear on leaving. They retain no native
pointers. Every host/peer installs its own layer holds; scheduled map effects use
the existing host broadcast/retry system. Phase initialization repeats on restart.

MapEffects.Apply now uses typed DirectorModule.ActiveContentDirector with a null
check, and returns not-ready when a slot has no LayoutId, so its existing bounded
retry queue cannot silently lose a setup call before binding. The P2 phase uses
weather35 through the existing sky-only weather abstraction. It does **not** write
WeatherManager's forced slot; that state difference remains intentional until its
lifecycle restoration is verified. No festival, content-condition, director789,
scene-selector or packet-injection write was added.

Code touched: `FruZone.cs`, `MapController.cs`, `ILayoutFunctions.cs`,
`LayoutFunctions.cs`, `MapEffects.cs`, their fakes, `FruArenaDiagnosticTests.cs`
and new `Core/Map/MapControllerTests.cs`. Light Rampant gameplay is unchanged.

### Required manual validation

1. Reload the built development plugin outside a running simulation. Select the
   existing FRU P2 Arena diagnostic entry and Start Solo; wait at least3 seconds.
2. Verify the P2 floor/background, no extraneous phase geometry, and walk across
   the arena center in both directions. Capture
   `/anomech snapshot anomech-p2` so the same map/binding/collider comparison can
   verify the correction. Confirm0x18's CollisionBox collider is inactive.
3. Leave safely, confirm the inn's weather/presentation, re-enter diagnostic
   without restarting FFXIV and repeat movement/capture.
4. Only after the diagnostic passes, start Light Rampant and check its native
   mechanic presentation and safe completion/leave. Native tower presentation
   remains independently unverified.

No post-correction native capture has yet been provided. Rendering, actual physics
traversal, weather restoration and repeated in-game entry are still unverified;
this is not labelled a complete native P2 transition reconstruction or crash fix.

Validation after correction: plugin build0 errors/21 existing warnings; plugin
suite**684/684 passed**, relay**41/41 passed**. The two added tests check all54
resting states precede the P2 transition, the15 layer requests/phase weather,
clearing on teardown, rejection outside a session, and leave/re-entry without
stale requests. Existing mechanic assertions were preserved.


## P2 stable-state confirmation and startup ordering — 2026-10-09

Compared the local 14:33:43 AnoMech P2 snapshot with the 14:00:52 Duty Recorder
P2 snapshot. All 54 map states and bindings match: slot0x17 is1, slot0x18 and
all other slots are4. All44 layer identities, filters and zero festival values
match. All BgPart and CollisionBox activation values match, including the
inactive central-ice CollisionBox (slot0x18, child0x07000000). Environment state
matches exactly, including active weather35 and the weather resource table.
The P2 layer and excluded phase layers now match the recording.

Remaining differences: content-condition1006 in replay versus0 in simulation;
WeatherManager35/forced versus2/unforced despite matching rendered Env weather;
several SharedGroup aggregate collider flags, and one VFX activation under
layer0x5EEF/layout10849287. Existing spawn-area collider removal can affect
aggregate flags, but each difference is not individually attributed to it.
The VFX identity remains unresolved. These are not a claim of byte-identical
native state. Crucially, measured background/physical-child activation matches,
and the user independently confirms the stable arena looks correct, has no
intermission obstacle, and permits movement across the center.

### Startup correction

The prior phase initializer explicitly waited one scheduler second before sending
its55 map calls. Native territory loading is asynchronous; layer holds are
registered during client setup and become eligible when native loading completes.
That scheduled delay unnecessarily allowed a default-layout presentation window.
It does not by itself explain the precise observed half-second wall-clock interval.

The phase now queues the unchanged preset immediately. FRU-only map dispatch
requires typed GameMain/ActiveLayout territory1238, territory load state2 and
layout init state7. Existing bounded retries handle later map binding readiness.
The existing layer holds,55 flags,weather and native APIs are unchanged. No new
hooks, raw pointers retained across frames, blocking waits, fades or unknown writes
were introduced. Restart discards stale FRU map requests; leave clears requests
and existing holds. Other territories retain their previous dispatch behavior.

Lightweight `[FRU arena]` elapsed-time diagnostics distinguish entry request,
zone-entry return, first observed layout readiness and map retry queue drainage.
Queue drainage is deliberately not labelled rendered-state completion: native
timelines and the first visible frame still require manual observation. This
removes an established artificial delay; elimination of every startup frame is
not yet verified. The stable preset is unchanged and headless assertions still
check all55 calls in order.

### Hammer hazard grace

The previous implementation used event-object appearance (+0.83s after impact)
as hazard activation. This timing establishes visual appearance, not damage
onset. FRU-Sim's puddle controller explicitly leaves proper eruption delay as a
TODO. Local log inspection found no Bleeding status in the complete LR windows;
one pair of other status applications cannot establish which puddle or entry
caused it, so it cannot measure an activation boundary.

Impact damage remains immediate; event-object appearance remains+0.83s. Persistent
practice hazard activation is now+1.5s from impact, explicitly **UNVERIFIED** as
an exact server timing and based on the user's1.5–2s in-game estimate. This gives
0.67s between visible appearance and activation. Expiration stays at+10.83s from
impact and visual lifetime remains12.85s from appearance; the precise server
hazard cutoff and real Bleed ticks remain unresolved. Tests independently check
visible-but-safe puddles, escape before activation and death when remaining after
activation. This is a provisional practice correction, not newly verified game data.

### Validation and next in-game check

Final incremental plugin build succeeds with0 errors/1 existing warning (earlier
full compilation:21 warnings). Plugin tests **690/690**
pass (baseline684 plus four readiness/lifecycle tests and two hazard-grace cases);
relay tests **41/41** pass. Tests cover delayed readiness, ordered one-time calls,
leave before readiness, restart replacement, re-entry and an unrelated territory.
Existing mechanic assertions were preserved. Native rendering/physics after this
ordering change and crash-free full gameplay still require manual validation.

1. Reload the development plugin outside a simulation. Start FRU P2 Arena
   Diagnostic from the inn. Observe the very first visible frame, then cross the
   center. Leave and re-enter without restarting FFXIV. Capture
   `/anomech snapshot anomech-p2` if geometry differs or flicker remains; retain
   the `[FRU arena]` log timings.
2. If the arena remains correct, select Light Rampant/Mana at normal event speed.
   In settings force `Puddle targets` to MainTank + OffTank and play MainTank.
   Move out after each Hammer impact; verify the persistent puddle no longer
   immediately kills its dropper. Exact grace still needs observation.
3. Complete the sequence, check readable towers/orbs, then Leave. Verify the inn,
   restart the scenario, and repeat with the other Banish III setting. Report any
   death's mechanic/time and any exit or restart crash separately.


## Holy Light presentation and movement-window investigation — 2026-10-10

The user confirms P2 presentation/collision and Hammer grace now work in game.
Startup flicker remains awaiting an explicit first-frame observation. This
iteration does not modify the working arena preset or grace timing.

### Orb identity and presentation correction

The scenario previously pre-spawned all six Holy Lights with IsVisible=false.
The native packet builder maps that to DisplayFlags0x6000B (hidden until an
animation reveal), rather than normal visible0x4000B. ShowOrbs only cleared
RenderFlags.Model/Nameplate through SetModelHidden(false); it did not execute
an animation reveal. This is a concrete mismatch between requested spawn and
reveal mechanisms, consistent with the reported invisibility. Headless model
flags alone had not exposed it.

Holy Light BNpcBase17826/0x45A2 maps to ModelChara4355 in the existing pinned
FFXIV datamining sheets: Type3, Model904, Base1, Variant10, AnimationVariant1;
BNpcBase scale2, BNpcName9318. It is a real model, not a meshless helper.
No model override, guessed VFX or native reveal animation was added.

Each trio is now spawned directly through the existing visible-NPC packet path
at its appearance event, untargetable and excluded from the enemy list. Both
orders retain the existing geometry: N/SW/SE versus NW/NE/S. Recorded AddCombatant
positions across all seven complete windows independently confirm radius16:
N/S at local(0,-16)/(0,16), diagonals at(±13.86,±8). First trio additions span
16.356–16.697s after LR cast start, second19.423–19.722s. Pull4 additions are
16.548/19.446s; prior model-show evidence is16.566/19.465s. The unchanged
scenario spawn times19.57/22.47s include its3s lead-in and follow those visual
observations. FRU-Sim's executable sequence separately animates orb appearance
and AoE growth; its comments alone are not treated as timing evidence.

Tuufless Mana guide steps22–25 describe first AoEs plus stacks, movement into
first explosions, second explosions, then center soak. BossMod independently
models HolyLightBurst as radius11 and notes actor spawns precede cast telegraphs
by about3s: https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod.Ultimate/Dawntrail/Ultimate/FRU/P2LightRampant.cs.
The current sequence already implements that ordering. Burst casts22.33/25.36s,
resolutions27.33/30.36s, and visual removals29.02/32.06s are retained. Removal
now disposes the actors rather than leaving invisible orb carriers until49s.
Whole-world teardown also disposes either partially spawned group on cancellation.
Native orb visibility, growth animation and readability still need manual testing;
this fix is not labelled visually verified before that test.

### Center timing and puddle lifetime

Re-extracted seven complete windows from the original local log. Observed
stack-to-central resolution intervals are8.905–8.980s; second Burst-to-central
intervals are3.473–3.527s. Simulation uses8.97s and3.48s respectively. Therefore
no evidence supports delaying the central tower. Timeline tests compare those
normalized intervals with±0.1s tolerance for frame quantization/network receive
variation; these are not assertions that receive times equal damage snapshots.

Each window contains ten puddle event-object additions/removals. Pull4 additions
span13.679–20.117s after LR cast start; per-object removal ages12.837–12.858s.
Across the seven windows removal ages span12.784–13.292s, supporting the existing
12.85s visual lifetime without establishing hazard deactivation. Prior model-state
changes near object age9.794/10.143s remain suggestive fade evidence only. No new
precise server cutoff was discovered. The1.5s impact-to-hazard grace is preserved;
hazard expiration remains10s after appearance, explicitly provisional.

Bots have no puddle immunity: Party.Find includes every living party member and
the same scenario collision check kills bot or player. New tests move an actual
SimPartyNpc into the first puddle just before/after expiration; it dies only
before. Full-route tests reconstruct all ten hazards and check every member's
position against active circles. Central soakers move at6y/s without Sprint;
from the18y camp to the4y tower edge requires14y/6=2.33s. Arrival at the exact
center takes3s, leaving a much smaller margin. The intended Mana route stays
on the camp side opposite the clockwise puddle placements, not through them.
Other existing AI repositioning legs still explicitly use7.8y/s; that training
simplification is not a claim that every player movement is identical.

A too-early tower or demonstrated bot hazard bypass was not found. The user's
reported difficulty is not fully diagnosed: invisible orbs removed an essential
movement cue, and visible fading puddles need not remain hazardous. Re-test the
same transition with readable orbs before changing timings/lifetimes or strategy.
No broad pathfinding or movement change was introduced.

### Validation and reproducible manual test

Changes this iteration are limited to FruLightRampantScenario.cs,
FruLightRampantTests.cs and this roadmap. Eight additional cases cover both
visible-spawn orders, positions/identities/flags, staggered casts and Burst
resolution, completion/cancel cleanup, active versus expired bot entry, safe
Mana routes, normal-speed center soakers and normalized timeline intervals.
Existing112 pair/Banish/orb combinations and seeded catalog tests remain intact.
Final plugin build:0 errors/1 existing warning. Plugin suite **698/698 passed**
(baseline690 plus8 cases), relay suite **41/41 passed**. No existing assertion
was weakened. Native orb rendering and the reported human movement difficulty
remain awaiting the reproducible manual comparison below.

1. Reload the built plugin outside a simulation. Start P2 Arena Diagnostic and
   observe the first visible frame; check center traversal, leave and re-enter.
   If flicker remains, preserve `[FRU arena]` timing logs. No new arena change was
   made this iteration.
2. Select Light Rampant/Mana at event speed1. Set Puddle targets=MainTank+OffTank,
   First orb trio=North+SW+SE and Banish III=Pairs. Play MainTank for Hammer, or a
   chain seat to focus on orb dodges. Confirm three native white orbs at scenario
   ~19.57s and the remaining three at~22.47s, readable telegraphs and ordered
   explosions at~27.33/30.36s.
3. Wait for the first explosions before moving into their safe area. After the
   second explosions, enter the center tower only with2 Lightsteeped stacks;
   reaching its4y radius is sufficient. Use the clear camp-side corridor. Observe
   the transition without Sprint and report the exact scenario time/location if
   a persistent puddle kills you. Tower resolution remains~33.84s.
4. Leave, restart without restarting FFXIV, force First orb trio=South+NW+NE and
   Banish III=Spread; repeat. Verify both groups disappear and cleanup is safe.
   If orbs remain invisible, retain native spawn/model-slot diagnostic lines;
   a passing headless test does not prove successful native model rendering.

## Diamond Dust development prefix — 2026-10-10

The standalone target is **Mirror Image through Hallowed Ray**, excluding the
following unrelated tankbuster. This iteration implements Mirror Image through
Frigid Needle only. `ScenarioCatalog` is unchanged. An explicitly
labelled experimental launcher, available in Release and Debug, permits native
testing of this prefix from an inn; it does not
present it as a full Diamond Dust clear.

### Recording coverage and provenance

All six local log files containing FRU territory transitions were scanned. Five
contain usable pulls; the October 9 file has no detected FRU pulls. There are
98 detected pulls, 50 Mirror Image windows, and **38 complete sequences** with
Hallowed Ray effects. “Complete” describes recording coverage, not a deathless
execution. The October 7 session contributes 11 complete sequences: pulls
2, 4, 6, 7, 8, 9, 12, 13, 14, 15, 16.

| Session | Detected FRU pulls | Mirror Image windows | Through Hallowed Ray |
| --- | ---: | ---: | ---: |
| September 23 | 23 | 7 | 4 |
| September 27 | 24 | 10 | 8 |
| September 30 | 14 | 9 | 8 |
| October 4 | 21 | 13 | 7 |
| October 7 | 16 | 11 | 11 |
| October 9 | 0 | 0 | 0 |

Sources are local `Network_30301_YYYYMMDD.log` files, read-only. The offline tool
`tools/fru_diamond_dust_evidence.py` takes explicit input paths and `--output`,
refuses to overwrite an input, counts malformed records, and exports public game
IDs, NPC positions, job codes and pull ordinals. Actor instance IDs, player names,
player coordinates and raw actor-control payloads are not exported. Generated
working evidence remains under `/tmp`; only sanitized normalized action times
and branch assignments are checked into `RecordedTimelines.json`.

Independent behavioral/strategy references:

- [BossMod Diamond Dust](https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod.Ultimate/Dawntrail/Ultimate/FRU/P2DiamondDust.cs),
  [enums](https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod.Ultimate/Dawntrail/Ultimate/FRU/FRUEnums.cs)
  and [states](https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod.Ultimate/Dawntrail/Ultimate/FRU/FRUStates.cs).
- Local Splatoon `P2 Diamond Dust.cs`: kick signatures, icicle direction tracking
  and marker/position logic.
- Tuufless `_mana/fru/02_usurper_of_frost.en.md`: Mana clocks, clockwise adjustment,
  closest-player baits, knockback groups, four moving-stack paths, gaze and slide
  sequence. No complete FRU-Sim scenario was sought or copied.
- Existing pinned datamining sheets: Action, BNpcBase, ModelChara, Omen,
  Knockback and Status. The existing native replay inspection corroborates the
  slot-0 Diamond Dust effects; no recorded packets were injected.

### Observed variation matrix

Among the 38 complete recordings: Axe 15 / Scythe 23; Stone on supports 19 /
DPS 19; first icicle axes N/S 7, NE/SW 12, E/W 6, SE/NW 13; all eight Oracle
positions; Twin Stillness first 18 / Twin Silence first 20. There are 36 distinct
observed combinations of these five dimensions. This does not demonstrate every
combination or independent/uniform server selection probabilities.

The prefix's deterministic overrides support all **128** combinations of four
axes × two kicks × two Stone groups × eight Oracle destinations. The later Twin
order is represented in evidence but is not rolled or simulated yet.

### Verified registry and timeline

Actor sheet mapping: Usurper of Frost BNpcBase **17823**, name **12809**;
Oracle's Reflection BNpcBase **17824 / 0x45A0**, name **13554**, ModelChara
**4375**. Both use visible native spawning rather than animation-hidden spawning.
Ground-target helpers use BNpcBase **9020**. No separate icicle NPC identity was
established: their cast ground targets come from ActorCastExtra **263**, not
potentially stale source coordinates in the ordinary cast line. Icicle casts use
the Action's Omen **58**; needle crosses use their sheet omen. Their native
presentation is awaiting manual validation; no invented model or VFX is used.

Times below are effect **receive timestamps relative to Mirror Image cast
start**, spanning all 38 complete sequences. They are not exact damage snapshots.
Scenario time includes a three-second native actor creation lead-in.

| Event | Action ID | Observed first effect range (s) |
| --- | ---: | ---: |
| Mirror Image | 40180 | 2.955–2.999 |
| Diamond Dust raidwide | 40197 | 10.072–10.147 |
| Axe Kick | 40202 | 18.946–19.062 |
| Scythe Kick | 40203 | 18.705–18.813 |
| Closest-four House of Light cones | 40206 | 19.512–19.869, kick-dependent |
| Frigid Stone | 40199 | 21.255–21.391 |
| First Icicle Impact pair | 40198 | 21.672–21.792 |
| Heavenly Strike | 40207 | 25.564–25.746 |
| Frigid Needle circle/cross | 40200 / 40201 | 28.328–28.477 |
| First Sinbound Holy stack | 40209, cast 40208 | 29.590–29.789 |
| Shining/Frost Armor | 40185 / 40184 | 37.999–38.191 |
| First Twin Stillness / Silence | 40193 / 40194 | 44.512–44.680 |
| Hallowed Ray hit | 40211, cast 40210 | 57.777–58.107 |

October 7 icicle waves are at approximately 21.69–21.78, 25.70–25.80 and
29.72–29.81s. First pair lies on a randomly selected opposite axis at radius16;
second wave occupies the other parity's four clocks; last pair occupies the
remaining perpendicular axis. The last pair is spawned/cast in the prefix but
**cancelled at prefix termination**, before its resolution overlaps the first
Holy stack. This is an intentional incomplete boundary, not a modeled full
icicle sequence.

Heavenly Strike effect data identifies Knockback row **156** in 87 October 7
effects. Its pinned sheet distance is **12y**, speed **20y/s**; the existing native
carry/forced movement facility is reused. Frigid Stone marker is **345**;
Hallowed Ray marker is **525**. Action-sheet cast bars are retained, including
both kick casts at5.7s; Axe starts later, rather than using a fabricated longer
cast. Kick, cone, spread, icicle and needle collisions use existing EnemyAction
resolution. Avoidable failures are lethal practice penalties; this does not
claim reproduction of exact damage amounts or every real vulnerability status.

Boss conceal/reveal uses existing native actor APIs and recorded action timelines
**0x11E5 / 0x1E43**. Diamond Dust slot0 effects **0x00020001 / 0x00080004** are
scheduled at observed cast-relative~5.1/~9.2s. The P2 environment preset and all
Light Rampant code remain unchanged.

### Implemented Mana behavior and remaining stages

| Stage | Implementation | Status |
| --- | --- | --- |
| Arena / actors / kick / icicles / Stone / cones | P2 reuse, native casts and omens, two kick branches, 2+4+2 icicle cast positions, closest-four cones, selected four Stone targets | Build/test validated; native presentation unverified |
| Knockback / needles | Two Mana groups, 12y knockback, four circles with overlapping orthogonal/diagonal crosses, early icicle avoidance | Build/test validated; last icicle resolution and moving stacks remain missing |
| Sinbound Holy stacks/persistent hazards | Four timed hits and four route variants researched | Not implemented; hazard activation/deactivation unknown |
| Thin Ice / gaze / slides / Twins / Hallowed Ray | IDs, ordering, durations and positioning researched | Not implemented; slide controller unresolved |
| Full scenario registration | No catalog entry | Blocked on required later gameplay, not on arbitrary cosmetic effects |

Mana initial clocks: MT N, OT S, H1 W, H2 E, D1 SW, D2 SE, D3 NW, D4 NE.
The whole party rotates clockwise45° when the marked group's parity matches the
first icicles. Marked players bait Stone farther out; the other group becomes
the closest four cone targets. After Stone, G1 (MT/H1/D1/D3) takes the first
icicle axis on the W→NE half, and G2 takes its opposite. Bots move at normal
speed6, with no invented Sprint or hazard immunity. Tests confirm actual
positions before knockback and radius18 afterward. Incorrect kick positions,
overlapping Stone, standing in an icicle, or standing in a Needle circle kill
through the same effect rules for bots and the player.

### Thin Ice and Holy hazard engineering gap

Thin Ice status **911**, param **0x0140**, is observed around38.2s; the logged
9999s duration is not its true expiry. In pull4, slot17 changes to
**0x00200010** at38.256s and back to **0x00400001** at50.232s. BossMod models
fixed32y slides. This supports presentation and distance research, **not proof
that a map effect alone supplies local-player sliding**.

AnoMech has knockback/carry movement but no ice-input controller. Its existing
`Statuses.AddStatusInit` invokes native AddStatus for doppels; for the local
player that operation is rejected by the client validity guard, so the existing
fallback writes the status slot and calls OnGainStatus. There is no verified
proof that this establishes the real locomotion mode/flags. No Thin Ice status,
new native hook, guessed offset, or experimental physics write was added.
A normal MoveTo would silently erase the defining mechanic and is not used as
an ice substitute.

Sinbound Holy EObj **0x1EBC4F** matches the Holy visual family, but October 7
object removal ages are~14.00–14.04s, unlike Light Rampant's~12.85s. Creation,
visual expiry and harmful lifetime need separate evidence. Bleeding3077/3078
and death events exist, but do not alone establish exact activation/deactivation.
The manually validated Light Rampant grace period is not copied into this
mechanic without verification.

Smallest next engineering step: use the working replay to capture **read-only
locomotion/status state and input-to-slide start/end behavior** in Thin Ice,
including slide duration and whether OnGainStatus alone is sufficient. Match it
against existing validated client structs before proposing a native abstraction.
Separately establish Holy hazard active-state transitions from replay/EObj
updates. The offline evidence is already sufficient to implement later attacks
once these required movement/hazard contracts are known.

### Validation and reproducible manual prefix test

Tests cover all128 supported branch combinations, 38 real normalized timelines
(with±0.2s receive/frame sampling tolerance), deterministic rolls, unique icicle
positions, range validation, deliberate failures, cancellation at several times,
actor cleanup and same-instance repeated runs. They do not establish native
rendering or full Diamond Dust playability.

1. Build with `dotnet build -c Release`, manually reload outside simulation,
   enter an inn, and keep event speed1. Open `/anomech`, select Light Rampant
   (or another catalog scenario) if none is selected, choose your role under
   **Setup**, then expand **Experimental / development** below Scenario settings.
   The entry is labelled **FRU P2 Diamond Dust — Incomplete — ends at Frigid Needle**.
   Release uses seed0 for this launcher; branch overrides remain available.
2. Force **Scythe / Supports / N-S / Oracle NW**, choose seed0, then click
   **Start incomplete Diamond Dust prefix**. Play MT to bait Stone, or a DPS role
   to bait a closest-player cone. Verify the correct existing P2 arena, visible
   bosses, cast bars, icicle ground tells, selected markers, kick, cones,
   knockback and Needle star. Mirror Image begins at scenario3s; Stone~24.34s,
   knockback~28.63s, Needle~31.1s; actors clean up at31.6s.
3. Run `/anomech leave`, then force **Axe / DPS / NE-SW / Oracle SE** and repeat.
   Verify the clockwise adjustment, the other kick and selected roles. Do not
   expect Sinbound Holy, ice slides or Hallowed Ray; the prefix stops before them.
4. Run `/anomech reset` during an active prefix and verify fresh actors/markers;
   leave during casting and re-enter without restarting FFXIV. Report any missing
   native omen/model, unexpected delayed cast after termination, or unsafe
   knockback. The development launcher is unavailable in multiplayer.

Final validation: plugin **883/883 passed** (698 baseline +185 Diamond Dust
cases), relay **41/41 passed**, offline tools **20/20 passed** (17 baseline
+3 evidence-inspector cases). Plugin build: **0 errors /1 warning**; the warning
is NU1900 because the existing NuGet vulnerability endpoint is unavailable.
No dependencies were installed and no existing assertion was weakened. The
final evidence scan reported zero malformed records across all six source files.

Reproducible Linux commands from the repository root:

```sh
DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev dotnet build -c Release --no-restore -v quiet
DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev dotnet test tests/AnoMech.Tests -c Release --no-restore -v quiet
dotnet test tests/AnoMech.Relay.Tests -c Release --no-restore -v quiet
python3 -m unittest discover -s tools/tests -v
```

Changed production files: `FruConstants.cs`, `Windows/DebugMenu.cs`, and the five
new `DiamondDust/` files (`Scenario`, `State`, `Actions`, `Ai`, `SettingsWindow`).
Test changes: the optional unregistered-scenario factory in `Support/ScenarioRun.cs`,
Diamond Dust tests and sanitized fixture, fixture copy entry in the test project.
The offline evidence tool/test and this roadmap complete the changes. No native
implementation, arena initialization, Light Rampant or reference source was edited.


### Release development launcher correction — 2026-10-10

The original launcher and Debug menu were both excluded by `#if DEBUG`. The
launcher now belongs to `MainWindow.DrawSections` outside conditional compilation,
under **Experimental / development**, with an explicit incomplete label. Starting
it selects the development instance without persisting it as a catalog selection.
`ScenarioCatalog` and Light Rampant are unchanged. Inn-only, solo-only and active
scenario guards are retained, including a second check on the framework thread.
The duplicate Debug-only launcher was removed. No automatic reload was added.

Use Release for future validation/manual testing unless explicitly requested
otherwise. Build command: `DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev dotnet build -c Release --no-restore -v quiet`.
The user continues to reload the plugin manually.

Release validation succeeded: the exact `dotnet build -c Release` command
(with DALAMUD_HOME set) completed with **0 errors /2881 warnings**, primarily
platform-analyzer warnings and dependency metadata warnings from the solution.
Release plugin tests **883/883 passed**; Release relay tests **41/41 passed**.
No plugin reload was triggered. Native UI appearance still needs the user's
manual reload, but the launcher now compiles into the Release MainWindow.

## Diamond Dust sidebar and native presentation audit — 2026-10-10

This section supersedes the experimental-launcher instructions above. The normal
Release sidebar now lists **P2 Diamond Dust (Incomplete)** before **P2 Light
Rampant (Mana)** under Futures Rewritten (Ultimate). Arena Diagnostic is no
longer in `ScenarioCatalog`; its class, arena initialization and snapshot commands
are retained. The separate development launcher is removed. Diamond Dust uses the
standard role/strategy/settings/start/reset UI. `IScenario.IsIncomplete` identifies
its status and produces a warning; it does not bypass the catalog's clear tests.
`MenuName` allows the Light Rampant label to say Mana without changing its persisted
`FullName` identity, settings or gameplay.

### Concrete defects and corrections

**Same-callback spawn/cast:** icicle, cone, Stone and Needle helpers were created
immediately before casting. Native NpcSpawn is asynchronous; an absent proxy
silently discards ActorCast/ActionEffect delivery. The previous fake instantiated
actors immediately and did not record received effects. All28 meshless helpers
are now prepared at scenario0, positioned/rotated at the original event times,
and reused once for their respective attacks. No additional icicle model was
invented. Before Mirror Image at3s, the prefix checks actor existence, draw-object
availability and spawn failure flags. Failed native setup cancels the timeline
and reports an initialization failure rather than running invisible mechanics.

**Missing effect targets:** native replay headers show Mirror Image targeting the
caster, Diamond Dust and Heavenly Strike carrying8 effect targets, Stone and
House of Light carrying1, and icicle/kick/needle effects carrying0. Previously,
the raidwide and knockback sent0 targets and Mirror Image sent0 instead of1.
New opt-in CastSpec presentation flags send live CharacterManager-resolved party
IDs (up to8) or the caster ID. Empty native effect entries retain client-only
presentation; damage remains in EnemyAction logic. The existing native0/1-target
path is unchanged for all actions not opting in, including Light Rampant. No raw
recorded packets, player identifiers or effect payloads were copied or injected.

**Wrong cone animation target:** the recorded cone's animation target is its
caster, while its effect target is the baited player. The previous implementation
used the player for both. An opt-in animation-target flag now separates these
without changing direction, bait selection or damage geometry.

**Uniform0.6s animation lock:** recorded locks are2.1s for Mirror Image, both
kicks, cones, Stone and icicles;2.6s for Diamond Dust;3.1s for Heavenly Strike;
1.1s for both Needle effects. These locks are now used by this scenario only.
The last Needle effects release at31.42s. Mechanical completion/cast cancellation
still occurs at31.6s; actors remain until32.52s for their1.1s release-animation
window. This is a visual cleanup tail, not Sinbound Holy or an extended mechanic.
Tests explicitly assert both boundaries; early leave/reset cancels the tail.

**Missing Oracle transitions / cut conceal:** Oracle timeline0x11D6 appears at
Mirror-relative6.104–6.128s in all11 October7 sequences. Oracle animation states
0xF9 and0xFC occur at kick-dependent~13.72–14.06s and~20.08–20.40s. Existing
PlayActionTimeline/SetAnimationState abstractions now perform these recorded
transitions at scenario9.12,16.77/17.04 and23.10/23.35s. Boss conceal0x11E5 was
immediately followed by model hiding, truncating its animation; hiding now waits
until18.30s. Reveal0x1E43 at26.55s remains. Initial facing is north, matching the
cast/replay coordinates. Models remain the verified BNpcBase mappings:
17823→4374,17824→4375,9020→480 (meshless).

**Hidden kick castbar:** Oracle was excluded from AnoMech's EnemyList HUD by
`EnemyListMode.Never`. It now uses `OnlyWhenVisible`, while remaining
non-targetable; helper actors remain excluded. This uses the existing practice
HUD to expose the actual kick cast name/bar. It does not establish that the real
encounter's enemy-list membership is identical.

### Event-by-event presentation audit

| Event / scenario time | Expected native presentation | Previous gap | Correction / remaining native check |
| --- | --- | --- | --- |
| Mirror Image,3–6s | Shiva cast/release; Oracle reflection transition | No effect target; missing reflection timeline; incorrect initial facing | Caster target,2.1s lock,north-facing actors,reflection timeline9.12s; verify initial appearance/release |
| Diamond Dust,8.1–13.1s | Shiva raidwide animation and arena response | Zero native targets; default lock |8 party targets,2.6s lock; recorded slot0 effects retained at8.1/12.2s; verify raidwide and arena visuals |
| Icicle cast waves,15.78/19.79/23.80s | Native icy formations/ground tells at8 locations | Same-frame helper creation lost casts |8 precreated9020 helpers, draw enabled, inward facing, ground-target casts40198; exact visible formation still needs in-game confirmation |
| Icicle resolution,24.78/28.79s | Native circles and explosions | Missing caster initialization / short locks | Native effect40198,2.1s lock; third wave intentionally cancelled at31.6s |
| Axe/Scythe,16.04/15.78s | Distinct cast names, kick stance and out/in release | Oracle transitions absent; HUD exclusion; default lock | Correct existing action IDs **40202/40203**, F9/FC transitions, visible-caster HUD,2.1s locks; verify stance differences |
| Stone markers,16.26s; impact24.34s | Marked four players and native spread hits | Lockons already scheduled; instant effect could hit an absent helper | Marker345 retained;4 prepared helper casters, native targeted40199 effect and2.1s lock; verify marker and spread impact |
| Cone baits,22.58/22.84s | Four directional native effects aimed at closest players | Absent helpers and incorrect animation target |4 prepared center helpers, caster animation target/player effect target,2.1s lock; action has no sheet omen, so no invented pre-cast wedge |
| Heavenly Strike,28.63s | Shiva animation, party hit effects and12y push | Native packet had zero targets; short animation lock |8 presentation targets,3.1s lock; Knockback156 and movement unchanged; verify local-player native carry |
| Needle cast26.42s / release31.42s | Four circles and8 overlapping cross effects | Absent cast sources; actors deleted during release |12 prepared helpers, **40200/40201** Needle actions, circle Omen196;1.1s locks and32.52s cleanup; verify star/impact completeness |

Action sheets identify AnimationEnd rows3205 (Mirror),3216 (Diamond Dust),3598
(icicles),1704 (Stone),3217/3218 (kicks),11296 (cones),3215 (knockback),1222/6490
(Needles). These are recorded as sheet references, not assumed raw timeline IDs;
the client resolves them through the native cast/effect path. Icicle Omen58 and
Needle circle Omen196 are retained. Scythe, Axe, cones and needle crosses have no
sheet omen; no generic markers or fabricated VFX paths were added.

Replay provenance: the existing format5 October7 FRU recording, pull4 window
694–728s, aligned with Mirror Image at694.578s. ActorCast opcode0x0162 matches
FFXIVClientStructs ActorCastPacket: action IDs repeated at+0/+4, type1, omen delay0,
cast bars2.7/4.7/8.7/5.7s. ActionEffect opcodes0x021C and0x0313 match the typed
Header: action ID+8, lock+16, spell+28, variation+30, flags+32,target count+33.
All investigated variations/flags are0; spell IDs match action IDs. Both kicks'
2.1s lock is checked across all recorded occurrences, not assumed from Scythe.
Only sanitized counts/IDs/relations are documented. Original files remain read-only.

### Validation limits and next manual test

A visible native result has **not yet been established** for these corrections.
Ground formation/ice meshes, action impact resources, clone appearance and the
actual rendered state transitions must be checked in FFXIV. The outgoing native
presentation uses empty effect entries, unlike real server damage packets; it
must not be mistaken for a byte-identical replay. No collision/layout changes
were made. Mana assignments, randomized branches, damage rules, knockback row and
normalized event times are preserved; all128 prefix branches remain covered.

The fake now records cast/effect/timeline/state delivery and can defer native
spawning. Tests exercise1/6/18-frame creation, the existing20-frame timeout,
leaving before packets arrive, cancellation during the final impact tail,
re-entry, status/label/order/override registration and all38 timeline fixtures.
A30-frame arrival must fail safely before mechanics; the shared timeout is not
changed to mask this failure.

1. Run `dotnet build -c Release` with the installed Dalamud SDK and manually reload
   outside simulation. Enter an inn. Open `/anomech` → **Futures Rewritten
   (Ultimate)** → **P2 Diamond Dust (Incomplete)**. Arena Diagnostic should be
   absent and Light Rampant (Mana) should follow Diamond Dust.
2. Under **Setup**, choose Mana/your role. Under **Scenario settings**, force
   Scythe, Supports, N/S, Oracle NW. Start normally at event speed1. Verify
   Shiva/Oracle are visible; Mirror Image and Diamond Dust animate; icicles are
   visible at the first/opposite/intermediate clocks; read Scythe's cast and
   animation. Check player lockons, cone and Stone impacts,12y knockback and
   Needle stars. Mechanical endpoint remains31.6s; visual actors vanish32.52s.
3. Reset, force Axe, DPS, NE/SW, Oracle SE and repeat. Verify Axe's distinct
   animation/cast and opposite Stone group. Leave during casting and re-enter;
   then leave during31.6–32.52s to check cleanup without restarting FFXIV.
4. Repeat the already validated Light Rampant briefly to confirm its native
   presentation and arena still behave normally. If DD reports actor setup
   incomplete, preserve the `[FRU Diamond Dust]` and native spawn diagnostics.

Recommendation: validate this prefix's rendered result before proceeding into
Sinbound Holy/Thin Ice. Additional headless passes cannot resolve a missing mesh
or animation. The phase's existing native preset and snapshot commands are intact.

Final Release validation: **892/892 plugin tests passed** (883 baseline plus9
cases, including the newly catalog-driven prefix clear), **41/41 relay tests
passed**. `dotnet build -c Release` completed with **0 errors /3023 warnings**,
principally Windows-platform analyzer warnings and dependency/deprecation warnings.
No existing clear/failure assertion was weakened; cleanup assertions now check the
explicit32.52s visual-tail boundary and still require all actors removed.

A final read-only model check used Marshal-reported offsets from the installed
FFXIVClientStructs SpawnNpcPacket (size656, common offset16). No compatible656-byte
NpcSpawn records or9020 field candidates were found in the pull4 replay window.
Therefore no replay-specific model/attribute override was decoded or guessed.
The model mappings above are verified **sheet mappings**, not claims that a raw
replay NpcSpawn model override was reconstructed. If formations remain invisible,
compare the real replay helper's native model/draw/timeline state with the prepared
AnoMech helper using read-only diagnostics before adding another actor or effect.


## P3 priority change and native-state evidence gate (2026-10-10)

Diamond Dust is shelved, not deleted. The reported `Mirror Image: Native actor
setup incomplete; leave and retry` originates in its scenario-local guard at
3 seconds. It requires every actor, including meshless helpers, to have
`HasDrawObject`. A permanently meshless helper may violate this requirement;
the exact failed actor is not yet known. Fake rendering readiness does not prove
native readiness. Light Rampant has no equivalent guard and does not opt into
the new Diamond Dust presentation flags. No shared regression has been
established; no actor/native changes were made in this iteration.

### Recorded P3 transition and missing state

Re-inspected the original October 7 native replay read-only. Pull 4 yields:

| Replay time (s) | Recorded input |
| --- | --- |
| 950.097 | MapEffect slot `0x17`: state 4, timeline 8 (`0x00080004`) |
| 950.097 | Slots `0x28`, `0x29`: state 1, timeline 2 (`0x00020001`) |
| 950.097 | Director `0x8000001E`, argument 790; weather 105, flags 1, fade 0 |
| 958.374 | Slot `0x28`: state 16, timeline 32 (`0x00200010`); weather 106 |
| 958.474 | Ultimate Relativity cast 40266, recorded cast duration 9.7 s |
| 968.449 | Ultimate Relativity action effect |
| 969.329 | Slots `0x1A`–`0x21`: state 1, timeline 2 |
| 972.595 | Speed cast 40293, recorded duration 5.2 s |
| 980.359 | First Dark Fire (40276) and Unholy Darkness (40277) effects |

The first meaningful resolution is 21.885 s after cast start in this replay.
This is a received-effect time, not an exact damage snapshot. A proposed opening
slice ends after that resolution, before the first Sinbound Meltdown casts at
981.432 s. Additional pulls must normalize and validate the production timeline.
The simulator's executable `p3_ur_main.tscn` track calls cast at 1.0, assignments
at 11.8, tethers at 15.0, and first fire/stack at 22.9; these offsets agree
approximately after shifting to cast-relative time. Method comments are not the
executable timeline.

The existing P2 preset explicitly holds some background layers disabled and
one enabled. Applying it unchanged to P3 is unsupported. Recorded map/weather
commands alone do not establish P3 layer/filter/festival, prefab and collider
state. No P3 preset, actors, gameplay slice or catalog entry was added. This is
the requested native-state evidence gate, not a claim that P3 rendering works.

### Opening mechanic and presentation checklist

Independent reference: BossMod `P3UltimateRelativity.cs`, `FRUEnums.cs` and
`FRUStates.cs`; Splatoon `The Futures Rewritten/P3 Ultimate Relativity.cs`;
FRU-Sim `ult_relativity_seq.gd`, `p3_ur_main.tscn`; Mana guide
`_mana/fru/03_oracle_of_darkness.en.md`.

- BossMod identifies P3 Oracle `0x45A7`, Delight's Hourglass `0x45A8`.
  NPC name/model mapping and visible spawn/animation configuration still need
  game-sheet/native confirmation before spawning them.
- Opening: Ultimate Relativity raidwide, delayed statuses, eight hourglasses,
  Speed with yellow/purple tethers, first fire spreads and central stack.
- Status references: Unholy Darkness 2454, Dark Fire 2455, Shadoweye 2456,
  Dark Eruption 2460, Dark Water 2461, Dark Blizzard 2462, delayed Return 2464;
  rotation 2970 (`0x10D` counterclockwise / `0x15C` clockwise).
- Tethers 133 slow/purple and 134 quick/yellow. Verify native animation/VFX
  presentation against replay; no paths or animation identities invented.
- Assignments: two short DPS, one short support; one medium of each group;
  two long supports, one long DPS. Short support or long DPS is replaced by ice.
  Unholy Darkness targets must exclude concurrent fires; simulator lists eligible
  groups separately for each of the three resolutions.
- Remaining full sequence: rotating hourglass lasers, medium fire/ice and stack,
  long fire/stack, Return snapshots, stun/rewind, delayed eruption/water/gaze.
  These are beyond the requested first-resolution slice.

### Mana markers and choreography

The Mana guide explicitly warns its tether orientation is upside down relative
to Y-Runytivity. Mana uses short DPS NW/NE, long supports SW/SE, short support
south, long DPS north, medium support west and medium DPS east (tether-relative).
FRU-Sim uses short DPS SW/SE and long supports NW/NE; its assignments cannot be
ported unchanged. Number-to-side tie-breaking still needs verification before
implementing deterministic overrides.

Required local markers: Attack 1/2 for short DPS; Bind 1/2 for long supports.
Both pairs remain eligible in either ice branch. Human role participates in the
same authoritative assignment state as bots; no human privilege or separately
randomized visual state. The guide uses player self-mark macros, so marker
number allocation should not silently inherit simulator role priority.

Read-only Lemegeton source inspected: MIT license; `Plugin.DrawSoftmarkerOn`
resolves actor position every draw, projects it to screen, and uses ImGui
`AddImage`. `Core/UserInterface.cs` uses game icons 61201/61202 (Attack 1/2),
61211/61212 (Bind 1/2). This local draw route is separate from its real marking
command paths. A future AnoMech implementation can draw these game textures from
simulated character positions, owning markers under the scenario/world lifetime;
it must not call Lemegeton's marker command/server paths or require the plugin.
No code/assets copied and no softmarkers implemented before the arena gate.
Acceptance remains actual FFXIV visibility, human eligibility and bot choreography.

### Required read-only P3 capture

Release commands now accept `replay-p3` and `replay-p3-entry`, retaining all P2
labels. Existing snapshot code captures typed layout identity/filter/festivals,
layers and object flags/collider observations, map slots and their bindings,
weather and environment resources. Scene selector, director downstream manager
and physics broadphase membership remain explicitly unavailable.

1. Build `dotnet build -c Release`; manually reload AnoMech. Do not start a
   simulation while collecting replay state.
2. Load the FRU Duty Recorder replay, use pull 4 or any pull reaching P3.
3. Optional: pause just after P3 appears, before Ultimate Relativity starts;
   run `/anomech snapshot replay-p3-entry` and wait for saved-file confirmation.
4. Pause when the P3 arena is fully established and Ultimate Relativity's cast
   begins (pull 4 native replay around 958.474 s, not encounter-relative time).
   Run `/anomech snapshot replay-p3` and wait for confirmation.
5. Supply the generated JSON under the plugin configuration's `arena-snapshots`
   directory. Original recordings stay local. No live progression required.

Compare with the existing manually validated P2 snapshots to identify layers,
collision and native bindings before implementing a P3-specific preset. No
unsafe native probing or speculative state writes are authorized by this step.

### Validation of shelving and capture preparation

`DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev dotnet build -c Release`:
0 errors, 3011 warnings. Release plugin suite: **893 passed**, 0 failed;
Release relay suite: **41 passed**, 0 failed. Count change from 892: one catalog
scenario case removed and two P3 snapshot-label cases added. Diamond Dust's
128 combinations and Light Rampant regressions remain covered. Shelved native
presentation tests now explicitly construct the unregistered scenario. No
assertion of actor rendering or P3 collision correctness follows from these tests.
Manual P3 state capture is the next required step; no automatic reload performed.


## P3 replay snapshots received: provisional arena preset (2026-10-10)

Compared `20261010-055028-5389143-replay-p3-entry.json` (P3 arrival) and
`20261010-055151-1604910-replay-p3.json` (Ultimate Relativity with hourglasses)
against the prior real P2 snapshot. Files were read-only, not copied into the
repository. Both have territory 1238, layout InitState 7, filter key 0 and zero
festivals. Entry weather is 105; established P3 weather is 106. No scene-selector
field or physics broadphase membership can be inferred from this schema.

- The same unwanted background/light layers `0x5F88`–`0x5F94`, `0x5F9F`
  remain inactive. P2 backdrop `0x5FC9` is now entirely inactive.
- Slot `0x17` binds the P2 shared group in layer `0x6053`, now inactive.
  Slot `0x18` remains state 4; central ice CollisionBox has ColliderActive false.
  CollisionBox IsActive can remain true: object activation and collider activation
  are different observations, not proof of broadphase state.
- Slot `0x28` binds layer `0x606E`: arrival state 1, established state 16.
  Its objects have mixed activation states and change across the snapshots.
  A whole-layer activation hold would destroy that distinction.
- Slot `0x29` binds layer `0x5FD8`, state 1 in both captures; its content
  becomes active in established P3. Native timelines must control these groups.
- Established snapshot additionally has slots `0x1A`–`0x21` at state 1.
  Replay puts these after Ultimate Relativity's raidwide; excluded from the
  arena-only preset until the opening mechanic implements their lifecycle.

`FruZone.P3` uses weather 106, initializes all 54 resting slots, then applies
recorded `0x17=0x00080004`, `0x28=0x00020001`, `0x29=0x00020001`,
`0x28=0x00200010` in order. Only the observed inactive background layers are
held inactive using existing guarded layout interfaces. No invasive native
hooks, scene/festival writes or director packet replay added. P2 code unchanged.
The preset is a supported reconstruction candidate, not manually validated.

Added unregistered, solo `FruP3ArenaDiagnosticScenario`, accessible explicitly by
`/anomech arena-p3` in Release. It uses normal Game/Map lifecycle, spawns no
bosses, bots, casts or tethers, and does not add sidebar entries. Snapshot label
`anomech-p3` is accepted. Diamond Dust stays shelved; Light Rampant stays in the
catalog. Ultimate Relativity gameplay and Mana softmarkers remain pending while
we validate this first native milestone; it is not presented as playable.

Manual procedure: build `dotnet build -c Release`, manually reload, exit Duty
Recorder and enter the inn. Run `/anomech arena-p3`; inspect the established
P3 background, no ice/mixed phases, walk through center and around arena. Run
`/anomech snapshot anomech-p3` and supply the saved JSON. Use `/anomech leave`,
then repeat entry without restarting the client. This establishes whether the
recorded map timelines are sufficient without the real director context. Once
confirmed, implement the visible boss/hourglass/markers and first fire-stack
slice against this phase; do not treat the actor-free test as that slice.

Validation: final `dotnet build -c Release` succeeded with 0 errors and 3032
warnings. Release plugin suite: **896 passed**, 0 failed; relay suite: **41
passed**, 0 failed. Added two P3 preset/actor-free/unload cases and one snapshot
label case. Initial cleanup test incorrectly assumed Game.Dispose performs
Map.Unload; corrected the test to call explicit unload, as the P2 test does,
without changing lifecycle implementation or weakening the cleanup assertion.
Native rendering, traversability and re-entry still require the manual check.


## P3 arena visual confirmation (2026-10-10)

User reports the arena looks good and supplied
`20261010-062154-6043701-anomech-p3.json`. Compared it read-only with the real
Ultimate Relativity P3 snapshot: weather 106, layout filters and festivals match;
all recorded layout instance identities are present. Both CollisionBoxes in
layers `0x5EEF` and `0x6215` have ColliderActive false, matching the replay.
This confirms appearance and those collider observations, not full physical
traversability or repeated entry; neither was explicitly reported here.

The snapshot is not identical to the replay's mechanic-active state. Slots
`0x1A`–`0x21` remain at resting state 4 instead of 1, as intended for the
actor-free diagnostic. Several VFX are inactive and some SharedGroup collider
getters differ (the simulation's existing spawn-barrier suppression applies).
Layer `0x5FD8` still has 16 BgParts, 32 Lights and 8 VFX inactive where the replay
has them active, despite slot `0x29` having the same resting P3 command state.
Do not claim those differences are all explained by the absent mechanic: native
timeline/context behavior remains to be checked during the opening presentation
work. No changes to the visually successful preset were made from this comparison.

P3 arena appearance is now manually validated. This clears the visual arena
milestone for opening Ultimate Relativity development, while exact VFX activation,
movement/collision testing and re-entry remain explicit validation items. The
opening slice and client-side Mana markers have not yet been implemented.
Documentation-only update; no build/test rerun required and no native writes made.


## Full Ultimate Relativity implementation (2026-10-10)

This supersedes the opening-slice plan above. P3 arena investigation was not
reopened. The complete supported FRU-Sim sequence is implemented, ending after
Shell Crusher rather than continuing into unrelated P3 tankbusters. The normal
catalog places Ultimate Relativity (Mana) after Light Rampant. Diamond Dust
remains paused and unregistered; its source and tests are preserved.

### Sources and recording coverage

Read the complete `ult_relativity_seq.gd`, `p3_ur_main.tscn` animation tracks,
`ult_relativity_pc_pos.gd`, and the common hourglass script/scene. Mechanic
behavior is independently implemented; no substantial licensed code or assets
were copied. FRU-Sim is GPL-3.0; Lemegeton is MIT and is a reference only, not a
runtime dependency.

Cross-checks: Tuufless `_mana/fru/03_oracle_of_darkness.en.md` and its assignment
diagram; BossMod `P3UltimateRelativity.cs`, `FRUEnums.cs` and `FRUStates.cs`;
Splatoon's `P3 Ultimate Relativity.cs`; pinned game sheets; native replay
segments from the October 7 recording. `tools/fru_ultimate_relativity_evidence.py`
is a read-only extractor exporting numeric mechanic events and relative times,
without player identities. Original logs and replay files remain unchanged.

Surveyed the available `Network_30301_*.log` recordings: eight sequences, five
complete through Shell Crusher. October 7 contains six sequences in pulls
4/6/9/13/15/16; pull 9 ends before final resolution, the other five complete.
September 30 and October 4 each contribute one incomplete sequence. The
sanitized `RecordedTimelines.json` fixture tests all five complete timelines.
It contains no character names or entity IDs. Receive-time comparison tolerance
is 0.25 seconds; damage snapshots are not asserted equal to receive timestamps.

### Sequence and native presentation

Times below are seconds from scenario start; the initial Ultimate Relativity
cast begins at 3 seconds, giving native actors time to materialize.

| Scenario time | Implemented behavior |
| --- | --- |
| 0 / 3 | Visible Oracle, meshless effect helpers / Ultimate Relativity cast and raidwide |
| 13.83 / 14.38 | Fire/ice/stack/final/Return assignments and local markers / eight visible hourglasses and map slots `0x1A`–`0x21` |
| 17.05 / 17.13 / 22.63 | Native slow/fast tethers / Speed / speed effects and tether removal |
| 24.90 / 34.90 / 44.90 | Three fire/stack resolutions; ice resolves with the middle wave |
| 25.88 / 35.94 / 46.93 | Hourglass rotation statuses, followed by their native casts |
| 29.80 / 39.83 | Record first five / remaining three Return positions |
| 30.03 / 40.07 / 51.06 | Three waves of nearest-player laser baits; each glass fires ten shots |
| 53.84 / 55.30 | Stun and movement lock / physical Return to stored positions, preserving facing |
| 56.90 | Four eruptions, four-player water stack and three gazes; markers removed |
| 57.83 / 61.10 | Shell Crusher cast / eight-player stack |
| 65 | End after native animation lock; remove actors, statuses, markers and restore hourglass map slots |

Subsequent laser shots begin 2.04 seconds after the bait and repeat every 1.07
seconds, rotating 15 degrees. Timings are supported by multiple receive timelines
and executable simulator tracks. No speculative native addresses, animation
states, VFX paths or packet replay were introduced.

Oracle uses BNpcBase 17831 (`0x45A7`), name 9832, model 3057. Hourglasses use
17832 (`0x45A8`), name 9825, model 476, explicitly visible spawns. ACT confirms
40291 is cast by hourglasses, while 40235/40292 are emitted by base-9020
helpers. Separate helpers own simultaneous fire, ice and stack effects to avoid
cancelling one another's native animations. Existing actor lifecycle and native
proxy facilities supply all spawning and cleanup.

Actions: 40266 Ultimate Relativity; 40293 Speed; 40294/40295 speed effects;
40291 hourglass wind-up; 40235 initial laser; 40292 repeating laser; 40276
fire; 40277 stack; 40279 ice; 40274 eruption; 40271 water; 40278 gaze;
40286/40287 Shell Crusher cast/effect. Cast times and geometry use the Action
sheet. Native replay action-effect headers support the implemented animation
locks and target lists, including zero-target ice/repeating lasers, five or six
stack targets, seven gaze targets and eight raidwide/Shell targets. An optional
presentation-target selector is the only shared action change; default behavior
for existing scenarios is unchanged.

Statuses: 2454 stack, 2455 fire, 2456 gaze, 2460 eruption, 2461 water, 2462 ice,
2464 delayed Return, 2452 Return, 4163 stun. Fire durations are 11/21/31 seconds;
ice is 21, final debuffs 43. Return records at the two observed timings.
Hourglass rotation status 2970 uses parameters `0x15C` clockwise and `0x10D`
counterclockwise, with sheet ParamEffect 28 and four-second duration. Native
status/cast/action facilities render these without invented resource paths.

### Mana assignments, markers and bots

Relative north is the single yellow tether; other yellow tethers are SW/SE,
purple E/W. This is Mana's 大 formation, not FRU-Sim's inverted Y convention.

| Assignment | Relative position | Local marker |
| --- | --- | --- |
| Two short DPS | NW / NE | Attack 1 / Attack 2 |
| Medium DPS / long DPS | E / N | None |
| Short support / medium support | S / W | None |
| Two long supports | SW / SE | Bind 1 / Bind 2 |

DPS receive short/short/medium/long; supports short/medium/long/long. Either the
short support or long DPS receives ice instead of fire. Stack targets are unique
and exclude concurrent fire targets. All eight roles are shuffled with scenario
RNG; the human has no privileged assignment. Marker identities, bot positions,
Return positions and validations derive from the same state. Overrides support
eight reference-north directions, either ice branch, any eight-bit spin mask and
any role's eligible assignment. Bots use normal movement speed, adjust bait
positions for each glass's spin, record appropriate eruption/water/gaze positions,
face outward before rewind, then restack for Shell Crusher.

`Core/Game/SoftMarkers.cs` is a per-world marker model; the UI projects actual
game icons 61201/61202/61211/61212 above moving actors. It never calls native
marking Set or sends party-mark commands. Rendering does not require Lemegeton.
Dead/despawned actors are filtered, failure clears markers, and world/scenario
teardown clears ownership. Human movement and facing remain under user control.

### Validation and remaining limits

Final `dotnet build -c Release`: **zero errors, 3,294 warnings** (primarily
Windows platform analyzers in Linux-built tests and existing compiler warnings).
Release plugin tests: **1,509 passed, zero failed** (previous 896 preserved).
Relay: **41 passed, zero failed**. Existing Python tools: **20 passed**.
Ultimate Relativity contributes 607 focused cases, including all 256 independent
spin masks with both ice branches, all eight reference directions, eligible
human-role/assignment overrides, deterministic state, native identities and
visible-spawn flags, status/effect presentation, timeline comparisons, Return,
full bot survival, cancellation during pending native spawning and repeated runs.
Five reusable softmarker tests cover authentic icon identities, actor attachment,
cleanup and absence of native marking Set calls. The catalog survival test now
includes the full scenario. Deliberate human and bot failures test fire overlap,
rotating lasers, ice donut, inward gaze, bad recorded eruption/water positions
and insufficient Shell stacking. No bot immunity or weakened old assertions.

Headless tests do not verify native model loading, cue readability or authentic
animation rendering. Specifically still require FFXIV checks: visible hourglass
rotation arrows; tether colors/direction; laser/fire/ice/gaze animations; status
icons; four projected softmarkers including the human; Return movement and facing;
cleanup/re-entry. The exact native ground Return trace has no verified VFX path
and is not claimed reproduced. The ice donut inner radius **3 is UNVERIFIED**:
BossMod likewise marks it provisional; outer radius 12 comes from the sheet.
Failures use AnoMech's practice severity model, not exact HP/mitigation simulation.
The known `0x5FD8` inactive background differences remain a presentation caveat;
the working arena preset was preserved. No additional snapshot is required to
begin this playtest.

### Release manual test

1. Run `dotnet build -c Release` (Linux SDK environment:
   `DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev`). Manually reload the plugin;
   no automatic reload is configured.
2. In an inn, select Futures Rewritten (Ultimate) → P3 → Ultimate Relativity
   (Mana), after P2 Light Rampant. Choose `Mana 大 / local markers`, scale 1.
3. First use single yellow tether N, all clockwise and long-DPS ice. Force your
   current DPS role to ShortDps1, or support role to LongSupport1, to check your
   Attack 1 or Bind 1 icon. Repeat with ShortDps2/LongSupport2 for the other icon.
4. At debuff assignment, read your icon/durations. Resolve fire at your clock,
   bait your assigned glass from its spin-safe side, record your final-debuff
   position, and face outward before stun/Return. Verify physical rewind and
   final water/eruption/gaze, then stack center for Shell Crusher.
5. Repeat with short-support ice, all counterclockwise, other reference norths,
   then random overrides. Check all four unmarked assignment categories as well.
6. Let completion remove all actors/icons/statuses; restart without restarting
   FFXIV. Leave once during hourglass casts and once during Return, re-enter,
   and verify no retained markers, lasers or hourglasses. Confirm Light Rampant
   still loads and behaves as before.

Remaining work is manual validation and any resulting focused visual/geometry
corrections, especially the provisional ice radius and Return trace. The entire
supported mechanic executes headlessly; it is not merely an opening prefix.


## Ultimate Relativity playtest polish: Oracle and eight markers (2026-10-10)

The user manually playtested the entire sequence and reports that it works well.
Hourglasses, four original local markers and gameplay are confirmed usable.
The Oracle was invisible; this update targets that appearance and extends the
marker scheme. Timings, RNG, bots, lasers, Return, arena presets, Light Rampant
and paused Diamond Dust remain unchanged.

### Oracle appearance correction

The existing actor is already created once, base 17831 (`0x45A7`), name 9832,
model 3057, center position, facing north, with visible spawn flags. It is not
intermission Gaia (17830 / `0x45A6`, model 4436, name 9326). The generic native
packet builder selected ModelChara but left equipment empty. Model 3057 is a
Type-2 demihuman: its body comes from equipment rather than the base model alone.
The missing body equipment is the identified configuration cause of invisibility;
the corrected native rendering still needs in-game confirmation.

Inspected every matching native NPC-spawn segment (opcode `0x020C`, length 656)
in the original replay, read-only. All seven Oracle spawns agree: body equipment
word 65537 (`0x00010001`: model 1, variant 1, stain 0), main-hand weapon word
4295237459, model 3057, name 9832, zero animation state and model-attribute flags.
Equipment/customize offsets were obtained from the installed typed
FFXIVClientStructs CommonSpawnData structure, not guessed. No raw packet,
character identity or recording content was copied into source.

Added opt-in `NpcAppearance` to EnemySpawnConfig. BattleCharas applies only its
body and main-hand fields through typed CommonSpawnData/LegacyEquipmentModelId
members. Ultimate Relativity supplies the verified Oracle values; absent
appearance preserves every existing encounter's spawn fields. The real spawn's
`0x6000B` display flags include hidden-until-revealed; practice retains existing
visible flags `0x4000B` so no guessed reveal animation is necessary. The same
boss continues casting Ultimate Relativity, Speed and Shell Crusher, with the
existing enemy-list cast bars and action animations. No duplicate boss, readiness
change, unknown native write or arena change was introduced.

### Complete local marker scheme

Validated against the full optional macro block in
[Tuufless's Mana guide](https://ffxiv.tuufless.com/mana/fru/03_oracle_of_darkness/)
and Lemegeton's icon table (read-only). The original four markers retain their
assignment/RNG meaning. Every player now receives one of eight distinct markers:

| Marker | Eligible strategy slot | Existing tether-relative hourglass | Game icon |
| --- | --- | --- | --- |
| Attack 1 | First short DPS | NW | 61201 |
| Attack 2 | Second short DPS | NE | 61202 |
| Attack 3 | Short support, including Ice replacement | S | 61203 |
| Ignore 1 | Medium support | W | 61221 |
| Ignore 2 | Medium DPS | E | 61222 |
| Bind 1 | First long support | SW | 61211 |
| Bind 2 | Second long support | SE | 61212 |
| Bind 3 | Long DPS, including Ice replacement | Single yellow tether (reference N) | 61213 |

The optional DPS-long macro literally says South, as did the requested table.
That text conflicts with the single-yellow reference-north 大 formation and the
successfully playtested existing long-DPS bait at that yellow hourglass. Assigning
that player to reference S would instead select an empty-tether short-fire glass.
This iteration adds Bind 3 to the established long-DPS slot and does not change
bot movement or invert the working formation. All positions rotate with reference
north. Pair numbering retains seeded shuffle order, without human preference or
new role-priority rules. Ice changes the debuff, not the slot or marker.

Markers still appear at 13.83 seconds, follow their actors, disappear at final
resolution (56.90), and clear on completion/failure/cancellation/restart/actor
teardown. The existing projected-icon renderer is unchanged. No native marking
Set, server marker command, shared party-marker write or Lemegeton dependency
was added.

### Validation and manual check

Release plugin suite: **1,532 passed**, zero failed (23 new cases above the
1,509 baseline). Relay: **41 passed**. Python tooling: **20 passed**.
Tests cover the verified body/weapon fields, opt-in isolation, boss identity and
visible-spawn configuration, all eight markers across both Ice branches and eight
reference directions, all eligible human overrides, actor attachment/destruction,
no native marking Set calls and the full existing 512 spin/Ice survival matrix.
Original four-marker Fire eligibility assertions remain in place.

`dotnet build -c Release`: **zero errors, 3,395 warnings** versus the previous
3,294 reported warnings. Expanded test coverage adds Windows-platform CA1416
warnings; no new production warning category is caused by the appearance or
marker changes. Existing obsolete API, analyzer and package-audit warnings are
not cleaned up in this focused iteration. Build warning totals include repeated
diagnostics and vary with which projects recompile.

Manual procedure: build Release, manually reload, select Futures Rewritten
(Ultimate) → P3 → Ultimate Relativity (Mana), Mana strategy, scale 1. With reference
N fixed, confirm one visible Oracle at center facing north, with body and weapon,
Ultimate Relativity/Speed/Shell casts, animations and enemy-list cast bars. At
13.83 seconds confirm eight recognizable markers attached to all eight players.
Use Force role assignment to cycle your role through its four eligible slots:
DPS gets Attack 1/2, Ignore 2, Bind 3; supports get Attack 3, Ignore 1, Bind 1/2.
Repeat both Ice branches, confirming Ice retains Attack 3 or Bind 3. Change
reference north to E and confirm marker assignments and choreography rotate
together. Complete, restart, then leave during casts and re-enter; verify no boss
or marker duplicates. The correction is ready for this in-game check, not yet
claimed manually visible. Previously documented provisional ice inner radius and
Return ground-trace limitations are unchanged.


## Phase music QoL (2026-10-11)

The owner reports the four FRU/DSR practice scenarios work in-game. This pass preserves Light Rampant, Ultimate Relativity, paused Diamond Dust, all assignments and the P2/P3 arena presets.

Playable FRU P2 maps to **Footsteps in the Snow — BGM 230**, `music/ffxiv/BGM_Ban_Shiva1.scd`; playable P3 maps to **The Extreme (Shadowbringers) — BGM 802**, `music/ex3/BGM_EX3_Raid_12.scd`. These are installed BGM sheet entries (client `2026.09.15.0000.0000`), cross-checked against [Orchestrion track metadata](https://github.com/perchbirdd/OrchestrionPlugin/blob/main/Orchestrion/Data/xiv_bgm_en.csv). Installed Orchestrion row 420 independently identifies the Shadowbringers arrangement; orchestration roll paths are different assets from encounter BGM paths. P2 does not use Return to Oblivion. No music files are extracted or bundled.

PhaseMusic uses the existing phase BGM interface; paused/incomplete Diamond Dust and all FRU/DSR arena-only diagnostics remain silent. Existing native BGM playback now has scene/territory ownership checks. Completion, death, reset/leave, failure and disposal release AnoMech's own Content scene override; the engine selects current underlying music instead of restoring a stale song. A changed external selection is not reset or reasserted. Other higher-priority audio controllers can still mask playback and are not forcibly disabled. Same-ID external ownership is inherently indistinguishable through the available read-only state.

The cog checkbox is now **Enable Scenario BGM**. New configs default on; existing serialized SuppressBgm preferences are preserved, so explicitly enable it if previously suppressed. Native playback should obey the game's BGM slider/mute and looping. **Native audibility/restoration require manual verification.** The existing offset playback facility used by unrelated scenarios is preserved.

Release validation: **2,806 plugin / 41 relay / 26 tooling tests passed**, build zero errors. Added tests verify both FRU music starts and full-scenario cleanup, alongside unchanged Light Rampant/Relativity regression coverage. Release setup also exposes **Advanced / RNG Seed**; existing FRU assignment controls remain unchanged. See DSR-ROADMAP's QoL section for ownership limitations and the full manual procedure.

Manual check: build `dotnet build -c Release`, manually reload, enable music in the cog settings, then start Light Rampant and Ultimate Relativity. Confirm Footsteps and The Extreme respectively, game volume/mute, clean restarts, release on completion/death/cancellation, and normal inn music after Leave. Check a plugin reload during a run releases its override. Do not treat headless results as proof the tracks are audible in FFXIV.
