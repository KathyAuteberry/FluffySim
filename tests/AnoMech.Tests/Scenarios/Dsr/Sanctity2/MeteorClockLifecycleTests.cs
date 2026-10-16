#pragma warning disable CA1416
using System.Reflection;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Scenarios.Dsr.Sanctity2;

namespace AnoMech.Tests;

public class MeteorClockLifecycleTests
{
    [TestCase(false, 0)] [TestCase(false, 8)] [TestCase(false, 45)]
    [TestCase(true, 0)] [TestCase(true, 8)] [TestCase(true, 45)]
    public void IdleBeforeFirstStartCannotConsumeTowerWindowsOrBotDeadlines(bool hard, int idle)
    {
        var scenario = new DsrSanctity2Scenario();
        var firstSeen = false;
        var secondSeen = false;
        var paused = false;
        FakeGame native = null!;
        var result = ScenarioRun.Execute(typeof(DsrSanctity2Scenario), 0, 37, new()
        {
            BotDifficulty = hard ? BotDifficulty.Hard : BotDifficulty.Easy,
            ScenarioFactory = () => scenario,
            ConfigureNative = f => native = f,
            BeforeStart = (game, fake) =>
            {
                for (var i = 0; i < idle * 60; i++) fake.Frame(1f / 60, game.Tick);
                Assert.That(game.Events.Elapsed, Is.EqualTo(idle).Within(.01));
            },
            Probe = p =>
            {
                if (p.Crossed(1))
                {
                    firstSeen = true;
                    Assert.That(scenario.ActiveTowerVisuals, Has.Count.EqualTo(8));
                    Assert.That(scenario.ActiveTowerVisuals.All(t => !t.SecondWave && t.Until == DsrSanctity2Timing.FirstTowers), Is.True);
                }
                if (p.Crossed(11.65f))
                {
                    Assert.That(scenario.ActiveTowerVisuals, Has.Count.EqualTo(8));
                    Assert.That(scenario.ResolvedTowers, Is.Zero);
                    var before = scenario.ActiveTowerVisuals.ToArray();
                    p.Game.Paused = true;
                    try
                    {
                        for (var i = 0; i < 40; i++) native.Frame(.5f, p.Game.Tick);
                        Assert.That(scenario.ActiveTowerVisuals, Is.EqualTo(before));
                        Assert.That(scenario.ResolvedTowers, Is.Zero);
                        paused = true;
                    }
                    finally { p.Game.Paused = false; }
                }
                if (p.Crossed(12.8f))
                {
                    Assert.That(scenario.ResolvedTowers, Is.EqualTo(8));
                    Assert.That(scenario.ActiveTowerVisuals, Is.Empty);
                }
                if (p.Crossed(25f))
                {
                    secondSeen = true;
                    var before = scenario.ActiveTowerVisuals.ToArray();
                    var elapsed = p.Game.Events.Elapsed;
                    var objects = native.EventObjects.OccupiedSlots;
                    p.Game.Paused = true;
                    try
                    {
                        for (var i = 0; i < 40; i++) native.Frame(.5f, p.Game.Tick);
                        Assert.That(p.Game.Events.Elapsed, Is.EqualTo(elapsed));
                        Assert.That(scenario.ActiveTowerVisuals, Is.EqualTo(before));
                        Assert.That(native.EventObjects.OccupiedSlots, Is.EqualTo(objects));
                    }
                    finally { p.Game.Paused = false; }
                    Assert.That(scenario.ActiveTowerVisuals, Has.Count.EqualTo(8));
                    Assert.That(scenario.ActiveTowerVisuals.All(t => t.SecondWave && Math.Abs(t.Until - DsrSanctity2Timing.SecondTowers) < .001), Is.True);
                }
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(result.Passed, result.ToString);
        Assert.That(firstSeen && secondSeen && paused, Is.True);
        Assert.That(scenario.ResolvedTowers, Is.EqualTo(16));
        Assert.That(scenario.FailedTowers, Is.Zero);
        Assert.That(scenario.ResolvedMeteors, Is.EqualTo(14));
        Assert.That(scenario.Completed, Is.True);
    }
}
