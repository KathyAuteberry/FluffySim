"""Source wiring checks complement managed policy tests; these do not certify native transport."""
from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2] / 'AnoMech'
def read(path): return (ROOT / path).read_text()
def section(text,start,end): return text.split(start,1)[1].split(end,1)[0]

class PacketFilterWiringTests(unittest.TestCase):
    def test_arming_and_snapshot_precede_zone_mutation(self):
        text = read('Core/Native/Implementations/ZoneControl.cs')
        enter = section(text,'public bool Enter(', '// Immediately set the active weather')
        self.assertLess(enter.index('EnableFirewall();'),enter.index('LoadZoneInternal('))
        self.assertLess(enter.index('if (!FirewallArmed('),enter.index('practiceRecasts.Capture();'))
        self.assertLess(enter.index('practiceRecasts.Capture();'),enter.index('LoadZoneInternal('))

    def test_only_policy_approved_heartbeat_reaches_original_and_no_deferred_queue_exists(self):
        text=read('Core/Native/Implementations/ZoneControl.cs')
        send=section(text,'private byte SendPacketDetour(', 'private void ReceivePacketDetour(')
        self.assertEqual(send.count('sendPacketHook.Original('),1)
        self.assertIn('if (outboundPolicy.Allow(opcode))',send)
        self.assertNotIn('Task.',send)
        self.assertNotIn('Queue',send)
        policy=read('Core/Native/Policies/PacketFilterPolicy.cs')
        self.assertIn('heartbeat != 0 && opcode == heartbeat',policy)

    def test_unload_cannot_bypass_native_inn_or_pending_action_verification(self):
        guard=read('Core/Native/Implementations/ZoneControl.Guard.cs')
        lift=section(guard,'private string? LiftBlockedReason()', 'private string? pendingLift;')
        self.assertIn('FirewallLiftPolicy.BlockedReason(NativeTerritory() == innClientTerritory, restoredFor, pendingAction)',lift)
        self.assertNotIn('pendingLiftMayRetry && NativeTerritory()',lift)
        self.assertIn('am->ActionQueued',lift)
        self.assertIn('AreaTargetingActionId',lift)
        release=section(guard,'private void TryLift()', 'private void LogStaySummary()')
        self.assertLess(release.index('LiftBlockedReason()'),release.index('practiceRecasts.Restore()'))
        self.assertLess(release.index('practiceRecasts.Restore()'),release.index('DisableFirewall()'))
        self.assertLess(release.index('DisableFirewall()'),release.index('TransitionInput = false'))

    def test_revert_blocks_inputs_discards_local_queue_and_fails_closed_on_error(self):
        text=read('Core/Native/Implementations/ZoneControl.cs')
        revert=section(text,'public void Revert()','private void RestoreBeforeLift(')
        self.assertLess(revert.index('TransitionInput = true'),revert.index('LoadZoneInternal('))
        self.assertLess(revert.index('QuiesceLocalActions();'),revert.index('LoadZoneInternal('))
        self.assertIn('am->ActionQueued = false;',read('Core/Native/Implementations/ZoneControl.Practice.cs'))
        self.assertIn('AutoAttackState.SetImpl(false, false, true)',read('Core/Native/Implementations/ZoneControl.Practice.cs'))
        self.assertIn('Die($"the inn reload threw:',revert)
        self.assertNotIn('UseAction(',revert)
        resync=section(text,'private void OpenSocialForPartyResync()', 'private void OnSocialPostSetup(')
        self.assertIn('Current != this || IsActive || guardArmed || !IsInInn()',resync)

    def test_native_calls_are_guarded_and_defenses_are_independent_of_optional_module(self):
        hooks=read('Core/Native/Implementations/LocalPlayerInputHooks.cs')
        for start,end,original in [('private bool UseActionDetour(', 'private bool UseActionLocationDetour(', 'useActionHook.Original('),
                                   ('private bool UseActionLocationDetour(', '\n}', 'useActionLocationHook.Original(')]:
            body=section(hooks,start,end)
            self.assertLess(body.index('ZoneControl.VerifyActionIsolation();'),body.index(original))
        actions=read('Core/UserActions/UserActions.cs')
        body=section(actions,'private void OnActionExecuted(', 'private void ResolvePendingCast(')
        self.assertLess(body.index('PracticeDefensiveActions.Apply('),body.index('foreach (var handler in handlers)'))
        self.assertIn('NotePracticeAction(actionId)',body)
        start=section(actions,'public void OnScenarioStart()', 'public void OnSessionStart()')
        self.assertIn('if (!SimActive) return;',start)
        self.assertIn('if (hooks.LastUsedActionSequence is { } seq) processedSeq = seq;',start)
        self.assertIn('PracticeDefensiveActions.RecastsForJob(Natives.BattleCharas.LocalPlayer.ClassJob)',start)
        self.assertIn('Natives.Zone.ResetPracticeRecast(action);',start)
        self.assertLess(start.index('ResetPracticeRecast'),start.index('foreach (var handler'))

    def test_ui_buttons_are_fallback_only_and_low_level_command_is_purely_local(self):
        ui=read('Windows/MainWindow.cs')
        self.assertIn('controls.Visible && !Natives.Zone.NativePracticeReady',ui)
        self.assertIn('antiControls.Visible && !Natives.Zone.NativePracticeReady',ui)
        body=section(read('Plugin.cs'),'case "antikb":','case "arena-dsr-p2":')
        self.assertIn('PracticeAntiKnockbackControls.Press(',body)
        for forbidden in ['UseAction(', 'Recast', 'SendPacket', 'ActionManager', 'ClassJob', 'Level']:
            self.assertNotIn(forbidden,body)

    def test_disposal_errors_fail_closed_and_fatal_logging_is_best_effort(self):
        text=read('Core/Native/Implementations/ZoneControl.cs')
        dispose=section(text,'public void Dispose()', 'private void DisposeCore()')
        self.assertIn('if (IsActive || guardArmed || practiceRecasts.Captured)',dispose)
        self.assertIn('Die(',dispose)
        fatal=section(read('Core/Native/Implementations/ZoneControl.Guard.cs'),'private static void Die(', '\n    }')
        self.assertIn('try { note = DiagnosticLog.Fatal(',fatal)
        self.assertIn('Environment.FailFast(',fatal)
