# Development workflow

How FluffySim work is done day to day: building and testing, Jujutsu integration, and turning
in-game evidence into simulator values. Repository rules in `docs/development/AGENTS.md` take
precedence over anything here.

## 1. Build and test (Debug only)

The plugin only runs inside the game. Headless tests cover the simulation logic; the owner checks
native presentation in game.

```bash
export DALAMUD_HOME="$HOME/.xlcore/dalamud/Hooks/dev"   # required: the test project has no Linux default
dotnet build -c Debug                                  # solution; plugin output AnoMech/bin/x64/Debug
dotnet test tests/AnoMech.Tests -c Debug               # scenario/unit suite (slow; see README for fast mode)
dotnet test tests/AnoMech.Relay.Tests -c Debug
python -m unittest discover -s tools/tests            # Python tooling (91 tests; rtrace tests need the zstd CLI)
```

- Fast full run with optimised Debug output and isolated intermediates:
  `tests/AnoMech.Tests/README.md`.
- Safety suite (ordinary and optimised-TRACE Debug artifacts; four tests skip on Linux):
  `tests/SafetyTests/README.md`. Point its optimised output at a scratch directory, not `/tmp`.
- Reference run (2026-11-12, fast mode, `cs_CZ` locale): plugin 9,590 passed in ~6 min; relay 67;
  safety 319 + 4 skipped (after the hook-history locale fix); tooling 91.
- Focused runs: `--filter FullyQualifiedName~<TestClass>`. Some large matrices are opt-in; see
  `tests/AnoMech.Tests/EXTENDED-TESTS.md`.
- Never `-c Release`. Never copy into installed output. Dalamud watches the installed Release DLL,
  and changing it hot-reloads the plugin and freezes the running client.
- `DALAMUD_HOME` must be set. The plugin project's SDK finds `~/.xlcore/dalamud/Hooks/dev` on its
  own, but `AnoMech.Tests.csproj` falls back to a Windows `%AppData%` path and fails with ~100
  CS0246/CS0012 errors if the variable is unset. Expect 0 errors and ~10k analyzer/platform warnings. Headless tests read real sheets from
  xivapi/ffxiv-datamining CSVs pinned by `DataminingCommit` in `AnoMech.Tests.csproj`, cached in
  `tests/AnoMech.Tests/obj/datamining/<commit>/`.

## 2. Jujutsu workflow

Inspect before changing anything: `jj st`, `jj log`, `jj bookmark list --all-remotes`,
`jj git remote list`, `jj op log`, and every revision described `chore: integration commit`
(check whether each is mutable). Always pass `--no-pager` and `-m` in non-interactive use.

### A work commit

1. Start from `master@origin`: `jj new master@origin -m "fix(scope): description"`, or edit an
   empty change the owner pre-created. Use Conventional Commit messages. Anonymous branches are
   fine; create a `codex/*`-style bookmark only when a branch needs a durable name.
2. Edit, then build and test in Debug.
3. Integrate immediately through the shared integration change:
   - **If an unpublished (mutable) `chore: integration commit` exists**, add your change as a
     parent without dropping existing parents:
     `jj rebase --source <integration> -d <existing parents…> -d <feature>`. Then re-verify the
     release-preparation child, if any, still sits on top.
   - **If the latest integration is already published (immutable)**, start the next generation:
     `jj new master@origin <feature> -m "chore: integration commit"`. Example: `knmkvu`, parents
     `nuvyxn` (published release) and `rzpppu` (feature). Never rewrite a published integration.
4. Verify ancestry rather than trusting the graph:
   `jj log -r '<feature> & ancestors(<integration>)'`, and inspect `jj diff --git -r <integration>`.
   Resolve conflicts in the integration change by editing the files, hunk by hunk. Wholesale
   `jj restore --from` over a conflicted integration was rejected repeatedly in the Codex era
   because it discards concurrent work.
5. Do not move `master`, push, tag or publish.

### Releases (owner authorization required, every time)

A separate child of the verified integration changes only `FluffySim.Version.props` and
`docs/releases/CHANGELOG.md`, described `chore(release): prepare X.Y.Z.W`. Advancing local
`master`, pushing, tagging and the plogons manifest are each separate authorised steps. Tags are
immutable: a failed tag stays unreleased and the next version supersedes it (example: `v3.3.0.0`
failed packaging and was superseded by `v3.3.1.0`). `docs/releases/RELEASING.md` has the details.
`tools/package_release.py` packages an explicit document allowlist. Validate it from a
tracked-files-only copy, because an untracked local file passes locally and fails in CI.

