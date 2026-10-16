#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.Sanctity2;

namespace AnoMech.Tests;

public class FirstTowerRouteTests
{
    [Test]
    public void EverySideTowerRouteClearsFireAndIceBeforeActivationAndArrivesBeforeSnapshot()
    {
        for (var pattern = 0; pattern < 810; ++pattern)
        {
            var state = new DsrSanctity2State(new Rng(pattern),new(){AdvancedMode=true,TowerPattern=pattern,TargetPair=pattern%12},PartyRole.MainTank);
            foreach (var role in DsrSanctity2State.Roles.Where(r => state.FirstTowers[(int)r] < 12 && state.FirstTowers[(int)r]%3 != 1))
            {
                var route = DsrSanctity2Ai.FirstTowerRoute(state,role);
                var distance = 0f;
                for (var i = 1; i < route.Count; ++i)
                {
                    var length = Vector3.Distance(route[i-1],route[i]);
                    for (var step = 0; step <= 20; ++step)
                    {
                        var fraction = step/20f;
                        var position = Vector3.Lerp(route[i-1],route[i],fraction);
                        foreach (var quadrant in Enumerable.Range(0,4))
                            Assert.That(Vector3.Distance(position,DsrSanctity2Scenario.FirePosition(quadrant)),Is.GreaterThan(7),$"pattern {pattern}, {role}");
                        if (8.05f+(distance+length*fraction)/6 >= DsrSanctity2Timing.IceActivation)
                            Assert.That(Vector3.Distance(position,state.PairPosition(role)),Is.GreaterThan(7));
                    }
                    distance += length;
                }
                Assert.That(8.05f+distance/6,Is.LessThan(11.7f));
                Assert.That(route[1].X*state.PairPosition(role).Z-route[1].Z*state.PairPosition(role).X,Is.Not.Zero);
            }
        }
    }

    [TestCase(BotDifficulty.Easy)]
    [TestCase(BotDifficulty.Hard)]
    public void CurvedRoutesClearWithContinuousHazardsAndReachTheirClaims(BotDifficulty difficulty)
    {
        for (var seed = 0; seed < 40; ++seed)
        {
            var scenario = new DsrSanctity2Scenario();
            var run = ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,seed,new()
            {
                ScenarioFactory=()=>scenario, BotDifficulty=difficulty,
                Probe=p=>
                {
                    if (!p.Crossed(11.6f)) return;
                    var state = scenario.LastState!;
                    foreach (var role in DsrSanctity2State.Roles)
                        Assert.That(Vector3.Distance(p.Member(role)!.Position,state.FirstPosition(role)),Is.LessThan(.1f),$"{difficulty}, seed {seed}, {role}");
                },
                WriteArtifactsOnFailure=false,
            });
            Assert.That(run.Passed,Is.True,run.ToString);
        }
    }
}
