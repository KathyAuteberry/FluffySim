#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.Apocalypse;

namespace AnoMech.Tests;

public class FruApocalypseTests
{
    [Test]
    public void EveryDistributionHasPermanentBalancedGroupsAndManaPriorities()
    {
        Assert.That(FruApocalypseState.Distributions.Length,Is.EqualTo(2520));
        var counts=new int[3];
        for(var i=0;i<2520;i++)
        {
            var state=State(i,0,true);var a=new ApocalypseManaAssignments(state);
            var swaps=FruApocalypseState.Roles.Count(a.Swapped);counts[swaps/2]++;
            foreach(var group in new[]{0,1})
            {
                var roles=FruApocalypseState.Roles.Where(r=>a.Group(r)==group).ToArray();
                Assert.That(roles.Length,Is.EqualTo(4));Assert.That(roles.Select(state.Order).Order(),Is.EqualTo(new[]{0,1,2,3}));
                Assert.That(roles.Select(a.Position).Order(),Is.EqualTo(new[]{0,1,2,3}));
            }
            Assert.That(a.Swapped(PartyRole.ShieldHealer)||a.Swapped(PartyRole.CasterDps),Is.False);
        }
        Assert.That(counts,Is.EqualTo(new[]{576,1728,216}));
    }
    private static FruApocalypseState State(int water,int axis,bool cw)=>new(new Rng(0),new(){Advanced=true,WaterPermutation=water,InitialAxis=axis,Clockwise=cw},PartyRole.MainTank,PartyRole.OffTank);
    private static IEnumerable<TestCaseData> AllWaterPatterns()=>Enumerable.Range(0,2520).Select(i=>new TestCaseData(i));
    [TestCaseSource(nameof(AllWaterPatterns))]
    public void EveryWaterDistributionClearsWithPermanentManaGroups(int permutation)
        => CompleteManaRun(permutation%4,(permutation/4)%2==0,permutation);
    private static IEnumerable<TestCaseData> Patterns()=>from axis in Enumerable.Range(0,4) from cw in new[]{true,false} from water in new[]{0,37,151,900,1700,2519} select new TestCaseData(axis,cw,water);
    [TestCaseSource(nameof(Patterns))]
    public void CompleteManaRun(int axis,bool cw,int water)
    {
        var scenario=new FruApocalypseScenario();
        var run=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,water,new(){ScenarioFactory=()=>scenario,Overrides=o=>{var x=(FruApocalypseStateOverrides)o;x.Advanced=true;x.InitialAxis=axis;x.Clockwise=cw;x.WaterPermutation=water;},WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(scenario.Completed,Is.True);Assert.That(scenario.ResolvedWaves,Is.EqualTo(6));Assert.That(scenario.ResolvedWaters,Is.EqualTo(3));
    }

    private static IEnumerable<TestCaseData> Practices()=>from role in FruApocalypseState.Roles from water in Enum.GetValues<ApocalypseWater>() select new TestCaseData(role,water);
    [TestCaseSource(nameof(Practices))]
    public void EverySeatCanPracticeEveryWaterCategory(PartyRole role,ApocalypseWater water)
    {
        var scenario=new FruApocalypseScenario();
        var run=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,42,new(){PlayerRole=role,ScenarioFactory=()=>scenario,
            Overrides=o=>((FruApocalypseStateOverrides)o).PracticeWater=water,WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(scenario.LastState!.Order(role),Is.EqualTo((int)water));
    }
    [Test]
    public void HumanSwapRequestsOnlySelectValidPermanentAdjustments()
    {
        foreach(var role in FruApocalypseState.Roles)
        foreach(var water in Enum.GetValues<ApocalypseWater>())
        foreach(var request in new[]{ApocalypseSwapPractice.Swap,ApocalypseSwapPractice.Stay})
        {
            var settings=new FruApocalypseStateOverrides {PracticeWater=water,SwapPractice=request};
            if(request==ApocalypseSwapPractice.Swap && role is PartyRole.ShieldHealer or PartyRole.CasterDps)
            {Assert.Throws<ArgumentException>(()=>new FruApocalypseState(new Rng(99),settings,role,PartyRole.OffTank));continue;}
            var state=new FruApocalypseState(new Rng(99),settings,role,PartyRole.OffTank);
            Assert.That(new ApocalypseManaAssignments(state).Swapped(role),Is.EqualTo(request==ApocalypseSwapPractice.Swap));
            Assert.That(state.Order(role),Is.EqualTo((int)water));
        }
    }
    [Test]
    public void BothDirectionsAndAllAxesMatchTheGuidesReferenceSectors()
    {
        for(var axis=0;axis<4;axis++)foreach(var cw in new[]{true,false})
        {
            var state=State(37,axis,cw);var a=new ApocalypseManaAssignments(state);
            var reference=(axis<2?axis:axis+4)*MathF.PI/4;
            var safe=reference-(cw?1:-1)*MathF.PI/4;
            foreach(var role in FruApocalypseState.Roles)
            {
                var p=a.Spread(role);Assert.That(p.Length(),Is.LessThan(20));
                if(a.Position(role)==2 || a.Position(role)==3)
                {var expected=FruApocalypseState.Direction(safe+a.Group(role)*MathF.PI+(a.Position(role)==2?1:-1)*MathF.Atan2(8,44))*18.95f;
                    Assert.That(Vector3.Distance(p,expected),Is.LessThan(.001f));}
                Assert.That(state.Wave(0).All(c=>Vector3.Distance(c,p)>9),Is.True);
                Assert.That(state.Wave(1).All(c=>Vector3.Distance(c,p)>9),Is.True);
            }
            Assert.That(state.Wave(0).Count,Is.EqualTo(4));Assert.That(state.Wave(1).Count,Is.EqualTo(6));
            Assert.That(Enumerable.Range(0,6).Sum(i=>state.Wave(i).Count),Is.EqualTo(34));
        }
    }
    [Test]
    public void NativeEffectsUseReadyActorsAndMatchFourReceiveTimelines()
    {
        FakeGame? fake=null;var scenario=new FruApocalypseScenario();
        var run=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,3,new(){ScenarioFactory=()=>scenario,ConfigureNative=f=>{fake=f;f.BattleCharas.PacketSpawnDelayFrames=5;},
            Probe=p=>
            {
                if(p.Crossed(12.9f))
                {
                    Assert.That(FruApocalypseState.Roles.Count(r=>p.Member(r)!.HasStatus(2461)),Is.EqualTo(6));
                    var bosses=p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Where(e=>e.BNpcBaseId==17831).ToArray();
                    Assert.That(bosses.Length,Is.EqualTo(1));
                    Assert.That(bosses[0].SpawnConfig.IsVisible,Is.True);
                    Assert.That(bosses[0].SpawnConfig.Targetable,Is.True);
                    Assert.That(bosses[0].SpawnConfig.Appearance,Is.Not.Null);
                }
                if(p.Crossed(31.5f))Assert.That(scenario.LightPositions.Count,Is.EqualTo(6));
                if(p.Crossed(55f))
                {
                    var effects=p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Select(e=>e.Proxy).OfType<FakeBattleChara>().SelectMany(e=>e.ReceivedEffects).ToArray();
                    Assert.That(effects.Count(e=>e.ActionId==40297),Is.EqualTo(34));
                    Assert.That(effects.Count(e=>e.ActionId==40271),Is.EqualTo(6));
                    Assert.That(effects.Count(e=>e.ActionId==40274),Is.EqualTo(8));
                    Assert.That(effects.Count(e=>e.ActionId==40289),Is.EqualTo(1));
                }
            },WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(fake!.BattleCharas.OccupiedSlots,Is.Zero);
        float[][] waves=[[31.170f,33.177f,35.187f,37.194f,39.204f,41.212f],[31.199f,33.200f,35.212f,37.216f,39.226f,41.236f],[31.153f,33.160f,35.168f,37.177f,39.180f,41.189f],[31.155f,33.159f,35.166f,37.172f,39.177f,41.182f]];
        var actual=scenario.Resolutions.Where(r=>r.Action==40297).Select(r=>r.Time-2.1f).ToArray();
        foreach(var recording in waves)for(var i=0;i<6;i++)Assert.That(MathF.Abs(actual[i]-recording[i]),Is.LessThan(.2f));
        Assert.That(scenario.LightPositions,Is.Empty);Assert.That(scenario.CurrentExplosion,Is.Empty);
    }
    [Test]
    public void ExactOverridesRemainIndependentOfPracticeFiltersAndReproduceSeeds()
    {
        var settings=new FruApocalypseStateOverrides {Advanced=true,WaterPermutation=37,InitialAxis=2,
            Clockwise=false,SpiritTarget=PartyRole.CasterDps,PracticeWater=ApocalypseWater.None,SwapPractice=ApocalypseSwapPractice.Swap};
        var first=new FruApocalypseState(new Rng(123),settings,PartyRole.ShieldHealer,PartyRole.OffTank);
        var second=new FruApocalypseState(new Rng(123),settings,PartyRole.ShieldHealer,PartyRole.OffTank);
        Assert.That(first.Water,Is.EqualTo(second.Water));Assert.That(first.Permutation,Is.EqualTo(37));
        Assert.That(first.Axis,Is.EqualTo(2));Assert.That(first.Clockwise,Is.False);
        Assert.That(first.SpiritTarget,Is.EqualTo(PartyRole.CasterDps));
    }
    [TestCaseSource(nameof(SpiritSeats))]
    public void EverySpiritTargetClearsTheEntireFragment(PartyRole target)
    {
        var run=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,73,new(){Overrides=o=>
            {var x=(FruApocalypseStateOverrides)o;x.Advanced=true;x.SpiritTarget=target;},WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);
    }
    private static IEnumerable<PartyRole> SpiritSeats()=>FruApocalypseState.Roles;
    [TestCase(22.6f,0f,0f,40271u)]
    [TestCase(33.1f,0f,0f,40297u)]
    [TestCase(46.7f,19f,0f,40271u)]
    public void IncorrectHumanPositionsReceiveTheSameFailures(float time,float x,float z,uint action)
    {
        var run=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,3,new(){PlayerRole=PartyRole.MeleeDpsA,
            Takeovers=[new(time,new Vector2(x,z))],WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,Is.False);Assert.That(run.Deaths.Any(d=>d.ActionId==action),Is.True,run.ToString);
    }
    [TestCase(0.05f)]
    [TestCase(21f)]
    [TestCase(34f)]
    [TestCase(47f)]
    public void CancellationReleasesNativeActorsAndAReentryStartsClean(float stop)
    {
        FakeGame? fake=null;
        var run=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,7,new(){StopAt=stop,ConfigureNative=f=>fake=f,WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(fake!.BattleCharas.OccupiedSlots,Is.Zero);
        var retry=new FruApocalypseScenario();
        var next=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,7,new(){ScenarioFactory=()=>retry,WriteArtifactsOnFailure=false});
        Assert.That(next.Passed,next.ToString);Assert.That(retry.Completed,Is.True);
        Assert.That(retry.ResolvedWaters,Is.EqualTo(3));Assert.That(retry.LightPositions,Is.Empty);
    }

    [Test]
    public void KnockbackCanPushABotOutOfTheArena()
    {
        var run=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,3,new(){PlayerRole=PartyRole.MeleeDpsB,
            Takeovers=[new(46.7f,new Vector2(19,0),Bot:PartyRole.MeleeDpsA)],WriteArtifactsOnFailure=false});
        Assert.That(run.Deaths.Any(d=>d.Role==PartyRole.MeleeDpsA && d.Cause.Contains("arena")),Is.True,run.ToString);
    }

    [Test]
    public void ValidKnockbackActuallyDisplacesThePartyBeforeItReturnsToTheStacks()
    {
        Vector3 before=default,after=default;
        var run=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,3,new(){PlayerRole=PartyRole.MeleeDpsB,
            Probe=p=>{if(p.Crossed(46.89f))before=p.Member(PartyRole.MeleeDpsA)!.Position;
                if(p.Crossed(47.6f))after=p.Member(PartyRole.MeleeDpsA)!.Position;},WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);
        Assert.That(Vector3.Distance(before,after),Is.EqualTo(21).Within(.1f));
        Assert.That(after.Length(),Is.LessThan(20));
    }

    [Test]
    public void CatalogExposesFullProvisionalFragmentAfterUltimateRelativity()
    {
        var catalog=ScenarioCatalog.Create().Scenarios;
        Assert.That(catalog[3],Is.TypeOf<FruApocalypseScenario>());
        Assert.That(catalog[3].IsIncomplete,Is.True);
        Assert.That(catalog[3].EnablePhaseBgm,Is.True);
        Assert.That(catalog[3].Phase,Is.SameAs(catalog[2].Phase));
        Assert.That(catalog[3].SettingsOverrides,Is.TypeOf<FruApocalypseStateOverrides>());
        Assert.That(AnoMech.Core.Game.Game.DisplayName(catalog[3]),Is.EqualTo("P3 Apocalypse"));
    }

    [Test]
    public void HumanDanceBaitMustActuallyUseInvulnerability()
    {
        var run=ScenarioRun.Execute(typeof(FruApocalypseScenario),0,3,new(){PlayerRole=PartyRole.OffTank,
            Takeovers=[new(43.69f,null)],WriteArtifactsOnFailure=false});
        Assert.That(run.Deaths.Any(d=>d.Role==PartyRole.OffTank && d.ActionId==40182),Is.True,run.ToString);
    }

}
