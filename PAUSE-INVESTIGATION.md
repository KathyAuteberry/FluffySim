# Shared Pause / Resume

## Current upstream integration — 2026-10-15

See [integration decisions, exact source commits, recovery checkpoint and manual checks](UPSTREAM-INTEGRATION.md). The runtime now uses upstream InstanceSession/ScenarioSession, packet-handler spawning and per-object motion, combined with Evernow PRs 35, 38 and 50. FluffySim version remains 1.2.0.0. Historical implementation notes below describe earlier architectures; the integration document supersedes their old ZoneSession paths and teardown assumptions. Debug builds only; no installed Release output was changed.


Implemented for **local practice across all nine FRU/DSR scenarios**. Main scenario controls show
Pause beside Stop, then Resume plus **PAUSED** and the frozen event timestamp. Stop/Leave remain
available. Multiplayer Pause is disabled: its peer clocks/poses are not synchronized by this change.
Native animation, cast bars, tether/VFX animation and native status displays may continue. That
cosmetic limitation is explicit in the UI; the authoritative mechanic clock remains frozen.

## Simulated death: actual call path

`Core/Game/Game.Kill` calls `SimPlayer.OnKilled`. The player gets a private `Dead` flag, stopped
scripted movement, a locally presented one-HP bar, status 896 (Down for the Count), and the KO
animation. This is not a server death or a server-applied incapacitation status. Simulated NPC party
members separately use local `ApplyDeadState` (HP/mana/mode presentation).

`SimPlayer.SyncInputLock` derives `ZeroMovement` and `DisableAllActions` from private death,
scripted movement and simulation-owned status rows. Displaying status 896 is not the input lock.
The actual enforcement is `Core/Native/Implementations/LocalPlayerInputHooks`:

- RMIWalk movement sums are zeroed after native input sampling.
- Strafe keybinds and keyboard/controller jump identifiers are rejected.
- UseAction and UseActionLocation can return false before their original functions.

The ordinary mechanic lock also has an auto-attack cancellation branch: its ActionManager Update
hook calls the real `UseAction(GeneralAction, 1)` when autos are engaged. The existing session
firewall contains outgoing simulation traffic, but that call is not needed or used for Pause.
Fake-death cleanup remains the existing SimPlayer.Despawn path: clear simulation statuses,
restore HP presentation, end KO presentation, and rederive normal input locks.

## Reused hooks, separately owned Pause restriction

`ILocalPlayerInput.PauseInput` is a managed, per-hook-instance flag owned by Game. The extracted
`LocalInputLock` combines it with the existing mechanic flags; this reuses the existing hooks,
not a second global input-hook system.

While PauseInput is set:

- Normal left/forward movement sums, turning sums, strafe and jump inputs are suppressed.
- Both action detours return false before the original call; direct actions also return before
  activity/action bookkeeping. The stop-auto exception is **not** permitted.
- CancelCast returns before its native original and before latching a cancellation request.
- The auto-cancel branch is bypassed (`DisableAllActions && !PauseInput`). Native ActionManager
  Update itself still runs: no real cooldown/recast clock is paused, reset or overwritten.

Pause applies **no** HP, death-mode or incapacitation status. It makes no real action call,
sends no marking/status/action/movement packet and changes no firewall rule. Normal death/stun
behavior outside Pause remains unchanged. Resume clears only the pause-owned flag, so a dead
player or a player still held by Floating Fetters remains correctly restricted.

The common walk/action entry paths are the same ones used by the existing fake-death lockout.
Keyboard/mouse/controller behavior and interactions with the live client still require manual
validation. This is not a claim of zero account risk or control over arbitrary third-party plugins.

## Authoritative freeze

`Game.Paused` stops the event scheduler, World and scenario ticks. EventScheduler also respects a
suspended flag, including pausing between callbacks, so another due event cannot resolve after a
callback pauses the run. Resume resets the between-frame clock anchor: real pause time is never
added to the simulation timeline. Bot destinations, speed, assignments, turn targets and RNG state
remain stored. Status countdowns, scripted carry/push interpolation, hazards, damage, failure checks,
result detection and virtual practice defenses do not advance.

UserActions.Tick and action callbacks are separately gated by the same pause flag. Already-started
player casts retain a simulation-owned remaining-time clock when paused; their local effect/Dualcast
completion is deferred through that remaining time after Resume even if the native bar has finished.
Scenario reset/session end or an explicit post-resume cast cancellation clears that deferred state.
SimCast's busy/animation lock uses its own ticking cast lifetime, rather than allowing a native bar
that finished during Pause to release a following boss's movement early.

