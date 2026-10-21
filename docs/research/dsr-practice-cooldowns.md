# Local practice cooldown restoration — 2026-10-21

## Causes and scope

`UserActions.OnScenarioStart` previously reset invulnerability only when `PracticeInvulnerability.Enabled`, and anti-knockback only when its optional practice substitute was enabled. Most DSR entries never enable those modes. CT separately reset its mitigation list; FRU's optional modes therefore appeared more reliable. Restart already uses the same ScenarioSession.Start path, after scenario initialization and player placement; there was no missing Restart callback.

The central callback now restores a job-specific allowlist on every Start/Restart, independent of optional substitute modes and handler enablement. All DSR and FRU isolated entries share this path. Continuous mechanics inside an attempt do not call Start, so cooldown planning between segments remains meaningful. No per-frame replenishment is introduced. Sprint's existing handler remains compatible. Scenario-specific LB/gauge setup remains owned by the encounter (P4 rescue and P5 meteor controls); no global LB refill was added.

Coverage: Hallowed Ground30, Holmgang43, Living Dead3638, Superbolide16152; each tank's old/current major mitigation, supported party/personal mitigation, Rampart/Reprisal, Sheltron/Holy Sheltron/Intervention, Raw Intuition/Bloodwhetting/Nascent Flash, TBN/Oblation, Heart of Stone/Corundum; Arm's Length for physical jobs, Surecast for casters/healers; Interject/Head Graze where applicable. Action data confirms shared upgrade groups and Oblation's two charges. Gauge/MP costs are not waived. Existing local job-gauge controls remain separate.

Two additional contract defects were corrected: missing groups at initial capture can now be captured before their first reset, with their own wall-clock timestamp; reset sets Elapsed=Total, rather than zero. ClientStructs documents Elapsed as charge credit and Total as the full charge capacity. Inactive with zero credit was not a reliable charged-action restoration. Native GetRecastGroup resolves upgrade/trait aliases; shared groups are captured/restored once.

## Isolation and lifecycle

Requests wait behind NativePracticeReady, matching job, both filters and SafeMode. Pending resets are attempted on guard ticks, removed on success, canceled for the entire recast group when an action is used, and cleared before Leave restoration. This prevents a late retry from refreshing an already-consumed cooldown. The managed session rejects unsupported actions and unrelated shared-group owners. Originals are captured before writes and restored with real wall-clock progress while the firewall is still armed; pause snapshots only the private practice timers. No real action request or installed binary is involved.

Native tests must confirm all four invulnerabilities, charged Oblation, short mitigation, Surecast/Arm's Length and P1 interrupts: initial inn/offline entry, consume then Restart, repeated failure/retry, within-attempt cooldown enforcement, Pause/Resume, then Leave and original real timer continuity. Synthetic recast storage checks cannot prove hotbar/native charge rendering. Resource-limited abilities may still require the normal local resource controls.

## P5 conventional swap follow-up

Owner confirms prior cooldown improvements natively. Provoke7533 is now registered for all four tank jobs and in the same scoped recast capture/reset/restore allowlist. The local Provoke handler runs independently of optional UserActions simulation, while preserving its guarded local action dispatch. No recast restoration occurs between the three Might hits. Retest consuming Provoke then restarting each P5 entry, and real timer continuity after Leave.
