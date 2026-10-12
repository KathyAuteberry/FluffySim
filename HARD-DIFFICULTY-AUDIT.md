# Hard difficulty audit — FluffySim 1.0

Easy keeps the existing requests, routes, speeds, assignments and resolution schedule. Hard uses the same requests with eligible absolute arrival deadlines: `departure = deadline - distance/current speed - 0.45s`. Requested movement starts immediately when there is insufficient slack, a route is already in progress, movement is locked, or an active obstacle makes a straight-line travel estimate unsafe. These guards are safety exemptions, not proof that a delayed route through hazards is possible. No RNG, extra movement speed, immunity, teleport or soak exemption is introduced.

Every movement path in the eight AI classes was traced. Shared helper calls without a deadline intentionally remain immediate. Direct MoveTo bypasses are listed explicitly below. The paused Diamond Dust scenario is outside this audit.

| Scenario | Delayed eligible movement and arrival deadlines | Prompt movement/exemptions |
|---|---|---|
| FRU Light Rampant | Initial lineup by 8.0; initial towers/puddle preparation by 14.6; final clocks by 45.4 before House of Light at 46.0 | Hammer drop trajectory, chain arcs, orb camps, central tower, Banish and non-soaker exits remain prompt: overlapping explosions, live chains, puddles and snapshot transitions constrain routes. All calls use BotMovement. |
| FRU Ultimate Relativity | First Fire by 24.6, second Fire by 34.6 | Center/laser bait/record/return/gaze-facing and intermediate Fire movement preserve preparation. Third Fire cannot wait: second-wave laser resolution at 42.12 intersects a late escape. All position requests use BotMovement; facing remains explicit. |
| FRU Apocalypse | Initial box by 12; permanent group swap by 19; spread by 30.1 | Water stacks, Spirit Taker return, active-wave corridor, second stack, knockback and final stack preserve tightly timed routes/snapshots. Direct tank bait uses the existing legitimate Sprint at 42.04; the 43.7 defensive action and subsequent boss snapshots leave no safe generalized extra travel delay. |
| FRU Darklit | Initial lineup by 7; final Water-adjusted tower/cone formation by 14.4 | Side adjustment and intermediate formation must finish in sequence. Spirit spread, Wings/Water and Dance regrouping preserve live tether/damage constraints. Direct tank bait at 26.5 and return at 30.2 preserve farthest/closest Dance targeting; the two hits require these timed transitions. |
| DSR Strength Part 1 | Elemental triangle by 19.2 | Heavy Impact radial escape, inward movement and Mercy dodge remain prompt under expanding pulses/snapshot constraints. All calls use BotMovement. |
| DSR Strength Part 2 | Leap/stack/tank preparation by 6.6/6.5/6.8; towers by 15.3 | Tether pickup and crossing, visible fixed tower communication and stack collapse retain original timing. Leap positioning must finish before tether application, rather than delaying until Leap damage. Tower deadline uses the 15.52 cast snapshot, not the later 17.93 failure event. All calls use BotMovement. |
| DSR Sanctity Part 1 | Initial sword groups by 17.1 | Double-gaze facing is explicitly set. Final orb rotation directly uses MoveTo at the established safe opening; delaying beyond that window threatens consecutive jumps/orbs. |
| DSR Sanctity Part 2 | Pair preparation by 3.4; return for ice by 8.25; first towers by 12.35, additionally capped by the safe ice-exit deadline; final outer towers by 22.6 | Initial cardinal arrangement, visible tower commitment, meteor arcs/drop cadence, inner knockback setup and landing exits remain prompt. Existing Sprint remains part of the meteor trajectory. All calls use BotMovement. |

Scenario-level position changes were also inspected: they belong to bosses/helper actors, encounter knockbacks or Return/rewind effects, rather than an additional ordinary bot-navigation path. These mechanics are unaffected by difficulty.

These exemptions preserve communication and validated continuous movement; Hard is not intended to obscure a required mechanic cue or intentionally make bots late.

## Automated evidence

`HardAuditTests` compares full Easy/Hard runs using identical seeds and human seats across all eight scenarios. It asserts identical authoritative state, observes a specific Hard actor still waiting while the Easy counterpart has actually walked toward the same destination, and requires both executions to finish without deaths. Movement telemetry includes source, destination and the actual actor to distinguish real movement from passing a setting flag.

`SharedPracticeTests` covers all seats/seeds in Hard with invisible bots and checks speed/deadline limits. `HardPatternTests` covers all 1,152 Darklit assignment graphs and 810 Meteor outer patterns without God Mode. Existing scenario tests retain damage/failure, communication and cleanup coverage. Native animation and human cue readability still require manual testing; passing these tests is not a native rendering claim.
