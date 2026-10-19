using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.Pandora;

public sealed class FruAkhMornAi : IScenarioAi
{
    public string Name => "Nukemaru";
    public string Group => "Mana";
    public void Run(SimWorld world)
        => new AiManager(world).Move(0f, () => AiMove.Create(
            new(-4, 4), new(4, 4), new(-4, 4), new(4, 4),
            new(-4, 4), new(4, 4), new(-4, 4), new(4, 4)).NaturalOrder(),
            jitter: 0.15f, arrivalTime: 7f);
}
