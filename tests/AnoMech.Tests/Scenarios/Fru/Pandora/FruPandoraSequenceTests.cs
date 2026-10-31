using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Pandora;
using AnoMech.Scenarios.Fru.Upstream.P5FulgentBlade;

namespace AnoMech.Tests;

public class FruPandoraSequenceTests
{
    [TestCase(0)]
    [TestCase(2)]
    public void FirstFulgentKeepsItsRemainingExalinesDuringParadise(int seed)
    {
        List<(float Time, string Action)> Exalines(Type scenario)
        {
            FakeGame? native = null;
            var run = ScenarioRun.Execute(scenario, 0, seed, new ScenarioRunOptions
            {
                StopAt = 43f,
                ConfigureNative = fake => native = fake,
                WriteArtifactsOnFailure = false,
                Probe = p =>
                {
                    if (scenario == typeof(FruPandoraSequenceScenario) && p.Crossed(42f))
                        Assert.That(p.World.Children.OfType<SimEnemy>().Count(), Is.EqualTo(11));
                },
            });
            Assert.That(run.Passed, run.ToString);
            return native!.Timeline.Rows
                .Where(row => row["kind"] == "ability" && row["id"] is "40308" or "40309")
                .Select(row => (float.Parse(row["t"], System.Globalization.CultureInfo.InvariantCulture), row["id"])).ToList();
        }

        var isolated = Exalines(typeof(FruP5FulgentBladeScenario));
        var continuous = Exalines(typeof(FruPandoraSequenceScenario));
        Assert.That(isolated.Any(effect => effect.Time > 37.35f), Is.True);
        Assert.That(continuous.Select(effect => effect.Action), Is.EqualTo(isolated.Select(effect => effect.Action)));
        for (var i = 0; i < isolated.Count; i++)
            Assert.That(continuous[i].Time, Is.EqualTo(isolated[i].Time).Within(2 * ScenarioRun.FrameSeconds));
    }

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
                    if (enrage && time == 248)
                        Assert.That(p.World.Children.OfType<SimEnemy>().Single(enemy => enemy.CastActionId == 40328).CastTotalSeconds,
                            Is.EqualTo(21.2f).Within(0.001f));
                }
            },
        });
        Assert.That(seenStages, Has.Count.EqualTo(7));
        if (enrage)
        {
            Assert.That(run.Deaths, Has.Count.EqualTo(8), run.ToString);
            Assert.That(run.Deaths.All(d => d.ActionId == 40328), Is.True, run.ToString);
            Assert.That(run.Deaths.All(d => MathF.Abs(d.Time - FruPandoraSequenceScenario.EnrageHitAt) < 3 * ScenarioRun.FrameSeconds), Is.True, run.ToString);
        }
        else Assert.That(run.Passed, run.ToString);
    }
}
