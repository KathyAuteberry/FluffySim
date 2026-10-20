# Operation Dragonsong: source and evidence inventory

Audit: 2026-10-20. Source inspection, not native playtesting. Implementation order: P2 improvements → P3 → P4 → P5 → P6 → P1. P7 is excluded.

## Baselines and ancestry

Read-only GitHub API verification on the audit date found:

| Repository | Actual master SHA | DSR content |
| --- | --- | --- |
| FluffySim | `235f704cccd364d70d349f94a663a868f6b0b018` | Four Elemental P2 drills and an unregistered arena diagnostic |
| AsuMech | `b7a31b630ebae5e1076f65ed069f3a50dda64272` | Nine registered entries covering P1–P6 |
| AnoMech | `6aec4a5940df56ebc4f70a2fdddeceb31c7848b8` | No DSR source or registered DSR scenarios |

FluffySim master matches local `master@origin`, local master, and `v2.2.0.0`. GitHub reports the release published at 2026-10-20 04:32:53 UTC, neither draft nor prerelease. One clean local workspace; no unpublished shared integration was found. Published integration `towxun` and historical `plkqvn` must remain immutable.

The separate local `anomek/AnoMech` checkout is an old FluffySim integration (`23842e…` parent), **not upstream master**. Actual upstream was inspected with `jj file list -r master@upstream`. Asu cached `d3f4f3…` contains upstream `6aec4a…` by ancestry query; live master is 27 commits beyond that cache. The live pinned source archive was downloaded read-only into `/tmp/dragonsong-asumech`, without changing the reference checkout. Changes since the previous audit affect P3, P5 Death and P6 as well as unrelated M10S. SSH fetch failed on system SSH config permissions; public API verification succeeded. No remote URL or SSH alias changed.

