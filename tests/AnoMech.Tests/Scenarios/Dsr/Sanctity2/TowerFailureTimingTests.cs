#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.Sanctity2;

namespace AnoMech.Tests;

public class TowerFailureTimingTests
{
    [TestCase(false)] [TestCase(true)]
    public void LateEntryCannotUndoTheSnapshotButPunishmentWaitsForEternalConviction(bool pause)
    {
        var state = new DsrSanctity2State(new Rng(0),new(),PartyRole.MainTank);
        var role = DsrSanctity2State.Roles.First(r => !state.IsMeteor(r) && !state.TakesKnockback(r));
        var tower = state.FirstPosition(role);
        var scenario = new DsrSanctity2Scenario();
        FakeGame native = null!;
        var checkedSnapshot = false;
        var checkedEffect = false;
        var run = ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new()
        {
            PlayerRole=role,ScenarioFactory=()=>scenario,StopAt=14.7f,ConfigureNative=f=>native=f,
            Takeovers=[new(11.7f,Vector2.Zero),new(11.95f,new(tower.X,tower.Z))],
            Probe=p=>
            {
                if (p.Crossed(12.2f))
                {
                    Assert.That(scenario.FailedTowers,Is.EqualTo(1));
                    Assert.That(p.Member(role)!.IsAlive(),Is.True);
                    checkedSnapshot=true;
                }
                if (p.Crossed(13.8f) && pause)
                {
                    p.Game.Paused=true;
                    try
                    {
                        for (var i=0;i<40;++i) native.Frame(.5f,p.Game.Tick);
                        Assert.That(p.Member(role)!.IsAlive(),Is.True);
                        Assert.That(Failures(p),Is.Empty);
                    }
                    finally { p.Game.Paused=false; }
                }
                if (p.Crossed(14.1f)) Assert.That(Failures(p),Is.Empty);
                if (p.Crossed(14.2f))
                {
                    Assert.That(Failures(p),Has.Count.EqualTo(1));
                    Assert.That(p.Member(role)!.IsAlive(),Is.True);
                    checkedEffect=true;
                }
            },WriteArtifactsOnFailure=false,
        });
        Assert.That(checkedSnapshot && checkedEffect,Is.True,run.ToString);
        Assert.That(run.Deaths,Has.Count.EqualTo(8),run.ToString);
        Assert.That(run.Deaths.All(d=>d.ActionId==25568 && MathF.Abs(d.Time-14.37f)<.05f),Is.True,run.ToString);
    }

    [Test]
    public void StopCancelsPendingPunishmentAndRestartClearsFailedTowerActors()
    {
        var scenario = new DsrSanctity2Scenario();
        var stopped = ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new()
        {
            PlayerRole=PartyRole.MeleeDpsA,ScenarioFactory=()=>scenario,StopAt=13,
            Takeovers=[new(11.7f,Vector2.Zero)],WriteArtifactsOnFailure=false,
        });
        Assert.That(stopped.Passed,stopped.ToString);
        var restarted = ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new()
        {
            ScenarioFactory=()=>scenario,WriteArtifactsOnFailure=false,
        });
        Assert.That(restarted.Passed,restarted.ToString);
        Assert.That(scenario.FailedTowers,Is.Zero);
    }

    private static List<uint> Failures(ScenarioProbe probe) => probe.World.Children.OfType<SimEnemy>()
        .Select(e=>e.Proxy).OfType<FakeBattleChara>().SelectMany(e=>e.ReceivedEffects)
        .Where(e=>e.ActionId==25568).Select(e=>e.ActionId).ToList();
}