## 3. Evidence workflow

The goal is to match the game where it matters for learning a mechanic (timing, positions,
hitboxes, what must be read off the screen), and to keep each claim labelled as observed,
corroborated, inferred or unknown.

### 3.1 Where evidence lives (owner's machine)

| Evidence | Location | Reader |
|---|---|---|
| IINACT/ACT network logs | `~/Dokumenty/Final Fantasy XIV/Logy/Network_<plugin>_<YYYYMMDD>.log` | `tools/parser.py` |
| Native Duty Recorder replays | `~/.xlcore/ffxivConfig/replay/autorenamed/*.dat`; older ones under `~/.local/share/dev.goats.xivlauncher/ffxivConfig/replay/autorenamed/`; supplied ones may sit in `~/Stažené/` | `tools/replay_inspector.py` |
| BossMod Reborn replays | `~/Dokumenty/Final Fantasy XIV/Replaye/*.log`: **binary** (`BLCB` = brotli), despite the extension | BossMod's own reader (`BossMod/Replay/ReplayParserLog.cs`, `ReplayLogFormat.cs`), **not** `parser.py` |
| RaidTrace | `~/Dokumenty/Final Fantasy XIV/Trace/<stamp>-duty<CFC>.rtrace` (RaidtraceTracer plugin) | `tools/rtrace.py` |
| Simulator diagnostics | `anomech-logs/` **beside the loaded plugin DLL**. On the owner's machine that is the dev build at `AnoMech/bin/x64/Release/anomech-logs/AnoMech-{Debug,Offline}-<stamp>-<build>.log.gz` (git-ignored; header names the build ID) | `zcat` + `rg` |
| Dalamud log | `~/.xlcore/logs/dalamud.log`, `dalamud.old.log`: rotates fast, holds only about the last day | `rg` |
| Real-duty event log | `~/.xlcore/pluginConfigs/FluffySim/logs/anomech-*.log`, written by `LogManager` when "Enable event logging" is on: only `EnterInstance`, `MapEffect`, `CombatStart/End` from **real** duties, no simulator diagnostics | text |
| Arena snapshots | `~/.xlcore/pluginConfigs/{FluffySim,AnoMech}/arena-snapshots/*.json` | JSON diff |
| Game data | `~/.xlcore/ffxiv/game/sqpack` (Lumina); pinned datamining CSVs | Lumina console probe, Python `csv` |
| Recovered Codex scratch evidence | `~/Dokumenty/Final Fantasy XIV/FluffySim-evidence/tmp-recovery-2026-11-11/` | see `tools.md` |

**Evidence attrition:** ACT logs older than about a month disappear (on 2026-11-12 the oldest was
`Network_30301_20261012.log`). Deleted Duty Recorder files move to `replay/deleted/`. `dalamud.log`
keeps about a day. Several sources cited in older docs (`Network_30301_20261007/10/11.log`,
`Futures Rewritten (Ultimate) 2026.10.07 23.02.28.dat`) are already gone. Copy anything a committed
value depends on into `~/Dokumenty/Final Fantasy XIV/FluffySim-evidence/` when you use it. A snapshot
of all logs and ultimate replays as of 2026-11-12 is already in `FluffySim-evidence/{logs,replays}/`.

Logs and replays contain other players' names and IDs. Never commit raw logs, replays, captures or
unsanitised extracts. Committed evidence (e.g. `docs/encounters/dsr-sanctity-meteors-events.json`,
`docs/tea/evidence-summary.json`) is aliased and checked for player names and actor IDs first.

### 3.2 From a log to a timeline

1. `python tools/parser.py "$LOG"` lists encounters. `python tools/parser.py "$LOG" <territory>`
   lists pulls (FRU 1238, DSR 968, TEA 887). Pull boundaries come from `260` InCombat, with `33` director
   codes as a fallback. Director `0x40000003` marks a clear. Combat segments are not always whole attempts.
2. Anchor on a named, unambiguous boss cast (`20` StartsCasting) and express times relative to
   it. Read `21/22` abilities (targets, damage, source and target position and heading), `26/30`
   statuses, `25` deaths, `27` head markers, `35` tethers, `257` MapEffect, `261/270/271`
   positions. Coalesce multi-target rows of one release.
3. Compare several executions. Keep cast start, target selection, snapshot, effect receipt,
   visible animation/omen and cleanup separate. All ACT times are **receive times**: tens of ms of
   jitter, no server snapshot. Positions in `21/22` rows can be stale, and the heading in an effect
   row is not necessarily the snapshot heading.
