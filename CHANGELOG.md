# FluffySim 1.2.0.0 — 2026-10-15

- Adds normal hotbar defensive practice with scoped client-local cooldown resets for tank invulnerability in Darklit/Apocalypse and anti-knockback in Sanctity Meteors.
- Preserves pre-entry cooldowns and restores them on Leave with elapsed real time accounted for. Pause/Resume preserves owned practice recasts without refreshing them.
- Hides the dedicated virtual defensive buttons when native practice is ready. Low-level Meteor players retain `/fluffysim antikb`, an explicit six-second, once-per-run local substitute requiring no unlocked ability.
- Hardens packet-filter restoration and unload ordering, clears pending local action intent, and adds `/fluffysim firewall` diagnostics without packet payload logging.

Leave and wait for the inn to finish restoring before disabling or reloading the plugin. An unverified unload now fails closed by stopping the client. Native reset/restore integration still requires client validation; this release does not guarantee account safety. See PACKET-FILTER-AUDIT.md for evidence, limitations and manual checks.

Validation: full Debug plugin regression 13,009 passed; final focused safety/Pause checks 75 passed; relay 41 passed; tooling 65 passed. Runs overlap and are not additive totals.

# FluffySim 1.1.0.0 — 2026-10-15

- Adds independently playable FRU P1 Fall of Faith (Mana), including all four Fire/Lightning tethers, assignment practice options and post-resolution repositioning.
- Keeps Fatebreaker stationary for the correctly positioned first tether, preserving melee uptime; distant targets retain charge behavior.
- Adds shared Pause/Resume controls for solo practice across all nine FRU/DSR scenarios. Simulation time, movement, statuses and local defensive durations remain frozen without refreshing buffs or removing debuffs.
- Pause uses local input restrictions, preserves existing death/Fetters restrictions, and releases its restrictions on Resume, Stop, Leave and teardown. Native animations and cast displays may continue while paused; multiplayer Pause is unavailable.

Fall of Faith and Pause have been manually playtested by the owner. Additional native presentation limitations remain documented in the roadmaps and PAUSE-INVESTIGATION.md.

# FluffySim 1.0.0.0

First versioned Fluffy community release, extending AnoMech 0.4.2.0. This is not an upstream AnoMech release history.

- Eight FRU Mana / DSR Elemental practice scenarios, including experimental Sanctity Meteors.
- Shared Easy/Hard movement, simulated-party visibility, God Mode and fully local practice defensive controls.
- Audited Hard integration across all eight scenarios; final Light Rampant clock spread also departs late in Hard.
- FluffySim branding, versioned DLL/manifest, non-destructive legacy settings import and repeatable dependency-aware ZIP packaging.

Native presentation and timing limitations remain recorded in FRU-ROADMAP.md and DSR-ROADMAP.md. Credits and upstream functionality are retained in README.md; license remains AGPL-3.0-or-later.
