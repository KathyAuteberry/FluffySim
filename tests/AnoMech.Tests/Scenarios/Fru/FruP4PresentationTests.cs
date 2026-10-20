using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Darklit;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

namespace AnoMech.Tests;

public class FruP4PresentationTests
{
    [TestCase(typeof(FruDarklitScenario), 0)]
    [TestCase(typeof(FruP4CrystallizeTimeScenario), 1)]
    public void BothP4DrillsRegisterCrystalAndBothVisionActors(Type type, int strat)
    {
        var checkedActors = false;
        var run = ScenarioRun.Execute(type, strat, 0, new()
        {
            StopAt = 1f,
            Probe = p =>
            {
                if (!p.Crossed(.5f)) return;
                var actors = p.World.Children.OfType<SimEnemy>().ToArray();
                foreach (uint id in new uint[] {17841, 17844, 17845})
                {
                    var actor = actors.Single(a => a.SpawnConfig.BNpcBaseId == id);
                    Assert.That(actor.Proxy?.Exists, Is.True);
                    Assert.That(actor.SpawnConfig.IsVisible, Is.True);
                    if (id == 17841) Assert.That(actor.SpawnConfig.DisplayFlags, Is.Null);
                    else Assert.That(actor.SpawnConfig.Appearance, Is.Not.Null);
                }
                checkedActors = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedActors, Is.True);
    }
}
