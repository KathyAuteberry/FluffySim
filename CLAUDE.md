# CLAUDE.md

Claude Code instructions for FluffySim. The complete repository instructions are imported below
and remain authoritative; this file adds what Claude needs on top of them.

@docs/development/AGENTS.md

## Handover documentation

Development moved from Codex to Claude Code on 2026-11-11. Read these before encounter work:

- [docs/handover/workflow.md](docs/handover/workflow.md): evidence workflow, build/test, jj integration
- [docs/handover/tools.md](docs/handover/tools.md): repository tools, in-plugin diagnostics, log and replay formats
- [docs/handover/resources.md](docs/handover/resources.md): strategy sources, reference code, what informed what
- [docs/handover/open-issues.md](docs/handover/open-issues.md): known inaccuracies, source conflicts, do-not-retry list

## Rules that override defaults

- **Debug only.** Never build Release locally. Never write into installed plugin output. Never
  publish, tag, push, or touch the release pipeline, `FluffySim.Version.props`, `docs/releases/`
  or `../plogons` without the owner's explicit authorization in the current conversation.
- **Jujutsu, not Git history commands.** Work commits are children of `master@origin` and are
  integrated through the shared `chore: integration commit`. Release commits are separate from
  encounter work. See `workflow.md` for the exact commands.
- **Write only inside this repository.** Treat everything else as read-only, including the
  third-party reference repositories under `~/Projekty/` (all except `KathyAuteberry/*`), logs,
  replays, Dalamud and plugin configuration. Ask before writing anywhere else.
- **Japanese strategies first.** Mana for FRU/TEA and Elemental for DSR are the selected strategies;
  never substitute another region's strategy silently.

## Evidence hierarchy

Highest first:

1. In-game evidence: IINACT/ACT network logs, FFXIV Duty Recorder replays, FluffySim's own logs
   and arena snapshots.
2. Original external sources: the strategy authors' own material (Japanese docs, macros, videos),
   datamining, community encounter code.
3. The owner's guide site "The Last Laugh": a cross-check only, never a source of truth.

When sources disagree, record the conflict in `docs/handover/open-issues.md` and tell the owner.
Never resolve a conflict silently. Receive timestamps in logs are not server snapshots. Values
copied from BossMod or other simulators are reference-derived, not retail measurements.