4. If the extraction will be reused, put it in a sanitised `tools/<encounter>_<mechanic>_evidence.py`
   with a test under `tools/tests/`, following the existing scripts.

### 3.3 Duty Recorder replays

- `tools/replay_inspector.py` validates the native format-5 framing (header 0x68, 64 chapters,
  `<HHII>` segments: opcode, length, ms, actor). It prints sanitised metadata; payloads stay opaque
  and are reachable by offset. Opcodes change every game build, so decode a payload only for a
  known build and recognise packets by shape. Example: TEA build 14155597, ActorCast = opcode 127,
  32 bytes, action ID at +0 and +4, cast duration float at +8.
- Align a replay to an ACT log by voting on `ACT epoch − replay_ms/1000` over matching casts.
  Keep only unique matches within 0.5 s and take the median (`tea_evidence.align_casts`). This
  aligns two receive clocks, typically to ±25 ms.
- `tools/replay_export_duty_init.py` extracts the entrance barrier, resting MapEffect states and
  RSV/RSF strings into a zone's `DutyInitData` C# file.
- ActorCast byte 3: see the open conflict in `open-issues.md` before using it as an omen delay.

### 3.4 Arena state

Native arenas (layers, map-effect slots, weather, colliders) were reproduced by diffing
`/fluffysim snapshot <label>` (also `/anomech snapshot`) taken in a paused Duty Recorder replay of
the real phase against one taken in the simulator. (Codex's early empty-arena helpers are gone from
the UI: the `/anomech arena-p3` command was removed, the FRU arena-diagnostic scenarios were deleted
and `Dsr/DsrArenaDiagnosticScenario.cs` remains in source but nothing references it.) Where a capture
has the server's MapEffect snapshot (RaidTrace `map_state`), declare it as `IZone.ArenaMapEffectSnapshot`
plus `IPhase.ArenaMapEffectOverrides` rather than replaying single effects. A snapshot is one client sample; it cannot see physics broadphase membership, director
managers or the environment-scene selector. Apply presets after native layout readiness, once per
layout generation. Do not hold layers every frame (that caused the offline DSR flicker).

### 3.5 Simulator logs

Scenario code writes tagged diagnostics through `DiagnosticLog` into the `anomech-logs/` session
files (and the Dalamud log) (e.g. `[TankSwap]`, `[ProvokeInput]`,
`[TeaI2TemporalStasis]`, `Eyes … orb`, `SimOmen:`, `Game:`, `Cast:`, `Kill:`, `MapController`,
`LoadZone`). `AiManager` logs planned bot moves (`Move@… <role>`). `DiagnosticLog` writes session
logs; the Debug menu can capture positions and action effects to
`anomech-logs/captures/positions-*.csv`. Typical diagnosis: find the seed and scenario start, `rg`
the tag, rebuild the per-run timeline, and compare it with the mechanic's deadline. A planned move
is not an observed position. Replay at 10× compresses wall-clock time, so use replay time instead.

### 3.6 From evidence to code

- Cast times and shapes come from the Action sheet. Override only the dimension the sheet lacks
  (`AreaSpec.Size`: cone **half-angle** in radians, donut inner radius).
- Timelines use absolute `world.Events.Add(t, …)` literals from scenario start. Strategies live
  only in `*Ai.cs`. Randomness comes from `world.Rng` rolled into `*State`.
- Provenance (log names, pulls, measurements) goes into `docs/` (encounter roadmaps, research
  notes, `docs/tea/*`), not code comments. Mark never-observed values `UNVERIFIED` in code.
- Every strategy must clear with no deaths in `ScenarioCatalogTests`. Then list what still needs
  in-game verification for the owner.

## 4. Codex history

Codex (GPT-6.1 Sol) developed FluffySim from 2026-10-09 to 2026-11-11. Its transcripts are in
`~/.codex/sessions/YYYY/MM/DD/rollout-*.jsonl[.zst]` (mostly zstd-compressed). The main threads
were "Implement FRU and DSR mechanics" (2026-10-09, started inside `~/Projekty/anomek/AnoMech`),
the 2026-10-16 release/CI thread, the FRU P4/P5 and DSR P1–P7 threads (10-19 to 11-02), and
"Add TEA Mana strategy" (11-09). Commit authorship shows the owner's jj identity for Codex's work,
so the transcripts are the only record of what Codex did. If a thread must be consulted again, use
`codex exec fork <id> --ephemeral` with `sandbox_mode="read-only"`. The large 10-09 thread costs
about 12M tokens per question round.
