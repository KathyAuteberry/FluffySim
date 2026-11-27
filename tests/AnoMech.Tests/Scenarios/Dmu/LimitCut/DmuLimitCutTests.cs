using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dmu.LimitCut;
using AnoMech.Scenarios.Umad;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

public class DmuLimitCutTests
{
    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    public void EitherTankCanHoldChaos(int seed, bool mainTankOnChaos)
    {
        var run = ScenarioRun.Execute(typeof(DmuLimitCutScenario), 0, seed, new()
        {
            Overrides = o => ((DmuLimitCutStateOverrides)o).MainTankOnChaos = mainTankOnChaos,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void CyclonesWithoutATankLimitBreakKillTheStack()
        => Negative<DmuLimitCutScenario>(CasterDps)
            .At(14.0f, p => p.World.Party.LimitBreak.Set(0f))
            .ShouldKill(UmadConstants.ActionId.Cyclone, PerRole.All);
}
