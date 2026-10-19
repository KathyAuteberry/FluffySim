# FRU Completionist: CT edge Return and P2 practice

Development date: 2026-10-20, Asia/Tokyo. This supersedes the earlier delivery's inward CT recording coordinates and shelved Diamond Dust status. The owner confirmed the Paradise corrections, JP/Mana defaults and Akh Morn menu cleanup in game; those behaviors were retained. New native presentation still needs client testing.

## Source inventory and provenance

| Scope | Actual source state | Decision |
|---|---|---|
| P1 Fall of Faith | Existing FluffySim Mana implementation; the local AnoMech checkout also contains historical FluffySim custom work | Retain measured implementation |
| P2 Diamond Dust | FluffySim's hidden, capture-tested opening ended before moving stacks/ice; upstream has a complete encounter with NA movement | Complete the measured FluffySim opening with adapted continuation, shared native actions/puddle/slide infrastructure, Mana choreography; register one drill |
| P2 Mirror, Mirror + Banish III | No implementation in either inspected upstream FRU catalog | New drill built on existing native P2 infrastructure; observed local events and BossMod geometry, Mana guide assignments |
| P2 Light Rampant | Main Mana drill plus distinct existing upstream NA alternative | Retain both identities; no mechanic changes |
| P3 Ultimate Relativity / Apocalypse | Already available in main Mana and imported alternatives | Retain; no byte-identical reimports |
| P4 Darklit / Crystallize Time | Already integrated, including Mana CT | Correct CT recording destinations; retain clocks, strategies and other encounter rules |
| P5 Fulgent/Akh Morn, Paradise/Polarizing, Box and sequence | Already implemented; Asu/upstream FRU files duplicate the inspected source generation | Retain working content, no duplicate Akh Morn entry |
| P1 Cyclonic Break / Burnt Strike / Turn of Heavens; P3 later transitions | BossMod/Splatoon references exist, but no additional complete simulator drill in the inspected sources | Exclude from this delivery rather than manufacture a supposedly imported complete scenario |
| P2 Endless Ice Age crystals | Guide/BossMod describe crystals, damage immunity and HP-dependent transitions; no reusable complete upstream simulator | Exclude: credible practice requires additional crystal lifecycle and combat/HP state, not a scripted automatic success |
| Arena diagnostics / original upstream DD | Research tools and a redundant NA implementation | Keep outside the normal practice catalog |

Local AsuMech reference: `d3f4f3e07a9af49d66a2637ef3f329c76fd174d9`. All **45 FRU C# files** compare byte-for-byte with AnoMech `6aec4a5940df56ebc4f70a2fdddeceb31c7848b8`. Their eight scenario families are DD, LR, UR, Apocalypse, Darklit, CT, Fulgent and Paradise (including Polarizing). This is shared AnoMech provenance, not an Asu-only FRU contribution.

A read-only public API check found a newer AsuMech master, `b7a31b630ebae5e1076f65ed069f3a50dda64272`. Its 27 newly included commits add M10S, DSR corrections and release metadata; its comparison against the local reference changes **no FRU source files**. The catalog changes concern unrelated content. None of those commits or releases was imported. AnoMech master remains `6aec4a5`. The local `/home/kath/Projekty/anomek/AnoMech` checkout is at historical `23842e7`, containing custom historical work; it was not mistaken for current upstream. The actual upstream revision was inspected through the existing local Jujutsu reference. Both external checkouts/remotes were left untouched. Existing license notices and upstream attribution remain intact.

