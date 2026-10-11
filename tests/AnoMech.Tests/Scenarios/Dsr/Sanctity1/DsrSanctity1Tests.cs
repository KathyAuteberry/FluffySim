#pragma warning disable CA1416 // Native interfaces are replaced with headless fakes.
using AnoMech.Scenarios;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.Sanctity1;
namespace AnoMech.Tests;
public class DsrSanctity1Tests
{
    private static IEnumerable<TestCaseData> Patterns() => from z in Enumerable.Range(0,4) from cw in new[]{false,true}
        from pair in DsrSanctity1State.SwordPairs select new TestCaseData(z,cw,pair.One,pair.Two);
    [TestCaseSource(nameof(Patterns))]
    public void FullFourJumpFragmentClears(int z,bool cw,PartyRole one,PartyRole two)
    {
        var completed=false;
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new()
        {
            ScenarioFactory=()=>new DsrSanctity1Scenario(),
            Overrides=o=>{var s=(DsrSanctity1StateOverrides)o;s.AdvancedMode=true;s.ZephirinPosition=z;s.Clockwise=cw;s.Sword1=one;s.Sword2=two;},
            Probe=p=>{if(p.Crossed(26.1f)){var s=(DsrSanctity1Scenario)p.Game.ActiveScenario!;completed=s.Completed;Assert.That(s.ResolvedJumps,Is.EqualTo(4));Assert.That(s.ResolvedFlares,Is.EqualTo(18));Assert.That(s.ResolvedCharges,Is.EqualTo(6));}},
            WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,run.ToString);Assert.That(completed,Is.True);
    }
    [Test]
    public void EverySwordPairPreservesFourPlayersAndRolePartners()
    {
        foreach(var (one,two) in DsrSanctity1State.SwordPairs)
        {
            var groups=DsrSanctity1State.AssignGroups(one,two);
            Assert.That(groups[(int)one],Is.Zero);Assert.That(groups[(int)two],Is.EqualTo(1));
            Assert.That(groups.Count(g=>g==0),Is.EqualTo(4));
            foreach(var role in DsrSanctity1State.Roles)
                Assert.That(groups[(int)role],Is.Not.EqualTo(groups[(int)DsrSanctity1State.Partner(role)]));
        }
    }
    [Test]
    public void AllSeatsCanPracticeBothSwordsAndUnmarkedAdjustmentsDeterministically()
    {
        foreach(var role in DsrSanctity1State.Roles)
        foreach(var practice in Enum.GetValues<SacredSeverPractice>())
        for(var seed=0;seed<20;++seed)
        {
            var o=new DsrSanctity1StateOverrides{PracticeAssignment=practice};
            var a=new DsrSanctity1State(new Rng(seed),o,role);var b=new DsrSanctity1State(new Rng(seed),o,role);
            Assert.That((a.Sword1,a.Sword2,a.ZephirinPosition,a.Clockwise,a.EyePosition),Is.EqualTo((b.Sword1,b.Sword2,b.ZephirinPosition,b.Clockwise,b.EyePosition)));
            Assert.That(DsrSanctity1State.MatchesPractice(a.Sword1,a.Sword2,role,practice),Is.True);
        }
    }
    private static IEnumerable<TestCaseData> Gazes()=>from boss in Enumerable.Range(0,8) from eye in Enumerable.Range(0,5) select new TestCaseData(boss,eye);
    [TestCaseSource(nameof(Gazes))]
    public void BothGazesCanBeAvoidedInEveryConfiguration(int boss,int eye)
    {
        for(var z=0;z<4;++z)
        foreach(var cw in new[]{false,true})
        {
            var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,z,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),
                Overrides=o=>{var s=(DsrSanctity1StateOverrides)o;s.AdvancedMode=true;s.ThordanPosition=boss;s.EyeSeparation=eye;s.ZephirinPosition=z;s.Clockwise=cw;},WriteArtifactsOnFailure=false});
            Assert.That(run.Passed,run.ToString);
        }
    }
    [TestCase(0f)] [TestCase(8.4f)] [TestCase(9.6f)] [TestCase(17.8f)] [TestCase(21f)] [TestCase(26.1f)]
    public void CancellationAndCompletionReleaseNativeActors(float time)
    {
        FakeGame? fake=null;
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),StopAt=time,
            ConfigureNative=f=>{fake=f;f.BattleCharas.PacketSpawnDelayFrames=5;},WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(fake!.BattleCharas.OccupiedSlots,Is.Zero);
    }
    [TestCase(12592u,3633u,3268u)] [TestCase(13070u,4385u,3501u)]
    public void VisibleNativeActorsUseVerifiedModels(uint id,uint name,uint model)
    {
        var c=AnoMech.Scenarios.Dsr.DsrActors.Config(id,default);
        Assert.That(c.NameId,Is.EqualTo(name));Assert.That(c.ModelCharaId,Is.EqualTo(model));Assert.That(c.IsVisible,Is.True);
        Assert.That(c.InitialModeAttributeFlags,Is.Zero);
        if(id==12592){Assert.That(c.Appearance!.BodyModel,Is.EqualTo(65537));Assert.That(c.Appearance.MainhandWeaponModel,Is.EqualTo(12887197149ul));}
    }
    [Test]
    public void MenuPlacesSacredSeversImmediatelyAfterStrengthTwo()
    {
        var list=ScenarioCatalog.Create().Scenarios.ToList();var index=list.FindIndex(s=>s is DsrSanctity1Scenario);
        Assert.That(index,Is.GreaterThan(0));Assert.That(list[index-1],Is.TypeOf<AnoMech.Scenarios.Dsr.Strength2.DsrStrength2Scenario>());
    }
    private static DsrSanctity1State FixedState()=>new(new Rng(0),new(){AdvancedMode=true,ZephirinPosition=1,Clockwise=true,ThordanPosition=0,EyeSeparation=2,Sword1=PartyRole.MainTank,Sword2=PartyRole.OffTank},PartyRole.MeleeDpsB);
    private static void Fix(object o)
    {
        var s=(DsrSanctity1StateOverrides)o;s.AdvancedMode=true;s.ZephirinPosition=1;s.Clockwise=true;s.ThordanPosition=0;s.EyeSeparation=2;s.Sword1=PartyRole.MainTank;s.Sword2=PartyRole.OffTank;
    }
    [TestCase(false)] [TestCase(true)]
    public void LookingAtEitherGazeKillsTheHuman(bool eye)
    {
        var s=FixedState();var pos=s.InitialPosition(PartyRole.MeleeDpsB);var d=(eye?s.EyeSpawn:s.ThordanSpawn)-pos;
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),PlayerRole=PartyRole.MeleeDpsB,Overrides=Fix,
            Takeovers=[new(17.72f,new(pos.X,pos.Z),MathF.Atan2(d.X,d.Z))],WriteArtifactsOnFailure=false});
        Assert.That(run.Deaths.Any(d=>d.Role==PartyRole.MeleeDpsB&&d.ActionId==(eye?25554u:25553u)),Is.True,run.ToString);
    }
    [TestCase(25570u,17.66f)] [TestCase(25295u,19.73f)]
    public void ActiveChargeAndOrbKillHumansAndBots(uint action,float time)
    {
        var points=FixedState().ChargePoints(true);
        var pos=action==25570?(points[0]+points[1])/2:FixedState().OrbPositions(true)[2];
        foreach(var bot in new[]{false,true})
        {
            var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),PlayerRole=PartyRole.MeleeDpsB,Overrides=Fix,
                Takeovers=[new(time,new(pos.X,pos.Z),Bot:bot?PartyRole.MeleeDpsB:null)],WriteArtifactsOnFailure=false});
            Assert.That(run.Deaths.Any(d=>d.Role==PartyRole.MeleeDpsB&&d.ActionId==action),Is.True,run.ToString);
        }
    }
    [Test]
    public void WrongGroupLeavesAnUnderfilledJump()
    {
        var s=FixedState();var wrong=s.InitialPosition(PartyRole.OffTank);
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),PlayerRole=PartyRole.MeleeDpsA,Overrides=Fix,
            Takeovers=[new(17.5f,new(wrong.X,wrong.Z))],WriteArtifactsOnFailure=false});
        Assert.That(run.Deaths.Any(d=>d.ActionId==25571&&d.Cause.Contains("3/4")),Is.True,run.ToString);
    }
    [Test]
    public void PresentationAndWaitingRemainAttachedToTheAssignmentModel()
    {
        var samples=0;
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),Overrides=Fix,Probe=p=>
        {
            var state=((DsrSanctity1Scenario)p.Game.ActiveScenario!).LastState!;
            if(p.Crossed(10f)) foreach(var role in DsrSanctity1State.Roles)
                Assert.That(p.Member(role)!.LastLockonVfxId,Is.EqualTo(role==state.Sword1?50u:role==state.Sword2?51u:(uint?)null));
            if(p.Crossed(17f)||p.Crossed(20.75f))
            {++samples;foreach(var role in DsrSanctity1State.Roles) Assert.That(System.Numerics.Vector3.Distance(p.Member(role)!.Position,state.InitialPosition(role)),Is.LessThan(role == p.World.Party.PlayerRole ? 6 * ScenarioRun.FrameSeconds + .001f : .05f));}
            if(p.Crossed(22f)) foreach(var role in DsrSanctity1State.Roles)
                Assert.That(System.Numerics.Vector3.Distance(p.Member(role)!.Position,state.FinalPosition(role)),Is.LessThan(role == p.World.Party.PlayerRole ? 6 * ScenarioRun.FrameSeconds + .001f : .05f));
        },WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(samples,Is.EqualTo(2));
    }
    [Test]
    public void NativeReleaseTimelinesMatchBothCompleteRecordings()
    {
        var emitted=new Dictionary<uint,List<float>>();var seen=new Dictionary<FakeBattleChara,int>();
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),Overrides=Fix,Probe=p=>
        {
            foreach(var actor in p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>())
                if(actor.Proxy is FakeBattleChara proxy)
                {
                    var count=seen.GetValueOrDefault(proxy);
                    foreach(var effect in proxy.ReceivedEffects.Skip(count))
                    {if(!emitted.TryGetValue(effect.ActionId,out var list))emitted[effect.ActionId]=list=[];list.Add(p.Time);}
                    seen[proxy]=proxy.ReceivedEffects.Count;
                }
        },WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);
        using var doc=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"Scenarios/Dsr/Sanctity1/RecordedTimelines.json")));
        var complete=doc.RootElement.GetProperty("sequences").EnumerateArray().Where(r=>r.GetProperty("jumps").GetInt32()==4&&r.GetProperty("flares").GetInt32()==18).ToArray();
        Assert.That(complete.Length,Is.EqualTo(2));
        foreach(var record in complete)
        foreach(var id in new uint[]{25553,25554,25570,25571,25295})
        {
            var actual=emitted[id].Order().ToArray();var expected=record.GetProperty("effects").GetProperty(id.ToString()).EnumerateArray().Select(x=>x.GetSingle()).Order().ToArray();
            Assert.That(actual.Length,Is.EqualTo(expected.Length),$"count {id}");
            for(var i=0;i<actual.Length;++i) Assert.That(actual[i],Is.EqualTo(expected[i]).Within(.18f),$"release {id} #{i}");
        }
    }
    [Test]
    public void RepeatedRunsOnSameInstanceCleanAllActors()
    {
        var scenario=new DsrSanctity1Scenario();
        for(var seed=0;seed<20;++seed)
        {
            FakeGame? fake=null;
            var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,seed,new(){ScenarioFactory=()=>scenario,ConfigureNative=f=>fake=f,WriteArtifactsOnFailure=false});
            Assert.That(run.Passed,run.ToString);Assert.That(scenario.Completed,Is.True);Assert.That(fake!.BattleCharas.OccupiedSlots,Is.Zero);
        }
    }
    [Test]
    public void AdvancedPatternsAndSimplePracticeAreIndependent()
    {
        var o=new DsrSanctity1StateOverrides{Sword1=PartyRole.MainTank,Sword2=PartyRole.OffTank,PracticeAssignment=SacredSeverPractice.Sword1};
        var quick=new DsrSanctity1State(new Rng(0),o,PartyRole.CasterDps);Assert.That(quick.Sword1,Is.EqualTo(PartyRole.CasterDps));
        o.AdvancedMode=true;var advanced=new DsrSanctity1State(new Rng(0),o,PartyRole.CasterDps);
        Assert.That(advanced.Sword1,Is.EqualTo(PartyRole.MainTank));Assert.That(advanced.Sword2,Is.EqualTo(PartyRole.OffTank));
        o.AdvancedMode=false;Assert.That(new DsrSanctity1State(new Rng(0),o,PartyRole.CasterDps).Sword1,Is.EqualTo(quick.Sword1));
    }
    [Test]
    public void ProvisionalDistanceDamageFallsWithTravelDistance()
    {
        Assert.That(DsrSanctity1Actions.DistanceDamageScale(20),Is.GreaterThan(DsrSanctity1Actions.DistanceDamageScale(35)));
        Assert.That(DsrSanctity1Actions.DistanceDamageScale(35),Is.GreaterThan(DsrSanctity1Actions.DistanceDamageScale(40)));
    }
    [Test]
    public void UnmarkedRolePartnerMustActuallySwap()
    {
        var state=new DsrSanctity1State(new Rng(0),new(){AdvancedMode=true,ZephirinPosition=1,Clockwise=true,ThordanPosition=0,EyeSeparation=2,Sword1=PartyRole.ShieldHealer,Sword2=PartyRole.OffTank},PartyRole.RegenHealer);
        Assert.That(state.Swaps(PartyRole.RegenHealer),Is.True);
        var wrong=state.InitialPosition(PartyRole.MainTank);
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),PlayerRole=PartyRole.RegenHealer,
            Overrides=o=>{Fix(o);((DsrSanctity1StateOverrides)o).Sword1=PartyRole.ShieldHealer;},
            Takeovers=[new(17.5f,new(wrong.X,wrong.Z))],WriteArtifactsOnFailure=false});
        Assert.That(run.Deaths.Any(d=>d.ActionId==25571&&d.Cause.Contains("3/4")),Is.True,run.ToString);
    }
    [Test]
    public void MovingIntoTheFinalPositionTooEarlyHitsTheUnexplodedOrb()
    {
        var state=new DsrSanctity1State(new Rng(0),new(){AdvancedMode=true,ZephirinPosition=0,Clockwise=true},PartyRole.MeleeDpsB);
        var destination=state.FinalPosition(PartyRole.MeleeDpsB);
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),PlayerRole=PartyRole.MeleeDpsB,
            Overrides=o=>{var s=(DsrSanctity1StateOverrides)o;s.AdvancedMode=true;s.ZephirinPosition=0;s.Clockwise=true;},
            Takeovers=[new(21.4f,new(destination.X,destination.Z))],WriteArtifactsOnFailure=false});
        Assert.That(run.Deaths.Any(d=>d.Role==PartyRole.MeleeDpsB&&d.ActionId==25295),Is.True,run.ToString);
    }
    [Test]
    public void CancellationClearsEyeAndCannotRepeatStaleCallbacks()
    {
        FakeGame? fake=null;AnoMech.Core.SimObjects.SimWorld? world=null;var callbacks=0;
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),Overrides=Fix,StopAt=9.6f,
            ConfigureNative=f=>fake=f,Probe=p=>{if(p.Crossed(9.5f)){world=p.World;p.World.OnDespawn(()=>++callbacks);}},WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(callbacks,Is.EqualTo(1));
        Assert.That(fake!.MapEffects.Applied.Last(),Is.EqualTo((0x00400020u,(byte)4)));
        var count=fake.MapEffects.Applied.Count;world!.Despawn();Assert.That(callbacks,Is.EqualTo(1));Assert.That(fake.MapEffects.Applied.Count,Is.EqualTo(count));
    }
    [Test]
    public void BotMovementUsesNormalSpeedAndTheSameFinalGroups()
    {
        var previous=new Dictionary<PartyRole,System.Numerics.Vector3>();var sampled=0;
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),Overrides=Fix,Probe=p=>
        {
            foreach(var role in DsrSanctity1State.Roles)
            {
                var pos=p.Member(role)!.Position;
                if(previous.TryGetValue(role,out var before)) Assert.That(System.Numerics.Vector3.Distance(before,pos),Is.LessThanOrEqualTo(6f*ScenarioRun.FrameSeconds+.002f));
                previous[role]=pos;
            }
            ++sampled;
        },WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(sampled,Is.GreaterThan(1500));
    }
    private static IEnumerable<TestCaseData> PracticeSeats()=>from role in DsrSanctity1State.Roles from practice in Enum.GetValues<SacredSeverPractice>() select new TestCaseData(role,practice);
    [TestCaseSource(nameof(PracticeSeats))]
    public void EverySeatClearsWithItsRequestedPracticeAssignment(PartyRole role,SacredSeverPractice practice)
    {
        var checkedAssignment=false;
        var run=ScenarioRun.Execute(typeof(DsrSanctity1Scenario),0,(int)role,new(){ScenarioFactory=()=>new DsrSanctity1Scenario(),PlayerRole=role,
            Overrides=o=>((DsrSanctity1StateOverrides)o).PracticeAssignment=practice,
            Probe=p=>{if(p.Crossed(10f)){var state=((DsrSanctity1Scenario)p.Game.ActiveScenario!).LastState!;Assert.That(DsrSanctity1State.MatchesPractice(state.Sword1,state.Sword2,role,practice),Is.True);checkedAssignment=true;}},WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(checkedAssignment,Is.True);
    }
}
