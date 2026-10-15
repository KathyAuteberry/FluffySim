# FluffySim upstream / Evernow integration

This integration keeps FluffySim 1.2.0.0 and its existing configuration identity. It is a local integration, not a published release. Native behavior needs a fresh client playtest after the runtime changes below.

## Sources and graph

- AnoMech master: `1babdfbe3615cc1c68b66a403f0c0dd710ddd7c9`.
- Evernow PR [35](https://github.com/anomek/AnoMech/pull/35), offline experiment: `440eec337fed4747cbf40a962e9d610d32034d70`.
- Evernow PR [38](https://github.com/anomek/AnoMech/pull/38), safety tests: `aa96ab9d800bf1868d5fecf3eebccf7b1d66b31a`.
- Evernow PR [50](https://github.com/anomek/AnoMech/pull/50), consistency fixes: `4a1f8c2d93d5054a22d225a38c995d8aa1cb095d`.

Each PR contributes one distinct authored commit. PR 50 does not contain PR 38; their fixes overlap in intent and are reconciled in the local feature revisions. None of the three tips is an ancestor of another. The first feature (`ysutpo`) has four parents: upstream master and the three original PR tips. This makes the PR APIs available while adapting the feature stack. The prepared `plkqvn` anchor is empty above the integrated feature tip; redundant PR parents were removed from that anchor. The graph therefore retains a real four-parent merge at the feature base rather than storing code in the final anchor. Evernow attribution is retained in history. The 13 FluffySim feature changes remain decomposed above master. Historical operation inventories show 50 distinct conflicted paths across the initial rebase and four-parent merge (43 at the rebased stack tip, 15 at merge creation, with overlap); inherited conflicts are not summed per descendant. The user's `xznrxk` working-area change remains a descendant.

Encounter-specific adaptations were absorbed into their owning features where unambiguous; shared UI/Pause work remains in the corresponding features and native/offline/safety reconciliation is in the native-handling feature. The final integrated source tree was checked byte-for-byte through Jujutsu tree comparison against the pre-redistribution tree. The empty user working-area descendant is preserved.

The original base was `375f3e6eaa6d47ab2781c712809f503a34b5977d`. Upstream introduced a different session, movement and native packet architecture; simply resolving textual conflicts was insufficient.

## Recovery

Before rewriting, the operation was `5b8f1ae125df`; the original working-area commit was `ff0bfb469e5bbf601298278735687d47dba41e96`, and the original merge anchor was `bb887e7df7d293b7d2be1e11b22fa523b2b7c483`. The temporary local checkpoint bookmark was removed after validation, and the 15 obsolete original-stack revisions were abandoned at the user’s request. No divergent changes remain visible. The original graph remains recoverable through operation history; no history was pushed.

Inspect with `jj op log` or `jj --at-operation 5b8f1ae125df log`. To inspect the fully reconciled tree before history redistribution, use commit `c9fd0e036c93a6eca6ca5a1da2208fcc5a654974` (operation `2cf81f90d74c`). To recover the original tree without erasing later operations, first preserve current work, then create a new change from full commit `ff0bfb469e5bbf601298278735687d47dba41e96`. Recover abandoned revisions before repository garbage collection. Do not blindly restore the whole repository operation: concurrent user operations could be lost.

## Reconciliation decisions

- Adopt upstream `InstanceSession`, `ScenarioSession`, party providers and per-object motion, rather than retaining a second `Game` runtime. Keep FluffySim's nine scenarios, assignments, seeded overrides and shared training features.
- Adopt packet-handler NPC/doppel spawning, enemy HUD handling, typed action-effect targets and native status-list ownership. Do not run the old spawner or an orphan sweeper alongside it. A fake delayed spawn is canceled when its owner is freed, matching production packet cancellation.
- Retain the verified FRU/DSR phase presets, layer holds, waymarks, equipment and native presentation data. Unknown entrance-barrier IDs are not guessed; the original (0,0,16) local entry position is retained without spawning an unverified wall. Diamond Dust remains shelved and unregistered.
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

Current validation gates (Debug only):

| Gate | Result |
|---|---|
| Solution build | Passed, zero errors; CA1416 platform warnings suppressed for the Linux build. Remaining warnings: NU1900 vulnerability-feed access, one scenario nullable warning and one test nullable warning. |
| Ordinary + optimized Debug safety IL and production-linked lifecycle | 300 passed, three Windows-native hook cases skipped; repeated successfully with the SafetyHook failure model. |
| Relay | 44 passed. |
| Python tooling | 65 passed. |
| Full plugin/scenario suite | Final run in progress; result to be recorded before handoff. |

Graph checks: upstream master and all three exact PR tips are ancestors; all 13 local feature change IDs and the user's empty working-area descendant remain; no visible divergent revisions or unresolved conflicts remain. `plkqvn` has no diff. Before documentation updates, its tree matched `c9fd0e036c93a6eca6ca5a1da2208fcc5a654974` exactly. Documentation changes are also absorbed into the native integration feature. No Release outputs, installed files, remote refs or version numbers were modified.

## First manual validation

1. Build Release yourself only after reviewing the integration. Leave any current simulation before allowing reload. Never load both plugin registrations.
2. In an inn, confirm version/settings and the nine FRU/DSR entries; Diamond Dust stays hidden. Start Light Rampant, then Fall of Faith and Darklit; inspect native NPCs, equipment, orbs, towers and cone facing.
3. Check Easy/Hard, invisible bots and God Mode. Pause during movement, a vulnerability and a cast; resume without status cleansing or assignment changes.
4. In a filtered online simulation, use your normal defensive hotbar ability, restart, and repeat. Check filter counters and local cooldown. Leave, wait for the inn, and compare pre-entry cooldown/action state and normal gameplay. Never disable the filter to test it.
5. Separately test offline mode at title, preferably with a disposable local profile and a backup of character settings. Confirm no live session, all guards armed, ordinary scenarios/bots/audio, and explicit return. Verify source configuration files are unchanged. Do not provoke a live login as a safety test; a detected attempt intentionally closes the client.
