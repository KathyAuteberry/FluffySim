# DSR P3 native presentation audit (2026-10-22)

The correction is confined to P3's action 26378 and the three numbered dive actions. No changes
to Drachenlance, other Geirskoguls, bot routes, assignments, tank tethers or damage geometry.

## Geirskogul evidence and sequence

Read-only extraction of the two format-5 Duty Recorder files in the archived recording
`Dragonsong's Reprise (Ultimate) 2024.11.06 23.44.18.7z` (game build 10710093, territory content
788) yielded 66 and 44 ActorCast packets for action 26378. All 110 have:

- packet length 32, action ID at offset 0 = 26378 and duplicate ID at 4 = 26378;
- ActionType at 2 = 1;
- OmenDelay byte at 3 = 35, meaning 3.5 seconds;
- CastTime at 8 = 4.2 seconds (float precision 4.1999998).

The installed Action sheet gives a 4.5-second action duration, rectangle length 62 and width 8.
The existing EnemyAction handler subtracts the usual 0.3 seconds for the native castbar. BossMod
DSW2 P3Geirskogul independently identifies the same clone/self-target rectangle and cast.
AsuMech's CastGeirskogulsFrom also locks nearest-player direction at cast start, but supplies
zero omen delay; it is not evidence that zero delay is authentic.
The available recent IINACT logs did not yield matching P3 action-26378 casts; names alone also
match unrelated actions. Recorder bytes provide the precise delay that combat logs cannot.

| Event relative to CastLines | Before | After |
|---|---|---|
| Closest living player selected; clone direction fixed | 0.0 | 0.0 |
| Native ActorCast/castbar starts | 0.0 | 0.0 |
| Line omen becomes visible | 0.0 | 3.5 |
| Damage snapshot and failures | 4.2 | 4.2 |
| Native ActionEffect/impact | 4.5 | 4.5 |
| Existing warp timeline | 6.6 | 6.6 |
| Existing hide | 7.3 | 7.3 |

Thus the warning lasts 0.7 seconds before snapshot, or 1.0 before impact. This is recorded
packet timing, not a guessed millisecond window. The three numbered rounds start at 46.51,
56.50 and 67.48; the four-tower lines start at 86.68. All use this P3-specific EnemyAction.
The cast targets the clone itself after its facing is chosen, so the late omen is not retargeted
to a moving baiter. Snapshot uses the unchanged caster placement.

Native ActorCast handles the warning; there is no second manually spawned ground VFX. Pending
casts retain the original rotation and subtract elapsed simulation time from cast and omen delay
when actor registration completes. They now retain the queued packet while the proxy is absent,
and discard it on expiry, cancellation, release or despawn. A delayed native omen's countdown
would otherwise continue during Pause: pausing cancels its native presentation, and resuming
reissues only the remaining cast and omen time. This also applies to other positive-OmenDelay
casts using the shared machinery; the mechanical event queue is unchanged. The resumed bar
represents the remaining cast duration. Native inspection of pause/resume is still required.

## Numbered dive landing cue

Installed Action rows 26382/26383/26384 all select AnimationEnd 4574,
`mon_sp/[SKL_ID]/hide/mon_sp002`; clone BNpcBase 12606 uses ModelChara 3147, monster m0711.
The resolved resource exists at `chara/action/mon_sp/m0711/hide/mon_sp002.tmb` and references:

- `vfx/monster/m0711/eff/m0711_hide_01c0x.avfx`;
- `vfx/monster/m0711/eff/m0711_hide_02t0x.avfx`;
- `sound/vfx/monster8/SE_Vfx_Monster_m0711_hide_sp02.scd`.

Its tracks extend to frame 30, including target-side presentation from frame 12. Recorded
ActionEffect1 packets for all three actions use animation lock 1.1, variation 0, ActionType 1,
flags 0 and one target, matching the existing synthetic effects. No alternate action ID or
invented replacement effect is needed. The existing code forcibly hid the source at 0.57 seconds,
cutting the native timeline short. It now preloads the resolved landing timeline before any
round and hides after the existing 1.1-second animation lock. No extra ActionEffect, clone or
independent VFX is emitted. Whether this fully repairs the reported missing cue needs native
verification; headless fakes cannot render TMBs or prove the cue's perceptibility.

Position and facing lock still occur inside DiveAt at 37.28 / 47.27 / 58.29, before the native
action is emitted. Dive damage resolves immediately there. Tower positions still use the locked
player pose and existing 14-yalm scenario offset, and materialize/soak through the unchanged
41.38 / 51.38 / 62.40 tower casts. The visual correction does not claim the later tower spawn is
the movement-safe moment. Tuufless likewise describes offsets from facing when the dive lands:
https://ffxiv.tuufless.com/elemental/dsr/03_nidhogg/

## Verification boundary

