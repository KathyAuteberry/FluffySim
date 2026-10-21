# FluffySim 3.0.2.0 — release prepared

## Fixed

- FRU Darklit Dragonsong now shows native overhead Dark Water indicators on both assigned players, including bots, with cleanup on resolution, Restart and Leave.
- FRU Crystallize Time now calculates Hallowed Wings damage by actual hit order, including Magic Vulnerability Up, active mitigation, shields and remaining HP. The full simulated Mana party-support plan covers both Wings; correctly positioned DPS and healers do not need extra personal mitigation. The reported D3 fourth-to-third hit order no longer causes an automatic vulnerability death, while genuinely lethal damage and mechanic failures remain punishable.
- DSR Wrath and Death of the Heavens now show Vedrfolnir's Twisting Dive cast in the native Enemy List while preserving the dragon's untargetability and existing attack timing.
- Both P5 tankbuster sequences accept local human Provoke during Heavenly Heel without requiring tank stance. MT takes Heavenly Heel and a valid ST swap controls all three Ascalon's Might hits; missed swaps, cleave positioning and mitigation requirements remain meaningful.
- Wrath's former Skyward Leap and Spiral Pierce targets now follow a clockwise Twister dodge relative to the white dragon, starting after their attacks resolve and continuing into their spreads. Human-controlled assignments retain responsibility for their own movement.

## Improved

- Integrates upstream multiplayer improvements for action, cast, interruption and auto-attack presentation; remote walking and jumps; spawn appearance; status-source retention; shared limit-break gauge; and party result marks.
- Improves peer startup ordering, connection-lag handling and delayed Black Hole tether acquisition.
- Registers upstream duty text before zone loading, preserving FluffySim's native environment readiness, FRU arena setup and JP Mana/Elemental defaults.

Thanks to Adodo [DOZY] and the community testers for the reports and native retesting. Damage budgets and some presentation remain simulation approximations. Multiplayer participants should use matching plugin builds.

# FluffySim 3.0.1.0 — release prepared

## Fixed

- FRU Paradise Regained now uses the correct Mana DPS tower assignments: D1/D2 cross to the far tower that was just cleaved, while D3/D4 take the near tower. Both wing orders, rotating tower patterns and continuous P5 use the corrected assignment; H1/H2 retain the first tower.
- DSR Wroth Flames now follows the primary Elemental L-shaped dodge: three horizontal Akh Morn drops, then an inward curve for the fourth, mirrored for the safe side and Sable Price pattern.
- Wroth Flames now assigns all eight players local-only automarkers using the existing practice marker renderer. Purple spreads use Attack 1–4 West; Bind 1/2 form the inner East stack pair, and Ignore 1/2 form the East-wall pair. Bots follow those marker identities while encounter debuffs remain randomized.
- Wroth markers clear on completion, failure, Restart and Leave. Overlapping Flames stacks now enforce Fire Resistance Down, while persistent puddles, bleed punishment and the quick Mortal Vow handoff remain intact.

## Verification and practice limitations

- Added deterministic tower occupancy/recipient checks, complete mirrored Wroth clears, all 420 valid marker configurations, and human movement checks with reaction delay and position slack. Thanks to Adodo [DOZY] for reporting the strategy discrepancies.
- Wroth event timings and puddle exposure behavior remain provisional. Native marker rendering and movement readability still need in-game confirmation; headless clears do not replace those client checks.

# FluffySim 3.0.0.0 — release prepared

## Added

- Seven new Dragonsong's Reprise practice entries: The Holy See, Dive from Grace, Eyes with Haurchefant rescue, Wrath and Death of the Heavens, and both Double Dragons halves. DSR now has eleven entries in phase order; Dragonking Thordan is not included.
- Elemental assignments and seven-bot choreography across the new phases, with role selection, Easy/Hard movement and mechanic-specific practice preferences, including P5 assignment selection.
- Native DSR actors, phase environments and readiness-gated initialization for inn and title-screen offline practice.

## Improved

- Existing P2 drills retain their measured timelines and Elemental assignments with improved native presentation and lifecycle handling.
- Nidhogg choreography, Eyes orb handoffs and North-relative Mirage Dive swaps, scenario feedback and event-level orb diagnostics.
- Central local practice cooldown restoration for supported defensive actions, tank invulnerability, anti-knockback and relevant interrupts, with restoration on exit.
- Short chronological DSR menu names and repeated-entry/retry reliability.

