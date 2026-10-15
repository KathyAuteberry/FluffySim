using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr;

public sealed class DsrArenaDiagnosticScenario : IScenario
{
    public string Name => "P2 arena inspection";
    public IPhase Phase => DsrZone.P2;
    public bool SupportsSolo => true;
    public bool EnablePhaseBgm => false;
    public IReadOnlyList<IScenarioAi> AiStrats => [];
    public void Run(SimWorld world, int? selectedAi) { }
}
