# Post-3.0.2 upstream audit

Starting working copy: `vvxqwn` / `e9448a6bbd9cc658eb3d11dc1386185f803a6657`, empty child of published `pqurow` / `29a7fd4cae271c81b9f2e21fbd7093302da680c0` (`v3.0.2.0`). Its source tree equals that published baseline. Only the default workspace existed; no unpublished integration or concurrent feature work was present. Historical six-parent `plkqvn` / `9810be4fa280ad3da2b74e214e1bd0953d2352ee` remains immutable.

Remotes retain SSH alias `kathyauteberry.github.com`: origin KathyAuteberry/FluffySim, upstream anomek/AnoMech, evernow evernow/AnoMech. All were fetched. No publication or plogons changes.

Previously incorporated upstream: `1b704bccdc5c974173d2fee5efc1527eb0f7f257` (PR #63).
New upstream: `250b953959c560f49cc9207539bf06baade0d962` (PR #50), through `d5be13` (PR #54).
New source commits: `7b8f3cf6dcb0cd0a74df23f2470acae2dd128985` (default relays) and `b3f7222ebaf30258bb7d094c6b17b8d7bcabbebd` (filter consistency).

## Semantic resolution

- Adopt default TLS relay, relay groups/discovery, ping and server status UI, host selection, relay group wire/server support, and associated upstream tests. Preserve origin-bound access tokens and FluffySim UI identity.
- Filter consistency changes overlap extensively with fixes already independently incorporated in FluffySim: action sequence/recast restoration, status settling, Threshold and emote/accessory guards, completed Teleport latch, inn-only debug holds, guarded timeline playback, party resynchronization after verified lift, and session processed-sequence reset.
- Resolve Configuration imports by retaining both collections and multiplayer namespaces.
- Preserve FluffySim ZoneControl, Guard, TimelineDebug and UserActions implementations after inspecting upstream hunks against the baseline. They already implement the corrections and add stricter missing-counter failures, synthetic-action isolation, native practice readiness, offline handling, paused-recast restoration and transition input protection. Upstream's nullable counter snapshot and warning-only failed restoration must not replace fail-closed behavior.
- Preserve Plugin pause cancellation and disposal ordering; add upstream OpenSettings for relay configuration links.
- Deduplicate automatically merged EndSpeedBuffs interface/proxy/fake members; retain existing practice implementation and Game entry call. Add upstream RunController call before settle waiting.
- Keep FluffySim unload restoration contracts rather than adopting upstream's unconditional fatal unload during any active stay. Existing production lifecycle tests remain authoritative and are not weakened.
- No encounter mechanics, timings or action handlers are changed by this workstream.

## Checks before independent feature work

Debug x64 solution build passed (8.62 s MSBuild elapsed; existing platform warnings).
Upstream relay/configuration focused suite: 36 passed, 7 s runner duration.
Relay suite: 67 passed, 1 s runner duration.
Final integration must also pass both packet backends and ordinary/optimized artifact freshness checks.

Native checks: default/custom relay switching, group discovery and host selection with real relays; repeated inn/offline entry, exit, pause and unload; speed-buff settling and native action restoration. Headless checks cannot establish native rendering or live network service availability.
