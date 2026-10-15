using System;
using AnoMech.Core.Native.Implementations;

namespace AnoMech.Core.UserActions;

// Fresh-pull resources; defensive recasts are reset separately by the scoped practice session.
internal sealed class ResetHandler : IUserActionHandler
{
    private readonly Random rng = new();

    public bool RunsWhenDisabled => true;

    public void OnScenarioStart()
    {
        if (ZoneControl.Current?.NativePracticeReady != true) return;
        ClearGauge();
        if (Plugin.Game?.Player is { } player) JobActions.ApplyStartingResources(player, rng);
    }

    // Its own scope: the seed opens gauge edits of its own.
    private static void ClearGauge()
    {
        using var edit = JobGaugeEdit.Begin();
        edit.Clear();
    }
}
