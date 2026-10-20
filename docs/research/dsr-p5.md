# P5 Wrath / Death — implementation and native checks

2026-10-20. Existing anonymous change `koonkp`, parent shared native infrastructure `ozrvwr`. Resumed without resetting after usage interruption; the change was empty when recovered. P2/P3/P4 integration remains `xwtnow`.

Primary strategy: [Elemental Alternate Thordan](https://ffxiv.tuufless.com/elemental/dsr/05_alternate_thordan/). Reviewed Wrath and Death diagrams from the local Tuufless source. Reference implementation pinned to AsuMech `b7a31b630ebae5e1076f65ed069f3a50dda64272`, P5Wrath/P5Death. BossMod DSW2 P5 components corroborate geometry (6y Empty Dimension safe hole, 8y Spiral Pierce half-width, 6y persistent Liquid Heaven, Heavy Impact rings, gaze/knockback). Reference clocks are not local IINACT measurements.

## Entries

- **Wrath of the Heavens (Elemental)**: opener/dragons, two frontal autos, native trio, crossed dragoon tethers, NW blue jump, clockwise marked movement, twisters, individual Mercy cones, four eruption baits, five Liquid Heaven baits with persistent pools, random green-target Cauterize, Empty Dimension, two Lightning bursts, Quaga and tank-buster connection.
- **Death of the Heavens (Elemental)**: warrior-relative role order MT/ST/H1/H2/D1/D2/D3/D4, Dooms North 2-2 spread, fixed dives/lightning and expanding earthquakes, twisters, four non-Doom cleansing puddles, anchored Doom Playstation allocation, both gazes, knockback/chains, Heavensflame and Doom cleansing, eight targetable meteors and LB2/deadline, Quaga/tank-buster tail and scripted sub-3% yield.

Seven bots/all eight human slots, Easy/Hard, pause, God Mode, restarts and world-owned teardown use the established engine. Human caster LB2 resolution is observed through the same local action event used by the P4 rescue; no fabricated server-bound actions.

## Reference corrections

- Lightning cannot also be an eruption/Liquid Heaven target. Green dive target can receive eruptions; its route holds the Cauterize bait until the cast locks, then moves.
- Marked players dodge clockwise by mechanic assignment, not an arbitrary role order. Dooms use Elemental conga order and the non-Doom marker line is west-to-east north of center.
- Removed an unverified restriction forbidding all four Dooms in one light party; primary strategy describes four random players. Gaze sources are constrained to 135/180 degrees apart.
- Circles select the farthest **pair of Doom players**, using their actual positions; they are not simply the two players farthest from center. Circle holders move inward after baiting symbols. Cross priority follows conga order.
- Liquid Heaven pools persist and punish Magic Vulnerability, unlike the reference's cosmetic-only pool. Native effect helpers are pre-spawned before use.
- Meteors do not auto-fade as success. Bot caster/ranged LB2 destroys a three-meteor arc, bounded bot DPS clears the remaining five, and surviving meteors fail at the deadline. Human caster must supply LB2 or enough targeted actions. This is a qualitative practice budget, not a potency/DPS simulation.

## Arena and evidence limitations

Territory968/origin(100,0,100), native weather46/BGM925, director layout0x64/map765. Weather46 now uses the readiness-gated native weather finalization already established for P3/P4. Phase slots are cleared and director/MapEffects wait for readiness. Dark-Thordan scenery expectations derive from weather-keyed bg_07 selectors and sanitized IDs from the existing P2 arena snapshot; collision selectors retain the native floor. SharedGroup roots can remain active independent of selected descendants, so the P5 signature checks BgPart instances and loaded collision state rather than assuming every root toggles. Fixture is **derived**, not a captured P5 native success.

Absolute mechanic clocks, twister activation/1.5y touch radius, Mercy cone angle, pool pulse intervals and the additional chain-stretch threshold are inherited or simplified estimates. The post-Meteor connection/yield uses a practice schedule and scripted HP. Gaze is a drill failure rather than a full Hysteria movement simulation. Meteor damage is simplified. Some native departure/jump/yield animations still require capture and user comparison. No P5 native fidelity claim before owner playtesting.

## Validation / recovery

Debug build passed. Initial Wrath4 seeds703ms; initial Death4 seeds1.9s. Bounded mechanics/restart20 cases8.6s (includes48 randomized executions). Assignment/seat/difficulty/pause/human-failure/native-policy set48 passed6.0s. Integrated P5/native contracts212 passed13.6s. Both safety backends308 passed/3Windows-only skips each18s against fresh ordinary and optimized Debug artifacts. No Release builds/push/tag/version/plogons/FRU edits.

`koonkp` is integrated into existing `xwtnow`, preserving `ktmmtz`, `suprns`, `kltull`; catalog count is eight DSR entries. Native startup and packet-isolation checks pass. Continue P6, then P1.

## Native playtest

1. Direct inn/offline entry into each entry: weather46 scenery/collision, no stale P1/P3/P4 state, correct boss/knight/dragon models and BGM.
2. Wrath: native jump/charge/tethers, twisters and spread geometry, clockwise routes for all marked roles, green eruption bait, Lightning exclusion, native Mercy/eruption/pool/dive visuals and safe final donut/Lightning placement.
3. Death: every Doom pattern, north-relative spread and quake safe lanes, dives/twisters, actual puddle/PS marker placement, farthest Doom circles, both gazes and real knockback/chains. Deliberately miss a cleanse and overlap Heavensflame.
4. Meteors: LB gauge/targeting/arc presentation, human caster LB2, ranged fallback, missed deadline and final boss departure. Confirm tail timings from capture before treating them as retail measurements.
5. Pause/Restart/Leave during each mechanic; re-enter P2/P3/P4/P5 and check actors, statuses, VFX, collision, input and packet isolation.

## Lower-priority P3 visual investigation

Recovered native feedback: Dive from Grace jump/tower presentation feels incomplete. Action data confirms native Dark High Jump/Spineshatter/Elusive animations (26382-4, animation-end4574) and Darkdragon Dive (26385, animation-end4562). P3 already sends these actions, but hides divers after0.57s and keeps tower casters hidden through their cast/release until2.85s afterward. This is a plausible presentation issue, not native proof. Compare render state/animation delivery before changing it; preserve all mechanics and snapshots. Handle as a separate follow-up after P5 validation, without blocking P6/P1.

## Native playtest repair follow-up

Both invisible Thordan spawns now share the installed Dark Thordan body/weapon appearance; other knights reuse working P2 configurations. See [root causes, validation and retest checklist](dsr-native-repairs.md). Native rendering/results await owner confirmation.

## Focused assignment practice — 2026-10-21

Wrath offers Random (default), Blue marker, Dragoon tether, Green marker, Thunderstruck, Eruption bait, Liquid Heaven bait and No trio assignment (no initial blue/tether; later green/Lightning remain possible). These are assignment preferences, not invented debuffs. A normal valid roll is generated first; a randomly chosen eligible holder is exchanged with the human across **all** assignments. Role symmetry in the existing encounter permits every seat. This keeps two distinct tethers, one blue/one green, two Lightning targets, Liquid Heaven restricted to plain non-Lightning players and Eruption restricted to marked/green non-Lightning players. Single selection prevents requesting incompatible late assignments. Random retains the original RNG path; forced rolls remain seed-reproducible and bots read the resulting state normally.

Death offers Random (default), Doom and No Doom, independently of its existing relative-North conga slot control. A swap between eligible Doom/non-Doom roles preserves exactly four Dooms and four cleansing-puddle players. Playstation preferences are intentionally not exposed: circles depend on the farthest actual Doom pair, and forcing a marker independently would violate targeting. Both preferences persist in configuration and survive retries; per-run assignment state is rebuilt. No stable scenario IDs or names changed.

Owner has confirmed both Thordans now render. Retest each preference, especially Liquid Heaven/Eruption versus Lightning, Doom cleansing and repeated practice after reopening the menu. No new native timing/appearance claim is made by headless checks.
