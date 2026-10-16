#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios.Dsr.Sanctity2;

namespace AnoMech.Tests;

public class NativeTowerPresentationTests
{
    private const string TowerPath = "vfx/omen/eff/d1025_sp012_bunsan01s.avfx";

    [TestCase(false)] [TestCase(true)]
    public void BothWavesUseTheActionsNativeAssetAtWorldPositionsAndRetainOwnershipDuringPause(bool hard)
    {
        var scenario = new DsrSanctity2Scenario();
        FakeVfxFunctions fx = null!;
        FakeGame native = null!;
        var pauses = 0;
        var run = ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,37,new()
        {
            ScenarioFactory=()=>scenario,
            BotDifficulty=hard ? BotDifficulty.Hard : BotDifficulty.Easy,
            ConfigureNative=f=> { native=f; fx=(FakeVfxFunctions)Natives.Vfx; },
            Probe=p=>
            {
                if (p.Crossed(1) || p.Crossed(16))
                {
                    Assert.That(scenario.ActiveNativeTowerCount,Is.EqualTo(8));
                    var current=fx.StaticSpawns.Where(s=>s.Handle.RemoveCalls==0).ToArray();
                    Assert.That(current,Has.Length.EqualTo(8));
                    Assert.That(current.All(s=>s.Path==TowerPath && s.Scale==new Vector3(3,1,3)),Is.True);
                    Assert.That(current.Select(s=>s.Placement.Position),Is.EquivalentTo(scenario.ActiveTowerVisuals.Select(t=>t.Position+p.World.ScenarioOrigin)));
                }
                if (p.Crossed(DsrSanctity2Timing.FirstTowers-.1f) || p.Crossed(DsrSanctity2Timing.SecondTowers-.1f))
                {
                    var live=fx.StaticSpawns.Where(s=>s.Handle.RemoveCalls==0).ToArray();
                    var elapsed=p.Game.Events.Elapsed;
                    p.Game.Paused=true;
                    try
                    {
                        for (var i=0;i<40;i++) native.Frame(.5f,p.Game.Tick);
                        Assert.That(p.Game.Events.Elapsed,Is.EqualTo(elapsed));
                        Assert.That(scenario.ActiveNativeTowerCount,Is.EqualTo(8));
                        Assert.That(live.All(s=>s.Handle.RemoveCalls==0),Is.True);
                        ++pauses;
                    }
                    finally { p.Game.Paused=false; }
                }
                if (p.Crossed(12.2f))
                {
                    Assert.That(scenario.ActiveNativeTowerCount,Is.Zero);
                    Assert.That(fx.StaticSpawns,Has.Count.EqualTo(8));
                    Assert.That(fx.StaticSpawns.All(s=>s.Handle.RemoveCalls==1),Is.True);
                }
                if (p.Crossed(25.6f)) Assert.That(scenario.ActiveNativeTowerCount,Is.Zero);
            },WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,Is.True,run.ToString);
        Assert.That(pauses,Is.EqualTo(2));
        Assert.That(fx.StaticSpawns,Has.Count.EqualTo(16));
        Assert.That(fx.StaticSpawns.All(s=>s.Handle.RemoveCalls==1),Is.True);
    }

    [TestCase(1f)] [TestCase(12f)] [TestCase(13f)] [TestCase(16f)] [TestCase(25.3f)] [TestCase(27f)]
    public void StopAndRestartReleaseEveryNativeHandleExactlyOnce(float stop)
    {
        var scenario=new DsrSanctity2Scenario();
        for(var iteration=0;iteration<3;iteration++)
        {
            FakeVfxFunctions fx=null!;
            var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,iteration,new()
            {
                ScenarioFactory=()=>scenario,StopAt=stop,
                ConfigureNative=f=> { f.BattleCharas.PacketSpawnDelayFrames=5; fx=(FakeVfxFunctions)Natives.Vfx; },
                WriteArtifactsOnFailure=false,
            });
            Assert.That(run.Passed,Is.True,run.ToString);
            Assert.That(fx.StaticSpawns,Is.Not.Empty);
            Assert.That(fx.StaticSpawns.All(s=>s.Handle.RemoveCalls==1),Is.True);
            Assert.That(scenario.ActiveNativeTowerCount,Is.Zero);
        }
    }

    [Test]
    public void NativeAllocationFailureDoesNotChangeMechanics()
    {
        var scenario=new DsrSanctity2Scenario();
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,37,new()
        {
            ScenarioFactory=()=>scenario,
            ConfigureNative=_=>((FakeVfxFunctions)Natives.Vfx).FailStaticSpawns=true,
            WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,Is.True,run.ToString);
        Assert.That(scenario.ResolvedTowers,Is.EqualTo(16));
        Assert.That(scenario.ActiveNativeTowerCount,Is.Zero);
    }
}
