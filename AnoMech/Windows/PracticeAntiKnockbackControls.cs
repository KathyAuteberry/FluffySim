using AnoMech.Core.Game;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.Sanctity2;

namespace AnoMech.Windows;

internal static class PracticeAntiKnockbackControls
{
    internal static PracticeInvulnerabilityControls Evaluate(IScenario? scenario, bool alive, PracticeAction action)
    {
        var visible = scenario is DsrSanctity2Scenario;
        return new(visible, visible && alive && action.Ready,
            !alive ? "Unavailable — KO" : !action.Enabled ? "Used — restart to reset"
            : action.Active ? $"Active — {action.Remaining:F1}s remaining"
            : action.Ready ? "Ready — 6 seconds, once per run" : "Used — restart to reset");
    }
    internal static bool Press(IScenario? scenario, bool alive, PracticeAction action)
        => Evaluate(scenario, alive, action).CanActivate && action.Activate(true);
}
