#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.Sanctity2;
using AnoMech.Windows;

namespace AnoMech.Tests;

public class SharedPracticeTests
{
    public static IEnumerable<TestCaseData> AllScenarios() => ScenarioCatalog.Create().Scenarios
        .Where(s => s.Phase.Zone is AnoMech.Scenarios.Fru.FruZone or AnoMech.Scenarios.Dsr.DsrZone)
        .Select(s => new TestCaseData(s.GetType()).SetName("SharedPractice_" + s.GetType().Name));

    public static IEnumerable<TestCaseData> DelayedMovementScenarios() => AllScenarios()
        .Where(t => t.Arguments[0] is Type type && type != typeof(AnoMech.Scenarios.Fru.FallOfFaith.FruFallOfFaithScenario));

    [TestCaseSource(nameof(AllScenarios))]
    public void HardInvisibleBotsClearAllEightSeats(Type type)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            for (var seed = 0; seed < 8; seed++)
            {
                var delayed = 0;
                var run = ScenarioRun.Execute(type,0,seed,new()
                {
                    PlayerRole=role, BotDifficulty=BotDifficulty.Hard, ShowSimulatedParty=false,
                    Probe=p =>
                    {
                        delayed = Math.Max(delayed,p.World.BotMovement.DelayedMoves);
                        foreach (var departure in p.World.BotMovement.Departures.Where(d => d.DepartAt > d.RequestedAt))
                        {
                            Assert.That(departure.Speed, Is.LessThanOrEqualTo(7.8f));
                            Assert.That(departure.DepartAt + departure.Distance/departure.Speed, Is.LessThanOrEqualTo(departure.Deadline!.Value-.44f));
                        }
                        foreach(var bot in p.World.Party.ActiveMembers().OfType<SimPartyNpc>())
                        {
                            Assert.That(bot.PresentationVisible,Is.False);
                            Assert.That(((FakeBattleChara)bot.Proxy!).ModelHidden,Is.True);
                            Assert.That(bot.IsActive,Is.True);
                        }
                        Assert.That(((FakeBattleChara)p.World.Party.Player!.Proxy!).ModelHidden,Is.False);
                    }, WriteArtifactsOnFailure=false,
                });
                Assert.That(run.Passed,run.ToString);
                Assert.That(delayed, type == typeof(AnoMech.Scenarios.Fru.FallOfFaith.FruFallOfFaithScenario) ? Is.Zero : Is.GreaterThan(0), type.Name);
            }
    }

    [TestCaseSource(nameof(AllScenarios))]
    public void GodModeRecordsHumanMistakesAndCompletesWithoutNativeHpChanges(Type type)
    {
        var diagnosed = false; var observed = false;
        var scenario=(IScenario)Activator.CreateInstance(type)!;
        var run = ScenarioRun.Execute(type,0,2,new()
        {
            ScenarioFactory=()=>scenario,GodMode=true, BotDifficulty=BotDifficulty.Hard, ShowSimulatedParty=false,
            Probe=p =>
            {
                if(p.Crossed(2f))
                {
                    var player=p.World.Party.Player!;var proxy=player.Proxy!;var hp=proxy.Health;
                    player.Die(1,"Deliberate practice mistake");
                    Assert.That(proxy.Health,Is.EqualTo(hp));
                    Assert.That(player.Dead,Is.False);
                    Assert.That(Plugin.Game.HasScenarioMistake,Is.True);
                    observed=true;
                }
                diagnosed |= Plugin.Game.HasScenarioMistake;
            }, WriteArtifactsOnFailure=false,
        });
        Assert.That(observed && diagnosed,Is.True);
        Assert.That(run.Deaths,Is.Empty);
        Assert.That(type.GetProperty("Completed")!.GetValue(scenario),Is.True);
        Assert.That(run.Elapsed,Is.GreaterThan(20));
    }

    [TestCase(false)] [TestCase(true)]
    public void VisibilityIsIndependentFromDifficultyAndRestoresOnReentry(bool hard)
    {
        foreach(var show in new[]{false,true})
        {
            var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,7,new()
            {
                BotDifficulty=hard?BotDifficulty.Hard:BotDifficulty.Easy,ShowSimulatedParty=show,
                Probe=p=>
                {
                    foreach(var bot in p.World.Party.ActiveMembers().OfType<SimPartyNpc>())
                        Assert.That(((FakeBattleChara)bot.Proxy!).ModelHidden,Is.EqualTo(!show));
                    foreach(var boss in p.World.Children.OfType<SimEnemy>())
                        Assert.That(((FakeBattleChara)boss.Proxy!).ModelHidden,Is.False);
                }, WriteArtifactsOnFailure=false,
            });
            Assert.That(run.Passed,run.ToString);
        }
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void VirtualAntiKnockbackProtectsOnlyWithinItsActualWindow(int activation)
    {
        var scenario=new DsrSanctity2Scenario();Vector3 before=default;Vector3 after=default;
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,1,new()
        {
            ScenarioFactory=()=>scenario,PlayerRole=PartyRole.MainTank,StopAt=22.55f,
            Overrides=o=>((DsrSanctity2StateOverrides)o).PracticeAssignment=MeteorPractice.Meteor,
            Takeovers=[new(21.4f,null)],
            Probe=p=>
            {
                if(p.Crossed(22.2f)) before=p.World.Party.Player!.Position;
                if(p.Crossed(activation==2?15f:21.8f) && activation!=0)
                    Assert.That(PracticeAntiKnockbackControls.Press(Plugin.Game.ActiveScenario,true,p.World.PracticeAntiKnockback),Is.True);
                if(p.Crossed(22.53f)) after=p.World.Party.Player!.Position;
            },WriteArtifactsOnFailure=false,
        });
        Assert.That(Vector3.Distance(before,after),activation==1?Is.LessThan(.01f):Is.GreaterThan(1));
    }

    [Test]
    public void HardActuallyWaitsWhereEasyHasAlreadyWalked()
    {
        var travel = new Dictionary<BotDifficulty,float>();
        foreach(var difficulty in new[]{BotDifficulty.Easy,BotDifficulty.Hard})
        {
            Vector3 start=default;
            ScenarioRun.Execute(typeof(AnoMech.Scenarios.Dsr.Strength.DsrStrengthScenario),0,0,new()
            {
                BotDifficulty=difficulty,StopAt=12.6f,
                Probe=p=>
                {
                    if(p.Crossed(11.4f))start=p.World.Party.Get(PartyRole.MeleeDpsA)!.Position;
                    if(p.Crossed(12.5f))travel[difficulty]=Vector3.Distance(start,p.World.Party.Get(PartyRole.MeleeDpsA)!.Position);
                },WriteArtifactsOnFailure=false,
            });
        }
        Assert.That(travel[BotDifficulty.Easy],Is.GreaterThan(5));
        Assert.That(travel[BotDifficulty.Hard],Is.LessThan(.01f));
    }

    [Test]
    public void InvisiblePartyKeepsTheHumanSoftmarkerAndSuppressesBotOnlyIcons()
    {
        var observed=false;
        ScenarioRun.Execute(typeof(AnoMech.Scenarios.Fru.UltimateRelativity.FruUltimateRelativityScenario),0,0,new()
        {
            ShowSimulatedParty=false,StopAt=15,
            Probe=p=>{if(p.Crossed(14.5f))
            {
                Assert.That(p.World.SoftMarkers.Snapshot(p.World.ScenarioOrigin).Count,Is.EqualTo(8));
                Assert.That(p.World.SoftMarkers.Snapshot(p.World.ScenarioOrigin,false).Count,Is.EqualTo(1));
                observed=true;
            }},WriteArtifactsOnFailure=false,
        });
        Assert.That(observed,Is.True);
    }

    [Test]
    public void PracticeActionBoundaryHasNoNativeActionOrCooldownDependency()
    {
        var directory=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(directory is not null && !File.Exists(Path.Combine(directory.FullName,"AnoMech","Core","Game","PracticeAction.cs")))directory=directory.Parent;
        Assert.That(directory,Is.Not.Null);
        var source=File.ReadAllText(Path.Combine(directory!.FullName,"AnoMech","Core","Game","PracticeAction.cs"));
        foreach(var forbidden in new[]{"Natives","ActionManager","Recast","SendPacket","FFXIVClientStructs"})Assert.That(source,Does.Not.Contain(forbidden));
        var anti=File.ReadAllText(Path.Combine(directory.FullName,"AnoMech","Core","UserActions","AntiKnockbackHandler.cs"));
        Assert.That(anti,Does.Not.Contain("ActionManager"));Assert.That(anti,Does.Not.Contain("Recast"));
    }

    [Test]
    public void GodModeTeardownDoesNotHealAnUntouchedRealHpBar()
    {
        FakeGame? fake=null;uint observedHp=0;
        ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new()
        {
            GodMode=true,StopAt=2,ConfigureNative=f=>fake=f,
            Probe=p=>{if(p.Crossed(1))
            {
                var player=p.World.Party.Player!;
                fake!.BattleCharas.Player.Health=12345;observedHp=player.Proxy.Health;
                player.Die(1,"God Mode should never touch real HP");
            }},WriteArtifactsOnFailure=false,
        });
        Assert.That(fake!.BattleCharas.Player.Health,Is.EqualTo(observedHp));
    }

    [Test]
    public void GodModeKeepsEvaluatingPersistentMistakesWithoutPerFrameChatFlood()
    {
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new()
        {
            GodMode=true,StopAt=4,
            Probe=p=>{if(p.World.Events.Elapsed is >=1 and <3)p.World.Party.Player!.Die(1,"Persistent hazard");},
            WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Deaths,Is.Empty);
        Assert.That(run.Warnings.Count(w=>w.Contains("Persistent hazard")),Is.InRange(2,3));
    }

    [Test]
    public void DeparturePlanningUsesTravelDistanceAndNeverRaisesSpeed()
    {
        Assert.That(BotMovement.DepartureDelay(12,6,5),Is.EqualTo(2.55f).Within(.001));
        Assert.That(BotMovement.DepartureDelay(30,6,5),Is.Zero);
        Assert.That(BotMovement.DepartureDelay(36,6,5),Is.Zero);
    }

    [Test]
    public void SharedDefaultsAndOldSettingsRemainEasyVisibleAndNormal()
    {
        var config=Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>("{}")!;
        Assert.That(config.BotDifficulty,Is.EqualTo(BotDifficulty.Easy));
        Assert.That(config.ShowSimulatedParty,Is.True);Assert.That(config.PracticeGodMode,Is.False);
    }

    [Test]
    public void PuddlePresentationUsesObservedActorArrivalWindows()
    {
        var scenario=new DsrSanctity2Scenario();var checkedOrder=false;
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new()
        {
            ScenarioFactory=()=>scenario,
            Probe=p=>{ if(p.Crossed(9.1f))
            {
                var fire=scenario.PuddlePresentation.Where(x=>x.Object==0x1EB686).ToArray();
                var ice=scenario.PuddlePresentation.Where(x=>x.Object==0x1EB682).ToArray();
                Assert.That(fire.Length,Is.EqualTo(4));Assert.That(ice.Length,Is.EqualTo(4));
                Assert.That(ice.Min(x=>x.Time)-fire.Max(x=>x.Time),Is.EqualTo(DsrSanctity2Timing.IcePresentation-DsrSanctity2Timing.FirePresentation).Within(.03f));
                Assert.That(ice.Min(x=>x.Time),Is.EqualTo(DsrSanctity2Timing.IcePresentation).Within(.03f));checkedOrder=true;
            }},WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,run.ToString);Assert.That(checkedOrder,Is.True);
    }

    [Test]
    public void VirtualAntiKnockbackNeedsNoJobLevelStatusOrNativeEffect()
    {
        var scenario=new DsrSanctity2Scenario();var observed=false;
        ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new()
        {
            ScenarioFactory=()=>scenario,PlayerRole=PartyRole.CasterDps,StopAt=2,
            Probe=p=>{if(p.Crossed(1))
            {
                var action=p.World.PracticeAntiKnockback;var player=p.World.Party.Player!;
                var proxy=(FakeBattleChara)player.Proxy!;var effects=proxy.ReceivedEffects.Count;var statuses=player.ActiveStatuses.ToArray();
                Assert.That(PracticeAntiKnockbackControls.Evaluate(Plugin.Game.ActiveScenario,true,action).CanActivate,Is.True);
                Assert.That(PracticeAntiKnockbackControls.Press(scenario,true,action),Is.True);
                Assert.That(proxy.ReceivedEffects.Count,Is.EqualTo(effects));Assert.That(player.ActiveStatuses,Is.EqualTo(statuses));
                Assert.That(action.Remaining,Is.EqualTo(6));Assert.That(action.Activate(true),Is.False);
                action.Tick(6.1f);Assert.That(action.Active,Is.False);action.Start();Assert.That(action.Ready,Is.True);
                observed=true;
            }},WriteArtifactsOnFailure=false,
        });
        Assert.That(observed,Is.True);
    }
}
