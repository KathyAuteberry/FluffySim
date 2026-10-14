# Shared Pause / Resume investigation

Status: **not a complete user-facing Pause feature**. No new input hooks, real action calls,
recast/status writes, packet manipulation or global client-clock modifications were added.
The existing internal `Game.Paused` remains an internal/debug freeze, not a guarantee that the
human or native presentation is frozen. This finding applies to all nine FRU/DSR scenarios.

## What the existing runtime freezes

`Core/Game/Game.cs: Tick` returns before the event scheduler, world and scenario ticks when
`Paused` is true. `EventClockNow` stops extrapolating between framework ticks. As a result:

- Scheduled assignments, impacts and damage callbacks do not fire.
- Bot movement destinations remain stored; `Movement.Tick` does not advance position, turning,
  eased pushes or the simulator's carry fallback.
- `SimStatus` elapsed time, `SimCast`'s simulated animation lock, simulated VFX lifetimes,
  practice invulnerability and practice anti-knockback durations do not advance.
- Scenario elapsed time, result detection and movement scheduler departures remain frozen.
- No scheduled RNG draws take place. Stop/reset clears events and the existing paused flag.

This is a useful simulation-only foundation. It is not a single clock governing the whole plugin.

## Paths outside that freeze

`Plugin.cs: OnFrameworkUpdate` always calls `UserActions.Tick` after `Game.Tick`, including while
paused. `Core/UserActions/UserActions.cs: Tick` advances sprint, cast-time, effect-handler and
job-handler state and resolves pending actions on that separate path. `ActionExecuted` callbacks
can also enter action resolution independently of the scenario clock.

The framework continues draining queued work. Native NPC spawn completion and pending framework
callbacks can finish. `Game.Tick` deliberately still services BGM and waiting-start readiness;
`ZoneSession.TickGuard` deliberately runs even if the scenario is paused or throws. Session safety,
real territory changes, connection liveness and teardown must never be stopped to implement Pause.
`EventClockNow` also extrapolates from the last unpaused framework timestamp: a raw flag change to resume can expose the entire pause interval until the first unpaused Tick resets that timestamp. A real pause-state transition must reset that display/peer-clock anchor. Multiplayer transport and received poses are another independent path; a host-only clock freeze
would not provide a synchronized peer pause.

## Native presentation

`Core/SimObjects/SimCast.cs` reads cast progress from `IBattleCharaProxy.CurrentCastTime`; the
client advances that clock independently. The proxy exposes cast injection/clear operations but
no supported per-object pause-and-restore interface. Simulated timers can stay frozen while the
native cast bar completes and its action animation continues.

`Core/SimObjects/SimStatus.cs` owns simulated expiration but refreshes the native status display
only during its Tick. A stopped simulator does not imply that native countdown presentation or
Floating Fetters' animation/lift stops. Likewise, run-loop animations, action timelines, tethers,
VFX and ground effects already handed to the client have their own native updates.
`IStaticVfxProxy` supports Remove, not freezing/restarting at a preserved animation frame.
There is no verified per-object time-freeze abstraction here. Reissuing casts/VFX after resume
would restart presentation, potentially duplicate effects and alter visibility; that has not been
validated as an exact restore mechanism. We must not freeze the global FFXIV engine.

## Human input safety

`Core/Native/Implementations/LocalPlayerInputHooks.cs` already implements mechanic lockouts:

- `RMIWalkDetour` samples normal movement intent, then zeroes left/forward movement sums.
- `CheckStrafeKeybindDetour` rejects strafing while `ZeroMovement` is set.
- `IsInputIdPressedDetour` rejects keyboard/controller jump identifiers.
- `UseActionDetour` and `UseActionLocationDetour` return false for blocked actions before calling
  their original functions, except the stop-auto-attack general action.

These are existing input-path hooks, not a complete audited Pause input scope. A real player or native carry can still change native position while the simulator is not sampling it; the next unpaused character Tick reads that changed position back into the simulation. Keyboard/controller
rotation, mouse/camera-driven facing, auto-run, an already-started jump/carry, queued casts,
auto-attacks and all possible action entry paths require explicit verification. Facing matters
for gaze scenarios, even when the player position does not change.

Crucially, `UpdateDetour` calls the real ActionManager Update first. With `DisableAllActions`, it
then invokes `UseAction(GeneralAction, 1)` to stop auto-attacks; `IsStopAutosAction` permits that
through to the original function. Reusing this flag for Pause would therefore not meet the requested
no-real-action guarantee. Pausing `Game.Tick` also stops `SimPlayer.SyncInputLock`, which normally
re-derives the flags from death, forced movement and statuses each frame. `Game.Paused` alone does
not set or release either flag.

`ZoneSession.cs: SendPacketDetour` forwards only the configured heartbeat and holds other packets
while its hook is armed. Enter checks both firewall hooks and the guard monitors their health.
That session firewall is relevant containment, but it is **not** a local input suppression API;
using it to make Pause safe would fail the requirement not to manipulate server-bound messages.
No firewall changes are proposed or made here.

## Required work before a full Pause button

1. Introduce an owned, scoped pause state that is valid only for an active run, and release it
   synchronously on Resume, Stop, Leave, reset, completion, disposal and exception recovery.
2. Route every simulator-owned ticking/callback path (including user-action handlers and peer
   poses) through the pause state without stopping the native session guard, UI or network liveness.
3. Verify a strictly local input gate for movement **and facing**, keyboard/mouse/controller,
   jump, queued actions and auto-attacks. It must make no real action call and have a tested
   fail-open teardown. The existing stun flags are insufficient evidence.
4. Define and verify per-object native presentation suspension/restoration, or explicitly adopt
   a limited cosmetic contract with local frozen cast/status displays. Exact native animation-frame
   restoration is currently unsupported by the available interfaces.
5. Add full-clock native/manual validation: pause a running cast, lift, charge, knockback, moving bot,
   gaze and virtual defensive action; test multiple cycles, Stop/restart, unload and exceptions.

Until those gates are closed, no normal Pause/Resume control is exposed. The player is not trapped
behind a new input restriction, and the working scenarios and cleanup behavior remain unchanged.
