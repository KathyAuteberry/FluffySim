# Nidhogg implementation and evidence

2026-10-20. Feature change `zmrxpo`, from published 2.2.0.0. Headless validation is not native-client verification.

## Implemented

Independent P3 Nidhogg entry, default Elemental Easthogg, seven bots, all eight human seats, both bot difficulties, forced player number/arrow and valid seeded rolls. Full arrival/Final Chorus through numbered dives, arrows, wheels, Eye stacks, single towers, Geirskoguls, random-target Drachenlance, four counted towers, Soul Tethers, final south lance and enrage windup. The drill finishes before enrage damage; boss HP/DPS is not simulated.

West/up and east/down players face east. Healers/ranged anchor SW H1, SE H2, NW D3, NE D4; tanks/melee adjust cw>ccw>across. Only anchors bait the four-tower Geirskoguls. MT takes Nidhogg's tether; ST the clone's. They approach different positions under the boss so beams do not exchange accidentally. Tether endpoints remain passable, not assigned by an immunity exception. Human tank hits require invulnerability when local action resolution is enabled; seven bots use the existing mitigation abstraction.

Mechanics use EnemyActions and real Action-sheet cast times/shapes, with explicit measured/reference schedule events. Dive and tower fire/physical vulnerabilities, exact occupancy, stack minimum, overlapping dives, wheel/line/cone hits and tether damage use the same pipeline for bots/human. World-owned actor/tether cleanup and shared pause/God Mode/restart are preserved. P3 weather3/BGM922, reference director layout0x48/map758/slot8 and map teardown are implemented provisionally. Entry is marked incomplete pending native playtests, with phase music enabled.

## Evidence and provenance

State, settings and initial AI structure adapted from Asunnya/AsuMech `b7a31b630ebae5e1076f65ed069f3a50dda64272`; attribution recorded in THIRD-PARTY-NOTICES.md. This is not a wholesale Legacy DamageSolver/raw-packet import. Timelines are inherited source evidence, not locally measured P3 execution. Territory-scoped local IINACT inventory found no P3-specific actions.

Reviewed [Tuufless Nidhogg](https://ffxiv.tuufless.com/elemental/dsr/03_nidhogg/) and local Elemental arrow, four-tower and Soul Tether diagrams. BossMod's P3DiveFromGrace/P3DarkdragonDiveCounter corroborate14y offsets, radius5 towers,8y wheel boundary and exact tower occupancy. These correct Asu's unverified6y donut hole and minimum-only four-tower check. Tuufless's approximately15y arrow description is treated as approximate; no local capture resolves finer geometry. The first lance now chooses a seeded random party target; final lance faces south.

Delayed native spawning exposed a reusable defect: SetModelHidden before actor arrival was lost. SimNpc now reapplies the latest hidden request upon validated packet arrival. Focused tests cover hide/show changes before and after arrival. No FRU encounter source was modified.

## Validation and gaps

Initial all-seat suite exposed tank beam interference; resolved with distinct under-boss positions and passable nonexclusive endpoints. Final deterministic/all-seat/failure/lifecycle suite:32 cases passed in11s, including one bounded test running64 randomized Easy/Hard executions. Earlier native helper suite plus deferred casts:38 cases passed in11s before the final west/east soaker mapping refinement. Native-opening/catalog/visibility subset:4 passed in295ms. Two-artifact safety:308 passed, three Windows-only skips,16s (ordinary and isolated optimized Debug). Additional final west-claim/visibility result belongs in mission progress.

No native rendering, packet capture or user playtest was performed. Native selector/layout, boss/diver models, numbered lockons, helper cast/omen visibility, animation durations, phase music and swap appearance remain provisional. Damage is qualitative practice severity, not a reproduced game HP formula. Large stress runs remain opt-in; the default new bounded matrix is64 executions.

## Native playtest checklist

- Cold title-screen/offline and inn entry: P3 environment, weather, barrier, origin and BGM.
- Boss untargetable during Final Chorus, targetable at engagement; no exposed waiting divers.
- Every number/arrow in each role: west/up, east/down, east facing, correct tower placement and vulnerability lifetimes.
- Both wheel orders, Eye stacks, tower explosions and Geirskogul visual/impact agreement.
- Random Drachenlance and final south cone; counted four-tower pips and H/R baits.
- MT boss/ST clone pickups, both under boss, real invulnerability actions and failed mitigation.
- Pause mid-cast/status/tether, God Mode, hide party, restart before/after resolution, Leave and reentry after P2. No leftover slot8 state, actors, statuses or native casts.