References: [AnoMech revision](https://github.com/anomek/AnoMech/tree/6aec4a5940df56ebc4f70a2fdddeceb31c7848b8/AnoMech/Scenarios/Fru), [AsuMech revision](https://github.com/Asunnya/AsuMech/tree/b7a31b630ebae5e1076f65ed069f3a50dda64272/AnoMech/Scenarios/Fru), [Mana P2 guide](https://ffxiv.tuufless.com/mana/fru/02_usurper_of_frost/), [Mana P4 guide](https://ffxiv.tuufless.com/mana/fru/04_gaia_and_shiva/). Local reference revisions: original BossMod `40b0abdd35e81517409bdc81446910f943a75a49`, BossMod Reborn `343f7646ca8ae89055e6e9a61aaa5fe328f8e2f1`, Splatoon `f43b92fa6c130aff00035240adb18901f904f557`. BossMod originals and Reborn P2 DD/Mirror modules were inspected locally; Splatoon's P2 Mirror script independently confirms slot 1=N through 8=NW and blue/red MapEffect flags.

## CT Return recording

The arena radius is 20 yalms. MT records at relative **(-12, -14.5)** and ST at **(+12, -14.5)**, radius **18.822**, about **1.178 yalms inside the boundary**. A South-origin second Tidal set rotates both coordinates by 180 degrees. These implement the guide's outer arms with a collision margin; exact yalms are an inferred placement, not a measured native coordinate.

The working 40.55 crossing departure and all encounter timestamps are unchanged. Recording remains at 44.8. Tests assert the literal tank coordinates and radial clearance at that snapshot for every role, four corners, both hourglass patterns and both difficulties, then finish Spirit Taker, both wings and final damage.

Knockback row 108 is a 20-yalm **directional** push (`Direction=3`), already handled by the core; this is independently corroborated by BossMod's `DirForward` sources. It is not radial from the distant actor. The outer recording arms therefore still land inside the arena after the second wing. No knockback length, immunity window, damage or failure rule was relaxed. The later Spirit Taker tank spreads remain separate destinations.

## Diamond Dust implementation and evidence

The existing opening's cast times, positions, action IDs and effect transitions were preserved. The Reflection remains hidden until its existing reveal timeline instead of appearing before Mirror Image. The third icicle pair now resolves instead of being cancelled at the former prefix cutoff. The original opening AI now respects human control instead of reissuing moves after a takeover.

The continuation adds four healer-targeted four-person Holy stacks, native lingering puddles, transformation and gaze, Thin Ice, two opposite Twin cleaves and Hallowed Ray. Mana uses the original clock assignments with the whole-party clockwise adjustment for stone/icicle parity; H1/MT/D1/D3 and H2/ST/D2/D4 stay together. The later clockwise/counterclockwise rim paths depend on the reflection's relation to the two parties. An aligned-pattern angular stagger leaves room between eight-way needle lanes. Bots plan 32-yalm straight ice slides through unoccupied corridors, use a recovery slide where needed, look away from the gaze, and return for the final eight-person line stack. They remain damageable and wrong decisions really fail.

`FruP2DiamondDustAi` reuses the existing slide and puddle-clearance solver through a Mana continuation mode; its original NA entry remains hidden and unchanged by default. Encounter scheduling does not branch on the selected regional strategy. The 1.03 continuation offset aligns the preserved main knockback timeline with the shared continuation functions; it does not account for loading or idle time.

The existing **38 sanitized DD recordings** now test the continuation too, not just the prefix. Receive-time action effects match within 0.2 seconds, including all three icicle waves, four Holy impacts, gaze/frost, both possible Twin orders and Hallowed Ray. Receive timestamps establish presentation ordering; they are not proof of every damage snapshot or earliest puddle damage.

| Event, scenario-relative | Native IDs / method | Evidence and confidence | Remaining limits |
|---|---|---|---|
| Mirror Image 3.0, DD 8.1 | 40180 / 40197; prepared native actors | **Observed** existing fixtures | Rendered actor/model state in client |
| Icicle starts 15.78 / 19.79 / 23.8 | 40198; sheet 9s bar, engine snapshot offset | **Observed** all three native effect times | No invented cancellation after wave two |
| Kick / cones / stones / needles / KB | Existing 40202/3, 40206, 40199, 40200/1, 40207 | **Observed** prefix fixtures; game-sheet geometry | Scythe hit damage delay retains the upstream unverified estimate |
| Holy effects 32.73, 34.297, 35.863, 37.43 | 40208/9; healer stacks, EObj puddles | **Observed** effect times; **corroborated** targets/stack size | 1.5s puddle arming grace is **inferred/unverified**; clean pulls do not establish earliest damage |
| Gaze/Frost 41.08; ice 41.20–53.48 | 40185/4, status 911 param 320, freeze/thaw MapEffects | Gaze effects **observed**; 88 local FRU Thin Ice status applications all have param 320; slide pathway corroborated upstream | Human input-driven native slide and collision require client verification |
| Twins 44.03 / 49.68 cast starts | 40193/4, then 40196/5; native cone actions | Effects **observed** in fixtures, opposite geometry corroborated BossMod | Exact damage delay and model animation alignment |
| Hallowed Ray cast 55.33, effect 60.93 | 40210/11, random seeded target, marker 525, eight-player line stack | **Observed** effect windows, target/icon corroborated BossMod | Native marked-target presentation |

All seeds roll valid support/DPS stone splits, four icicle axes, eight reflection positions, eight gaze positions and either Twin order. Settings force useful patterns. A separate read-only scan of the October 7 archive found 88 FRU status-911 applications, all with parameter 320. The native status pathway supplies this observed Thin Ice parameter to the human; bots use the existing simulation-only `IceSlideTo` pathway. Headless fakes do not reproduce a human pressing a movement key under real Thin Ice. If that native behavior is inert, the client test must report it; this delivery does not claim that bot slides alone prove human native sliding.

## Mirror, Mirror implementation and evidence

The encounter creates one blue and two red mirror sources, with red mirrors 90 degrees apart and no occupied slot duplicated. All six relative red-pair choices are tested for each blue octant; five of those relative choices occur in the 17 observed windows. The sixth satisfies the corroborated encounter constraints but was not observed in this small sample.

The boss follows MT between attack setups; following is explicitly released before the cast. The party AI places tanks/melee opposite blue, healers/ranged at blue, with support players outside and DPS inside each fan. It bases the first fan on the boss's actual dragged position, not an ideal cardinal coordinate. After the simultaneous reflection, melee take the nearer red mirror (clockwise on a tie), ranged take the other. Banish uses MT/D1, ST/D2, H1/D3 and H2/D4 pairs or eight separated players; healers move inward while tanks stay near the edge. Boss movement follows the scheduled MT tank ownership; a full Provoke/Shirk threat table is not implemented.

Local read-only parsing of the October 7 and October 14 logs found **17 FRU Mirror, Mirror windows**, including 15 Banish resolutions. The committed fixture contains only source date/ordinal, game action IDs, relative times, mirror pattern and MapEffect state/index; no player identity, actor ID, log packet or raw capture is committed.

| Event, scenario-relative | Native action/state | Evidence | Outstanding |
|---|---|---|---|
| Mirror cast 1.0, mirrors appear 6.05 | 40179; blue `0x00020001`, red `0x02000100`, slots 1–8=N through NW | **Observed** log packets; Splatoon corroborates slots/colors | Native rendering with inn/offline cold load |
| Boss Scythe 12.14 cast; blue effect 18.20 | 40203 / 40204, 4–20y donuts | **Observed** effects/casts; sheets/BossMod geometry | Damage/visual timing at unsafe donut edges |
| Red glow 18.11; red cast 18.20 | `0x08000001` on both red slots; 40205 ten-second casts | **Observed** packets/casts | Actual reflected animation appearance |
| Cones 18.82 / 28.80 | 40206, four closest at each source, 15-degree half-angle | **Observed** effect times; targeting/geometry independently corroborated | Native hit/impact boundary and telegraph fidelity |
| Mirror hide 20.20 blue / 30.20 red | `0x00080004` on relevant slots | **Observed** packets | Restart while layout effects are still playing |
| Banish 29.30 cast / 34.36 effect | 40220/21 visual, 40222 pairs / 40223 spreads | **Observed** 15 resolutions; target rules/geometry corroborated | One/four-orb native tell |

Mirror colors come from native layout MapEffects. Attack actors are invisible native helpers at the matching positions; unrelated visible boss models are not substituted for mirrors. An optional **Mirror marker fallback** uses a blue bind marker and red attack markers if a native layout effect fails to render. It is explicit, off by default, and not presented as verified native mirror rendering.

## Resulting normal FRU menu

- P1: Fall of Faith.
- P2: **Diamond Dust**, **Mirror, Mirror**, Light Rampant.
- P3: Ultimate Relativity, Apocalypse.
- P4: Darklit Dragonsong, Crystallize Time.
- P5: Fulgent Blade (including Akh Morn), Paradise Regained (including Polarizing), isolated Polarizing Strikes, Pandora's Box, continuous Pandora sequence.

The distinct existing upstream alternatives remain available under their existing identities. No new duplicate of an existing FRU mechanic, no standalone P5 Akh Morn, no DSR mechanic addition, and no release/version metadata change.

## Validation and integration

- Full optimized **Debug** unit/scenario suite: **14,976 passed**, 0 failures. Includes the catalog's 40 randomized seeds per strategy, existing FRU/DSR regressions and both newly registered drills. `/tmp/fluffy-completionist-full-final.log`; `tests/AnoMech.Tests/TestResults/fru-completionist-full-final.trx`.
- Final ordinary **Debug** CT/P2 focused suite: **837 passed**, 0 failures. This subsequent run includes the final Reflection-hide/reveal assertion. It covers literal CT snapshot coordinates, full DD/mirror matrices, 38 DD + 17 mirror recordings, gaze/Twin/stack/donut/Banish failures, all invisible-Hard player seats, native setup failure, Pause, repeated Restart and delayed offline entry/Leave. `/tmp/fluffy-completionist-focused-final.log`; `tests/AnoMech.Tests/TestResults/fru-completionist-focused-final.trx`.
- Both final safety backends: **308 passed, 3 Windows-only native-hook skips each**, inspecting ordinary Debug plus an isolated optimized TRACE Debug plugin. `/tmp/fluffy-completionist-safety-final.log`, `/tmp/fluffy-completionist-safetyhook-final.log`.
- Relay **44 passed**; Python tooling **76 passed**. `/tmp/fluffy-completionist-relay.log`, `/tmp/fluffy-completionist-tooling.log`.
- Final solution **Debug build succeeded**, 0 errors, two pre-existing test warnings (CS8602/CS0067). Optimized TRACE Debug build succeeded with no warnings/errors. `/tmp/fluffy-completionist-build.log`, `/tmp/fluffy-completionist-safety-build.log`. Final safety output is `/tmp/fluffysim-completionist-safety-Debug/`; all test output/intermediates remain in Debug-specific paths.

Catalog/BGM regression assertions were updated for two new entries and completed DD music; existing encounter rules and failure assertions were retained. The needle-failure test now takes actual human control so the continuation AI cannot walk it out of the hazard. No damage rule was weakened to make a bot clear. No firewall allowlist, hook ordering, packet relay or teardown implementation was changed.

Anonymous CT change **wsyttunzyvss** is the two-file destination/snapshot fix. Anonymous P2 change **qkqvlnwusrxm** follows it. Both are incorporated in shared **towxunptvwlp**, with exact description `chore: integration commit`; its original parents **uwrxzomlkwxz**, **towkrqovnplv**, **yuzulstmpsqq** and **suxsukpzsnrz** were retained and the P2 tip added. Feature/integration trees compare equal; no new bookmark was created. Recovery checkpoint: `/tmp/fluffy-completionist-jj-checkpoint.log`.

Local master and master@origin remain **5d6b648b6ea6b1e4d0003494e00e2a363f4691d1**. Published historical **plkqvn / 9810be4fa280ad3da2b74e214e1bd0953d2352ee** is unchanged. No concurrent feature work was discarded. No Release build, installed-output write, version bump, push, tag, publication, external source checkout modification or plogons change was performed.

## Native playtest priorities

1. **CT**: inspect Return markers at the actual snapshot, MT/ST within about 1.18y of the boundary on the correct relative outer NW/NE arms. Test North/South origins, both tank seats, early crossing, then spread, first-wing immunity and second-wing landing.
2. **DD**: verify P2 layout/Usurper/Reflection, cast bars, all three icicle waves and stone parity. Play both healer groups through all four reflection-relative cases; deliberately miss a stack and enter an armed puddle. Confirm human movement actually produces an uncontrollable 32y slide, gaze facing matters and both Twin orders require the opposite follow-up. Check final Ray target/line stack.
3. **Mirrors**: verify blue/red native colors and slot placement, both simultaneous donuts and eight cones, delayed red glow/reflections, and clockwise tie handling. Drag as MT from the human spawn; also test ST/healer/ranged. Read one-orb versus four-orb Banish and deliberately resolve one incorrectly. Enable the named marker fallback only if needed.
4. Both drills: Easy/Hard, hidden bots, God Mode, Pause/Resume, Restart during ice or red casts, Stop/Leave during pending spawns; repeat in inn and title-screen offline entry. Verify no icy floor, mirrors, puddles, statuses or helpers leak into the next scenario. Automated safety and lifecycle checks cannot establish native rendering fidelity.
