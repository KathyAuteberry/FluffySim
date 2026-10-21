# Community QA history cleanup and upstream integration — 2026-10-21

This is an unreleased integration checkpoint for native acceptance testing. No release preparation,
version change, published bookmark movement, push, tag, publication or plogons change was performed.

## History and content checkpoints

Published baseline: `xorxqy`, commit `f0f74baff26a0d8de9e9f82bc875214483146e0d`
(`master`, `master@origin`, published 3.0.1.0). Those references did not advance.

Before cleanup, the completed changes formed this linear stack, oldest first:

```text
master@origin → kxkywx → zxqmxx → yowxqp → mozkkp → xlkrwp → ruwtky → knuuox → mvooqr
                                                                                 ↑
                                                                    empty working change lknlwu
```

Created `kyqwrzrptzmumzkpmnvxlyymxsxrsmzy` FIRST above `mvooqr`, with the exact description
`chore: integration commit`. Then ran:

```sh
jj parallelize 'kxkywx | zxqmxx | yowxqp | mozkkp | xlkrwp | ruwtky | knuuox | mvooqr'
jj simplify-parents -r kyqwrz
```

The eight changes have distinct implementation responsibilities and no required API/data dependency
on one another. Each fix's implementation and its focused tests remain together in their original
change. The QA test-only change uses baseline APIs; the reports describe combined results, but do not
provide implementation dependencies. Wrath and Death edit separate scenario actor registrations;
Provoke edits shared action routing; CT edits damage/mitigation; clockwise movement edits Wrath AI.
Sharing the overall encounter does not require serial implementation ancestry.

Descriptions and patches were preserved. Their change IDs are stable; unpublished commit SHAs changed
when their parents changed. No feature was squashed. There were no cleanup conflicts.

**The history-only combined tree was exactly preserved before fetching/integrating upstream.**
The original checkpoint was `b4a005f02053401f8fd7424559e516e4714b6188` (also the tree of the
initial empty working change). The cleaned eight-parent integration was `2f08f9`.
`jj diff` between them reported **0 files changed**. Full `jj debug tree` manifests compared equal,
including executable flags and file object IDs. SHA-256 of both manifests:

```text
e91c7162fa0fa1e605ca03f1733c91895de936d115efce264162b338db382ca8
```

## Final integration parents

```text
                     kyqwrz — chore: integration commit
                  / / / / / / / /                \
             eight QA siblings                 upstream master
                  \ \ \ \ \ \ \ \                |
                 published 3.0.1.0             new upstream work
                         \                       /
                       previously shared ancestry
```

All eight local feature parents have `master@origin` as their immediate parent. Upstream retains its
original author/merge history. `jj simplify-parents` found no redundant edges among these nine parents.

| Change | Actual parent commit of integration | Responsibility |
|---|---|---|
| `kxkywx` | `36f6a64c4848aee29d6fbc71228fff0d25d8299d` | Darklit native Water indicators |
| `zxqmxx` | `846f7a27681a317437f4225e0f1a94d1c1ba6485` | Wrath native Twisting Dive Enemy List |
| `yowxqp` | `cc97475862efe6939d4cec91bca7c60ce5d7c766` | CT/tankbuster QA tests and report |
| `mozkkp` | `2f172fdc39994648eb4424ae90069017490183ee` | Death native Twisting Dive Enemy List |
| `xlkrwp` | `58eb9134115b9af6b0d99b8e106fbf4c25a38bab` | Human Provoke action routing/window |
| `ruwtky` | `d920a55b75ae71c51e36e288507a4bdd73a60c63` | Ranked CT Wings damage/full Mana support |
| `knuuox` | `8b95b2cd502dba11ce9968560c7cd1db4636a899` | Wrath marked targets' clockwise movement |
| `mvooqr` | `e1ce69148427affac344d70391046f36fd67aa3f` | CT/Wrath research and validation report |
| `lpvklk` | `1b704bccdc5c974173d2fee5efc1527eb0f7f257` | Updated `master@upstream` |

Only the default workspace exists. Unrelated mutable changes `xuoxsm` (`c3d47f5593ec`), `wqwkwo`
(`19d057651079`) and `oxmnvo` (`fe4c82431043`) were left unchanged. Published `plkqvn`
(`9810be4fa280`) remains an ancestor, as do the shipped Paradise Regained/Wroth Flames repairs.
No additional bookmarks were created.

## Fetch and upstream review

Ran `jj git fetch --all-remotes`. Verified `upstream` points to
`kathyauteberry.github.com:anomek/AnoMech`; origin is KathyAuteberry/FluffySim and lowercase evernow is
Evernow/AnoMech. The SSH alias was preserved. Origin master did not independently advance. Evernow's
other feature tips were inspected as references, not indiscriminately merged. Fetch pruned obsolete
remote-only commits; no local QA or unrelated development was discarded.

