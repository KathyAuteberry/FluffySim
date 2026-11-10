using AnoMech.Core.Game.Ai;
using AnoMech.Scenarios.Tea.P2BjCc;

namespace AnoMech.Tests;

internal sealed class TeaP2BjCcLegacyScenario : TeaP2BjCcScenario
{
    protected override float EndAt => 195.87f;
    public override IReadOnlyList<IScenarioAi> AiStrats =>
        [new TeaP2BjCcLegacyAi(false), new TeaP2BjCcLegacyAi(true)];
}
