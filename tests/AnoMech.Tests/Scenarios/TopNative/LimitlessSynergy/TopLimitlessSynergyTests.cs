using AnoMech.Scenarios.TopNative.LimitlessSynergy;

namespace AnoMech.Tests;

public class TopLimitlessSynergyTests
{
    [Test]
    public void TheRunEndsShortlyAfterCosmoMemory()
    {
        var run = ScenarioRun.Execute(typeof(TopLimitlessSynergyScenario), 0, Random.Shared.Next(), new());
        Assert.That(run.Passed, run.ToString);
        Assert.That(run.Elapsed, Is.LessThan(TopLimitlessSynergyScenario.CosmoMemoryHit + 8f), run.ToString);
    }
}
