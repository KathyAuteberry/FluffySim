using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained;

namespace AnoMech.Scenarios.Fru.Pandora;

public sealed class FruPolarizingStrikesScenario : IScenario
{
    public bool SupportsMultiplayer => true;
    private readonly FruP5ParadiseRegainedScenario encounter = new();
    public string Name => "Polarizing Strikes";
    public IPhase Phase => FruZone.P5;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP5ParadiseRegainedAi(true)];
    public void Run(SimWorld world, int? selectedAi) => encounter.Run(world, selectedAi, true);
    public void Tick(float delta, float elapsed) => encounter.Tick(delta, elapsed);
}
