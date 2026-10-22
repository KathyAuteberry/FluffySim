# Post-3.0.2 upstream and Wrath integration audit

Starting working copy: `vvxqwn` / `e9448a6bbd9cc658eb3d11dc1386185f803a6657`, empty child of published `pqurow` / `29a7fd4cae271c81b9f2e21fbd7093302da680c0` (`v3.0.2.0`). Its source tree equals that published baseline. Only the default workspace existed and its working copy was empty; no existing unpublished shared integration was found. A later audit of all mutable heads also found older anonymous work, preserved unchanged: `xuoxsm` / `c3d47f5593ec725592efa4ad951c7b77aed3e72d` (FRU audit/practice), `wqwkwo` / `19d05765107926fbbf4096d3ebd35eda8dd8a024` (workflow docs), and `oxmnvo` / `fe4c82431043e111d984fa0de8224367b55d8347` (empty working area). None was rebased or pulled into this task. Historical six-parent `plkqvn` / `9810be4fa280ad3da2b74e214e1bd0953d2352ee` remains immutable.

Remotes retain SSH alias `kathyauteberry.github.com`: origin KathyAuteberry/FluffySim, upstream anomek/AnoMech, evernow evernow/AnoMech. All were fetched. No publication or plogons changes.

Previously incorporated upstream: `1b704bccdc5c974173d2fee5efc1527eb0f7f257` (PR #63).
New upstream: `250b953959c560f49cc9207539bf06baade0d962` (PR #50), through `d5be13cc0f1eb814684e6c405a51b3e557e718f6` (PR #54).
New source commits: `7b8f3cf6dcb0cd0a74df23f2470acae2dd128985` (default relays) and `b3f7222ebaf30258bb7d094c6b17b8d7bcabbebd` (filter consistency).

## Semantic resolution

- Adopt default TLS relay, relay groups/discovery, ping and server status UI, host selection, relay group wire/server support, and associated upstream tests. Preserve origin-bound access tokens and FluffySim UI identity.
- Filter consistency changes overlap extensively with fixes already independently incorporated in FluffySim: action sequence/recast restoration, status settling, Threshold and emote/accessory guards, completed Teleport latch, inn-only debug holds, guarded timeline playback, party resynchronization after verified lift, and session processed-sequence reset.
- Resolve Configuration imports by retaining both collections and multiplayer namespaces.
- Preserve FluffySim ZoneControl, Guard, TimelineDebug and UserActions implementations after inspecting upstream hunks against the baseline. They already implement the corrections and add stricter missing-counter failures, synthetic-action isolation, native practice readiness, offline handling, paused-recast restoration and transition input protection. Upstream's nullable counter snapshot and warning-only failed restoration must not replace fail-closed behavior.
- Preserve Plugin pause cancellation and disposal ordering; add upstream OpenSettings for relay configuration links.
- Deduplicate automatically merged EndSpeedBuffs interface/proxy/fake members and adjacent multiplayer StartScenario calls; retain existing practice implementation and Game entry call. Add upstream RunController call before settle waiting.
- Keep FluffySim unload restoration contracts rather than adopting upstream's unconditional fatal unload during any active stay. Existing production lifecycle tests remain authoritative and are not weakened.
- No encounter mechanics, timings or action handlers are changed by this workstream.

## Checks before independent feature work

Debug x64 solution build passed (8.62 s MSBuild elapsed; existing platform warnings).
Upstream relay/configuration focused suite: 36 passed, 7 s runner duration.
Relay suite: 67 passed, 1 s runner duration.
Final integration safety results for both packet backends and ordinary/optimized artifact freshness checks are recorded below.

Native checks: default/custom relay switching, group discovery and host selection with real relays; repeated inn/offline entry, exit, pause and unload; speed-buff settling and native action restoration. Headless checks cannot establish native rendering or live network service availability.

## Wrath workstream

Independent feature `qmwzpp` is a direct child of published `29a7fd4c`, not of upstream branch `utzpkx`. The merge has both changes as direct parents. No parallelize operation is needed because no false serial dependency was created. simplify-parents leaves both genuine parents intact.

Snapshots remain at 44.97, initial attacks at 45.06/45.10, object spawn at 45.70, activation at 46.70, removal at 50.90. Immutable owner/position records expose exactly the same positions used by collision and rendering. Consumed/expired hazards disappear from the routing input. No bait position is moved; the human is never steered by this AI.

The existing clockwise waypoints and 8.3 y/s sprint speed remain. Before issuing a move, a bounded circle visibility graph checks continuous segment/circle intersection only during the danger interval, with 0.35 y clearance and an activation timing allowance. The existing collision uses actor centers within 1.5 y (not hitbox-expanded circles), so the routing margin does not change damage. Marked routes are monotonic in clockwise bearing, stay outside radius 12 and inside radius 20, and cannot cross through the arena. Candidates come only from small offsets around obstructing circles. Clear direct routes remain unchanged. Blocked destinations get a radial adjustment of at most 2.5 y on the same bearing and return to their assigned spread point after expiration. Route lengths include segment handoff allowance and must finish before expiration, so detours cannot take over subsequent bait movement. Each leg is checked again when it starts; newer waypoint orders cancel stale legs. If no safe route fits the deadline, the AI logs a warning and ordinary lethal mechanics remain in effect.

No new general per-frame steering loop is introduced. The existing reactive ObstacleField can choose either side and can return a blocked heading when boxed in; using it alone would not establish the explicit clockwise and continuous-path constraints here. The small timed-circle planner lives in Core geometry; strategy and execution remain in Wrath AI.

Diagnostics are event-level: bait owner, exact snapshot, activation/expiration, planned route, chosen detour, changed hazard and failed route, and actual collision. No per-frame logging.

Bounded tests cover unaffected routes, active direct obstructions, obstructed waypoints, two nearby circles, own-bait departure before activation, stationary humans remaining punishable, immutable baits after human movement, expiration, all four cardinal rotations, Easy/Hard, and existing human blue/tether input replay. The complete obstructed-waypoint tests run through 88.10 with no deaths, including spreads, divebomb, Thunderstruck, later baits and tank swaps. Additional full-resolution cases put the human bait between clockwise waypoints. Human input is supplied by the test at normal sprint speed; later input reuses the successful reference run.

Reference reviewed: https://ffxiv.tuufless.com/elemental/dsr/05_alternate_thordan/ — Wrath steps 3–5 require post-hit Twister dodges, marked players clockwise, divebomb behind Charibert and east-side spreads.

## Starting source fingerprint

918 tracked files at published baseline. SHA-256 of `jj file list -r master@origin`, NUL, then `jj file show -r master@origin .`:
`a382b1df6c0445aa2c585a3572d0b677374c971d5b59a4e461d0a02d712e4f0c`.

## Graph

Before: empty `vvxqwn` -> published `pqurow` (`master`, `master@origin`, `v3.0.2.0`) -> published integration `kyqwrz`; upstream `lpvklk` was already an ancestor of that release.

After (edges point to parents):

```text
wllxlr  chore: integration commit
|\
| qmwzpp  fix(dsr): avoid snapshot Twisters along clockwise Wrath routes
| | 
utzpkx  chore(upstream): integrate AnoMech relay and consistency updates
|\ |
|  \|
|   pqurow  published 3.0.2.0 (master and master@origin unchanged)
250b9539  newest upstream, with its original PR parents/ancestors
```

The ancestry check `(qmwzpp | master@upstream) & ancestors(wllxlr)` returns both revisions. `plkqvn` remains `9810be4fa280ad3da2b74e214e1bd0953d2352ee` with six parents. Each changed file from the 28-file upstream diff and 6-file Wrath diff was compared byte-for-byte against the merged tree; no mismatches. Version, changelog, ZoneControl, ZoneControl.Guard and UserActions were compared to the published baseline and remain unchanged.

## Remaining native playtesting

- Bait near/in the marked clockwise lane, including late movement and nearby baits, with white dragon at each cardinal. Check that native models follow the modest detours and retain spacing for Mercy.
- Human blue marker, either tether and unrelated roles; Easy and Hard; stationary human must still trigger lethal Twister collision.
- Confirm no early movement affects initial Leap/Pierce snapshots and no old route callback interferes with later baits.
- Exercise relay discovery/default/custom selection against actual services, then inn/offline entry, exit, pause and native actor readiness.
- Three actual Windows hook-patching tests cannot execute on Linux; both modeled backend failure semantics and SDK metadata contracts are covered headlessly.

No Release build, installed plugin writes, version bump, release preparation, master advancement, push, tag, publication or plogons edits were performed.

## Validation commands and results

All commands used Debug only, with `DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev`.

- Independent upstream build: `dotnet build AnoMech.sln -c Debug -p:Platform=x64`; 0 errors, 8.62 s MSBuild elapsed.
- Independent upstream checks: 36 relay/configuration tests and 67 relay library tests passed.
- Independent Wrath/P5/geometry checks: 259 passed, 24.493 s wall (20 s runner).
- Final affected P5, timed geometry, relay/configuration, native, instance/run control and offline checks: 2,006 passed, 47.293 s wall (40 s runner), Extended excluded. Includes the final deadline bound and both between-waypoint cases. Output/intermediates were `tests/AnoMech.Tests/bin/next-validation-Debug/` and project-local `obj/next-validation-Debug/`.
- Integrated relay library: 67 passed, 2.265 s wall (1 s runner).
- Ordinary final Debug plugin build: 0 warnings/errors, 3.736 s wall.
- Optimized TRACE final Debug plugin build: 0 warnings/errors, 3.718 s wall; isolated `/tmp/fluffysim-next-safety/` and project-local `obj/safety-Debug/`.
- Final Reloaded backend safety run: 313 passed, 3 Linux skips, 20.298 s wall (18 s runner).
- Final SafetyHook backend safety run: 313 passed, 3 Linux skips, 19.042 s wall (18 s runner).
- Both backend runs used `ANOMECH_DLLS` naming ordinary and optimized Debug binaries simultaneously; no stale override. Architecture, production lifecycle, metadata contracts and both modeled failure semantics passed.
- Negative freshness check: a temporary copy of the Debug DLL, timestamped 2000-01-01, was rejected with “older than the plugin's sources; rebuild it before running these tests.” One intentional test failure, 1.092 s wall. No installed artifact was involved.
- An exploratory final-focused run outside the repository passed 2,005 checks but failed the source-boundary test's repository lookup. Moving only the output directory back under the repository resolved it; no test assertions were changed.

The broad routine candidate suite runs against the initially integrated binary. The final 2,006-check affected suite above additionally covers the later duplicate-call cleanup, deadline bound and added deterministic cases. Extended stress tests remain opt-in.

- Broad routine candidate suite: 7,421 passed, 577.350 s wall (9 m 35 s runner), Extended excluded. Three optional replay/export utilities were listed as skipped by the adapter; its final totals reported 7,421 passed and zero failures.
- Final source-tree Debug x64 solution build: 0 errors, 14 existing NUnit analyzer warnings, 5.322 s wall (5.19 s MSBuild).

Shared integration change: `wllxlrkxrsoltlyyltxyxlklzusnnlzs`, description exactly `chore: integration commit`. Its final feature parents are `utzpkx` (upstream, `d7bdc3473977`) and `qmwzpp` (Wrath, `52d9d72cbd85`). The working copy is left as an empty child of this integration; local master and master@origin remain at published 3.0.2.0. The integration is ready for native playtesting, not publication.
