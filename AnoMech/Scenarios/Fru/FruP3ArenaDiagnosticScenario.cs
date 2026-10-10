using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru;

public sealed class FruP3ArenaDiagnosticScenario : IScenario
{
    public string Name => "P3 arena inspection";
    public IPhase Phase => FruZone.P3;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [];
    public void Run(SimWorld world, int? selectedAi) { }
}