Native carries/root motion can drift visually. Resume resynchronizes captured character poses through
the existing local per-actor transform interface only when needed. Logical movement destinations and
pending turn targets are not cancelled. A visible correction on Resume is possible; animations/VFX
are not rewound or reissued, and duplicate actors/effects are not created.

BGM, UI, framework callback draining, session guard and connection liveness remain serviced. The
native client/global clock is never frozen. Multiplayer transport continues normally; manual Pause
is unavailable in multiplayer rather than pretending a local host freeze pauses every participant.

## Complete simulation-owned status preservation

On entering Pause, Game captures every filled party slot (including dead members) and every
simulation-character encounter actor, including helpers. Each immutable status snapshot includes:
ID, stack/param count, source GameObjectId, instance identity, original duration, elapsed simulation
time, remaining duration, active state and native-presentation classification. Character ownership
is retained directly. Poses are captured alongside those records. These are in-memory diagnostics,
not exported private actor data.

The live SimStatus objects are retained unchanged throughout Pause and Resume. There is nothing to
remove/reapply or refresh: a 12-second status with seven seconds left still has seven seconds left
immediately after Resume, with the same stacks, source and instance. Pause adds no status and cannot
replace another immobilization. Repeated cycles do not refresh duration or duplicate status instances.
Virtual defensive actions also reject activation while suspended and retain their remaining duration.

No native status snapshot is imported as authoritative. Real/server-origin statuses that FluffySim
does not own are not cloned, frozen or modified by Pause. Native timers/icons may disappear during
a long pause; the existing ordinary SimStatus Tick resumes their local presentation where supported.
No new status packets, OnGain calls or special native status-restoration writes are issued on Resume.
Native transformation/loop-VFX visuals need not return at precisely the old animation frame; the
simulation-owned damage/vulnerability/immobilization state remains correct regardless of that display.

## Cleanup and recovery

Resume releases only Pause ownership after pose resynchronization. Stop/Leave/restart, completion,
Game disposal and plugin-unload finally paths release the pause gate and discard snapshots. Exceptions
in Game.Tick, simulation user-action ticking or scheduled callbacks also release it; a callback that
pauses and then throws cannot leave the player locked. Recovery never removes mechanic-owned statuses.
Hook disposal clears its managed flags before unhooking.

No new persistent native incapacitation/control flag is set. A catastrophic process/host failure may
prevent normal cleanup; no plugin can guarantee executing finally after process termination. Normal
unload removes the hooks. If UI is closed while paused, use `/fluffysim` (or `/anomech`) to reopen it and
Resume/Stop. If the plugin UI fails, unload FluffySim via Dalamud's mouse-accessible plugin UI. A client
restart clears runtime hooks/state if the host itself cannot recover. Do not reload/build installed
Release output automatically during development.

## Validation and manual checklist

Focused tests cover all nine complete scenarios with five repeated pauses, Hard/invisible/God Mode,
UI control eligibility/toggling, death/Fetters ownership, native cast continuation with frozen virtual
defenses, stop/leave/restart/dispose, callback/runtime exceptions, exact multi-owner status equivalence
on party/enemies, overlapping expiry, once-only events, native carry drift and input-lock policy.
Source-contract tooling checks both real action detours, the auto-cancel exclusion, cancellation gate,
no status/recast/packet operations in the pause transition and actual common UI wiring. Headless tests
cannot certify native input interception or rendering.

Owner builds/reloads Release personally. In Fall of Faith, pause before the first reveal, while bots
move, during a tether/Fetters and just before impact. Confirm the timestamp stays fixed, movement,
jump and ability/hotbar/macro inputs are blocked, and the mouse can click Resume/Stop. Test controller
movement/actions separately. Wait longer than a vulnerability/defensive duration, Resume, and confirm
that the remaining simulated protection/debuff still resolves normally. Repeat on each scenario,
with Hard/invisible/God Mode, while already dead, and with an active virtual defense. Stop/Leave and
re-enter while paused; readiness and controls must reset. Native bars/lift/VFX may continue or correct
visually on Resume; capture any discrepancy that changes actual mechanic resolution.

### Pause release validation — 1.1.0.0

Owner reports successful native Pause playtesting. Debug regression: 11,291 plugin tests passed; final focused checks after cleanup refinements: 97 passed. Relay: 41 passed; tooling: 58 passed. Debug build: zero errors, 3,965 warnings. The 26,880 Fall of Faith assignment/element combinations passed the prior exhaustive sweep. These counts are separate runs, not additive coverage totals. Native cosmetic timers may continue while authoritative statuses remain frozen.
