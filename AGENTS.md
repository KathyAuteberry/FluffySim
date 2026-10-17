# FluffySim agent instructions

Persistent instructions for agents working in this repository.

## What this is

AnoMech is a Dalamud plugin that simulates FFXIV ultimate-raid mechanics client-side, so players can
practice them solo or in multiplayer. It spawns fake `BattleChara`s (party doppels and bosses) in an
inn, drives their casts and movement, and renders the real VFX, cast bars, tethers and HUD. 

## Build / run

- Development build: `dotnet build -c Debug` from the repo root. The Dalamud SDK resolves from
  `%AppData%/XIVLauncher/addon/Hooks/dev/`.
- Never build Release during development or copy/publish into `bin/Release`. The installed
  Release output is watched by Dalamud; changing it hot-reloads the plugin and freezes the
  running FFXIV client. Only isolated GitHub Actions runners may build Release as part of automation.
- Keep development output and intermediates in Debug-specific `bin`/`obj` trees, separate
  from Release (the solution plugin output is `AnoMech/bin/x64/Debug`; direct project builds
  use `AnoMech/bin/Debug`). Run tests with `-c Debug`; do not introduce automatic reloads.
- The plugin can't run standalone: it only runs inside the game, loaded by Dalamud.

## Tests

- The user verifies most changes in game; unit tests cover the logic that can run headless.
- NUnit suites: `tests/AnoMech.Tests` and `tests/AnoMech.Relay.Tests`. 
- Scenarios run headless against fakes (`ScenarioCatalogTests`: every strat, random seeds, no
  deaths). `IGameData` in tests (`DataminingGameData`) reads real sheets from
  xivapi/ffxiv-datamining CSVs downloaded into `obj/datamining/<commit>/`; bump `DataminingCommit`
  in `AnoMech.Tests.csproj` for a newer patch, and add a sheet to `DataminingSheet` when `IGameData`
  starts reading it.
- Layout: one test file per concept tested, placed in the directory that mirrors the production
  code it covers (`AnoMech.Relay/Server/` → `tests/AnoMech.Relay.Tests/Server/`). 
  Fakes of a production interface go in a `Fakes/` folder beside that interface's package
  (`AnoMech/Core/Native/Interfaces/` → `tests/AnoMech.Tests/Core/Native/Fakes/`);

## Comments

Write a comment only for what the code cannot say itself — rationale, a non-obvious constraint, why
the obvious approach was avoided. Avoid comments that:
- restate a symbol's name in prose (`// P5 arena state` above `InitP5Arena`);
- restate literals or contract visible at a glance (`// TerritoryId 1363` beside
  `TerritoryId => 1363`);
- narrate what an edit changed (git records that);
- re-explain what an interface member's own doc already says;
- pile on examples for something trivial;
- cite where a value came from — log filenames, pull numbers, capture dates, sample sizes, tool
  invocations (`// Confirmed against 3 real pulls (Network_30208_*.log, pulls 2/3/5)`). State what
  the game does, not how it was measured. Provenance belongs in the session notes, not the source.
  The exception is a caveat that changes how the code should be trusted: mark a value that was never
  observed as UNVERIFIED, and keep a note that an approach was tried and found inert so nobody
  retries it.

When unsure, cut it — sparse and load-bearing beats thorough. Scenario AI strats (`*Ai.cs`) go
stricter: no comments, intent carried entirely by descriptive method names.

**Don't trust comment**. They are often straight up wrong, written based on false evidence, not true in every 
context or just out of date. Always take into consideration that comment might be false,
communicate that with user and try to verify first.

## Architecture

- **Projects.** `AnoMech/` (the plugin), `AnoMech.Relay/` (multiplayer relay library: wire format
  in `Network/`, server in `Server/`), `AnoMech.Relay.Host/` (standalone relay exe + Dockerfile).
- **Frame loop.** `Plugin.OnFrameworkUpdate` → `Natives.Tick` (first, so the zone guard runs even
  when `Game.Tick` is paused or throws) → `RunController.Tick` → `Game.Tick` (`Core/Game/Game.cs`)
  → `Multiplayer.Tick`. `Game.Tick` ticks the `InstanceSession` and checks for falls, then, unless
  paused, ticks `EventScheduler` → `SimWorld.Tick` (map, party + `PartyHud` refresh, children, then
  `EnemyList`) → `ScenarioSession`. All on the Framework thread. `EventTimeScale` scales all three
  of those ticks, so sim-side cast, status and movement timers speed up with it; only the client's
  own animation runs at real time.
