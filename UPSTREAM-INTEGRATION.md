# FluffySim upstream / Evernow integration

This integration keeps FluffySim 1.2.0.0 and its existing configuration identity. It is a local integration, not a published release. Native behavior needs a fresh client playtest after the runtime changes below.

## Sources and graph

- AnoMech master: `1babdfbe3615cc1c68b66a403f0c0dd710ddd7c9`.
- Evernow PR [35](https://github.com/anomek/AnoMech/pull/35), offline experiment: `440eec337fed4747cbf40a962e9d610d32034d70`.
- Evernow PR [38](https://github.com/anomek/AnoMech/pull/38), safety tests: `aa96ab9d800bf1868d5fecf3eebccf7b1d66b31a`.
- Evernow PR [50](https://github.com/anomek/AnoMech/pull/50), consistency fixes: `4a1f8c2d93d5054a22d225a38c995d8aa1cb095d`.

Each PR contributes one distinct authored commit. PR 50 does not contain PR 38; their fixes overlap in intent and are reconciled in the local feature revisions. None of the three tips is an ancestor of another. The first feature (`ysutpo`) has four parents: upstream master and the three original PR tips. This makes the PR APIs available while adapting the feature stack. The prepared `plkqvn` anchor records the runtime-fix tip and the three original PR heads as explicit parents, retaining the requested integration-merge identity. Those PRs are already ancestors through the first feature, so these parent edges do not apply their code twice. The anchor currently needs no additional code delta; functional corrections are in the local revisions before it. Evernow attribution is retained in history. The 13 FluffySim feature changes remain decomposed above master. Historical operation inventories show 50 distinct conflicted paths across the initial rebase and four-parent merge (43 at the rebased stack tip, 15 at merge creation, with overlap); inherited conflicts are not summed per descendant. The user's `xznrxk` working-area change remains a descendant.

Encounter-specific adaptations were absorbed into their owning features where unambiguous; shared UI/Pause work remains in the corresponding features and native/offline/safety reconciliation is in the native-handling feature. The final integrated source tree was checked byte-for-byte through Jujutsu tree comparison against the pre-redistribution tree. The empty user working-area descendant is preserved.

The original base was `375f3e6eaa6d47ab2781c712809f503a34b5977d`. Upstream introduced a different session, movement and native packet architecture; simply resolving textual conflicts was insufficient.

## Recovery

Before rewriting, the operation was `5b8f1ae125df`; the original working-area commit was `ff0bfb469e5bbf601298278735687d47dba41e96`, and the original merge anchor was `bb887e7df7d293b7d2be1e11b22fa523b2b7c483`. The temporary local checkpoint bookmark was removed after validation, and the 15 obsolete original-stack revisions were abandoned at the user’s request. No divergent changes remain visible. The original graph remains recoverable through operation history; no history was pushed.

Inspect with `jj op log` or `jj --at-operation 5b8f1ae125df log`. To inspect the fully reconciled tree before history redistribution, use commit `c9fd0e036c93a6eca6ca5a1da2208fcc5a654974` (operation `2cf81f90d74c`). To recover the original tree without erasing later operations, first preserve current work, then create a new change from full commit `ff0bfb469e5bbf601298278735687d47dba41e96`. Recover abandoned revisions before repository garbage collection. Do not blindly restore the whole repository operation: concurrent user operations could be lost.

## Reconciliation decisions

- Adopt upstream `InstanceSession`, `ScenarioSession`, party providers and per-object motion, rather than retaining a second `Game` runtime. Keep FluffySim's nine scenarios, assignments, seeded overrides and shared training features.
- Adopt packet-handler NPC/doppel spawning, enemy HUD handling, typed action-effect targets and native status-list ownership. Do not run the old spawner or an orphan sweeper alongside it. A fake delayed spawn is canceled when its owner is freed, matching production packet cancellation.
- Retain the verified FRU/DSR phase presets, layer holds, waymarks, equipment and native presentation data. The initial zero-ID entrance placeholder was incorrect and is superseded by the recorded bindings in the runtime-repair section below. The validated (0,0,16) player entry remains separate from the native wall position. Diamond Dust remains shelved and unregistered.
- Light Rampant has no competing upstream encounter implementation. Preserve Mana mechanics; adapt its scripted pushes and native effect delivery to upstream APIs. Its final puddle waypoint shifts two degrees to provide clearance for the upstream human movement driver's one-frame arrival margin. Puddle geometry, timings and damage remain unchanged. Headless waypoint assertions allow one input frame for the player only; bot precision and mechanic collisions remain checked.
- Preserve owned persistent VFX behind the native interface for encounter effects that need explicit lifetime management. This does not restore the obsolete enemy spawning implementation.
- Preserve Pause's authoritative statuses, virtual defenses and input ownership. Add frozen poses because upstream player position reads the live native model. Native animations may continue, as previously accepted. Resume refreshes owned status presentation; no server status packets are sent.
- UI extension helpers now delegate to FluffySim display labels while retaining legacy full names for saved selection. FluffySim branding, assembly name, version source and legacy configuration import remain intact.

## Firewall and local actions

The production implementation is `Core/Native/Implementations/ZoneControl` and its Guard/Practice partials. The send detour drops every non-heartbeat or unreadable packet; it has no deferred packet queue. Gameplay filtering is armed and verified before local zone mutation. Keepalive allowance is not an action whitelist.

PR 50's whole-session snapshot of 80 recast groups and both action sequence counters is authoritative on exit. FluffySim's targeted defensive reset/pause tracking operates inside that session; it restores first, then the full snapshot reconciles to pre-entry state with real elapsed time. Upstream's automatic all-recast clear is deliberately omitted: normal practice resets only the applicable defensive actions, preserving FluffySim's scoped-reset contract. The fresh-pull job-gauge reset still uses upstream machinery inside a verified session. The all-recast diagnostic is guarded at the native method itself and is never invoked automatically. These are client-local practice changes, not server cooldown resets. The full snapshot prevents unrelated pre-entry cooldowns from being lost. Actual server synchronization still needs in-client observation.

Leaving blocks new inputs, clears queued/ground-target actions and local auto-attack state, reloads the inn, restores position, waits for verified native territory/position/job/action conditions, restores recasts/sequences, then lowers filtering. Social-party resynchronization happens only afterward. This addresses the premature-lift path described in Hyperborea issue 34. Unloading while still inside a virtual zone deliberately terminates the client; there are no remaining frames to prove asynchronous restoration. Leave and wait for the inn before unloading/rebuilding.

This is defense in depth, not a guarantee of account safety. Changed native signatures/opcodes, third-party hooks, unobserved transports and native/Wine behavior remain outside headless proof. A safety stop refuses normal teardown; it cannot certify that no packet escaped before a fault.

## Offline mode

Evernow's imported offline implementation creates a local inn from a selected local character/gear/appearance and optionally imports hotbars/HUD/keybinds into memory. It does not request a server login. Source character files are read; offline choices and diagnostics are saved separately in the plugin directory.

Network, connection and file guards must arm before state changes. Added checks verify hook-enabled state, recheck disconnection before configuration import, stop on lost guards each frame, and block reviewed game connection imports during startup as well as while tainted. Failed termination falls back to `Environment.FailFast`. Login-triggered shutdown and firewall trip/refused-lift stops cannot be bypassed by a diagnostic exception; guard checks run before informational logging. The installed Dalamud getter was checked: login detection reads AgentLobby.IsLoggedIn rather than the existence of the offline player. Offline checks run independently before scenario updates.

The file guard blocks reviewed new write-capable opens and path mutations. Existing writable handles/memory mappings and unrelated direct system calls are not covered. The existing logout/file-module settle checks reduce exposure but are not proof that every possible writer is absent. The connection guard covers the reviewed game imports, not all networking by every plugin. Offline mode is experimental pending actual Windows/Wine validation; avoid plugins that automate login or edit character settings while testing.

The nine FluffySim scenarios share the same runtime after entering the offline inn. Headless registration/scenario tests do not prove the native title-screen bootstrap, layout readiness, imported equipment, audio or exit behavior. Those need manual validation. Do not describe offline native launch as verified yet.

With autoload disabled, no title-screen entry exists until Dalamud loads FluffySim. Load it manually if your Dalamud UI is available there, or load normally, log out to title and wait at least ten seconds. Do not change autoload preferences silently. Select **FluffySim offline mode**, choose a local character/loadout, review the other-plugin warning, and enter the offline inn. Return using its explicit offline controls; unexpected login attempts or unloading a tainted session may close the client intentionally.

## Validation

The imported 292-case suite was adapted to the cached NUnit runner and installed Dalamud Mono.Cecil, preserving its assertions and test data without downloading new packages. It source-links the actual ZoneControl/MapController/filter/recast code into a fake client/server; IL rules inspect both ordinary and optimized Debug plugin artifacts. The obsolete single-Game call-chain proofs were adapted to the new production session architecture. Older tests accepting post-load packet exposure on unload now require a fatal stop instead. Additional tests cover full recast/counter restoration, queued inputs and offline guard ordering.

Linux cannot execute the three Windows-native hook patching tests; these must run on Windows. Metadata contracts and simulated hook-failure models still run. Mutation tools inherited from PR 38 target older API spellings and have not been revalidated as an integration gate; do not assume their edits still apply. No live game actions are used in automated tests.

Pre-runtime-repair validation gates (Debug only):

| Gate | Result |
|---|---|
| Solution build | Passed, zero errors; CA1416 platform warnings suppressed for the Linux build. Remaining warnings: NU1900 vulnerability-feed access, one scenario nullable warning and one test nullable warning. |
| Ordinary + optimized Debug safety IL and production-linked lifecycle | 300 passed, three Windows-native hook cases skipped; repeated successfully with the SafetyHook failure model. |
| Relay | 44 passed. |
| Python tooling | 65 passed. |
| Full plugin/scenario suite before runtime repairs | 13,036 passed. This did not establish native entry correctness; the user then identified the regressions below. |

Graph checks: upstream master and all three exact PR tips are ancestors; all 13 local feature change IDs and the user's empty working-area descendant remain; no visible divergent revisions or unresolved conflicts remain. `plkqvn` has no diff. Before documentation updates, its tree matched `c9fd0e036c93a6eca6ca5a1da2208fcc5a654974` exactly. Documentation changes are also absorbed into the native integration feature. No Release outputs, installed files, remote refs or version numbers were modified.

## First manual validation

1. Build Release yourself only after reviewing the integration. Leave any current simulation before allowing reload. Never load both plugin registrations.
2. In an inn, confirm version/settings and the nine FRU/DSR entries; Diamond Dust stays hidden. Start Light Rampant, then Fall of Faith and Darklit; inspect native NPCs, equipment, orbs, towers and cone facing.
3. Check Easy/Hard, invisible bots and God Mode. Pause during movement, a vulnerability and a cast; resume without status cleansing or assignment changes.
4. In a filtered online simulation, use your normal defensive hotbar ability, restart, and repeat. Check filter counters and local cooldown. Leave, wait for the inn, and compare pre-entry cooldown/action state and normal gameplay. Never disable the filter to test it.
5. Separately test offline mode at title, preferably with a disposable local profile and a backup of character settings. Confirm no live session, all guards armed, ordinary scenarios/bots/audio, and explicit return. Verify source configuration files are unchanged. Do not provoke a live login as a safety test; a detected attempt intentionally closes the client.


## Native entry repairs after the first client test

The user confirmed three actual regressions despite the preceding 13,036 passing headless tests. They were not attributed to stale installation.

**Duty entrance:** FRU and DSR were reconciled with zero EObj/layout/gimmick identifiers. That skipped `InstanceSession.OpenEntranceBarrier`, leaving the layout's native entrance wall without its controlling EObj or open event. Read-only decoding of the installed `SpawnObjectPacket` layout and the native recordings established:

| Territory | Entrance EObj | Placement LayoutId | Wall GimmickId | World position |
|---|---|---|---|---|
| FRU 1238 | 2007457 | 0xA58C08 | 0xA58C07 | (100,0,115) |
| DSR 968 | 2007457 | 0x875BE2 | 0x875BE0 | (100,0,118) |

Sources are `Futures Rewritten (Ultimate) 2026.10.14 23.01.42.dat` and `Dragonsong's Reprise (Ultimate) 2026.10.11 00.16.23.dat`. Both recordings contain entrance-targeted ActorControl category 0x6A, argument 7. The existing upstream EObj spawn/open mechanism now uses those exact bindings. It is not a broad collision disable. The existing two-second attachment window begins after layout readiness and successful EObj creation, rather than at the initial territory-load request. The wall remains owned by the instance across Stop/Restart and is despawned on Leave. The human spawn remains local (0,0,16); a new defaulted `IZone.PlayerSpawn` separates that practice position from the wall's authentic position. This avoids changing validated travel/tether timing.

**Offline arena and actor race:** `EnterAndStart` previously issued party packets immediately after requesting native territory loading, then used a fixed two-second callback for phase/scenario start. The user's native logs show offline DSR at layout init 6 while party packets were already issued, reaching init 7 later. That can lose character/HUD initialization or apply environment state before native layout finalization. Entry now polls the existing native territory/layout readiness predicate (matching territory, territory load 2, layout init 7), creates and opens the entrance, then seats the party and starts the common phase/scenario pipeline. No server login or duty-ready request is added. Failed EObj creation retries without spawning combatants; a preparation timeout reports failure while the isolated instance remains available to Leave. Stop/Leave cancel a waiting start, and the main controls display the preparation state with Stop/Leave still available.

Online and offline entry use the same pipeline. FRU's existing P1/P2/P3/P4 presets and layer holds are retained. DSR P2 explicitly applies the nine resting MapEffect states of 4 from its accepted P2 replay snapshot. `MapController.BeginPhase` clears obsolete queued map/director work and previous phase holds before the new phase's setup, so P1 holds cannot suppress later FRU scenery after a same-territory switch.

**Offline party presentation:** party packets no longer precede native layout completion. `PartyHud.Refresh` also compares the actual native group with the expected actor roster, rather than trusting only its last-send cache; lost native membership is repaired through the existing local UpdateParty handler. Entries use GameMain's current native territory instead of Dalamud's potentially stale title-screen territory. Visibility preferences still apply only to simulated party models; roster/status presentation remains populated in invisible mode. There is no alternate actor-spawning implementation or server-side party creation.

Diagnostics now report native readiness transitions, party roster/native count and `_PartyList` visibility, plus each bot's requested visibility/draw state after spawning. These are observations, not proof of native rendering. If the native addon remains absent, the diagnostic distinguishes missing UI from a missing native roster; no unverified forced-addon creation was added.

**Regression scope:** the new production-entry matrix covers all nine custom scenarios × online/offline context × Easy/Hard × visible/invisible × eight seats (576 cases). It asserts no early party or phase mutation, frozen pre-start event time, the closed-to-open entrance transition, actual simulated actor/roster population, phase-specific map/hold state and cleanup. Additional cases cover original scenarios, failed barrier creation, cancellation, phase switching/restart and native group-cache invalidation. Fakes now model asynchronous doppel creation and model visibility; native collision and rendering still require client confirmation. The safety suite additionally checks the production party-HUD wiring and native-territory source.

Functional repairs live in `oqwrrs` before `plkqvn`; no fix commits are appended above the merge. The pre-runtime-repair graph is inspectable at operation `aa82f11f5d33` (merge commit `15d1ac`). The temporary runtime-checkpoint bookmark is removed at handoff to avoid retaining obsolete divergent revisions; operation history remains the recovery mechanism. Post-repair validation:

- Complete plugin/scenario suite: **13,625 passed**, zero failures (optimized **Debug**, unchanged 40-seed catalog settings, 5m02s). The ordinary Debug baseline before these repairs had 13,036 passes; that baseline alone missed the native regressions.
- Focused ordinary Debug entry/positioning checks: **599 passed** (overlap the full suite).
- Safety: **301 passed, three Windows-native cases skipped**, repeated successfully with both modeled hook backends, inspecting ordinary and optimized Debug native artifacts. Imported production-linked firewall/fuzz/restore checks remain enabled.
- Relay: **44 passed**. Python tooling: **65 passed**.
- Debug builds: zero errors. Remaining warnings are the unreachable NuGet audit feed and existing nullable warnings; Linux CA1416 platform warnings were suppressed for these local build commands.
- No Release build, installation, reload, publication or version change was performed. Native barriers/collision, offline scenery and party rendering remain pending the manual matrix below.

### Two-mode native test matrix

Leave any current simulation before rebuilding/reloading. Build Release personally only when ready; development touched Debug outputs only.

| Check | Inn entry | Title-screen offline entry |
|---|---|---|
| Original scenario control | Visible initial wall opens; movement resumes | Same, if that original scenario supports the imported local character |
| FRU Fall of Faith | P1 floor; seven bots and eight party frames; entrance opens | Same; no mixed-phase scenery |
| FRU Light Rampant | P2 floor/ice, native orbs and towers | Same P2 environment |
| FRU Ultimate Relativity or Apocalypse | P3 floor, Oracle and mechanic cues | Same P3 environment |
| FRU Darklit | P4 floor, both bosses, Fragment and towers | Same P4 environment |
| Any DSR Strength/Sanctity fragment | Established P2 Thordan arena | P2 environment, not the P1 scene |

For each representative entry, walk away from the spawn circle, Stop and Restart, then Leave. Repeat one case with hidden bots and Hard: models disappear, party frames remain and mechanics continue. Check Pause/Resume and a defensive hotbar action in one active run. Offline: exit the scenario to the virtual inn before using the explicit return-to-title control. Do not unload while in an isolated or offline session; a deliberate safety stop may close the client. Native wall visibility/collision, cold-load phase scenery, bot equipment and party-addon appearance are pending user validation.

## Remaining offline regressions after the second client test

The user confirmed the entrance/collision repair, movement in both modes, online arenas and
FRU's offline phase presets. Those paths are retained. The following changes address the
remaining failures; their native rendering results still require another client test.

### DSR P2 scenery

The BDEAD2 client logs distinguish a fully ready layout from the wrong phase: both modes
reach territory 968/load 2/layout init 7 and accept all nine resting MapEffects, but offline
retains P1 layers `EE3C`, `EE3F`, `EE40`, `F2DC`, `F9D`, `139F`, `FF52` and leaves P2 layers
`EE79`, `EE7A`, `160C` inactive. The working inn logs and the accepted P2 replay snapshot
agree on their opposite states. MapEffect slots B47/B48 control separate shared groups;
setting their state to 4 does not switch this background scenery.

The initial layer-hold repair was superseded after the user's flicker report. Offline
ServerWeather retained current 2 while next 45 was queued; continuous P2 holds competed
with native phase selection. DSR now commits the existing native pending weather once per
ready layout generation and leaves scenery ownership to the native environment selector.
See [OFFLINE-DSR-REPAIR.md](OFFLINE-DSR-REPAIR.md) for snapshot/runtime evidence, native
method verification, identity fixes and remaining client checks. FRU holds are unchanged.

### Offline party models and party HUD

The same seven doppels have visible draw objects and RenderFlags=0 in inn logs, but hidden
draw objects and RenderFlags=`0x40000000` offline, despite Show Simulated Party being true.
Their race, skeleton, scale and appearance are populated. This is **not** a missing spawn or
permanently missing draw object. Intentionally invisible bots additionally have the existing
Model/Nameplate mask (`0x802`); their draw objects can be unloaded by the client.

Read-only inspection of the installed executable traces bit 30 to the native character
RepresentationContainer visibility update (function at image VA `1408D6E50` in this binary).
The non-local PC branch can set render bit 30 via the native render-bit setter; the local
player/non-PC branches clear it. The corresponding native clear path at `14089B730` removes
only the requested bit and restores draw visibility once no hide reasons remain. The SDK
names only Model/Nameplate in VisibilityFlags, so the new internal name
`RepresentationSuppressed` documents this inspected bit rather than pretending it is an
SDK enum value. Exact upstream bootstrap/global representation-context causes remain a
native integration uncertainty; no login/global visibility state is fabricated to fix it.

The shared native party-model setter now reconciles this suppression **only** while offline
and only for a PC whose reserved slot and synthetic doppel entity ID match. It retains all
other hide reasons and obeys the existing visibility preference, restoring only the loaded draw
object's requested-draw bit, exactly as the inspected native clear path does. Native culling
and asynchronous model loading retain their own bits; no draw object is reallocated. Missing draw objects remain
under the normal asynchronous spawn/load lifecycle and are reconciled when they arrive.
Presentation reconciliation also runs during pause without ticking mechanics or statuses.
Neither the human, enemies, unrelated players, online presentation nor packet filtering is
changed. Tethers/markers remain governed by the existing presentation policy.

Native-structure tests exercise this actual production reconciliation with loaded/hidden
and delayed draw objects, repeated suppression, both visibility settings, foreign slots,
non-PCs and unrelated hide flags. They do not execute FFXIV's renderer.

All six relevant offline BDEAD2 logs already report `requested=8, native=8, party frames=visible`
after the earlier roster fix. There is no evidence of a remaining shared party-HUD failure
in these captures. The existing UpdateParty local-handler path and native-roster repair are
preserved and regression-tested independently; correct rows/status icons still need visual
confirmation. New bounded actor diagnostics include slot, entity, ready-to-draw state,
representation type, native party flag and offline state alongside draw/skeleton/render flags.
Existing map diagnostics report phase territory/layout readiness and layer counts.

### Meteors clock regression

Comparison against pre-merge `bbd8c479` confirms its `ResetInternal` always cleared Events.
After integration, `EndRun` clears them only through an existing ScenarioSession. Time can
therefore accumulate in a newly entered/stopped instance with **no** active run. On its first
start, relative event offsets began at that accumulated time while Meteor tower expiration,
Hard departure deadlines and ScenarioSession.Elapsed still assumed zero. This explains
short/missing tower windows and disagreement between hazards and movement without changing
FFXIV's frame delta or inventing longer tower durations.

Every ScenarioSession now explicitly clears its run scheduler before phase/scenario setup.
The existing first/second tower snapshot times remain 12.4/25.8 seconds (release 12.7/26.1),
Fire presentation 5.5 and Ice/Storm 8.5. Pause still suspends that same scheduler; native
cosmetic timers may continue as previously documented. The local tower diagnostic displays
the authoritative simulation clock. New regressions wait 8/45 seconds before first start,
compare complete Easy/Hard clears, freeze a visible tower window across a 20-second pause,
and exercise idle/Stop/restart in both entry contexts. No assignment, attack geometry,
choreography, provisional impact interval or tower renderer was replaced.

### History and manual validation

The recovery checkpoint is operation `2acfe2457244`, with working revision `xznrxk/fb3947`
and merge `plkqvn/956c05` before these edits. Inspect with
`jj --at-operation 2acfe2457244 log`; review later/concurrent work before any restore.
Functional changes are in `soxsns`, inserted after `oqwrrs` and before `plkqvn`. The existing
four merge parents and original Evernow commits remain represented. Version remains 1.2.0.0.

After the user builds Release: test an original scenario offline, FRU P4 offline, and DSR P2
offline; verify seven equipped bots, eight party entries and the correct floor/scenery.
Toggle Show Simulated Party, pause for several seconds, resume, Stop/restart and Leave.
Repeat representative inn runs. For Meteors, wait at least 15 seconds in the loaded instance
before Start; observe towers from ~0.7–12.4 and ~15.1–25.8 seconds, Fire before Ice, pause
with towers visible, and complete both Easy and Hard with the correct human assignment.
Keep tower diagnostics open if a discrepancy remains. Do not unload the plugin inside an
isolated/offline session; use Leave and the explicit offline exit first.

### Final Debug validation for the remaining offline repairs

- Root `dotnet build -c Debug`: passed, zero errors; existing NU1900 feed warning and two
  nullable warnings remain. Output is isolated from Release.
- Complete plugin suite: **13,647 passed**, including scenario/assignment exhaustives,
  original/custom entry paths, all nine custom scenarios and the new clock/model/layout cases.
  The 611-case focused matrix is an overlapping subset, not an additional total; the final
  complete run also includes the added native-culling preservation case.
- Relay: **44 passed**. Python tooling: **65 passed**.
- Safety: **301 passed / three Windows-native skips** on each modeled backend, against final
  ordinary and optimized Debug native artifacts. These repeated counts are not additive.
- Native model/culling, cold DSR scenery, visible party rows and wall-clock tower durations
  remain subject to the manual client checks above. No Release/installed output was written,
  no version was changed, and no packet-filter/action/recast production code was modified.
- The merge remains empty with four parents, all three accepted PR heads and upstream master
  are ancestors, the working area is empty above it, and no conflicts/divergent changes remain.

## Final offline follow-up

The local functional change before the four-parent merge replaces DSR's competing holds
with bounded weather finalization, initializes configured world metadata and local display
name caches, and adds change-only diagnostics. The offline lobby remains disconnected;
Discord Chat Bridge requires an invalid-HomeWorld guard and should be disabled manually
for offline testing until updated. No installed external plugin is modified. Details and
proposed unpublished upstream patch: [OFFLINE-DSR-REPAIR.md](OFFLINE-DSR-REPAIR.md).

### Offline follow-up from the user's 3308CD trace

The actual trace shows server weather 45 but rendered weather 2, and correct eight-person
actor/group names despite broken HUD text. The next pre-merge repair uses the complete
native weather-manager transition, read-only recorded object-state verification, consistent
reserved IDs in offline spawn/roster data, and a typed offline party-HUD publication bridge.
No phase holds, raw environment flags, real identities, network traffic or third-party DLL
patches are introduced. The trace UI shows its exact active path and bounded before/after
HUD tables. See OFFLINE-DSR-REPAIR.md; fresh native validation remains required.

## 2.0.1 integration

`mptttx` builds on published 2.0.0.0 and merges AnoMech `d576199a` (PR #56),
including its original Evernow multiplayer ancestry. The original offline, safety-test and
consistency tips remain ancestors through 2.0.0.0; redundant direct parent edges were removed
with `jj simplify-parents -r mptttx`. No upstream commits were rebased or copied. The test-fix branch and `codex/dsr-strength-dash-telegraphs` feed this merge.
Only unpublished integration history was changed. The published 2.0.0.0 tag is preserved.

The new host/peer packet recorder and native wrappers retain FluffySim's local visibility,
VFX, arena-readiness and practice-recast interfaces. Multiplayer's prepare-only stage waits
for native layout and entrance readiness before seating the party, without starting mechanics.
Packet application, including buffered entry packets, goes through `ApplyGamePackets` and
its `PeerInRun` guard. Architecture checks verify this caller boundary and the forwarding
zone wrappers; the fake transport still exercises the production filter lifecycle.

The new upstream layout IDs and action/job validation are included. Native multiplayer,
normal/offline arena presentation and the late Strength Part 1 dash telegraphs still need
in-client verification. Headless success does not establish native rendering correctness.

### Strength Part 1 dash telegraph timing

The preserved `Dragonsong's Reprise (Ultimate) 2024.09.26 00.42.01.zip` archive contains
63 and 60 Spiral Thrust casts across two native recordings (game build 10710093).
All ActorCast packets for action 25556 encode omen-delay byte 50, or 5.0 seconds,
and cast length 5.700 seconds. One representative cast starts at replay 101.819s,
encodes omen onset at 106.819s, ends its bar at 107.519s and releases at 107.836s.
This establishes the packet timing; visible fade/brightness onset still needs video comparison.
The October 10 combat log independently confirms approximately 5.974s from cast to effect,
but does not contain the omen-delay byte. Only the omen delay changes; hit resolution and
movement timing remain unchanged. Original recordings stay local and are not bundled.
