# JESUS TAKE THE WHEEL — FRU/DSR coverage

The actual catalog contains 28 selectable entries and 30 strategies. All are enabled through
zone capability, including entries carrying an existing incomplete/presentation disclaimer.
Those labels remain; no missing encounter is invented and no playable catalog entry is omitted.
The native DSR P3 camera experience was approved by the maintainer; camera code is unchanged.

The deterministic matrix runs every strategy, every watched role, and both bot difficulties
with eight ordinary SimPartyNpc participants. Separate lifecycle cases restart each strategy
from Pause, and after an intentional death, then Leave and assert empty actors/party/camera.
All scenarios retain ordinary assignments, damage, failure, targeting and phase logic.

| Catalog entry | Strategy | Responsibilities audited | Coverage |
|---|---|---|---|
| FRU P1: Fall of Faith | Mana West-to-East conga | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU P2: Diamond Dust | Mana Nukemaru | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU P2: Mirror, Mirror | Nukemaru | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU P2: Light Rampant | Mana 4:4 East adjust | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU P3: Ultimate Relativity (Mana) | Mana 大 / local markers | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU P3: Apocalypse (Mana) | Mana Permaswap / Coloured Apocalypse | Movement, assignments, damage, statuses and cleanup; tank invulnerability/baiting | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU P4: Darklit Dragonsong (Mana) | Mana Nukemaru / Bowtie / 扇前 | Movement, assignments, damage, statuses and cleanup; tank invulnerability/baiting | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P1 The Holy See: The Holy See — South-first / HMRT (Elemental) | South-first / HMRT | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P2 King Thordan: Strength of the Ward — Part 1 (Elemental) | Elemental triangles | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P2 King Thordan: Strength of the Ward — Part 2 (Elemental) | Elemental crossed tethers | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P2 King Thordan: Sanctity of the Ward — Part 1: Sacred Severs (Elemental) | Elemental DRK-relative role swaps | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P2 King Thordan: Sanctity of the Ward — Part 2: Meteors (Elemental) | Elemental cardinal pairs | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P3 Nidhogg: Nidhogg — Easthogg (Elemental) | Easthogg | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P4 Eyes: Eyes (Elemental) | T/H red, DPS blue | Movement, assignments, damage, statuses and cleanup; rescue, tank LB3, healing and spear damage | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P5 Alternate Thordan: Wrath of the Heavens (Elemental) | Elemental | Movement, assignments, damage, statuses and cleanup; MT→ST Provoke, mitigation and follow-up busters | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P5 Alternate Thordan: Death of the Heavens (Elemental) | Dooms North (2-2, anchored) | Movement, assignments, damage, statuses and cleanup; MT→ST Provoke, mitigation and follow-up busters | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P6 Double Dragons: Wyrmsbreath to Wroth Flames (Elemental) | Triangle / Elemental Wroth / 5-1 | Movement, assignments, damage, statuses and cleanup; persistent vows, multiple mechanics, twin boss HP | All roles, Easy/Hard; clear/restart/failure/leave passed |
| DSR P6 Double Dragons: Hallowed Wings 2 to Cauterize (Elemental 5-1) | Triangle / Elemental Wroth / 5-1 | Movement, assignments, damage, statuses and cleanup; persistent vows, multiple mechanics, twin boss HP | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU upstream P2: Light Rampant | NA · Conga | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU upstream P3: Ultimate Relativity | NA | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU upstream P3: Apocalypse | NA priority / static spreads / OT bait | Movement, assignments, damage, statuses and cleanup; tank invulnerability/baiting | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU upstream P4: Darklit Dragonsong | NA, NA · you take Somber Dance | Movement, assignments, damage, statuses and cleanup; tank invulnerability/baiting | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU upstream P4: Crystallize Time | NA priority / 7-1 Akh Morn (MT solo), Nukemaru / 7-1 Akh Morn (ST solo) | Movement, assignments, damage, statuses and cleanup; statuses, knockback and solo-tank stacks | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU upstream P5: Fulgent Blade | Nukemaru | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU upstream P5: Paradise Regained | Nukemaru | Movement, assignments, damage, statuses and cleanup; strict tank invulnerability option | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU P5: Polarizing Strikes | Nukemaru | Movement, assignments, damage, statuses and cleanup | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU P5: Pandora's Box | Tank LB3 | Movement, assignments, damage, statuses and cleanup; gauge-consuming tank LB3 and party protection | All roles, Easy/Hard; clear/restart/failure/leave passed |
| FRU P5: Pandora sequence | Nukemaru | Movement, assignments, damage, statuses and cleanup; persistent stages, LB3 and repeated strict invulnerability | All roles, Easy/Hard; clear/restart/failure/leave passed |

## Actions and human assumptions

Role-keyed practice settings apply to the watched seat, but it remains an ordinary bot. Existing
IsBotDriven routing already drives every seat, including tethers, tank baits, invulnerabilities,
LB3, healing and multi-stage movement. Party.Player is absent; local-input handlers stay locked.

Bot Provoke previously transferred aggro through the simulation event without an action visual.
It now also casts a local simulated Provoke at the boss. Heel still snapshots MT; ST owns the
subsequent cleaves. No hotbar or server action request is generated.

Strict Paradise wing practice previously examined only the human. Bot tanks now use their own
job invulnerability action/status at the proper window; the same optional check examines every
hit tank. Continuous Pandora repeats this logic independently in both Paradise stages. It does
not grant a spectator exception. Focused tests verify the active statuses across those stages.

Existing bot LB3 uses its job action, applies ordinary effects, plays native action presentation
and consumes the shared gauge. Focused Box tests check spent gauge and party-wide protection;
Eyes already uses the same infrastructure plus bot heals and spear damage. Terminal-enrage
options remain intentionally lethal; default practice endings are unchanged.

## Limits

Headless coverage proves simulated resolution and cleanup, not visual fidelity of all encounters.
Existing practice approximations (including terminal DPS-race handling and incomplete labels)
remain the same in normal and spectator modes. Native verification should sample FRU/DSR tank
swaps, rescue/LB3 and continuous phases, alongside party-list changes in the separate branch.