## Fixed

- Invisible P1/P5 bosses and incorrect P4 story NPC appearances.
- P1 Faith Unmoving displacement, distance-based Playstation chain breaks, artificial Dimension floor warnings and the Pure of Heart prison pull.
- P4 duplicate orb hits, support interception and delayed handoffs that left blue orbs uncollected; rescue now physically carries the party into Planar Prison before enforcing its boundary.
- Both P5 Heavenly Heel/Ascalon's Might sequences use conventional MT → ST swaps with simulation-local Provoke and damage checks on actual recipients.
- P6 Akh Morn puddle exposure no longer kills correctly moving players immediately; lingering remains lethal.

## Branding and documentation

- New FluffySim artwork, with a 512×512 plugin icon and 64×64 title-screen icon.
- Rewritten player-facing README with custom-repository installation, current encounter coverage, offline setup and troubleshooting.

Native testing confirmed the preceding DSR repairs, including the complete Eyes orb sequence. The final P4 prison-pull correction still needs native confirmation. Some effects, damage budgets and timing remain approximations; leave simulations cleanly before normal play.

# FluffySim 2.2.0.0 — release prepared

## More Futures Rewritten practice

- Adds Mana Diamond Dust and Mirror, Mirror to the standard FRU practice menu,
  including Banish III pairs/spreads, role assignments and seven strategy-aware bots.
- Expands Crystallize Time with Mana/Nukemaru movement, dragon catches, cleansing,
  Tidal Light, Return, Spirit Taker, Hallowed Wings and the ending sequence.
- Gives the four blue Wyrmfang players unique local 1–4 cleansing markers, ordered
  East to West. Bots follow their numbered assignments and avoid other players' puddles.
- Corrects tank crossings and outer-edge Return recording positions while preserving
  the later spreads and knockbacks.
- Adds Mana Fulgent Blade with its Akh Morn follow-up, Paradise Regained,
  standalone Polarizing Strikes, Pandora's Box and continuous P5 sequence practice.
- Corrects Paradise wing-facing, tank/tether handling and rotating tower formations,
  and keeps Mirror supports on their proper flanks while DPS adjust for reflections.

## Smoother local practice

- Defaults new configurations to JP region and Mana strategy, with Mana listed first;
  saved explicit strategy choices remain respected.
- Adds optional Crystallize Time tank-mitigation checks, disabled by default. Strict
  practice uses an approximate mitigation model and restores supported local tank
  defensive recasts at Start/Restart, without changing server cooldowns.
- Improves native P2/P5 arena and actor initialization, helper casts, and shared P4
  Crystal of Fate presentation. Startup diagnostics identify missing native dependencies.
- Strengthens Pause, Restart and Leave cleanup, deterministic bot/failure regressions
  and packet-isolation checks. Extensive stress tests remain available separately from
  the bounded routine suite. Existing FRU and DSR drills are preserved.

## Practice limitations

- Some CT/P5 timings and damage models remain inferred or inherited rather than
  combat-log measured. Strict mitigation is a training approximation.
- Continuous P5 practice joins mechanic fragments; a full threat table, DPS/enrage
  damage race and exact retail transitions are not simulated. Diamond Dust bot slides
  are simulated locally; human Thin Ice behavior and native presentation depend on
  the game client.

# FluffySim 2.1.3.0 — release prepared

## DSR Sanctity tower fixes

- Replaces the immediate wipe on a failed first-wave tower with the separate Eternal
  Conviction punishment, released at T+14.15 with HP loss about 0.22 seconds later.
- Preserves the T+11.75 soak check. Entering after that check cannot undo a missed tower;
  the later punishment does not extend the entry deadline.
- Keeps failed tower helpers alive for the punishment and cancels pending failures on
  Stop/Restart. Adds regressions for late entry, Pause, failure timing and cleanup.
- Includes Eternal Conviction in the combat-log analyzer so future timing audits retain
  failed-tower punishment separately from tower soak effects.
- Raises both Sanctity tower pillars from height scale 1 to 6 while keeping their radius
  unchanged. The separate height feature retains native omen ownership and cleanup.

# FluffySim 2.1.2.0 — release prepared

## DSR Sanctity meteor routes

- Makes meteor bots weave and stagger their movement on short and forced-cursed routes,
  adding travel distance around Fire/Ice edges and pausing between comet drops.