- **`Core/SimObjects/`** — in-world entities (`SimWorld` root, `SimCharacter` / `SimEnemy` /
  `SimParty`, `SimCast`, `SimStatus`, `SimTether`, …). Read the rules in the header of
  `ISimObject.cs` before adding a type: spawn through the parent's API (`SimWorld.SpawnEnemy`,
  `SeatParty`, `Tether`, …), never `new` from outside; helpers live in `Core/`, not here.
- **`Core/Game/`, `Core/Map/`, `Core/EnemyActions/`, `Core/UserActions/`** — orchestration (`Game`,
  `EventScheduler`, `Movement`, party creation, AI), arena/map state, boss action definitions, and
  client-side resolution of the player's own actions.
- **`Core/Native/`** — everything that touches the game client. `Interfaces/` is the game boundary
  and the only part of Native that sim code (`Core/SimObjects`, `Core/Game`, `Core/Map`,
  `Scenarios`) may name. Sim code reaches the game through the static `Natives` locator (Plugin
  installs implementations, tests install fakes via `FakeGame.Install`) and per-object proxies
  (`IBattleCharaProxy`, `IEventObjectProxy`). Implementations hold no sim logic. Plugin glue
  (`Plugin.cs`, `RunController.cs`), Windows, Multiplayer and UserActions handlers may use
  `Implementations/` directly. Where Dalamud already
  has a service interface (`IFramework`, `IChatGui`, …), call `Plugin.<Service>` and stand it in via
  `tests/AnoMech.Tests/Support/DalamudServices.cs`; `IGameData` is the exception, because Lumina
  rows can't be built without game files.
- **Native interop** walks `FFXIVClientStructs` types directly. 

## Scenarios

- **Purpose.** A scenario exists to let players practice a mechanic. Match the game where it
  matters for learning it (timing, positions, hitboxes, what you have to read off the screen), and
  simplify or skip the rest. Don't chase 1:1 fidelity that doesn't change how the mechanic is
  played.
- **Keep it simple.** A scenario should read as a plain description of the fight. Logic that more
  than one scenario needs belongs in `Core/`, built once and reused, not copied between scenarios
  or hidden in a family's helper class.
- **Structure.** `IZone` (territory, origin, waymarks) ← `IPhase` (weather, BGM) ← `IScenario`.
  Each scenario points up at its phase, and `ScenarioCatalog` builds the menu tree from that. Register new
  scenarios in `ScenarioCatalog`. A family folder (`Scenarios/Top/`) holds `<Family>Zone.cs`,
  `<Family>Constants.cs` (IDs) and `<Family>Actions.cs` (`EnemyAction` definitions). Each scenario
  folder holds `*Scenario` (timeline, spawns, casts), `*Ai` (party strats), `*State` (per-run
  rolls) and `*StateOverrides` + `*SettingsWindow` (user-forced rolls).
- **Strats live only in `*Ai`.** `*Scenario` and `*State` describe what the fight does, the same
  for every strat, and never branch on which strat is selected. The one allowed exception is a
  simplification that depends on the strat, e.g. skipping something the selected strat makes
  irrelevant.
- **Timeline.** Prefer `world.Events.Add` with an absolute time literal from scenario start, kept in
  ascending order, so the scenario reads as the fight's timeline. Avoid offset arithmetic and
  scheduling from inside handlers by default; exceptions are fine where they make the scenario
  simpler 
- **Boss attacks** are `EnemyAction`s. Take cast times and AoE shapes from the Action sheet rather
  than hardcoding them. Use literal coordinates for spawns and moves unless a position really
  changes from run to run.
- **Randomness** comes only from `world.Rng`, rolled into `*State`, so a seed reproduces a run.
  Every strat in `AiStrats` must clear the fight with no deaths: `ScenarioCatalogTests` checks this
  for every catalog entry.

## Multiplayer

Being rewritten. Existing code doesn't follow it yet.

- **Keep multiplayer out of Core and Scenarios where possible.** The sim runs once, on the host,
  as it does solo, and multiplayer logic belongs in `Multiplayer/`. Avoid host/peer branches,
  multiplayer-only flags, overloads or mirrored state in Core and Scenarios; add them only when
  there's no other way. Core objects that exist because of multiplayer (e.g. the remote player's
  puppet, `SimNetworkPuppet` / `RemoteMotion`) are fine. Keep them minimal.
