using AnoMech.Scenarios.Dmu.LimitCut;

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
}
