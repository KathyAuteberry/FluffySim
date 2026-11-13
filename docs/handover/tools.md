# Tools

Every tool Codex built or relied on, what it does, and where it is now. Run Python tools from the
repository root (`python -B tools/<tool>.py …`). Nothing here writes to its inputs.

## 1. Repository tools (`tools/`)

| Tool | Origin | Invocation | Input → output |
|---|---|---|---|
| `parser.py` | Upstream anomek ("vibe-coded"; leave as is) | `parser.py LOG` lists encounters; `parser.py LOG TERRITORY` lists pulls; `parser.py LOG TERRITORY PULL BEGIN END -x --x 100 --y 100` prints a relative timeline with arena-relative coordinates; `--raw` shows original records; `--code` emits a C# draft. Library: `iter_records`, `find_pulls(path, territory)`, `parse_timestamp` | ACT/IINACT network log → pulls, records. Output is **not sanitised** |
| `filter.py` | Upstream | Imported by `parser.py` | — |
| `replay_inspector.py` | Codex (2026-10-09) | `replay_inspector.py DAT [--expect-build N] [--from-seconds S --to-seconds S] [--opcode OP …] [--segments OUT.jsonl]`; library `Replay(path[, build])` with `.segments()` and `.payload(seg)` | Native format-5 Duty Recorder `.dat` → sanitised metadata (actors aliased, payloads opaque) |
| `replay_export_duty_init.py` | Upstream (PR #61) | `replay_export_duty_init.py DAT [DAT…] [--rs DAT…] [--namespace NS] [--class NAME] OUT.cs` (`--namespace` is required when `OUT.cs` is outside `AnoMech/`) | Replays → zone `DutyInitData` C# (barrier EObj, resting MapEffects, RSV/RSF strings). Supersedes the older `replay_export_rsv_rsf.py` |
| `fru_fall_of_faith_evidence.py`, `fru_darklit_evidence.py`, `fru_apocalypse_evidence.py` | Codex | `TOOL LOG --output OUT.json` | ACT log → sanitised mechanic timeline |
| `fru_diamond_dust_evidence.py`, `fru_ultimate_relativity_evidence.py` | Codex | `TOOL LOG [LOG…] --output OUT.json` | Same, several logs |
| `dsr_strength_evidence.py`, `dsr_strength2_evidence.py`, `dsr_sanctity_evidence.py` | Codex | `TOOL LOG --output OUT.json` | Same |
| `dsr_meteors_evidence.py` | Codex | `TOOL LOG` (JSON on stdout) | Short Meteors inventory |
| `dsr_meteors_reconstruction.py` | Codex | `TOOL LOG --output OUT.json` | Produced `docs/encounters/dsr-sanctity-meteors-events.json` (from `Logy/Network_30303_20261016.log`) |
| `fru_native_audit.py` | Codex | `TOOL LOG --native-replay DAT [--native-replay DAT] --output OUT.json` | FRU cast/MapEffect receive-time audit (→ `docs/research/fru-native-audit-2026-10-24.md`) |
| `rtrace.py` | Claude (2026-11-12) | `rtrace.py TRACE` prints a summary (tracer metadata, run territory/content, duration, record counts); `--schema` prints the embedded schema; `--kind NAME [--kind …] [--limit N]` emits JSON lines (`clock_s` plus fields). Player names and account/content IDs are redacted unless `--no-redact`. Library: `Trace(path).records()` → `Record(kind, name, clock, values)`. Needs the `zstd` CLI or the `zstandard` module | `.rtrace` → decoded records. Output is identifying without redaction; never commit it |
| `tea_evidence.py` | Codex | `TOOL LOG --replay DAT --replay DAT --output OUT.json` | TEA (territory 887, build 14155597): pulls, NPC IDs, events, numbered motion, replay casts/waymarks/alignment. **Does not regenerate `docs/tea/evidence-summary.json` verbatim**; that summary was post-processed by scratch scripts (§3) |
| `package_release.py`, `release_notes.py` | Codex | Used by CI and the release procedure only | Do not run or change without release authorization |
| `tools/tests/` | Codex | `python -m unittest discover -s tools/tests` | Regression tests for the tools above. Fixture `tests/fixtures/dsr_meteors_complete_failed.log` |

Pattern for a new evidence script: wrap `parser.py`, fix the action-ID set and window, alias
actors, emit JSON, add a test with a tiny sanitised fixture.

## 2. Diagnostics inside the plugin

| Facility | How | Output |
|---|---|---|
| Arena snapshot | `/fluffysim snapshot <label>` (or `/anomech …`); needs a ready, matching territory/layout | `~/.xlcore/pluginConfigs/FluffySim/arena-snapshots/<stamp>-<label>-<guid>.json` (layers, festivals, map-slot bindings, weather, environment, collider getters; no player identity). Writer: `Core/Native/Implementations/ArenaSnapshot.cs` |
| Diagnostic log | Always on; Offline Trace UI shows the active path and has Flush | `anomech-logs/AnoMech-{Debug,Offline}-<stamp>-<build>.log(.gz)` beside the loaded DLL (owner: `AnoMech/bin/x64/Release/anomech-logs/`); fatal notes `AnoMech-FATAL-*.txt`. Writer: `Core/DiagnosticLog.cs`. Tags include `[DsrP3Dive]`, `[EnemyPresentation]`, `[EnemyAction]`, `[AiManager]`, `[ArenaReady]`, `[SimEnemy.*]` |
| Real-duty event log | Config "Enable event logging" | `~/.xlcore/pluginConfigs/FluffySim/logs/anomech-*.log`: `EnterInstance`/`MapEffect`/`CombatStart`/`CombatEnd` of real duties only. Writer: `Core/LogManager.cs` |
| Tagged Dalamud log lines | Emitted by scenarios and core, e.g. `[TankSwap]`, `[ProvokeInput]`, `[TeaI2TemporalStasis]`, `[AiManager] Move@t: role …` | `~/.xlcore/logs/dalamud.log` |
| Position/effect capture | Debug menu (`Windows/DebugMenu.cs`) | `anomech-logs/captures/positions-*.csv` |
| Chat subcommands | `config`, `mp`, `start`, `start solo`, `reset`, `leave`, `snapshot <label>`, `firewall`, `antikb`, `offline` (`Plugin.cs`) | — |

## 3. Recovered Codex scratch tools

On 2026-11-11, after the 2026-11-08 reboot, these were still in `/tmp`. They were copied (verified identical) to
`~/Dokumenty/Final Fantasy XIV/FluffySim-evidence/tmp-recovery-2026-11-11/`:

- `fluffysim-tea-evidence/`: hard-coded-input scripts and outputs. `inventory.py` writes
  `log-summary.json`, `log-events.json` and `replay{1,2}-casts.json`. `align.py` writes
  `replay{1,2}-alignment.json`. `detail.py`, `ground.py`, `p1.py` and `p2.py` write IDs/statuses,
  ground effects and per-phase events. `lc.py` and `lc-dense.py` both write `limit-cut-motion.json`
  (last writer unknown). `wormhole.py` writes `wormhole-number-motion.json`. `nisi-distances.py` and
  `nisi-native.py` both write `nisi-pass-distances.json`. Inputs: `~/Stažené/Logs/Network_30304_20261108.log`
  and the two TEA `.dat` files in `~/Stažené/Duty Recording/`.
- `scripts/build_mana_lc_paths.py`: read `limit-cut-motion.json` to scaffold `TeaI1LimitCutAi.cs`
  paths. `rewrite-tea-p2.py`, `tea_restructure.py`, `tea_topology_groups.py`, `resolve_tea_merge.py`
  and `fluffysim_docs_cleanup.py` were source and history editing helpers, not evidence readers.

Everything Codex put in `/tmp` before 2026-11-08 is gone. The source of about 120 such scripts can be
recovered verbatim from the Codex transcripts if ever needed. They fell into these families:

- Lumina/FFXIVClientStructs C# console probes (`dotnet run`) referencing
  `~/.xlcore/dalamud/Hooks/dev/*.dll` and `GameData("~/.xlcore/ffxiv/game/sqpack")`, used to read
  sheets (Action, Omen, Lockon, Status, BGM, ModelChara), SGB/LGB layers and AVFX existence.
- Mono.Cecil probes of packet struct offsets.
- Per-mechanic ACT and replay one-offs.
- An IINACT OverlayPlugin WebSocket capture (`127.0.0.1:10501`). Abandoned; see `open-issues.md`.
- A BossMod-replay decoder (brotli body after a 4-byte `BLCB` header; `VER ` v31; `FRAM`, `ACT+`,
  `MOVE`, `CST+`, `CST!` records).
- An ENVS/OBSB environment-file decoder for DSR P7.

Rebuild such a probe when needed rather than reviving old copies.

## 4. External tools Codex used

`yt-dlp` (Japanese subtitles, ≤360p video for frames), `ffmpeg` (frame extraction), Python
`brotli` and PIL, `strings`, `monodis`, `ilspycmd`, `xxd`, `gh` and curl (GitHub API/CI status), and
the `rea` MCP server, which inspected `Lumina.dll` types twice for DSR P7.

## 5. Format notes

- **ACT/IINACT**: pipe-delimited text. `21/22` row fields: 2 source ID, 3 source name, 4 action ID (hex),
  6 target ID, 8–23 effect pairs, 24/25 target HP, 30–33 target x/y/z/heading, 40–43 source
  x/y/z/heading. Heading is in radians; the facing vector is `(sin h, cos h)` in (x, y).
- **Native Duty Recorder `.dat`**: `FFXIVREPLAY` header with format, build, content and chapter
  table, then segments. Opcodes are build-specific.
- **BossMod Reborn replay**: `BLCB` (brotli binary), `BLOG` (raw binary) or BMR text (its own
  schema). None of them is ACT format, and `parser.py` silently returns nothing on them.
- **RaidTrace `.rtrace`** (RaidtraceTracer 0.3.1, uploads to raidtrace.com when enabled): magic
  `RAIDTRCE`, then a self-describing schema (record types with typed fields), then records.
  Record types: `action` (server_time, caster, animation target, action ID/variation/animation,
  global/source sequence, **facing, animation_lock, position**, targets, effects), `effect_result`
  (HP, shield, statuses), `status_list`, `cast` (cast_time, target, facing, position), `movement`
  (facing and position updates), `actor_control`, `enmity`, `map_effect`, `npc_yell`,
  `system_message`, `gauge`, `spawn` (model, customize, tethers, owner/parent, position, player
  identity fields), plus `tether`, `headmarker`, `map_state`, `weather`, `waymark`,
  `party_positions`, `actor_move`, `object_spawn`, `zone_init`, `rsv`/`rsf`, own `position_report` and
  `action_request`, and raw `packet` fallbacks (70 kinds in 0.3.x).
  Encoding (reverse-engineered on 2026-11-12 from the captures and the tracer's unstripped Rust symbols,
  crate `trace::{file,columns,schema}`; validated by exact byte consumption on all 16 local captures):
  - **Schema:** varint kind count; per kind a varint ID, a length-prefixed name, a field count and
    fields as (name, type). Types: 0 u8, 1 u16, 2 u32, 3 u64, 4 f32, 5 string, 6 bytes, 8 group (followed
    by a child field count and child fields).
  - **Blocks:** `RTBK`, u32 length, u32 CRC-32, then a body of varint codec (2 = zstd), varint
    decompressed size and the zstd data. The file ends with an `RTIX` index.
  - **Inflated block:** varint row count, a kind ID per row, and a zigzag-delta local clock per row
    (100 kHz). Then per kind a varint ID, row count and byte length, followed by a column-major body:
    - u8 is a raw byte;
    - other integers, and f32 bit patterns, are zigzag deltas within the column;
    - strings and bytes are length-prefixed;
    - groups are a plain count per row followed by the child columns;
    - the `actor`/`caster` key column comes first and is in arrival order; every other column of
      a keyed kind is stably sorted by key. `rtrace.py` puts the rows back in arrival order (checked
      against ACT casts, actions, head markers, tethers and MapEffects).
  - `server_time` is epoch milliseconds; tracer 0.3.0 wrote 0. Kinds 1 and 2 (no schema) are tracer
    metadata JSON.
  - Capture filenames (`YYYYMMDD-HHMMSS-duty<CFC>`) are in **UTC**: `20261110-141323` is 23:13 JST.
  - **Only tracer ≥ 0.3.1 captures are reliable.** Earlier ones decode, but their content is
    incomplete or scrambled; `rtrace.py`'s summary warns about them. The `duty_recorder` kind's
    `playback` bits toggle at every pull reset even in live duties, so they do not mean "replay".