Source links: [FluffySim](https://github.com/KathyAuteberry/FluffySim/tree/235f704cccd364d70d349f94a663a868f6b0b018), [AsuMech](https://github.com/Asunnya/AsuMech/tree/b7a31b630ebae5e1076f65ed069f3a50dda64272), [AnoMech](https://github.com/anomek/AnoMech/tree/6aec4a5940df56ebc4f70a2fdddeceb31c7848b8).

## Complete scenario inventory

Paths below abbreviate `AnoMech/Scenarios/Dsr/`. Adjacent State, Ai, Overrides and Settings files are part of each implementation, not additional scenarios.

| Phase | FluffySim entries | AsuMech entries and shared implementations | Coverage / missing scope |
| --- | --- | --- | --- |
| P2 | `Strength/DsrStrengthScenario`, `Strength2/DsrStrength2Scenario`, `Sanctity1/DsrSanctity1Scenario`, `Sanctity2/DsrSanctity2Scenario` | `P2Thordan/DsrP2ThordanScenario`; `DsrP2MeteorsScenario` delegates to the same implementation with a cropped clock | FluffySim isolates both Strength and Sanctity halves. Asu adds opening Mercy/Might, Heel, Quaga, Ultimate End and Broad Swing around a continuous P2. Those connecting attacks are outside the existing isolated drills. |
| P3 | None | **Registered standalone** `P3Nidhogg/DsrP3NidhoggScenario`, State, Ai, Overrides and settings | Arrival/Final Chorus, autos, Dive from Grace 3/2/3 numbers, arrows, Lash/Gnash, Eye stacks, dive towers, Geirskogul, Drachenlance, four towers, Soul Tethers and enrage windup. Ends before Revenge lands; HP/DPS check absent. NAUR AI, not Easthogg. |
| P4 | None | `P4Eyes/DsrP4EyesScenario`, State, Ai, Overrides | Both eyes, Estinien/Alphinaud/Ysayle/Haurchefant presentation, souls, Hatebound orbs and swaps, four Mirage Dive sets, Steep in Rage, eyes defeat. Does not implement the full rewind/intermission rescue sequence. NA snake swaps. |
| P5 Wrath | None | `P5Wrath/DsrP5WrathScenario`, State, Ai, Overrides | Opening Thordan/dragons, twisters, dragoon tethers, blue marker, proteans, Eruption, Liquid Heaven, Chain Lightning, Cauterize, Empty Dimension, connecting Heel/Might. Persistent Liquid Heaven puddle hazard is absent. |
| P5 Death | None | `P5Death/DsrP5DeathScenario`, State, Ai, Overrides, settings | Doom, static dives, quakes, cleansing puddles, Lightning/twisters, Playstation chains/knockback, gazes and meteor ending. NA conga and anchored dooms require Elemental review. Hysteria simplified to death. |
| P6 | None | `P6DoubleDragons/DsrP6DoubleDragonsScenario`; `DsrP6CauterizeScenario` delegates to its second-half mode | First entry Wyrmsbreath→Wroth; second Hallowed Wings 2→Cauterize. Shared timeline/State/Ai includes WB1/2, glow variants, tank breaths, Mortal Vow, Akh Afah, Wings/Plumes, dual dives, Wroth orbs/Akh Morn, Hot Wing/Tail, Pyretic/Freeze cleansing, Touchdown. No registered continuous mode. NA fixed WB2 routes. |
| P1 (last) | None | `P1Knights/DsrP1KnightsScenario`, State, Ai | Independent Knights→Thordan sequence: Bash/Bladedance, dimensions, two slashes/portals, Shining Blade/orbs, Execution, Hallowing interrupts, Playstation/chains, prison, Pure of Heart/Brightwing/Haurchefant. Scripted defeats, no HP check; later timeline explicitly shifted provisionally. |

FluffySim's `DsrArenaDiagnosticScenario` is unregistered and has no combat actors. Asu's nine DSR entries are all explicitly registered, including Nidhogg: the README omission does not indicate absence. No P7 implementation was found in these DSR trees. Shared files: FluffySim `DsrActions`, `DsrActors`, `DsrZone`; Asu `DsrConstants`, `DsrNpcSpawn`, `DsrZone`. There is no hidden completed Elemental phase to enable.

## Native environment and actors

All use territory 968, origin (100,0,100). FluffySim has the recorded entrance barrier (EObj 2007457, layouts 0x875BE2/0x875BE0), radius-21 boundary, P2 weather45/Heroes, nine-slot reset and readiness/generation-aware offline weather handling. P2 verification includes thirteen layer/instance signatures (`DsrP2Layers.json`, `LayoutQuery.MatchesDsrP2`, `DsrP2StateVerificationTests`, `DsrLayoutLifecycleTests`). This authenticates P2 only.

Asu uses director category `0x80000016` for layouts: P1=1, prison=0x14, Thordan=0x1E, Ward=0x25, Sanctity=0x2F, Ultimate End=0x39, P3=0x48, P4=0x5A, Wrath=0x64, P6=0x2D, WB2=0x3C; map category `0x8000001F`, maps758/765. P3 enables MapEffect slot8 flags0x00020001. P2 gaze uses slots0–7. These are reference executable values, not independently validated native layout contracts. Asu's entrance barrier is Unknown; do not import that over FluffySim's known barrier.

| Phase | Native actors/helpers | Native environment and fidelity limits |
| --- | --- | --- |
| P2 | Thordan12604, the twelve knights, Brightsphere13070, Holy Comet12591, invisible effect helpers | FluffySim explicit model/equipment configs; native knight timelines, map eye and raised Meteor tower model/omen. Asu builds NPC spawn bodies from BNpc/NpcEquip data. Native renderer superiority is not established. |
| P3 | Nidhogg12605, diver12606 pool and four-tower divers, Soul Tethers | Asu weather3/music922; diver visibility/warp sequencing and native numbered/arrow statuses. Hitbox, donut hole, cone angle, damage and late Geirskogul omen window remain unverified. |
| P4 | Eyes12609/12610, Estinien13119, Alphinaud13769, Ysayle13118, Haurchefant13117, Gilded12608/Azure12607, diver pool | Asu weather3 explicitly unverified/music923; eye-to-Estinien and player red/blue native tethers; orb growth inferred through hitbox scaling. Eye HP scripted. |
| P5 | Dark Thordan12611, Vedrfolnir12646, Darkscale12647, Vidofnir12648, diver12630/12631, knights, helper pools, meteor circles13920 | Weather46/music925, director Wrath layout, twister and cleansing objects. Trigger distances, some cone widths and puddle persistence lack native proof. Death's recent helper/effect changes must be retained or evaluated explicitly. |
| P6 | Nidhogg12612, Hraesvelgr12613, Scarlet Price13238 and pooled helpers | Weather3/music926, simultaneous actors and casts, glow-sensitive tethers, native status lifecycles. Model facing, flight/return timing and simultaneous VFX still need client testing. |
| P1 | Adelphel12601, Grinnaux12602, Charibert12603, Zephirin12592, Haurchefant13117, spear11810, tear13071, Brightspheres and Thordan | Weather2/music920, square arena/prison layouts. Portal geometry, late timings and landing sound/timeline have explicit source caveats. |

Native presentation must survive delayed actor creation: FluffySim already defers pending casts, but not ActionEffects. Pre-spawn helpers before their release. Do not copy Asu's raw NPC packet builder into scenario code: Native boundary and existing authenticated actor configs are the local contract.

## P2 retain/improve decisions

The [previous comparison](asumech-comparison.md) contains cast-normalized executable timing comparisons. Asu P2 is unchanged between cached and live source. Its 2.7s Spiral/Heavy omen delay is explicitly UNVERIFIED; this is not stronger evidence than local measured releases. Keep measured times, crossed tank tethers, Zephirin-relative role swaps and Elemental Meteor center>ccw>cw priorities. Asu's 3-person stack minima and NA tower routes must not replace local failure contracts.

FluffySim Strength has 46 complete recorded timelines; Strength2 has 46. Sanctity1 has two complete captures; Meteors has two complete **failed** executions, not successful strategy validation. Preserve local source/receive/snapshot distinctions, raised tower heights, explicit native Omen90 asset and first-wave failure punishments. Inspect actual cast targets, model rotations and delayed-spawn behavior for demonstrable defects before changing presentation. A different source constant alone is insufficient.

## Strategy and failure model

Primary strategy: [Tuufless Elemental DSR](https://ffxiv.tuufless.com/elemental/dsr/), including linked phase diagrams; no strategy text/images are copied here. Bots remain seven and use the same mechanics as the human seat. RNG must be world-owned, with valid assignment sets and forced-roll controls preserving validity.

| Phase | Required Elemental adaptation from Asu | Existing reference failure model / gaps |
| --- | --- | --- |
| P3 | Face east for arrows; Easthogg dive/soak order. H/R fixed: NW D3, NE D4, SW H1, SE H2; T/M adjust cw>ccw>across. H/R bait Geirskogul. MT boss tether, ST clone tether, invulnerability support. | Overlapping dives, fire/physical vulnerability, tower under/overfill, wheel/cone/line hits and tether interception. Reference tank tether damage is simplified; no real invuln requirement yet. |
| P4 | T/H red and DPS blue, orb swaps; NORTH-relative healer→tank→DPS Mirage priorities around blue eye. | Tethers pass near Estinien; early orb touch/leftover orb wipe, repeated resistance hits and dive overlap. Reference initial red holders are tanks+melee, not Elemental. |
| P5 | Wrath white-dragon frame; Death 2-2 Dooms North, anchored dooms with non-dooms adjusting. | Twister contact, tethers, spreads, quakes, Doom timeout/cleansing, chains/knockback, gaze. Persistent bait puddles and meteor ending need independent review. |
| P6 | Triangle WB1, DTTMR Vow, north/south Akh Afah, MT edge/ST center Wings, west spreads/east stacks, D3 south 5-1 WB2, MT west/ST east invuln Cauterize. | Slack tether penalty unverified, shared tank orbs inferred, max-HP loss simplified to death, damage fractional, Vow and Pyretic tolerances approximate. Reference AI must not silently supply fixed WB2/NA allocations. |
| P1 | ST Adelphel/MT Grinnaux, MT tether invuln, south-first slashes, ST/D3/ST interrupts, Elemental Playstation priorities, H/R→melee→ranged→tanks Brightwing. | Reference scripted interrupts and anti-knockback, portals, chains, execution, prison. Full human action/target validation required; late Hallowing onwards explicitly shifted seven seconds. |

## Evidence, reuse and obligations

Existing local evidence: `DSR-ROADMAP.md`, `OFFLINE-DSR-REPAIR.md`, `docs/encounters/dsr-sanctity-{meteors-evidence,tower-height}.md`, sanitized recorded timeline fixtures, five `tools/dsr_*_evidence.py` parsers and Meteor reconstruction tooling. Native diagnostics: `ArenaSnapshot`, P2 fixture, layout lifecycle/verification tests, map/director retry queues and packet-isolated zone lifecycle. Raw combat logs and two named DSR replay logs exist outside the repository; existence alone does not prove phase completeness. New P3–P6/P1 receive inventories, native snapshots and BossMod corroboration remain evidence work before each implementation. Do not commit raw identities, captures or game/SDK files.

Reuse world-owned actors/tethers/omens, EnemyAction shape/cast sheet lookup, EventScheduler, AiManager difficulty behavior, local defensive/invulnerability and interrupt handling, practice overrides, Pause/Resume and standard run teardown. Asu scenarios directly use its Legacy DamageSolver and several client-struct types; wholesale copying would violate local architecture. Convert mechanics to EnemyActions and Native.Interfaces. Avoid importing unrelated M10S/FRU/native rewrites.

All inspected repositories carry AGPLv3 licenses; preserve existing AnoMech/FluffySim notices, identify Asunnya and exact source revision for adaptations, retain corresponding source and applicable AGPL notices/obligations on later distribution. This audit copies no implementation. Reference comments and commit messages are claims, not measurements. Asu tests establish only their own assertions; none were run or counted as FluffySim validation.

## Native playtest gates (every phase)

For each entry: cold title-screen/offline and inn entry; correct weather/layout/barrier; first visible actor/cast/omen; all eight human seats and both difficulties; randomized and forced assignments; target/action handling; God Mode; pause across status/tether/knockback; restart mid-mechanic and after completion; Leave at each stage; repeated entry after another phase; no residual objects/map state/audio/queued casts. P3: facing/numbered dives and tank invulns. P4: eye tether transfers/orb sizing and NORTH swaps. P5: twister visibility, puddle cleansing/baits and final knockback/gazes. P6: both actors/glows, Vow pass and dual-dive cleansing. P1: interrupts, south slashes, chains and Haurchefant sequence. Automated checks cannot mark these native-verified.

## Capture coverage check

A territory-968-scoped scan of all local `Network_*.log` files found DSR action traffic in October10 and October16 captures. P1-specific Slash/Pure of Heart/Brightwing and P2-specific Strength/Sanctity actions are present; **no P3, P4 or P6-specific action IDs** were found. P5 name matches were reused P1/P2 action IDs (for example gaze/quake/Heel), not evidence of Wrath/Death execution. Phase-specific anchors are required before treating such matches as coverage. The two DSR `.log` files in Replaye start with binary `BLCB`, not IINACT text or `FFXIVREPLAY` format5. Historical DSR Duty Recorder archives (2024) also exist; build compatibility and opcode schemas must be established before interpreting them. No raw logs were copied or changed.

P3 corroboration from local BossMod `BossMod.Ultimate/Endwalker/Ultimate/DSW2`: Dive from Grace uses14y offsets and radius5 towers; Gnash/Lash uses8y boundary (Asu's6y is unverified); Geirskogul is a62y line with4y half-width; four-tower counts are exact, not minimum-only. Tuufless describes arrow offset approximately15y and confirms east facing and MT/ST tether ownership. These disagreements must be resolved explicitly during P3 work; do not blindly inherit the Asu constants.
