# FluffySim 2.0.1.0 — release prepared

## Upstream integration

- Integrates AnoMech through `d576199a` (PR #56), retaining original upstream and Evernow
  attribution and the published 2.0.0.0 ancestry.
- Incorporates upstream's host-driven multiplayer packet recording/replay, reset handling,
  deterministic targeting improvements, named scenario layout IDs and refreshed game-data snapshot.
- Reconciles multiplayer preparation with FluffySim's native arena-readiness gate and local
  visibility/cooldown controls. Buffered and live host packets share an explicit peer-run guard.
  Native multiplayer remains experimental and needs in-client validation.

## Mechanic and test fixes

- Matches the three knights' late dash telegraphs in DSR Strength of the Ward Part 1 to
  native recordings: the omen starts 5.0 seconds into the cast, about one second before
  the dash effect. Action animations, timing and lethal hit detection are unchanged.
  Native appearance still needs playtesting.
- Fixes defensive-practice UI assertions on systems that format decimals with a comma, and
  reports scenario failures before secondary observation failures.
- Reduces Fall of Faith's repeated full simulations by 24× while retaining all 16 element patterns,
  all four-role target sets and every role/tether position; the inexpensive model checks still
  cover all 1,680 ordered assignments.
- Push/PR checks use optimized Debug builds. The simulation tests advance a fake clock;
  they do not wait for mechanics to run in real time.
- Extends entry-lifecycle and safety architecture checks for multiplayer preparation and
  the new native wrappers. Local development and validation remain Debug-only.

All nine existing FRU/DSR scenarios remain available. Diamond Dust remains shelved;
Sanctity of the Ward Part 2 and title-screen offline mode remain experimental.

# FluffySim 2.0.0.0 — release prepared

FluffySim 2.0 brings Evernow's title-screen offline simulator, packet-filter safety work
and client-state restoration into the FRU/DSR practice environment. This is a major runtime
integration over 1.2, with follow-up repairs for native arenas, actors, party UI and timing.
The release pipeline is supporting infrastructure, not the reason for the major version.

## Title-screen offline practice

- Start a local inn from the title screen without establishing a live game-server session.
  Select a locally saved character, appearance and equipment; optional hotbar, HUD and
  keybind imports are applied to the offline client state.
- Connection and file guards are checked before offline state changes and during the session.
  Unexpected login attempts or loss of required isolation trigger a safety stop. Offline
  choices and diagnostics are stored separately from the source character configuration.
- The existing nine FRU Mana / DSR Elemental scenarios use the same mechanics and practice
  controls in the offline inn. Load FluffySim before using its title-screen entry; see
  [installation and offline usage](https://github.com/KathyAuteberry/FluffySim/blob/v2.0.0.0/INSTALLATION.md).

## Packet filtering and lifecycle safety

- Integrates Evernow's offline, firewall/test and consistency branches while preserving their
  original authored history and AnoMech attribution.
- Verifies filtering before virtual-zone changes and checks it throughout the stay. Through
  the hooked send path, only the detected heartbeat is allowed; gameplay and unreadable
  packets are dropped, without a deferred queue to replay after leaving.
- Leaving blocks new input, clears queued/ground-target actions and local auto-attack state,
  restores the inn and position, and verifies native restoration before lowering filtering.
  Unsafe unload or failed restoration stops the client rather than continuing unprotected.
- Adopts the upstream session lifecycle and packet-backed actor, party, status and enemy-HUD
  handling instead of maintaining a parallel legacy runtime.

## Cooldowns, actions and Pause

- Adds whole-session preservation of all 80 recast groups and both action-sequence counters,
  restoring their pre-entry state with real elapsed time accounted for on exit.
- Retains 1.2's scoped native defensive practice: normal hotbar invulnerability and
  anti-knockback practice reset only the applicable actions. Unrelated cooldowns are not
  automatically cleared. The low-level `/fluffysim antikb` fallback remains available.
- Adapts Pause to the new actor/session lifecycle, retaining frozen simulation poses,
  movement, status durations and owned defensive cooldowns. Resume refreshes the owned
  presentation without cleansing debuffs or granting a fresh defensive window.

## FRU/DSR native integration repairs

- Restores recorded entrance-object bindings so native arena barriers open correctly.
  Party spawning and phase setup wait for native layout readiness; phase changes invalidate
  obsolete callbacks and presentation holds.
- Repairs offline bot representation visibility for FluffySim-owned actors while preserving
  native culling, delayed model loading and Show Simulated Party, including during Pause.
- Restores lost native party membership and uses current native territory data when rebuilding
  the eight-member roster.
- Resets the authoritative run scheduler at every scenario start. Waiting in the instance
  before starting no longer shortens Sanctity Meteors tower windows or offsets bot deadlines;
  the recorded mechanic timings and assignments remain unchanged.
- Replaces competing offline DSR scenery holds with a bounded native weather-manager
  transition per ready layout generation and checks actual P2 object activation afterward.
- Aligns offline spawn and roster identities using the existing reserved virtual IDs, then
  publishes all eight actor associations and names to the native party HUD data used by
  consumers such as DelvUI. No authenticated server identities or party traffic are fabricated.
- Improves optional Offline Trace with bounded environment/identity diagnostics, the exact
  active file path, archive location and a working Flush control.

## Release and compatibility

- Preserves all nine scenarios, Mana/Elemental strategies, seven simulated companions,
  Easy/Hard bots, God Mode, visibility controls, Pause/Resume, BGM and local ability practice.
  Diamond Dust remains experimental and unregistered.
- Adds tag-driven tests, an isolated CI Release build, validated packaging and publication,
  followed by a conflict-aware update of the separate custom-plugin repository. GitHub
  release notes come from this reviewed changelog entry.
- FluffySim remains the public plugin identity; AnoMech source namespaces, scenario IDs and
  relay protocol identity remain compatible. Existing FluffySim settings are retained.

## Validation and remaining client checks

The prepared integrated runtime passed 13,668 plugin tests, 44 relay tests and 305 safety
checks per modeled hook backend; three Windows-native hook tests are skipped on Linux.
These are separate suites, not additive evidence of native-client correctness.

The user confirmed offline FRU, visible offline bots, arena movement, inn-based scenarios
and the Meteors clock repair during integration. The latest cold offline DSR P2 scenery
and eight-name native/DelvUI publication repairs still need in-game confirmation. Offline
mode remains experimental; third-party plugin assumptions and native Windows/Wine behavior
are not fully covered by headless tests. Isolation improvements are not an account-safety
guarantee. Leave the simulator and wait for restoration before unloading or reloading.
See [integration details](https://github.com/KathyAuteberry/FluffySim/blob/v2.0.0.0/UPSTREAM-INTEGRATION.md) and [offline repair notes](https://github.com/KathyAuteberry/FluffySim/blob/v2.0.0.0/OFFLINE-DSR-REPAIR.md).

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