- Spaces drops around the other runner's starting position while retaining the opposite
  N/S second-wave tower assignments and the existing forced-cursed practice selector.
- Checks all 810 tower patterns on Easy and Hard, including both meteor runners' capture
  positions, stop intervals, puddle clearance and tower completion.
- Native movement appearance and stagger pacing still require in-game verification.

# FluffySim 2.1.1.0 — release prepared

## Upstream integration

- Integrates AnoMech 0.5.1.0 through `6aec4a59`, retaining upstream attribution,
  published history and both staged FluffySim feature branches.
- Adds multiplayer session-stream recovery after dropped links and corrects peer clock
  synchronization. Mechanic result marks now come from a helper so they do not interrupt
  the player's cast.
- Keeps peer status sources stable after their caster despawns, avoiding repeated
  removal and reapplication of persistent statuses.
- Adds TOP Party Synergy LPDU and UMAD Forsaken Yan, with Yan's sixth-tower movement
  delayed until peers can read its markers.
- Adds a separate Futures Rewritten (Upstream) scenario group with Light Rampant,
  Ultimate Relativity, Apocalypse, Darklit Dragonsong, Crystallize Time, Fulgent Blade
  and Paradise Regained. All nine existing FluffySim scenarios remain available with
  their saved identities; Diamond Dust remains shelved.
- Preserves native appearance support for custom DSR/FRU actors while adding upstream
  spawn equipment, customization, statuses and model state/scale. Crystallize Time's
  per-frame movement checks that the practice instance is active.
- Native rendering, new mechanics and multiplayer recovery still require in-client testing.

## Practice settings and commands

- Streamlines DSR and FRU practice labels and settings, removing obsolete Fall of Faith
  and Darklit overlay controls while retaining native mechanic presentation.
- Removes the obsolete `arena-p3` and `arena-dsr-p2` diagnostic entry subcommands.

# FluffySim 2.1.0.0 — release prepared

## DSR Sanctity Meteors reconstruction

- Reconstructs Meteor impact timing and geometry from real combat-log evidence, with
  corrected outer/inner towers, fixed Fire positions, pair-baited Ice and native knight placement.
- Brings the seven paired comet releases forward to their observed cadence and separates
  comet appearance from impact. Updates bot routes, Sprint, tower deadlines and knockback
  alignment while preserving all 810 supported tower patterns and Easy/Hard practice.
- Keeps tower presentation visible through its soak resolution. Separates puddle
  object presence from danger so the knockback path is traversable before the objects disappear.
- Adds repeatable, sanitized encounter analysis, source-event references and regression coverage
  for geometry, sequence, pause, delayed initialization and Stop/Restart.
- Renders both Meteor tower waves using the game’s native Conviction omen AVFX, resolved from
  each action’s Omen data and explicitly owned through resolution and cleanup. The obsolete projected tower
  overlay and diagnostic switches have been removed.
- Sends first-wave CW/CCW tower bots along curved diagonal routes after the ice drop,
  skirting fire puddles and reaching their towers before the soak snapshot.
- Native tower appearance/scale, Ice primary-target category and Fire/Ice hazard frames still
  require client testing. Native particle animation can continue during Pause even while logical
  ownership freezes. See docs/encounters/dsr-sanctity-meteors-evidence.md.

## Development workflow

- Documents feature bookmarks, successor integration changes and separate release preparation
  while preserving published Jujutsu ancestry. Publication remains a separate owner action.

# FluffySim 2.0.2.0 — release prepared

## Offline plugin compatibility

- Fixes the offline-entry crash when Discord Chat Bridge is enabled. FluffySim temporarily
  unloads Bridge before entering offline practice and reloads it after returning to the
  title screen. Bridge itself is unchanged; plugins that were already disabled stay disabled.
- Refuses offline entry if an incompatible plugin cannot be suspended, and attempts to restore
  suspended plugins if startup fails. Restoration failures are reported with manual recovery
  instructions. Discord Chat Bridge is the first entry in the incompatible-plugin list.
- Offline entry with Discord Chat Bridge enabled has been verified in game.

## Release pipeline

- Runs tag-triggered scenario tests in optimized Debug builds to shorten release validation.
  These artifacts use separate output and intermediate directories, preserving the ordinary
  Debug and safety-test builds.

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
