import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2] / 'AnoMech'


def source(path):
    return (ROOT / path).read_text()


def section(text, start, end):
    return text.split(start, 1)[1].split(end, 1)[0]


class PauseSafety(unittest.TestCase):
    def test_direct_and_ground_actions_are_rejected_before_original_or_bookkeeping(self):
        hooks = source('Core/Native/Implementations/LocalPlayerInputHooks.cs')
        direct = section(hooks, 'private bool UseActionDetour(', 'private bool UseActionLocationDetour(')
        self.assertLess(direct.index('if (PauseInput || TransitionInput)'), direct.index('RecordRecentAction'))
        self.assertLess(direct.index('if (PauseInput || TransitionInput)'), direct.index('useActionHook.Original'))
        ground = section(hooks, 'private bool UseActionLocationDetour(', '\n}')
        self.assertLess(ground.index('if (PauseInput ||'), ground.index('useActionLocationHook.Original'))
        for body in [direct, ground]:
            gate = section(body, 'if (PauseInput || TransitionInput)', 'var selectedTarget')
            self.assertIn('return false;', gate)

    def test_auto_cancel_is_not_invoked_under_the_pause_gate(self):
        hooks = source('Core/Native/Implementations/LocalPlayerInputHooks.cs')
        update = section(hooks, 'private void UpdateDetour(', 'private bool UseActionDetour(')
        self.assertNotIn('UseAction(', update)
        self.assertIn('updateHook.Original(self);', update)

    def test_pause_status_capture_is_readonly_and_scoped_recast_freeze_is_delegated(self):
        game = source('Core/Game/Game.cs')
        transition = section(game, 'public bool Paused\n', 'public SessionStatus Status')
        for forbidden in ['AddStatus(', 'AddStatusParam(', 'RemoveStatus(', 'ApplyStatus(',
                          'UseAction(', 'Recast', 'SendPacket', 'ReceivePacket', 'OnKilled(', 'DropHpBar(']:
            self.assertNotIn(forbidden, transition)
        self.assertIn('AllMembers()', transition)
        self.assertIn('s.Snapshot', transition)
        capture = section(source('Core/UserActions/UserActions.cs'), 'public void OnPause()', 'private void OnActionExecuted(')
        for forbidden in ['UseAction(', 'ApplyStatus(', 'SendPacket']:
            self.assertNotIn(forbidden, capture)
        self.assertIn('Natives.Zone.SetPracticeRecastsPaused(true)', capture)

    def test_simulated_actions_and_cancel_requests_are_gated_while_paused(self):
        actions = source('Core/UserActions/UserActions.cs')
        tick = section(actions, 'public void Tick(float deltaSeconds)', 'public void OnPause()')
        pressed = section(actions, 'private void OnActionExecuted(', 'private void ResolvePendingCast(')
        self.assertIn('if (!SimActive || hooks.PauseInput) return;', tick)
        self.assertIn('if (!SimActive || hooks.PauseInput || hooks.TransitionInput)', pressed)
        gate = section(pressed, 'if (!SimActive || hooks.PauseInput || hooks.TransitionInput)', 'if (hooks.LastUsedActionSequence')
        self.assertIn('return;', gate)
        self.assertLess(pressed.index(gate), pressed.index('NotePracticeAction'))
        hooks = source('Core/Native/Implementations/LocalPlayerInputHooks.cs')
        cancel = section(hooks, 'private void CancelCastDetour(', 'private void RMIWalkDetour(')
        self.assertLess(cancel.index('if (PauseInput || TransitionInput) return;'), cancel.index('cancelCastHook.Original'))

    def test_common_ui_uses_pause_controls_and_keeps_stop_leave_available(self):
        ui = source('Windows/MainWindow.cs')
        controls = section(ui, 'private void DrawPrimaryActions(', 'private void DrawSession()')
        self.assertIn('ScenarioPauseControls.Evaluate(game, inSession)', controls)
        self.assertIn('ScenarioPauseControls.Press(game, inSession)', controls)
        self.assertIn('DrawSemanticButton("Stop"', controls)
        self.assertIn('plugin.Runs.Leave()', controls)
        self.assertIn('PAUSED —', controls)
        self.assertIn('simulation statuses stay frozen', controls)


if __name__ == '__main__':
    unittest.main()
