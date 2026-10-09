# FRU implementation roadmap

Updated 2026-10-09. **P2 Light Rampant (Mana) is provisionally registered and
headlessly playable for the ring starting formation.** The full supported
sequence clears in automated tests. Native presentation and the limitations
below have not been manually validated in FFXIV.

Paired native snapshots now establish the map bindings, central obstacle collider
and background activation differences. A bounded P2 preset is implemented and
awaits the next arena-only in-game validation; see the final section below.

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

No separate simulation engine is needed. FRU arena initialization, native
telegraphs and actor animations still require investigation and in-game checking.

## Coverage

Statuses used here: Not researched; Research complete; Implemented but
unverified; Build/test validated; Manually validated in FFXIV. Inventory or partial
research does not advance a mechanic to Research complete.

| FRU-Sim selectable scenario | Reference sequence | AnoMech | Status / evidence |
| --- | --- | --- | --- |
| P2 Light Rampant | `scenes/p2/sequences/light_ramp_seq.gd`, `p2_lr_main.tscn` | Mana ring formation, provisional | Build/test validated; native presentation and gameplay caveats below |
| P3 Ultimate Relativity | `scenes/p3/sequences/ult_relativity_seq.gd` | Missing | Not researched; casts recorded in pulls 4, 6, 9, 13, 15, 16 |
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
dotnet build AnoMech/AnoMech.csproj
dotnet test tests/AnoMech.Relay.Tests
dotnet test tests/AnoMech.Tests
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
