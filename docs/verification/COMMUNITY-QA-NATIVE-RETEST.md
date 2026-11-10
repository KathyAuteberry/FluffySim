# P5 native retest corrections — unpublished

Native owner feedback confirms the Darklit Water presentation and Wrath Twisting Dive Enemy List repair. CT DPS/healer deaths remain unconfirmed. Neither FRU encounter was changed in this follow-up.

The starting working copy was clean, directly above `yowxqp`, with `kxkywx` and `zxqmxx` also in its ancestry. Published `master`/`master@origin` remain 3.0.1.0 (`xorxqy`). Paradise Regained, Wroth Flames and historical `plkqvn` were preserved. No unpublished integration generation existed. This work continues the existing anonymous local feature chain without moving bookmarks or preparing a release.

## Death's missing Twisting Dive

Death creates its own Vedrfolnir in `SpawnTrio`, separately from Wrath. Its generic `Spawn` helper defaulted to `listed=false`, producing `EnemyListMode.Never`. The previous correction affected only Wrath's trio spawn. Death already casts action 27531 on the visible native dragon, not an invisible carrier. Nothing removed or hid that actor before the cast finished.

Death's trio dragon now explicitly requests `listed: true`, matching Wrath's `EnemyListMode.Always`. Both use the existing `DsrActors.WithNativeAppearance`, `SimEnemy`/`SimCast` and shared `EnemyList` service. Registration uses the dragon's own entity ID; targetability stays false. The normal local Hater-list updates resend every second, including after delayed registration. No second dragon, helper cast, custom overlay or alternate cast ID was added.

Preserved scheduler clocks, within one frame:

| Scenario | Spawn | Cast start | Native bar | Damage snapshot | Native release |
|---|---:|---:|---:|---:|---:|
| Wrath | 37.40 | 38.98 | 5.7s | 44.68 | 44.98 |
| Death | 11.60 | 18.01 | 5.7s | 23.99 | 24.01 |

The six-second Action sheet cast uses the existing 0.3-second bar/release offset. Death retains its existing +0.28 snapshot offset, which explains its different snapshot-to-release interval. The native action name is localized by the game; the English sheet says “Twisting Dive.” All Twister snapshots, dive damage and geometry are unchanged. Death retains its existing actor lifetime through cleanup; only Enemy List eligibility changed.

Scenario-specific regressions assert one dragon, one native cast, its action and duration, end/release and actual damage-query time, Enemy List addition/removal, untargetability, 0/5/19-frame packet arrival, active-cast Leave and repeated entry. Existing Wrath checks remain passing. Actual Death Enemy List rendering awaits native confirmation.

## What the native Provoke evidence shows

The read-only native diagnostic logs contain a useful contrast:

- In one Wrath attempt, the player pressed Provoke **0.549 seconds after Heel's hit**. All three subsequent Might snapshots targeted ST. This establishes that native input could reach the local swap in that session; it does not establish survival or mitigation correctness.
- Other failed Wrath attempts pressed Provoke **3.056** and **2.519 seconds before Heel's hit**, during its cast. Might remained on vulnerable MT.
- The failed Death attempt pressed Provoke **1.174 seconds before Heel's hit**, also during the cast. All three Mights remained on MT.

The old `TankSwapSequence.Heel()` set `acceptingSwap=false` at cast start and did not open it until the hit's resolution callback. Its Provoke handler silently returned during the entire bar. Heel's target had already been captured as MT by `boss.Cast(..., target)` at cast start, so this gate unnecessarily rejected during-cast swaps that could safely affect subsequent attacks. The new callback-path tests reproduce that distinction.

**This is a demonstrated local timing defect matching the failed native presses.** Old logs recorded the button press but not the native return value, selected/requested target, execution-sequence rejection or local rejection reason. They cannot establish that every failed press reached the subscriber, nor rule out an additional native input issue. This correction is not native acceptance of the complete hotbar interaction; the new trace must confirm that.

Tank stance was never checked by the old swap code. The correction requires no stance and does not infer it from native visibility. The diagnostics report any simulation stance statuses separately, with `stanceRequired=false`. Empty simulation status data is not evidence of the real client's stance state.

## Corrected swap and input path

Heel now opens the local Provoke window when its cast starts, while retaining MT as the immutable Heel target. A valid human ST Provoke can change `Holder` during the cast or after the hit, before the first Might. Heel resolution does not reset that holder. The preparation prompt now describes this timing. Bot ST still automatically Provokes after Heel hits; human ST is never automatically swapped.