- **Broadcast through Natives.** Multiplayer installs its own `Natives` implementations (and
  proxies) that call the real one and forward the call to peers. Peers run no sim; they apply what
  they receive. Spawn ids are deterministic, so most calls replay verbatim; calls aimed at the local
  player (self-only ActorControl, local statuses, knockback, gauge, LB) and per-client views (party
  HUD, enemy list) are routed or rebuilt for each client's own seat.
- **Zone-in and start are separate.** Everyone zones in first; the host starts once all are in.
- **Movement.** Each client sends its own pose every tick; other seats are steered from those
  poses via `RemoteMotion`, like bots and NPCs. Forced moves go to the owning client, which runs
  them itself and reports the result through its poses.
- **Cleanup.** Multiplayer tracks what it spawned on each peer and clears it on run end,
  disconnect or host leave.

## FluffySim release identity

The public plugin is FluffySim; source namespaces, solution/project paths, scenario IDs and relay
protocol headers remain AnoMech for compatibility. `FluffySim.Version.props` is the only plugin
release-version source. Debug builds produce `FluffySim.dll`, with separate Debug intermediates.
Follow `RELEASING.md` for packaging an already built directory; never execute a Release build or
write into installed output during development. Legacy `AnoMech.json` is imported only when no
FluffySim configuration exists, without mutating the original. Never load both plugin registrations.

## Integration safety tests

`tests/SafetyTests` exercises the production zone/filter lifecycle against a simulated client and server. Run its Debug configuration with the plugin Debug binary; never run mutation tooling against installed files.

## Jujutsu, integration and releases

Use Conventional Commit messages for every new commit. Use jj locally, not Git history
commands. Inspect `jj status`, `jj log`, `jj bookmark list --all-remotes`,
`jj git remote list` and `jj op log` before changing either repository.
Source is KathyAuteberry/FluffySim; `../plogons` is a separate jj repository publishing master.
FluffySim publishes master. Origin is FluffySim, upstream is anomek/AnoMech, and lowercase
evernow is Evernow/AnoMech. Preserve the existing SSH host alias. Local master tracks
master@origin, not master@upstream. Repository-local `trunk()` is master.

Published 2.0.0.0 is the six-parent `plkqvn` integration followed by release preparation
`xznrxk`. Preserve that history and tag. For 2.0.1.0, `mptttx` merges the latest reviewed
upstream tip with published history, the test-fix branch and the separate
`codex/dsr-strength-dash-telegraphs` branch. Its child prepares the version/changelog.
Original Evernow offline/test/consistency tips are retained; already integrated remote tips
do not need rebasing. Rebase only unpublished local changes, never remote source history.
Integration conflict resolutions belong in the merge; version/release notes belong above it.

The earlier four-parent merge and pre-merge investigations are historical integration state;
see UPSTREAM-INTEGRATION.md and OFFLINE-DSR-REPAIR.md. Preserve original upstream/Evernow
attribution and ancestry. Never flatten the integration or rewrite published releases.
Only unpublished history may be restacked when explicitly authorized. `jj restack` inherits
the user-level alias without a repository override; inspect its configuration before use.
The integration is below trunk, so ordinary restacking above master does not touch it.
The repository log shows the nearest merge through the working copy and its immediate
parents, with older history collapsed. A fresh clone does not inherit jj configuration.

Everyday: edit → Debug build/tests → `jj commit -m "fix(scope): description"` on the intended
branch. Inspect the diff before advancing a bookmark. For recovery inspect `jj op log` and
`jj --at-operation <id> log` / `diff`; preserve current work and recover specific revisions
instead of blindly restoring an old whole-repository operation.

For upstream updates, `jj git fetch --remote upstream` and `jj git fetch --remote evernow`;
inspect `jj log -r 'master..master@upstream'` and candidate Evernow tips, including all newly
included ancestors. Create a reviewed merge with `jj new master <candidate>`, resolve with
`jj resolve --list` and edits, and run Debug regressions before moving master. Future updates
build on published ancestry; they need not recreate this initial six-parent integration.
Do not blindly merge every remote branch or replay old pre-release insertion instructions.

