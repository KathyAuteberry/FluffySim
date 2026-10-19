using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Upstream.P5FulgentBlade;

namespace AnoMech.Scenarios.Fru.Pandora;

public sealed class FruAkhMornScenario : IScenario
{
    private readonly FruP5FulgentBladeScenario encounter = new();
    public string Name => "Akh Morn";
    public IPhase Phase => FruZone.P5;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruAkhMornAi()];
    public void Run(SimWorld world, int? selectedAi) => encounter.RunAkhMorn(world, selectedAi);
}
