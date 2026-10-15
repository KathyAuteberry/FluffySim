using System.Collections.Generic;
using AnoMech.Core;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru;

public sealed class FruArenaDiagnosticScenario : IScenario
{
    public string Name => "Arena diagnostic (P2 initialization unresolved)";
    public IPhase Phase => FruZone.P2;
    public bool SupportsSolo => true;
    public bool EnablePhaseBgm => false;
    public IReadOnlyList<IScenarioAi> AiStrats => [];

    public void Run(SimWorld world, int? selectedAi)
    {
        world.Events.Add(1f, () => InspectArena(world, "FRU arena-only +1s"));
        world.Events.Add(3f, () => InspectArena(world, "FRU arena-only +3s"));
    }

    private static void InspectArena(SimWorld world, string label)
    {
        DiagnosticLog.Info($"[{label}] layers: {Natives.Layout.DescribeActiveLayers()}");
        world.Map.LogArena(label);
    }
}
