# Packet filter and native defensive practice audit — 2026-10-15

## Current upstream integration — 2026-10-15

See [integration decisions, exact source commits, recovery checkpoint and manual checks](UPSTREAM-INTEGRATION.md). The runtime now uses upstream InstanceSession/ScenarioSession, packet-handler spawning and per-object motion, combined with Evernow PRs 35, 38 and 50. FluffySim version remains 1.2.0.0. Historical implementation notes below describe earlier architectures; the integration document supersedes their old ZoneSession paths and teardown assumptions. Debug builds only; no installed Release output was changed.


## Verdict and confidence

FluffySim really contains Hyperborea-derived **outgoing and incoming** hooks. Ordinary hotbar
abilities already called the native action system before this change. The outgoing detour permits
only the signature-detected heartbeat; all other requests reaching that function are dropped,
without retaining their bytes. Native client recast changes are not server cooldown changes.

The current source, the established action simulation, and the owner's two independent observations
support scoped native defensive practice. Implemented: local recast snapshots/reset/restoration,
unconditional defensive effect resolution, exit hardening and main-UI button removal when the
native-practice gate is ready. This is a bounded engineering decision, **not an account-safety
guarantee or proof about every undisassembled game transport path**. The new native integration
still requires the manual checks below. No automated test sends a live ability to Square Enix.

## References and evidence categories

