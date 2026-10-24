# Native raid audit and local latency integration

Work performed October 25, 2026 JST. No release preparation, version changes,
bookmark advancement, tag, push, publication, installed output modification or
plogons operation was performed. Original private captures remained read-only.

## Independent workstreams

| Workstream | Change | Parent | Result |
|---|---|---|---|
| A: FRU evidence | `uqlusw` / `4d43da` | `vnrsos` | [Audit and timing tables](fru-native-audit-2026-10-24.md); no production correction |
| B: DSR evidence | `oztvsr` / `481ff2` | `vnrsos` | [Audit and timing tables](dsr-native-audit-2026-10-24.md); numbered statuses clear at their own dives |
| C: local latency | `komyxn` / `98736f`, followed by `vuuyzq` / `ac22b3` | `vnrsos` | [Model, configuration and native checklist](latency-simulation.md); optional RTT/2 outgoing receipt model and safety-wiring test update |

Separate Jujutsu workspaces prevented the three changes forming a serial stack.
The main workspace started clean at empty change `vqzmuk`, above published
`vnrsos` / `d12da9` (3.1.1.0). Every pre-existing integration was published;
there was no unpublished shared integration to reuse. Historical mutable tips
`msszqs`, `xuoxsm`, `wqwkwo` and `oxmnvo` were preserved untouched. In particular,
`msszqs` is the rejected clone-visibility experiment and was not incorporated.

Before:

```text
vqzmuk (empty working copy)
  |
vnrsos (master = master@origin, published 3.1.1.0)
  |
uxmkvx (published integration, existing ancestry)
```

After:

```text
               empty working copy
                        |
          lutnym (chore: integration commit)
                  /     |     \
             uqlusw   oztvsr   vuuyzq
                  |     |       |
                  |     |     komyxn
                   \    |      /
                    vnrsos (master = master@origin unchanged)
                       |
                    uxmkvx (published ancestry unchanged)
```

`jj simplify-parents -r @` found no redundant parents. Revset intersection
checks verified all three feature changes, `plkqvn`, confirmed native readiness
fix `xskups`, and spectator changes `xopvqt` and `nopmvn` are ancestors of
`lutnym`. This is ancestry verification, not an inference from descriptions.
The only merge conflict was `.gitignore`; all report allowlist entries were
retained. The integration also clarifies the DSR table's final status-removal
times alongside its historical values.

## Validation

Validation uses Debug only. SDK path is
`/home/kath/.xlcore/dalamud/Hooks/dev/`. Solution builds require `-m:1`,
`-p:Platform=x64` and `-p:EnableSourceControlManagerQueries=false`; the last
property avoids the SDK source-link reader rejecting Git's `relativeworktrees`
extension, without changing repository configuration.

| Combined check | Result | Runner/build elapsed | Complete command wall time |
|---|---|---:|---:|
| Full Debug solution build | Pass, 0 errors; 6,150 existing warnings | 11.41 s | 11.587 s |
| Optimized TRACE Debug plugin, isolated output/intermediates | Pass, 0 warnings/errors | 7.70 s | 7.854 s |
| Safety test project Debug build | Pass, 0 warnings/errors | 3.31 s | 3.464 s |
| Bounded latency/lifecycle/action/pause/spectator regressions | 676 passed, 0 failed/skipped | 123 s | 124.927 s |
| Scenario catalog, one seed per strategy | 55 passed, 0 failed; explicit replay helpers unexecuted | 8 s | 9.985 s |
| Relay suite | 67 passed, 0 failed/skipped | 1 s | 2.252 s |
| Safety, default backend; ordinary + optimized Debug | 315 passed, 4 skipped, 0 failed | 18 s | 20.008 s |
| Safety, SafetyHook backend; ordinary + optimized Debug | 315 passed, 4 skipped, 0 failed | 19 s | 20.173 s |
| Intentionally stale temporary DLL negative check | Expected refusal, 1 selected check rejects input | 0.015 s | 1.144 s |
| Python tooling, after safety-wiring update | 76 passed | 0.052 s | 0.054 s |

The 676-check selection includes `LocalLatencyTests`, FRU/DSR practice lifecycle,
P3 Nidhogg, spectator and spectator actions, pause, user actions, practice,
enemy actions and SimCast tests, excluding `Extended`. The dedicated latency
branch also passed 101 focused checks (39 new latency cases and 62 existing
action checks) in 9 s runner / 10.798 s wall; the DSR branch passed 60 focused
checks in 15 s runner / 23 s wall. These overlap the integrated selection and
must not be summed as unique coverage.

Both safety runs use the normal bounded 2,000-seed fuzz default and both
fresh artifacts through `ANOMECH_DLLS`. Skips are the three Windows native hook
cases and the optional installed DelvUI contract. Optimized output is
`/tmp/fluffysim-safety-native-latency/`, intermediates are
`obj/safety-native-latency-Debug/`; ordinary Debug artifacts retain their
unoptimized IL classification. A temporary DLL dated January 1, 2000 was
refused as older than plugin sources, without `ANOMECH_ALLOW_STALE_DLL`.

The initial full baseline run was canceled to prioritize the completed
integration; it is not counted as a pass. The first integrated Python run
exposed an old source-layout assertion after defensive dispatch moved into
its delayed resolver. `vuuyzq` updates the assertion to verify admission,
callback guards, defense-before-confirmation order and optional handler
gating in the actual new path. No production behavior or safety allowlist
was changed to satisfy it. Extended stress matrices were not run.

## Native acceptance boundary

Neither headless tests nor incoming replay timestamps establish rendered
first-visible frames or exact server receipt times. Retest native dive slam
and omen presentation, borderline dodges, hotbar queueing/hardcasts, Provoke,
mitigation, forced movement/Return, Pause/Restart/Leave and mode switching in
both inn and offline practice. Multiplayer intentionally ignores this local
latency preference. The model has no jitter, loss, retransmission, incoming
presentation delay, server tick emulation or RTT-dependent animation lock.
Changing simulation speed during an action's pending delivery changes its
remaining wall-clock wait; use a fixed speed for latency comparisons.