The previous integrated upstream tip was `6aec4a5940df56ebc4f70a2fdddeceb31c7848b8`.
The new tip is `1b704bccdc5c974173d2fee5efc1527eb0f7f257`, with these 23 newly included commits:

```text
1b704bccdc5c Merge PR #63, Evernow/mp-5-r2
47219981b3b3 Remove debug bot LB3 logic
1811f43887bf Merge PR #60, Evernow/mp-4-falling-behind
8fb9bfcdd8e8 Fix first-run blank boss-line text after restart
76d7d5a5738c Show other players' auto-attacks
136e6d64e13a Show other players' jumps
7800d1e18f79 Send poses only when they change
fc0ef24d6616 Walk other players' copies at their own pace
fbdae57b599b Relay spawn appearance, statuses, model state and scale
89aa31881d41 Acquire Black Hole's new tethers once the client shows them
20af083ee6a8 Apply peer enemy debuffs consistently with host
4ea41c26c616 Debug bot LB through its own client (removed by 47219981b3b3)
88063c78ce5e Preserve effect targets with no effects
dc6f138ffd2d Spend shared party gauge on peer limit break
ea913335ec4c Treat peer debug bot as bot
01c91ec73275 Apply host packets after peer run setup
82cef41382b8 Count any host message as alive
060a3f56e498 Unify peer message list between relay and host
fab593df83c2 Centralize relayed control rules
e0c03f006edb Retain status source after applier despawns
874799cc76c6 End run when peer falls 10 seconds behind
81347895b63d Show result marks to every player
30e27cca6e88 Relay players' own actions to party
```

The installed jj is 0.46.0. Its revset parser does not accept the proposed `all:` prefix inside an
`--onto` argument. The supported equivalent was used: rebase **only the integration and descendants**
with nine explicit `-o` destinations, the original eight feature changes plus `master@upstream`.
No feature branch was individually rebased onto upstream.

The imported code retains upstream authorship/ancestry and the existing `LICENSE.md` (AGPLv3).
No new third-party dependency or licensing change was introduced.

## Semantic conflict resolutions

Nine files conflicted. Resolutions live in the integration change, not rewritten feature commits.
The post-upstream tree intentionally differs from the content-preserving cleanup checkpoint.

| File | Resolution |
|---|---|
| `Core/Game/InstanceSession.cs` | Keep local refusal checks, offline/native readiness, deferred barrier/retries, cleanup and measured player spawn. Add upstream RSV/RSF registration before the zone load after readiness admission; carry barrier rotation. |
| `Core/Game/ScenarioSession.cs` | Keep `Map.BeginPhase`, zone/phase client setup and environment ordering; add upstream result marks for all player seats. |
| `Core/UserActions/UserActions.cs` | Preserve real local input callbacks, sequence deduplication, pause/transition gates, Provoke and practice defenses, cancellation/recast restoration. Add upstream action presentation signals, including the local early-return action paths and resumed casts. |
| `Multiplayer/PacketRecorder.cs` | Combine upstream control-routing rules and despawn tracking with local retained raw spawned-status-source identities. |
| `Multiplayer/PacketReplay.cs` | Retain spawned actor identity mapping for statuses after despawn, alongside upstream per-target/status/source cache and removal/reset cleanup. Preserve null effect targets and new spawn appearance payloads. |
| `Scenarios/Fru/FruConstants.cs` | Preserve local P1/phase/music/weather constants and JP behavior; upstream's simpler declarations must not remove them. |
| `Scenarios/Fru/FruZone.cs` | Preserve verified local layered FRU arena setup, phases, native readiness and JP waymarks. |
| `Scenarios/IZone.cs` | Keep local display/client-setup/player-spawn contract; add optional upstream `DutyInitData`, translating its global barrier position to the existing local coordinate contract exactly once. |
| `tests/Support/MultiplayerRun.cs` | Keep configured practice God Mode across start, add upstream client configuration/input replay support. |

Supporting compatibility edits preserve `IPhase.RunClientSetup`/the `Phase` client-setup parameter,
and default barrier rotation to zero for existing callers. TOP/UWU/UCOB/UMAD use the new duty-init data
without bypassing local entry readiness. The new two-case test verifies localized text is installed
before deferred layout readiness in both offline and inn entry, with no early actors/scenario.

The safety harness now compiles the **production** `PacketRules.cs` because `NetGuard` depends on it;
its whitelist and stale-binary safety proof were not replaced by test stubs. Python source assertions
were updated to check the existing diagnostic-bearing pause/transition blocks and the current
interface-backed sequence/recast reset path. Their ordering checks remain enforced.