`Record` now derives the damage target from the captured target actor, independently of current aggro. This matters when ST already owns aggro while the MT-locked Heel is resolving. Each Might selects `world.Party.Get(Holder)`, faces that actor, resolves the existing cone, and records actual recipients/statuses. No cached MT role selects the three cleaves.

Actual input remains on the existing path:

1. `UseAction` or `UseActionLocation` observes the request and records diagnostic context.
2. `ZoneControl.VerifyActionIsolation` remains before the original native call. No new native threat request, outgoing packet or firewall exception was introduced.
3. Only a successful native return raises `ILocalPlayerInput.ActionExecuted`; a queued/duplicate callback must also advance the execution sequence before dispatch.
4. The real `UserActions` subscriber requires an active sim and unlocked input. It dispatches Provoke independently of the optional combat-action toggle, tracks its existing practice recast, then resolves it immediately without waiting for a synthetic ActionEffect or server acknowledgement.
5. The local handler checks the player is alive, in a tank seat with one of the four tank jobs, and the requested ID resolves to an active targetable simulation enemy. It accepts that enemy's native GameObjectId or entity ID and forwards the canonical simulation identity.
6. The active tank-swap sequence checks that enemy is its Thordan and the Heel window has started, changes local aggro, and updates native target presentation.

Provoke remains canonical action **7533** for all four tanks; pinned Action data has zero cast time and EquivalenceGroup 0, and the native logs report action 7533. No replacement mapping was invented. The original requested target is preserved. The selected UI target is logged for comparison, never substituted for an invalid or missing request.

The execution counter is now read through the native input interface, backed by the same `ActionManager.LastUsedActionSequence` used before. Scenario-start initialization uses that boundary and the local player proxy's job. This lets tests exercise the production subscriber and lifecycle without dereferencing an invented native ActionManager. The existing Provoke recast allowlist and native execution requirement remain intact.

Early Provoke **before Heel begins** still fails. Missing, invalid-target, rejected native execution, unchanged-sequence, paused and transition-locked actions do not earn a swap. A Provoke after the first Might cannot undo that missed-swap death. MT reclaiming aggro still causes subsequent cleaves to fail. The 20% minimum human tank mitigation budget, vulnerability rules, damage, boss geometry and party positioning are unchanged.

## Why previous tests missed it, and the remaining test boundary

The old tank tests called `LocalProvokeHandler.OnAction` directly, supplying an already-correct boss ID at **77 seconds in Wrath or 66 seconds in Death**, after Heel's hit. They bypassed native execution, sequence deduplication and the real `UserActions` event subscription, and never tried the during-cast presses seen in the failing logs. Those tests established the internal post-hit swap, not the entire hotbar route.

New `LocalProvokeRoutingTests` subscribe a real `UserActions` instance to fake native input and explicitly report a declared execution result/sequence at that boundary. They exercise the production subscriber, local validation, aggro listener and real damage snapshots. They do **not** claim a physical hotbar press occurred, that the game returned true, or that its sequence advanced naturally. Rejected native results are modeled explicitly and do not emit an execution event.

Coverage includes:

- All four tank jobs, both P5 scenarios, optional action module on/off, stance absent, during-cast and post-hit swaps, and actual MT Heel / three ST Might recipients with vulnerability and mitigation checks.
- Invalid/missing requested targets, pre-cast and late Provoke, missing input, native rejection, unchanged sequence, Pause/transition locks, and duplicate callback suppression. Dispatch observations are counted separately from encounter acceptance.
- Repeated entries, plus a persistent-subscriber `Game.Reset` test in each scenario: actual scenario-start initialization captures the existing sequence, rejects its stale replay, accepts the next execution, resets Provoke's recast, and completes a second correct four-hit sequence. Local recast restoration now explicitly includes Provoke in its regression cases.
- Existing human MT/bot ST, four-job, missed-swap, deliberate MT reclaim, expired mitigation and post-mechanic completion tests.

Inn and offline use the **same** installed native hooks and local subscriber; no offline-specific action routing was added. Existing offline scenario tests cover running the P5 entries without a logged-in client state. Neither that test nor the fake callback reproduces a real offline ActionManager/hotbar execution. Native original-return, sequence/target identity and hotbar behavior must still be checked separately in inn and offline sessions. The new log includes `offline=true/false` to identify that context.

## Native diagnostic acceptance

Use the owner's normal Debug development setup and confirm the new `[ProvokeInput]` messages are present. Do not infer correct loaded code merely from the unchanged release version. Test both scenarios in both inn and offline practice, initially with stance off:

1. Human ST: target Thordan, press real Provoke once during Heel's cast, mitigate normally, and watch MT take Heel while ST takes all three Mights. Repeat with Provoke after the hit but before the first cleave. Confirm target presentation follows ST and bot MT never reclaims.
2. Human MT: mitigate Heel and move out of the frontal; confirm bot ST takes all three. Do not use human Provoke to rescue the bot's behavior.
3. Restart after using Provoke. Confirm its recast is restored for the drill and another real press works. Pause/Resume around the cast, then Restart/Leave and repeat.
4. Deliberately Provoke before Heel starts, too late, and with no/wrong target. These must remain failures; missing mitigation is a separate failure.
5. In Death, verify one untargetable Vedrfolnir entry in the normal Enemy List, with its native Twisting Dive name/bar and correct completion before the existing dive/Twisters. Recheck Wrath and active-cast Pause/Restart/Leave.

For a failed real press, retain the diagnostic run section containing these events:

| Event | Interpretation |
|---|---|
| No `[ProvokeInput] event=observed` at all | The new hook did not observe the press; inspect loaded build, hotbar/plugin routing, and `RecentActions`. Do not blame simulated aggro yet. |
| `event=observed ... reason=input-locked` | The hook observed the action but Pause/transition input blocked it before the original call. Selected-target data is explicitly unavailable in this branch. |
| `event=observed ... reason=native-result nativeResult=False` | Native execution did not succeed. No local execution event is fabricated; investigate the game's action/cooldown/target state. |
| `event=dispatch-rejected` | Local subscriber rejected inactive/locked input, unavailable manager, or unchanged execution sequence. |
| `event=local-validation` | Requested target, resolved simulation target, BNpc identity, role/job, diagnostic stance statuses and eligibility are recorded. |
| `[TankSwap] event=provoke` | Encounter acceptance/rejection with local owner before/after; pre-Heel and wrong-boss reasons are explicit. |
| `[TankSwap] event=heel-locked` | Records the deliberate MT initialization and window opening; it is not repeated on Heel impact. |
| `[Tank swap] ...` | Actual Heel/Might target and recipients, independent aggro holder, position, statuses and vulnerability at the snapshot. |
| `[TankSwap] event=failure` / `event=damage-failure` | Final missed-swap or actual damage failure reason. |

If native-result succeeds but raw and selected target IDs disagree, provide both IDs and local-validation output. If local acceptance changes MT→ST but a later snapshot still targets MT, provide all subsequent holder/reset and damage records. No event is emitted per frame; logging is tied to presses, dispatch decisions and attacks.

## Executed validation

- Final ordinary Debug plugin build: **0 errors, 5.06 seconds**; the final regression build also copied that artifact to the test host.
- Affected Wrath/Death, native presentation, offline scenario, input-lock and recast lifecycle batch: **245 passed, 28 seconds**.
- Added persistent Restart / recast cases: **68 passed, 7 seconds** (overlapping routing/recast subset).
- Final focused routing, both castbars, existing tank-target tests and recast regressions: **92 passed, 11 seconds**. Counts overlap; no large randomized run was added.
- Final isolated optimized TRACE **Debug** build: **0 errors, 5.57 seconds**, output `/tmp/fluffy-native-safety`, intermediates `obj/native-qa-safety-Debug` per project.
- Final safety, both default and SafetyHook backends: **313 passed, 3 Windows-only skips per backend, 18 seconds each**. Both fresh DLLs were supplied explicitly; the stale-artifact guard and ordinary/optimized IL classification checks passed. No safety allowlist or firewall policy change was made.

NuGet vulnerability-feed connectivity emitted NU1900 warnings; tests/builds succeeded. The test runner required its local communication socket permission. No Release build, installed-output write, version bump, publication, tag, push or plogons change occurred. Read-only investigation of existing native logs did not modify installed files or add private logs/captures to source control.

This is an implemented candidate correction with headless validation. **Human hotbar Provoke is not yet confirmed fixed in game.** The native trace and recipient checks above are the remaining acceptance requirement.

## Final local topology

`master@origin` / `master` (`xorxqy`, published 3.0.1.0) → `kxkywx` → `zxqmxx` → `yowxqp` → `mozkkp` → `xlkrwp`, followed by an empty working change.

- `mozkkp` (`480ecb`): `fix(dsr): show Death Twisting Dive in native enemy list`.
- `xlkrwp`: `fix(dsr): accept local Provoke during Heavenly Heel`, including diagnostics, input/lifecycle regressions and this report.

These two local anonymous changes continue the existing feature chain. Existing ancestors were not rewritten, no integration generation was created, and no bookmark or published reference moved. Native acceptance remains outstanding.