Preserve all nine custom scenarios: FRU Fall of Faith, Light Rampant, Ultimate Relativity,
Apocalypse and Darklit Dragonsong; DSR Strength 1/2 and Sanctity 1/2. Preserve Mana/Elemental,
seven bots, native arenas/party behavior, Easy/Hard, visibility, God Mode, Pause, local actions,
BGM and offline identity. Diamond Dust remains shelved. Packet filtering must stay armed while
client/server state differs; never bypass entry guards, move real positions outside a stay,
or send fabricated simulation traffic to the server. Comments are not proof of behavior.

Tag publication runs optimized Debug scenario tests in tests/AnoMech.Tests/bin/fast-Debug
with obj/fast-tests-Debug intermediates per project. Keep these separate from ordinary Debug
and optimized TRACE safety artifacts; optimizing ordinary safety input changes its IL classification.

Tests: tests/AnoMech.Tests, tests/AnoMech.Relay.Tests and tests/SafetyTests. Use Debug only;
see tests/SafetyTests/README.md for ordinary/optimized Debug artifacts and both failure models.
Python tooling tests are under tools/tests. Native rendering/hook behavior still needs client
checks; consult the integration, packet-filter and offline diagnostics documents.

FluffySim.Version.props is the only plugin version source (three parts; assembly/tag adds .0).
CHANGELOG.md supplies reviewed GitHub release notes through tools/release_notes.py.
.github/workflows/publish-fluffysim.yml verifies tags/ancestry/scenarios, tests, builds Release
only on an isolated runner, packages, publishes, then updates plogons/master with PLOGONS_TOKEN.
The old release-plugin.yml is only the inherited manual Debug package check. Exact setup,
versioning, publication and partial-failure recovery are in RELEASING.md.

Never push, tag, publish or update live metadata without explicit owner authorization. Never
replace released assets or downgrade manifest versions. Never commit/push secrets, private
configuration, logs, captures or game/SDK binaries; never bundle those, stale plugin DLLs,
test hosts or Debug check ZIPs. Keep AnoMech attribution and matching source available.

### Ongoing feature and integration workflow (jj 0.46)

Develop on anonymous feature branches from `master@origin`. Follow-up commits can remain on
an existing feature branch; do not create a bookmark for every task or commit. Use `codex/*`
bookmarks only when a branch needs a durable name. Prefer a working change supplied by the owner.

For an upcoming release, local `master` points to its release-preparation change and `master-`
is the shared `chore: integration commit`. Feature branches from `master@origin` merge into
`master-`; they do not form a serial stack above the release commit. The owner may pre-create
an empty feature change as an integration parent (for example with
`jj new -A master@origin -B <integration>`). Edit that change in place; Jujutsu carries its
integration and release descendants forward. Verify the actual parent relationships first.
Local `master` and published `master@origin` are separate references. Preserve this staging
structure and every concurrent feature parent; do not move master or publish during feature work.

Inspect all workspaces, existing feature bookmarks, and every revision whose description is
`chore: integration commit` before integration. Reuse the current **unpublished** shared
integration change, preserving all concurrent feature parents. Verify incorporation with
`jj log -r '<feature> & ancestors(<integration>)'`; descriptions and graph proximity are not
ancestry proof. With jj 0.46, `jj rebase --source <integration> -d <existing-parent> -d <feature>`
can add a feature parent while carrying its descendants; inspect all descendants and preserve
every existing parent first. In contrast, `jj rebase -r <integration>` moves only that revision
and reparents its former children onto its old parents. If using `-r`, explicitly rebase the
existing release-preparation child back onto the moved integration and verify ancestry again.
Inspect the resulting diff, resolve conflicts in the integration change, then rerun Debug validation.
Do not rebase unrelated feature changes or discard concurrent working-copy edits.

When the latest integration has already been published (for example mlyzss beneath 2.0.2.0),
it is immutable history. With owner authorization, start the next shared integration above
the published baseline and completed feature tips using `jj new <baseline> <feature>`, with
exact description `chore: integration commit`. This is the next integration generation;
never rewrite the published integration to insert a new task. If there is already an
unpublished integration generation, incorporate into that one instead of creating a rival.

Release preparation requires explicit owner authorization. Create a separate child of the
verified integration, change `FluffySim.Version.props` and `CHANGELOG.md` for the actual
included work, and use `chore(release): prepare X.Y.Z.W`. Advancing local master is a separate
explicitly authorized operation; publication, tags and plogons updates remain separate too.
The historical plkqvn merge, its full parent set and published release descendants must remain
intact. Use jj-native history/recovery commands and inspect `jj op log` before recovering
specific revisions; never restore an entire old operation over concurrent work.
