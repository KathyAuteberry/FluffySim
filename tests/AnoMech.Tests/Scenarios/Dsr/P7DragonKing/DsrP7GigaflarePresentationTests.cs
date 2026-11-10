#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P7DragonKing;
using static AnoMech.Scenarios.Dsr.DsrP7Constants;
using Scenario = AnoMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingScenario;

namespace AnoMech.Tests;

public class DsrP7GigaflarePresentationTests
{
    private static readonly uint[] Actions = [ActionId.GigaflaresEdgeFirst, ActionId.GigaflaresEdgeSecond, ActionId.GigaflaresEdgeThird];

    [TestCase(0, true)] [TestCase(1, true)] [TestCase(2, true)]
    [TestCase(0, false)] [TestCase(1, false)] [TestCase(2, false)]
    public void BothCyclesRevealTheRolledOrderBeforeTheFirstExplosion(int first, bool clockwise)
    {
        var step = clockwise ? 1 : 2;
        int[] order = [first, (first + step) % 3, (first + 2 * step) % 3];
        var scenario = new Scenario();
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 9, new()
        {
            ScenarioFactory = () => scenario,
            ConfigureNative = f => f.BattleCharas.PacketSpawnDelayFrames = 5,
            Overrides = o => ((DsrP7DragonKingStateOverrides)o).GigaflareOrder = [order, order],
            Probe = p =>
            {
                for (var cycle = 0; cycle < 2; cycle++)
                {
                    if (!p.Crossed(Scenario.GigaflareCasts[cycle] + .1f)) continue;
                    var casters = p.World.Children.OfType<SimEnemy>()
                        .Where(e => Actions.Contains(e.Casting.ActionId) && e.Casting.IsBusy).ToArray();
                    Assert.That(casters, Has.Length.EqualTo(3));
                    var warningEnds = new List<float>();
                    for (var blast = 0; blast < 3; blast++)
                    {
                        var caster = casters.Single(e => e.Casting.ActionId == Actions[blast]);
                        var cast = ((FakeBattleChara)caster.Proxy!).ReceivedCasts.Last();
                        Assert.That(cast.OmenDelay, Is.EqualTo(blast * 4f).Within(.001f));
                        Assert.That(cast.CastTime, Is.EqualTo(8.7f + blast * 4f).Within(.001f));
                        warningEnds.Add(cast.CastTime - cast.OmenDelay);
                        var spot = scenario.LastState!.GigaflareSpot(cycle, blast);
                        var global = p.World.Coordinates.ToGlobal(new Vector3(spot.X, 0, spot.Y));
                        Assert.That(Vector3.Distance(cast.Position, global), Is.LessThan(.01f));
                        Assert.That(Scenario.GigaflareCasts[cycle] + cast.OmenDelay,
                            Is.LessThan(Scenario.GigaflareHits[cycle][0]));
                        checks++;
                    }
                    Assert.That(warningEnds, Is.All.EqualTo(8.7f).Within(.001f));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(6));
    }

    [Test]
    public void PausingBetweenMarkerRevealsPreservesTheirRemainingDelays()
    {
        var checkedPause = false;
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 0, new()
        {
            StopAt = Scenario.GigaflareCasts[0] + 3,
            Probe = p =>
            {
                if (!p.Crossed(Scenario.GigaflareCasts[0] + 2)) return;
                var helpers = p.World.Children.OfType<SimEnemy>()
                    .Where(e => Actions.Skip(1).Contains(e.Casting.ActionId) && e.Casting.IsBusy).ToArray();
                Assert.That(helpers, Has.Length.EqualTo(2));
                var before = helpers.ToDictionary(e => e, e => ((FakeBattleChara)e.Proxy!).ReceivedCasts.Last());
                var clock = p.Time;
                p.Game.Paused = true;
                for (var tick = 0; tick < 60; tick++) p.Game.Tick(1f / 60);
                Assert.That(p.Time, Is.EqualTo(clock));
                p.Game.Paused = false;
                p.Game.Tick(1f / 60);
                foreach (var helper in helpers)
                {
                    var resumed = ((FakeBattleChara)helper.Proxy!).ReceivedCasts.Last();
                    Assert.That(resumed.OmenDelay, Is.EqualTo(before[helper].OmenDelay - 2f).Within(.06f));
                    Assert.That(resumed.CastTime - resumed.OmenDelay,
                        Is.EqualTo(before[helper].CastTime - before[helper].OmenDelay).Within(.001f));
                }
                checkedPause = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedPause, Is.True);
    }
}
