using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.Strength2;

namespace AnoMech.Tests;

public class DsrStrength2Tests
{
    private static IEnumerable<TestCaseData> Patterns() => from pattern in Enumerable.Range(0,8)
        from trio in Enumerable.Range(0,20) from order in Enumerable.Range(0,6) select new TestCaseData(pattern,trio,order);
    [TestCaseSource(nameof(Patterns))]
    public void FullFragmentClearsAllOrientationsAndBlueAssignments(int pattern,int trio,int order)
    {
        var completed=false;
        var run=ScenarioRun.Execute(typeof(DsrStrength2Scenario),0,trio*6+order,new()
        {
            ScenarioFactory=()=>new DsrStrength2Scenario(),
            Overrides=o=> { var s=(DsrStrength2StateOverrides)o; s.KnightPattern=pattern;s.LeapTrio=trio;s.LeapOrder=order; },
            Probe=p=> { if(p.Crossed(21.1f)) completed=((DsrStrength2Scenario)p.Game.ActiveScenario!).Completed; },
            WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,run.ToString); Assert.That(completed,Is.True);
    }
    [Test]
    public void AssignmentModelIsDeterministicAndDisjoint()
    {
        for(var seed=0;seed<200;++seed)
        {
            var a=new DsrStrength2State(new Rng(seed),new());var b=new DsrStrength2State(new Rng(seed),new());
            Assert.That(b.Leaps,Is.EqualTo(a.Leaps));Assert.That(b.TowerCommunicators,Is.EqualTo(a.TowerCommunicators));
            Assert.That(b.InitialTetherTargets,Is.EqualTo(a.InitialTetherTargets));
            Assert.That(a.Leaps.Concat(a.TowerCommunicators).Distinct().Count(),Is.EqualTo(6));
            Assert.That(a.InitialTetherTargets.Append(a.RageTarget).ToHashSet().SetEquals(a.TowerCommunicators),Is.True);
            Assert.That(a.Leaps.All(r=>!r.IsTank()),Is.True);
        }
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void RelativeGeometrySeparatesLeapsAndCrossesTanks(int pattern)
    {
        var s=new DsrStrength2State(new Rng(0),new(){KnightPattern=pattern});
        Assert.That(Vector3.Dot(s.WestKnight,s.Rotate(Vector3.UnitX)),Is.LessThan(0));
        Assert.That(Vector3.Dot(s.TankPosition(PartyRole.MainTank),s.Rotate(Vector3.UnitX)),Is.GreaterThan(0));
        Assert.That(Vector3.Dot(s.TankPosition(PartyRole.OffTank),s.Rotate(Vector3.UnitX)),Is.LessThan(0));
        foreach(var role in s.Leaps)
        {
            Assert.That(Vector3.Distance(s.LeapPosition(role),s.StackPosition),Is.GreaterThan(24));
            foreach(var other in s.Leaps.Where(r=>r!=role)) Assert.That(Vector3.Distance(s.LeapPosition(role),s.LeapPosition(other)),Is.GreaterThan(24));
        }
        foreach(var role in DsrStrength2State.NonTanks) Assert.That(s.TowerPosition(role).Length(),Is.EqualTo(12).Within(.001));
        foreach(var role in s.TowerCommunicators)
            Assert.That(Vector3.Distance(s.CommunicationPosition(role), s.StackPosition), Is.GreaterThanOrEqualTo(1.99f));
    }

    [TestCase(false, PartyRole.RegenHealer)] [TestCase(true, PartyRole.RegenHealer)]
    [TestCase(false, PartyRole.ShieldHealer)] [TestCase(true, PartyRole.ShieldHealer)]
    [TestCase(false, PartyRole.MeleeDpsA)] [TestCase(true, PartyRole.MeleeDpsA)]
    [TestCase(false, PartyRole.MeleeDpsB)] [TestCase(true, PartyRole.MeleeDpsB)]
    [TestCase(false, PartyRole.PhysRangedDps)] [TestCase(true, PartyRole.PhysRangedDps)]
    [TestCase(false, PartyRole.CasterDps)] [TestCase(true, PartyRole.CasterDps)]
    public void HumanReadsFixedBotClaimsAndCannotMakeThemYield(bool contest, PartyRole human)
    {
        var trio = Array.FindIndex(DsrStrength2State.LeapTrios, roles => !roles.Contains(human));
        var unmarked = DsrStrength2State.NonTanks.Except(DsrStrength2State.LeapTrios[trio]).ToArray();
        var communication = Array.FindIndex(DsrStrength2State.Orders, order => unmarked[order[1]] == human);
        var state = new DsrStrength2State(new Rng(0),new(){KnightPattern=3,LeapTrio=trio,LeapOrder=0,TowerCommunication=communication,TetherTargets=0});
        var bots = state.TowerCommunicators.Where(r=>r!=human).ToArray();
        var claims = bots.Select(state.TowerPosition).ToArray();
        var destination = contest ? claims[0] : state.TowerPosition(human);
        var observed = 0;
        var run = ScenarioRun.Execute(typeof(DsrStrength2Scenario),0,0,new()
        {
            ScenarioFactory=()=>new DsrStrength2Scenario(),PlayerRole=human,
            Overrides=o=> {var a=(DsrStrength2StateOverrides)o;a.KnightPattern=3;a.LeapTrio=trio;a.LeapOrder=0;a.TowerCommunication=communication;a.TetherTargets=0;},
            Takeovers=[new(8.6f,new Vector2(state.CommunicationPosition(bots[0]).X,state.CommunicationPosition(bots[0]).Z)),
                new(11.5f,new Vector2(state.StackPosition.X,state.StackPosition.Z)),new(14.9f,new Vector2(destination.X,destination.Z))],
            Probe=p=>
            {
                if(p.Crossed(9.8f)||p.Crossed(10.4f))
                {
                    ++observed;
                    foreach(var bot in bots) Assert.That(Vector3.Distance(p.World.Party.Get(bot)!.Position,state.CommunicationPosition(bot)),Is.LessThan(.05f));
                }
                if(p.Crossed(15.5f))
                    for(var n=0;n<bots.Length;++n) Assert.That(Vector3.Distance(p.World.Party.Get(bots[n])!.Position,claims[n]),Is.LessThan(.05f));
            },WriteArtifactsOnFailure=false,
        });
        Assert.That(observed,Is.EqualTo(2));
        if(contest) Assert.That(run.Deaths.Any(d=>d.ActionId==25568),Is.True,run.ToString);
        else Assert.That(run.Passed,run.ToString);
    }

    [Test]
    public void TanksHoldOppositeTethersAndRemainStunnedDuringBladedance()
    {
        var checkedTethers=false;var checkedStun=false;
        var run=ScenarioRun.Execute(typeof(DsrStrength2Scenario),0,0,new()
        {
            ScenarioFactory=()=>new DsrStrength2Scenario(),
            Probe=p=>
            {
                if(p.Crossed(12.4f))
                {
                    var s=((DsrStrength2Scenario)p.Game.ActiveScenario!).LastState!;
                    var beams=p.World.Children.OfType<AnoMech.Core.SimObjects.SimTether>().Where(t=>t.IsActive).ToArray();
                    Assert.That(beams.Length,Is.EqualTo(2));
                    Assert.That(beams[0].B,Is.SameAs(p.World.Party.Get(PartyRole.MainTank)));
                    Assert.That(beams[1].B,Is.SameAs(p.World.Party.Get(PartyRole.OffTank)));checkedTethers=true;
                }
                if(p.Crossed(16f))
                {
                    foreach(var role in new[]{PartyRole.MainTank,PartyRole.OffTank})
                    {
                        Assert.That(p.World.Party.Get(role)!.HasStatus(3165),Is.True);
                        Assert.That(p.World.Party.Get(role)!.MovementLocked,Is.True);
                    }
                    checkedStun=true;
                }
            },WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,run.ToString);Assert.That(checkedTethers&&checkedStun,Is.True);
    }

    [TestCase(0f)] [TestCase(3.5f)] [TestCase(7.5f)] [TestCase(14f)] [TestCase(21.1f)]
    public void CancellationReleasesAllNativeActors(float time)
    {
        FakeGame? fake=null;
        var run=ScenarioRun.Execute(typeof(DsrStrength2Scenario),0,0,new()
        {
            ScenarioFactory=()=>new DsrStrength2Scenario(),StopAt=time,
            ConfigureNative=f=> {fake=f;f.BattleCharas.PacketSpawnDelayFrames=5;},WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,run.ToString);Assert.That(fake!.BattleCharas.OccupiedSlots,Is.Zero);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
    public void EveryCommunicationAndInitialTetherPermutationClears(int communication)
    {
        for(var tether=0;tether<6;++tether)
        for(var pattern=0;pattern<8;++pattern)
        {
            var run=ScenarioRun.Execute(typeof(DsrStrength2Scenario),0,tether,new()
            {
                ScenarioFactory=()=>new DsrStrength2Scenario(),
                Overrides=o=> {var a=(DsrStrength2StateOverrides)o;a.KnightPattern=pattern;a.TowerCommunication=communication;a.TetherTargets=tether;},
                WriteArtifactsOnFailure=false,
            });
            Assert.That(run.Passed,run.ToString);
        }
    }
    [TestCase(25565u,12f,0f,-20f,PartyRole.RegenHealer)]
    [TestCase(25297u,12.3f,0f,-20f,PartyRole.MainTank)]
    [TestCase(25551u,12.32f,-10f,-17f,PartyRole.MeleeDpsB)]
    [TestCase(25299u,16.3f,6.1803f,-19.0211f,PartyRole.MeleeDpsB)]
    public void VerifiedMistakesKillHumansAndBots(uint action,float time,float x,float z,PartyRole role)
    {
        foreach(var bot in new[]{false,true})
        {
            var run=ScenarioRun.Execute(typeof(DsrStrength2Scenario),0,0,new()
            {
                ScenarioFactory=()=>new DsrStrength2Scenario(),PlayerRole=role,
                Overrides=o=> {var a=(DsrStrength2StateOverrides)o;a.KnightPattern=3;a.LeapTrio=0;a.LeapOrder=0;},
                Takeovers=[new(time,new Vector2(x,z),Bot:bot?role:null)],WriteArtifactsOnFailure=false,
            });
            Assert.That(run.Deaths.Any(d=>d.ActionId==action),Is.True,run.ToString);
        }
    }
    [Test]
    public void CatalogKeepsIndependentPartTwoImmediatelyAfterPartOne()
    {
        var catalog=ScenarioCatalog.Create().Scenarios;
        var first = catalog.ToList().FindIndex(s => s is AnoMech.Scenarios.Dsr.Strength.DsrStrengthScenario);
        Assert.That(first, Is.GreaterThanOrEqualTo(0));
        Assert.That(catalog[first + 1], Is.TypeOf<DsrStrength2Scenario>());
        Assert.That(catalog[first + 1].SettingsOverrides, Is.TypeOf<DsrStrength2StateOverrides>());
        Assert.That(catalog[first + 1].Phase, Is.SameAs(catalog[first].Phase));
    }

    [Test]
    public void NativeEffectTimelineMatchesAllFortySixPartTwoRecordings()
    {
        var emitted=new Dictionary<uint,float>();
        var run=ScenarioRun.Execute(typeof(DsrStrength2Scenario),0,0,new()
        {
            ScenarioFactory=()=>new DsrStrength2Scenario(),
            Probe=p=>
            {
                foreach(var actor in p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>())
                    if(actor.Proxy is FakeBattleChara proxy)
                        foreach(var effect in proxy.ReceivedEffects) emitted.TryAdd(effect.ActionId,p.World.Events.Elapsed+20.65f);
            },WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,run.ToString);
        using var doc=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Scenarios/Dsr/Strength2/RecordedTimelines.json")));
        var complete=doc.RootElement.GetProperty("sequences").EnumerateArray().Where(r=>r.GetProperty("reached_end").GetBoolean()).ToArray();
        Assert.That(complete.Length,Is.EqualTo(46));
        foreach(var record in complete)
        foreach(var id in new uint[]{25563,25564,25566,25567,25550,25551,25565,25297,25579,25298,25299})
        {
            Assert.That(emitted.ContainsKey(id),Is.True,$"missing native effect {id}");
            Assert.That(emitted[id],Is.EqualTo(record.GetProperty("effects").GetProperty(id.ToString())[0].GetSingle()).Within(.25f),$"native effect {id}");
        }
    }
    [Test]
    public void VisibleKnightsMeshlessTowerDrawAndBlueMarkersAreScheduled()
    {
        var checkedVisuals=false;
        var run=ScenarioRun.Execute(typeof(DsrStrength2Scenario),0,0,new()
        {
            ScenarioFactory=()=>new DsrStrength2Scenario(),
            Probe=p=>
            {
                if(!p.Crossed(5f))return;
                var actors=p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().ToArray();
                var visible=actors.Where(a=>a.SpawnConfig.BNpcBaseId!=9020).ToArray();
                Assert.That(visible.Length,Is.EqualTo(8));
                Assert.That(visible.All(a=>(a.SpawnConfig.Visibility == SpawnVisibility.Visible)&&a.SpawnConfig.Appearance!.BodyModel!=0),Is.True);
                var helpers=actors.Where(a=>a.SpawnConfig.BNpcBaseId==9020).ToArray();
                Assert.That(helpers.All(a=>a.SpawnConfig.Visibility == SpawnVisibility.InvisibleHelper),Is.True);
                var state=((DsrStrength2Scenario)p.Game.ActiveScenario!).LastState!;
                foreach(var role in DsrStrength2State.NonTanks)
                    Assert.That(p.World.Party.Get(role)!.LastLockonVfxId,Is.EqualTo(state.Leaps.Contains(role)?330u:(uint?)null));
                checkedVisuals=true;
            },WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,run.ToString);Assert.That(checkedVisuals,Is.True);
    }
    [Test]
    public void ReusingTheSameScenarioDoesNotLeakActorsOrAssignments()
    {
        var scenario=new DsrStrength2Scenario();
        for(var seed=0;seed<12;++seed)
        {
            FakeGame? fake=null;
            var run=ScenarioRun.Execute(typeof(DsrStrength2Scenario),0,seed,new()
            {
                ScenarioFactory=()=>scenario,ConfigureNative=f=>fake=f,WriteArtifactsOnFailure=false,
            });
            Assert.That(run.Passed,run.ToString);Assert.That(scenario.Completed,Is.True);
            Assert.That(scenario.FailedTowers,Is.Zero);Assert.That(fake!.BattleCharas.OccupiedSlots,Is.Zero);
        }
    }
}
