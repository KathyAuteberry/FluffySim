using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.Pandora;

public sealed class FruPandorasBoxAi : IScenarioAi
{
    public string Name => "Tank LB3";
    public string Group => "Mana";
    public void Run(SimWorld world)
    {
        new AiManager(world).Move(0f, () => AiMove.Create(
            new(-1, 6), new(1, 6), new(-1, 7), new(1, 7),
            new(-1, 8), new(1, 8), new(-1, 9), new(1, 9)).NaturalOrder(), jitter: 0f, arrivalTime: 4f);
        world.Events.Add(FruPandorasBoxScenario.BotLimitBreakAt, () =>
        {
            var role = world.Party.PlayerRole.IsTank() ? world.Party.PlayerRole : PartyRole.MainTank;
            world.Party.UseLimitBreak(role);
        });
    }
}
