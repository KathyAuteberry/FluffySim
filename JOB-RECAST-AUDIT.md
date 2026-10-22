# Job recast audit (2026-10-22)

Offline bootstrap constructs PlayerState.Attributes from equipment and ParamGrow. It initialized
Skill Speed (45) and Spell Speed (46), but left Haste (47) at zero. Haste is a percentage
multiplier: neutral is 100, not zero. The native stat adjustment has a 1500ms lower bound.
Consequently even PLD Fast Blade's 2500ms sheet recast becomes 1500ms. This affects both native
cooldown display and native action scheduling; there is no separate FluffySim GCD timer.
The defect is in offline initialization, not in the 1500ms cast of Holy Spirit.

Inn entry uses the existing live PlayerState and does not construct attributes. The fix changes
the synthesized offline loadout and offline level-sync packet, leaving equipment speed contributions intact. The table
below assumes zero equipment speed, not that equipment is rewritten to remove speed.
Read-only disassembly follows the installed SDK GetAdjustedRecastTime signature to the native
stat/status adjustment: it reads attributes 45/46 and 47, applies the 1500ms floor, then active
status modifiers. The attribute accessor is a direct array read, so permanent traits must be seeded
in the offline attribute, rather than assumed to be applied a second time.
Native comparisons in a running client remain required; these are code/data audit results.

Installed game data: /home/kath/.xlcore/ffxiv/game/sqpack, read with the installed Lumina SDK.
The test Action CSV is pinned by AnoMech.Tests.csproj. Both agree on the representative rows.
DSR is level 90, FRU level 100. Offline initialization seeds permanent haste from installed
Trait levels and ClassJobCategory flags: traits 364/365/366/367 and 584 replace rather than stack.
Their percentage semantics are explicit because Trait.Value is zero; the installed TraitTransient
descriptions establish the reductions. PGL receives only 364/365; ROG does not receive the
NIN-only trait 584. Offline level sync reselects these traits at the effective level, and Leave
restores its captured stats. Native GetAdjustedRecastTime still handles action upgrades,
charges, other action-specific traits and active status modifiers.

| Job | Representative action (ID) | Sheet cast / recast | Neutral baseline at 90/100 |
|---|---|---|---|
| PLD | Fast Blade (9) | 0 / 2.50 | 2.50 |
| WAR | Heavy Swing (31) | 0 / 2.50 | 2.50 |
| DRK | Hard Slash (3617) | 0 / 2.50 | 2.50 |
| GNB | Keen Edge (16137) | 0 / 2.50 | 2.50 |
| MNK | Bootshine (53) | 0 / 2.50 | 2.00, Enhanced Greased Lightning III (367), level 76, 20% |
| NIN | Spinning Edge (2240) | 0 / 2.50 | 2.125 raw / 2.12 tooltip, Increase Attack Speed (584), level 45, 15% |
| DRG | True Thrust (75) | 0 / 2.50 | 2.50 |
| SAM | Hakaze (7477) | 0 / 2.50 | 2.50 before active Fuka |
| VPR | Steel Fangs (34606) | 0 / 2.50 | 2.50 before active Swiftscaled |
| RPR | Slice (24373) | 0 / 2.50 | 2.50 |
| BRD | Heavy Shot (97) | 0 / 2.50 | 2.50 before active Army's Paeon/Muse |
| MCH | Split Shot (2866) | 0 / 2.50 | 2.50 |
| DNC | Cascade (15989) | 0 / 2.50 | 2.50 |
| WHM | Stone (119) | 1.50 / 2.50 | 2.50 before Presence of Mind |
| SCH | Ruin (17869) | 1.50 / 2.50 | 2.50 |
| AST | Malefic (3596) | 1.50 / 2.50 | 2.50; Lightspeed changes cast time |
| SGE | Dosis (24283) | 1.50 / 2.50 | 2.50 |
| BLM | Fire (141) | 2.00 / 2.50 | 2.50 before Ley Lines |
| SMN | Ruin (163) | 1.50 / 2.50 | 2.50 |
| RDM | Jolt (7503) | 2.00 / 2.50 | 2.50; native upgrades select current Jolt |
| PCT | Fire in Red (34650) | 1.50 / 2.50 | 2.50 before Hyperphantasia |
| BLU | Water Cannon (11385) | 2.00 / 2.50 | 2.50; limited job, no complete JobActions model |

All these rows use sheet cooldown group 58 (native zero-based group 57). Abilities use their own
groups; some actions have an additional shared group. No recast-group translation was changed.
Classes share the native action/trait system with their jobs. Limited-job BLU can enter through combat gearset selection, so its ordinary spell recast is also
checked; its job-specific local effects are not fully modeled. It cannot legitimately reach FRU
level 100.

Legitimate exceptions remain native: RDM Enchanted Riposte (7527) is 1.50s; VPR Reawaken
(34626) is 2.20s and First Generation (34627) is 2.00s. MCH Hypercharge actions, RPR Enshroud,
SMN summon actions, NIN mudras/ninjutsu, DNC dances and PCT motif/palette actions must not
be normalized to 2.50s. Trait/status mechanics can override the sheet recast; a sheet-only
calculator would be incorrect. MNK traits 364/365/366/367 reduce by 5/10/15/20% at levels
1/20/40/76. NIN Fleet of Foot is movement speed, not the attack-speed trait.

Start and Restart reset only scoped practice defensive cooldowns. PracticeRecastSession refuses
to reset unrelated actions sharing a group. Native actions consume cooldowns normally. Leave
restores the captured full native recast array and request counters, aged by wall time under
the packet firewall. Offline exit additionally restores its PlayerState byte snapshot. No
new ActionManager writes, recast hooks, equipment writes or inn-mode stat overrides were introduced.

Headless coverage checks the real representative Action rows, separate cast/recast fields,
accelerated sheet recasts, neutral attribute initialization, preserved equipment contributions,
permanent-haste tiers and replacement semantics, and the zero-haste 1500ms versus neutral-haste
2500ms stat calculation. The latter is a reference
calculation, not execution of the game's native function. Existing scoped-recast lifecycle tests
cover cooldown ownership, elapsed restoration and pause.

Native QA still required: equivalent zero-speed PLD inn/offline displayed recast and accepted
press intervals; MNK/NIN passive haste; apply/expire Fuka, Swiftscaled, Presence of Mind,
Hypercharge and Enshroud; upgraded actions at 90/100; charged abilities and shared GCD weaving;
Start, Restart and Leave in each environment. Headless tests cannot execute native job mechanics.