### Deliberate compatibility choices and native uncertainty

- Upstream FRU replay duty data is included, but automatic replacement of FluffySim's proven FRU
  setup by the simpler upstream FRU zone was not adopted. Local arena layers, readiness, phase
  setup, timings and JP defaults take precedence. This is a content resolution, not exclusion of
  the upstream ancestry.
- The temporary upstream debug-bot native LB execution was removed by upstream itself before its
  new master tip. It is absent from the integrated net change.
- Shared packet rules remain a bounded allowlist for local client/peer replay. No zone/firewall
  admission rule or game-server send path was loosened. Both ordinary and optimized TRACE Debug
  artifacts are checked by the stale-artifact guard.
- Multiplayer adds changed-only pose messages, owner velocity and jump presentation; velocity is
  bounded and pose extrapolation is capped. Upstream explicitly marks the native jump flags/air
  presentation **UNVERIFIED**. Headless checks cannot establish the actual animation behavior;
  this requires a client check before release. No automatic real-player movement is added.
- Updated peers need matching plugin builds; no relay server was deployed during this task.

## Validation

Wall-clock times include runner/build overhead. All artifacts were Debug, including the separately
isolated optimized TRACE build used for production-preprocessor safety checks. Extended/randomized
stress suites were not requested or run.

| Check | Result | Wall time |
|---|---|---:|
| Full solution final Debug build | 0 errors | 7.027 s |
| Ordinary direct-project Debug safety artifact | Success | 1.385 s |
| Isolated optimized TRACE Debug safety artifact | Success | 8.078 s |
| Final focused CT/Wrath/Provoke/lifecycle/upstream regressions | 595 passed | 107.995 s |
| Final delayed-entry cases + catalogue, one seed per strategy | 57 passed (2 entry + 55 catalogue) | 30.680 s |
| Extra Death/Darklit presentation, damage budget, mitigation, recast and Black Hole peer parity/reset | 58 passed | 11.129 s |
| Relay suite | 44 passed | 2.516 s |
| Packet safety, default backend, both fresh artifact variants | 313 passed, 3 Windows-only skips | 20.781 s |
| Packet safety, SafetyHook failure backend, both fresh artifact variants | 313 passed, 3 Windows-only skips | 20.231 s |
| Python tooling | 76 passed | 0.146 s |

The initial broader run covered 2,560 cases in 118.306 s: 2,556 passed, and four failed solely in a
new integration assertion that confused a native observed action relayed for presentation with an
accepted local aggro swap. Missing/wrong targets must be rejected for aggro while their observed
button action can still be shown to peers. That assertion was corrected without changing swap
validation. The broad passing coverage included the cold-entry matrix and 500 stream seeds.

A follow-up run passed 548/550 in 97.268 s; its two failures were the new deferred-entry test
asserting before the queued entry frame ran. The test now advances the existing deferred path;
both cases passed in the final 57-case run. Initial Python failures likewise reflected stale source
assertions against already committed QA code, corrected as described above.

## Native acceptance before any 3.0.2.0 preparation

1. CT D3/R1: repeat the known first-Wings fourth → second-Wings third ordering in both orientations
   and difficulties, confirm planned party mitigation/shields and survival without personal DPS
   defense; inspect rank/damage diagnostics. Keep anti-KB, cleanse and wrong-position failures.
2. Wrath: both Spiral Pierce bots and Skyward Leap bot move clockwise only after their attack
   snapshots, relative to rotated white-dragon North; verify Twister dodge and subsequent spreads
   through Altar Pyre/Liquid Heaven/Thunderstruck and the connecting tankbuster.
3. Wrath and Death: human ST targets Thordan and presses native Provoke during the valid window
   with no stance requirement; verify Heel MT, then all three Mights ST. Repeat human MT/bot ST,
   pause/restart, inn/offline, missing/wrong/late input and expired mitigation diagnostics. The
   fake input entry test cannot certify native hotbar interception.
4. Verify both native Enemy List Twisting Dive casts, actor readiness/untargetability, proper finish,
   restart/Leave cleanup; retain the previously accepted Darklit overhead visuals.
5. Cold start/restart/Leave each affected native arena (including TOP/UWU/UCOB/UMAD duty-init text,
   spawn wall and map reset). Check native FRU/DSR layer readiness stayed intact.
6. With matching multiplayer builds, verify action/cast/cancel/autoattack presentation, status
   sources after caster despawn, appearance, result marks, lag/timeout recovery, peer re-entry,
   walking/stopping and especially the upstream UNVERIFIED jump/landing flags. Confirm no duplicate
   self-effects or native castbars.

The integrated source is ready for these checks; native acceptance and release authorization remain
separate. No claim that pending human Provoke/CT/clockwise behavior is confirmed in-game is made here.
