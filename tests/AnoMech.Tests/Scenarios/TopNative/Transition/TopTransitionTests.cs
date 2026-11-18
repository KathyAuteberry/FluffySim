using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.TopNative.Transition;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

public class TopTransitionTests
{
    [Test]
    public void StayingInTheLineUpMeetsTheWaveRepeater()
        => Negative<TopTransitionScenario>(MainTank)
            .FreezeAt(TopTransitionScenario.DebuffsAt - 1f)
            .ShouldKill(ActionId.WaveRepeater2, MainTank);
}
