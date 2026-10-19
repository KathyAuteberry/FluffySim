using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Pandora;

namespace AnoMech.Tests;

public class FruPandoraSequenceTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void SequenceReusesOnePandoraAndOnlyWipesWhenTerminalResolutionIsEnabled(bool enrage)
    {
        var seenStages = new HashSet<int>();
        var run = ScenarioRun.Execute(typeof(FruPandoraSequenceScenario), 0, 0, new ScenarioRunOptions
        {
            StopAt = 269f,
            Overrides = value => ((FruPandoraSequenceOverrides)value).ResolveTerminalEnrage = enrage,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                foreach (var time in new[] { 1, 40, 97, 115, 157, 206, 248 })
                {
                    if (!p.Crossed(time)) continue;
                    seenStages.Add(time);
                    Assert.That(p.World.Children.OfType<SimEnemy>().Count(enemy =>
                        enemy.SpawnConfig.BNpcBaseId == 17839 && enemy.IsActive), Is.EqualTo(1));
                }
            },
        });
        Assert.That(seenStages, Has.Count.EqualTo(7));
        if (enrage)
        {
            Assert.That(run.Deaths, Has.Count.EqualTo(8), run.ToString);
            Assert.That(run.Deaths.All(d => d.ActionId == 40328), Is.True, run.ToString);
        }
        else Assert.That(run.Passed, run.ToString);
    }
}