- [Hyperborea README](https://github.com/kawaii/Hyperborea): documents keepalive-only communication
  while movement, chat and actions are filtered.
- [Hyperborea Memory.cs](https://github.com/kawaii/Hyperborea/blob/main/Hyperborea/Memory.cs):
  provenance of the existing send signature; upstream documentation is not a substitute for auditing
  FluffySim's installed code.
- [Hyperborea issue 34](https://github.com/kawaii/Hyperborea/issues/34): reports an exit that turns
  filtering off before the final zone-load packet. Normal FluffySim Leave already delays release;
  its unload exception to native-territory readiness remained relevant and is corrected here.
- [AnoMech](https://github.com/anomek/AnoMech): upstream attribution and virtual-zone architecture.
- [FFXIVClientStructs ActionManager](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/ActionManager.cs):
  documents native action execution/queueing, recast groups and client-side recast records. The build
  uses the installed Dalamud SDK, not a newly downloaded native library or guessed offsets.
- Existing installed ActionTransient evidence in DSR-ROADMAP.md establishes the six-second
  Arm's Length/Surecast protection. Existing JobActions models the four tank defenses for ten seconds.

**Owner observation A:** simulated Holmgang starts a local cooldown, then zoning after leaving
clears it. **Observation B:** IINACT/Cactbot announces Holmgang outside the simulator but not inside.
Both are meaningful support for an intercepted request and absent server response. Neither is a
wire capture proving that no outbound bytes left through any possible path. A logging tool may
observe a different interception point; absence of its announcement alone cannot establish that or exclude a rejected server request.
The new counters distinguish a request reaching the filter, permitted heartbeat traffic, and blocked
non-heartbeat requests. They do not claim a permitted request was actually transmitted or acknowledged.

## Actual call paths

Paths below are relative to this repository.

### Entry, runtime, Pause and restart

1. `Core/Game/Game.cs:RunScenarioInternal` → `Core/Map/MapController.cs:TryLoad/Load` →
   `Core/Native/Implementations/ZoneSession.cs:Enter`.
2. `StartBlockedReason` in `ZoneSession.Guard.cs` requires an idle inn player and settled activity;
   it now also refuses a queued action or active ground targeting before entry.
3. `EnableFirewall` enables send then receive. `FirewallArmed` verifies both hooks are enabled,
   not disposed, and the detected heartbeat is nonzero **before** loading any simulated territory.
   Partial enable aborts without a zone load; failed removal of a half-armed filter stops the client.
4. With both filters up, `PracticeRecastSession.Capture` reads the small defensive/Sprint recast set.
   Then `LoadZoneInternal` initializes the local duty/layout/sync state. `ArmGuard` owns the stay.
5. `Plugin.OnFrameworkUpdate` calls `ZoneSession.TickGuard` **before** `Game.Tick` and independently
   of it. Pause therefore does not pause the network guard. It does not alter filter ownership.
6. Restart and same-territory scenario switches reuse the armed zone. The once-per-session recast
   baseline is retained; `UserActions.OnScenarioStart` resets only the relevant practice actions.
   Switching territories requires leaving the current virtual zone through the existing UI lifecycle.

The hook constructors do not enable packet interception merely by loading the plugin. Hook-install
failure prevents successful construction/start rather than providing an unprotected practice mode.
`VerifyActionIsolation` now checks both live hooks immediately before the hooked native direct and
location action calls if a session or its restoration is armed. A failed check stops the client.
The frame guard also treats hook loss, logout, real zoning or unexpected territory drift as fatal.
A different plugin disabling/replacing a hook concurrently still cannot be ruled out atomically.

### Outgoing and incoming traffic

`ZoneSession.SendPacketDetour` reads the request opcode at the existing signature target.
`Core/Native/Policies/PacketFilterPolicy.cs` permits **only** the nonzero detected heartbeat.
All other opcodes, including unknown/unclassified opcodes, are blocked before `sendPacketHook.Original`.
This includes action, movement, chat and other gameplay requests traversing that send routine. There
is no gameplay allowlist and no special pass-through for tank actions. Null input and managed errors
return the same drop result. Counters retain integers/opcodes, never packet payloads or identifiers.

The drop returns the inherited success-like byte value `1`; no plugin retry/deferred packet queue
exists. The blocked bytes are not replayed at exit. The native client's independent action queue is
a separate concern, addressed in restoration below. Traffic already sent before arming cannot be
retracted. The existing start gate rejects unsettled activities to reduce that entry risk.

`ReceivePacketDetour` hooks `PacketDispatcher.OnReceivePacket`. Release always uses safe mode;
Debug honors the existing SafeMode option. In safe mode, only configured `ZoneDownOpcodes` pass to the
original dispatcher. `Interop/OpcodeUpdater.cs` refreshes that allowlist from Hyperborea's per-game-
version file; this is an inbound keepalive allowlist, **not an outgoing action exception**. Existing
local incoming-style action/ActorControl rendering calls enter client handlers directly; they do
not invoke the outbound send routine. This audit does not certify the semantic identity of every
configurable inbound allowlist entry or add a new packet injector.

`/fluffysim firewall` prints actual hook/session/restoration state, the native-practice gate and
lifetime intercepted/heartbeat-allowed/non-heartbeat-blocked/unreadable counters. Existing diagnostic
stay summaries also count blocked inbound traffic and outbound opcodes. Unknown gameplay is never
silently allowed. Counters are not action-specific opcode decoding or acknowledgements.

### Normal hotbar actions and simulation effects

`LocalPlayerInputHooks.UseActionDetour/UseActionLocationDetour` → verified isolation check → native
`ActionManager.UseAction/UseActionLocation` original → `ActionExecuted` →
`UserActions.OnActionExecuted`. The native action system checks availability and starts its local
recast/animation; normal FFXIV would send a request and await server validation/effects. Here the
existing outgoing filter drops that request. This change adds no automatic use of any ability.

`SimActive` requires an active simulated map and native zone. Sequence deduplication resolves an
executed action once, not every queued press. Scenario start now seeds that deduplication from the
current native sequence so even the first queued press cannot grant protection prematurely. Pause rejects action calls before the native original.
`PracticeDefensiveActions.Apply` now resolves the following private statuses **before** the optional
full combat-module gate, so enabling that beta module is unnecessary for defensive practice:

| Action | ID | Simulation status | Duration |
|---|---:|---:|---:|
| Hallowed Ground | 30 | 82 | 10 s |
| Holmgang | 43 | 409 | 10 s |
| Living Dead | 3638 | 810 | 10 s |
| Superbolide | 16152 | 1836 | 10 s |
| Arm's Length | 7548 | 1209 | 6 s |
| Surecast | 7559 | 160 | 6 s |

These reuse `SimCharacter.AddStatus` / `SimStatus` and the existing local StatusManager presentation;
no status is sent to the server. The modeled tankbusters already recognize these statuses, and
Meteor knockback already recognizes both anti-knockback statuses. Missing/expired protection still
fails. Tank-defense secondary effects such as Living Dead's healing lifecycle or Superbolide's
HP cost remain the existing simplified practice model, not a complete job-mechanic implementation.

The inherited stun path may call native GeneralAction 1 to cancel auto-attacks while the filter is
up. Pause and the new restoration input lock exclude that call. No new automatic combat-action
invocation was introduced. The beta action module has broader existing gauge/effect behavior outside
this focused defensive change; this document is not a certification of all job handlers.

### Recast ownership and preservation

The former anti-knockback reset wrote native `RecastDetail.IsActive=false` and `Elapsed=0` at start
without a matching exit snapshot. It was removed because that ownership and exit handling were
unverified; it did not literally reset a server timer. Sprint's legacy reset remained. This change
replaces that unowned Sprint reset and adds the six defensive actions through one owned facility.

`PracticeRecastSession` is a testable managed owner; `NativePracticeRecastStore` is the narrow native
adapter using installed `GetRecastGroup/GetRecastGroupDetail` and typed `RecastDetail` fields. It does
not use numeric memory offsets, invoke an ability, send a request or call a server cooldown API.
Only Sprint and these six actions are eligible. Scenario start resets Sprint as before; Darklit/
Apocalypse additionally reset the actual job's tank defense; Meteors resets the shared anti-knockback
group. Actual job identity, not the selected simulated seat, chooses the tank ability. No unlock or
level check is bypassed by clearing a recast. No unrelated action timer is deliberately reset.

Installed cached Action sheet confirms the four tank defenses share CooldownGroup 25, Arm's Length/
Surecast share group 49 and Sprint uses group 56 (sheet numbering; native lookup supplies its own
index). They have no additional cooldown group. The owner deduplicates native group indices and
preserves original ActionId, IsActive, Elapsed and Total. An unrelated action occupying a shared
group prevents its reset. Native ability use is tracked even when no reset was requested, so its
changed cooldown is still restored on exit. Only touched groups are written back.

The baseline is captured once before local level synchronization and reused on restart. At verified
inn restoration, while filtering is still engaged, original active timers are advanced by elapsed
**real** time and capped at their original total. Inactive timers stay inactive. Thus a real cooldown
already running before entry is not made ready outside the simulator; server time continues during
practice/Pause. Restoration cannot reset the actual server cooldown. Native recast bars themselves
continue visually on the native clock during Pause. The touched practice-group records are captured
on Pause and restored unchanged on Resume while still isolated, so waiting cannot recharge a defense.
Only owned/touched groups are affected; the original exit baseline still advances in real time.
Repeated pauses cannot refresh or consume these cooldowns. Simulation-owned status durations remain
frozen. Resume failures release pause-input restrictions in a finally block, and plugin disposal
still reaches verified zone cleanup if that release reports an error.

Writes require live hooks. Reset additionally requires active guarded simulation, captured baseline,
same job, and SafeMode enabled. Mismatched job on exit refuses filter release rather than writing the
wrong groups. Native practice never resets cooldowns from an idle UI outside simulation.

### Leave, exceptions and plugin unload

`Game.Leave` clears scenario state, restores its job-gauge snapshot and unloads the map.
`ZoneSession.Revert` now owns an independent `TransitionInput` lock. Resume/teardown of Pause cannot
remove that restriction. Direct/location actions, movement, jump and auto-cancel are blocked locally
while restoration runs; FluffySim UI remains usable.

With filtering still up, `QuiesceLocalActions` clears the existing native client action-queue flag
and pending ground-target intent. The installed typed `AutoAttackState.SetImpl(false, false, true)`
turns off the local auto-attack state with `sendPacket=false`; it does not issue GeneralAction 1.
The [SDK WeaponState API](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/UI/WeaponState.cs)
exposes this explicit non-sending variant. Auto-attack must be off before release. An active cast is cancelled through the **existing local**
ActorControl client-handler path, not an outbound cancel action. Inn reload follows. Restore failure
now stops the client instead of proceeding after a catch with an uncertain environment.

The delayed lift and per-frame retries require both hooks, login, inn territory, completed inn load,
no active duty, matching native GameMain territory, at least the existing one-second settling window,
no queued action/cast/ground target/auto-attack, same job, and restored position. Only then are touched recasts
restored. Receive interception is removed first; send interception is removed only after that
succeeds. Transition input is released last. No plugin-owned blocked gameplay queue is flushed.
Existing subsequent Social-window party refresh is normal inn traffic, not a simulated ability. Its
delayed callback now refuses stale/disposed ownership, a new simulation or an unsettled/non-inn state.

**Issue 34 was relevant to unload.** Previously `pendingLiftMayRetry=false` skipped the native-territory
check, and unload skipped the normal delay. Both exceptions are removed. If unloading/reloading cannot
wait for verified restoration, the existing fail-closed safety stop terminates the client rather than
allow Dalamud to dispose the filters early. Disposal errors during an armed/captured stay now use
that same fail-closed path. Fatal logging is best-effort and cannot bypass process termination.

**Operational consequence:** Leave the simulator, wait for the inn and `/fluffysim firewall` to show
`send=False, receive=False, restoring=False`, then disable/reload/rebuild the installed plugin. Do
not deliberately test the emergency path on a live connection. A client crash is a recovery cost,
not evidence that no packet could ever have escaped before a previously lost hook was detected.

## Remaining limits

- No packet capture or live action was generated by this development session. Managed policy/fake
  recast tests and source-wiring tests do not prove that a native hook is correctly located in every
  future client build. Native hotbar reset and exit restoration need owner validation.
- We did not disassemble every alternative send caller, transport thread, native retry buffer or
  other plugin hook chain. Standard action paths use the inherited filter, supported by the owner's
  observed results; a bypass in another client path remains a residual risk rather than a claim of
  universal proof. Preexisting in-flight traffic cannot be unsent.
- Delayed macro lines or another plugin scheduling a new action after a completed Leave are fresh
  outside-simulator inputs, not blocked packet bytes being replayed. Finish long-running action
  macros before leaving; this filter is scoped to the simulated stay, not a permanent action block.
- Guard checks cannot atomically prevent another plugin/patch from disabling or replacing a hook.
  Immediate direct/location action checks narrow that gap; lower native callers still depend on the
  send filter. Process corruption and native access violations are outside managed recovery.
- Incoming allowlists are version/config dependent. Source inspection plus observations provide
  confidence for normal operation, not transport certification or a Square Enix policy guarantee.
- Windows game code runs under Wine on the owner's Linux system; this implementation adds no
  Linux-specific alternate network path. Linux headless tests cannot validate Wine hook ABI or
  transport behavior. Diagnostics/manual evidence must be rechecked after game/Dalamud changes.
- Do not change jobs while isolated. An incomplete restore at unload now deliberately stops the
  client. Normal Leave should settle and preserve controls; report a safety stop rather than working
  around it by disabling filtering.

## UI and low-level compatibility

When `NativePracticeReady` is true, both dedicated virtual buttons disappear. Use normal hotbars.
The former virtual controls remain a fail-safe only when the native-practice gate is unavailable;
their timers/cleanup are retained for compatibility and tests. There is no permanent or automatic
invulnerability, nor an invulnerability cooldown reset for non-tank jobs pretending to be tanks.

Meteors retains `/fluffysim antikb`: six seconds of managed protection, explicit activation, once per
run. It checks scenario/alive/readiness/Pause, not character level, job or unlocked abilities. It
invokes no native action, recast, status or server operation. It can be put in an FFXIV macro hotbar
with just `/fluffysim antikb`; Dalamud handles this plugin command locally. Native-capable users do
not need it. Restart resets readiness; Stop/Leave/death/completion clear it as before.

## Manual validation (owner-built Release only)

1. Leave any current simulation and wait for the inn before building/reloading. Use a disposable
   Free Trial WAR with Holmgang unlocked (level 42), God Mode off. Record any preexisting Holmgang
   cooldown before entry. Do not deliberately disable a filter or force an unload during restoration.
2. Start Darklit with MT bait selected. `/fluffysim firewall` must show send/receive true and native
   practice true. The old virtual buttons should be absent. Check that the same shared controls,
   boss mechanics and Pause still work.
3. At the tankbuster window, target a valid simulated boss and press the normal Holmgang hotbar.
   Confirm a local cooldown, timed simulated protection and survival of both hits. Check the blocked
   counter increases. This counter also includes other traffic; do not label every increment Holmgang.
   Compare IINACT/Cactbot results with the earlier observation. Do not send diagnostic payloads.
4. Restart and repeat immediately: Holmgang should be ready again. Repeat with a mistimed/missed
   activation to confirm ordinary failure. Check Apocalypse too. Test PLD/DRK/GNB with their own
   unlocked hotbar defense when available; these are separate client validation cases.
5. Leave normally, wait until the firewall diagnostic shows both filters down/restoration false.
   The local cooldown must match the pre-entry cooldown advanced by time spent practicing. A
   previously ready ability should be ready. Zone normally afterward and compare; a legitimate
   outside action should again be visible to the normal combat logger. Never try to exercise an
   unavailable outside ability to bypass its server cooldown.
6. In Meteors, time native Arm's Length (level 32) or Surecast (level 44 caster/healer) for Faith
   Unmoving. Restart for another attempt. Missing/mistimed protection must still knock back/fail.
7. On a character without either skill, use a macro containing `/fluffysim antikb` at that same
   moment. It must work once without native recast/status changes, fail if used too early, and reset
   on restart. Paused/KO/idle use must be refused. Test Hard/invisible/God Mode combinations normally.
8. Test normal Stop, then restart; Pause/Resume, then Leave. Controls and baseline recasts must
   restore without a stuck transition lock. Do not intentionally trigger the fatal unload case.

Automated validation results are recorded in the roadmaps after this session's regression run.

### Validation results

- Full Debug plugin regression: **13,009 passed**, zero failures, including the complete 1,680 × 16 Fall of Faith sweep.
- Final isolated Debug focused checks after the restore/Pause refinements: **75 passed**, zero failures; includes all nine paused complete scenarios, cooldown ownership/shared groups, repeated native-timer pause restoration, missed/timed tank defenses and cleanup exceptions. These are overlapping runs, not additive test totals.
- Relay: **41 passed**. Tooling: **65 passed**.
- Debug build: **zero errors, 3,965 existing warnings**; no increase in the warning baseline. Diff whitespace checks passed.
- Debug ZIP packaging was checked in temporary output; no public Release artifact was produced. Release output, installed plugins, release version and repository history were not modified.
- Native recast reset/restoration, action presentation and Wine hook behavior remain subject to the manual client checks in PACKET-FILTER-AUDIT.md. No automated live gameplay actions or network payload capture occurred.