Focused tests cover four line sets, native delay and cast duration, closest-player facing,
locked release rotation, no early release, delayed registration before/after the omen deadline,
pause/resume remaining delay, no repeated logical cast and pending presentation teardown.
Dive tests cover all three rounds and all three arrows, one native effect per clone, visibility
through landing, locked placement and post-lock movement. Existing P3 tests cover all seats,
tethers, failures, delayed spawns, restart/leave and bounded seeds. Offline scenario tests cover
running without an online login; packet-safety tests cover the production lifecycle. Neither
is a substitute for running both native environments.

Native QA: watch all three jump types for visible landing/impact and sound, move on the cue
and confirm tower placement is already locked, compare the late line flash to retail, pause
before and during the flash, and Restart/Leave at both pending and visible presentation points.
Repeat in an inn and from title-screen offline practice. Confirm native castbar/enemy-list and
resource preload behavior with no duplicate or orphaned effects.

## Native retest failure and renderer correction (2026-10-22)

The previous lifetime/preload change did **not** repair the native jump. It must not be treated
as a verified visual fix. The native retest's Dalamud log shows all eight waiting clone actors
with `DrawObject=null` and empty animation slots at +5, +30, +90 and +210 frames. Their
BattleCharas and model IDs existed; their renderers/sequencers did not. An ActionEffect request
at the same frame as clearing the model-suppression mask cannot make that renderer already exist.
The earlier fake assumed renderers existed as soon as their actors registered and therefore
could not expose this failure. A successful TMB preload also says nothing about the actor's
ability to play it. Preloading this model-specific timeline has been removed; extending the
hide deadline again is not the correction.

AsuMech's `Core/SimObjects/SimEnemy.Fork.cs` preserves a desired visibility and applies it to an
existing DrawObject every tick. Its `BattleCharaProxy.IsDrawObjectVisible` setter writes the
DrawObject's IsVisible property, rather than GameObject.RenderFlags.Model. Its P3 Dive method
uses the same three native action IDs and targets; it does not supply a missing alternate jump
spell or a separate movement packet. The local AnoMech implementation exposes only a read-only
DrawObject visibility and model-mask hiding. FluffySim's adaptation lost this important
visibility distinction. The original recorded clone sequences contain ActorSetPos followed by
ActionEffect1 (26382/26383/26384), with the marked player as animation target. No additional
source ActorControl jump event appears in the inspected dive window. The recorded hidden spawn
flags are useful context, but old packet header/mode layouts have not been blindly copied.

The correction adds retained-renderer visibility at the native boundary and uses it only for
P3's clone staging, dives and tower/line reveals. Waiting clones may finish loading their model
and sequencer while their DrawObjects remain invisible. At the unchanged snapshot the source
is placed at the locked target pose, revealed, and the original targeted ActionEffect is sent
through ActionEffectHandler.Receive. Native timeline 4574 resolves the clone jump, target impact
AVFX and sound already identified above. No second action, manual fallback VFX or invented
flight path is added. The existing 1.1-second hide deadline is unchanged.

Only the three dive actions opt into a loaded-renderer/sequencer guard. Unexpectedly cold sources
retry presentation on the simulation scheduler until the original animation-lock deadline;
mechanical damage/status/placement snapshots have already resolved. The visual aim is captured
before waiting, and no stale jump is replayed after the deadline. Pause stops this retry clock;
Restart/Leave clear the world queue and destroy its actors. A genuinely late renderer cannot
render at a past timestamp: the fallback is explicitly logged, not claimed to be frame-perfect.

Diagnostics are prefixed `[DsrP3Dive]` (staged readiness, numbered snapshot, hide, tower cast,
world cleanup) and `[EnemyPresentation]` (waiting, dispatched sequence, expiry and three native
sequencer observations). A dispatch sequence means the native handler was called, not that pixels
were drawn. Inspect the observations for timeline 4574, advancing scheduler timestamps, loaded
resources, render flags and actual DrawObject visibility. Missing models/empty sequencers remain
an actionable failure even if every simulated timeline assertion passes.

New tests model asynchronous renderer allocation blocked by model suppression; cover the nine
number/arrow combinations with retained hidden renderers; detect unrenderable ActionEffect
requests; exercise cold-source retry, original snapshots, Pause and Leave cancellation. Existing
P3 tests continue to cover target IDs, facing offsets, human/bot targets, lifecycle, all seats
and the recorded Geirskogul delay. A metadata contract checks both Debug artifacts for the real
DrawObject visibility setter and ActionEffectHandler.Receive path.

**Native acceptance remains pending.** Retest all three numbered waves and jump types against
human and bot targets in both an inn and title-screen practice. Confirm a visible native clone
dive/impact at marker expiry, before the later tower. Use the new renderer/sequencer diagnostics
if the native presentation is still absent; passing headless tests does not close the visual bug.
