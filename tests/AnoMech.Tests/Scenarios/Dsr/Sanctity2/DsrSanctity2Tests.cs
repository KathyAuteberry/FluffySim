#pragma warning disable CA1416 // Native interfaces are replaced with headless fakes.
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.Sanctity2;

namespace AnoMech.Tests;

public class DsrSanctity2Tests
{
    [TestCase(0, -9f, -15.588f)]
    [TestCase(1, 0f, -18f)]
    [TestCase(2, 9f, -15.588f)]
    [TestCase(12, 4.243f, -4.243f)]
    [TestCase(13, 4.243f, 4.243f)]
    public void TowerGeometryMatchesRecordedCasterCoordinates(int index, float east, float south)
    {
        var position = DsrSanctity2State.TowerPosition(index);
        Assert.That(position.X, Is.EqualTo(east).Within(.01));
        Assert.That(position.Z, Is.EqualTo(south).Within(.01));
        Assert.That(position.Y, Is.Zero);
    }

    [TestCase(false)] [TestCase(true)]
    public void PuddlesBecomeTraversableForKnockbackWhileObjectsRemain(bool afterCutoff)
    {
        var scenario = new DsrSanctity2Scenario();
        var position = DsrSanctity2Scenario.FirePosition(0);
        var time = DsrSanctity2Timing.PuddleSafe + (afterCutoff ? .05f : -.05f);
        var role = PartyRole.MainTank;
        var run = ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new()
        {
            ScenarioFactory=()=>scenario, PlayerRole=role, StopAt=22.8f,
            Takeovers=[new(time,new(position.X,position.Z))],
            Probe=p=>
            {
                if (afterCutoff && p.Crossed(22.6f))
                    Assert.That(p.World.Children.OfType<SimEventObject>().Count(o=>o.EObjRowId is 0x1EB682 or 0x1EB686),Is.EqualTo(8));
            },WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Deaths.Any(d=>d.Role==role && d.ActionId==28591),Is.EqualTo(!afterCutoff),run.ToString);
        if (afterCutoff) Assert.That(run.Passed,Is.True,run.ToString);
    }

    [Test]
    public void OuterPatternsExhaustivelyMatchTheGuideConstraints()
    {
        var patterns=DsrSanctity2State.Patterns;
        Assert.That(patterns,Has.Length.EqualTo(810));
        Assert.That(patterns.Count(p=>p.OutsideCount==5),Is.EqualTo(324));
        Assert.That(patterns.Count(p=>p.OutsideCount==6),Is.EqualTo(486));
        Assert.That(patterns.Count(p=>p.ForcedCursed),Is.EqualTo(54));
        Assert.That(patterns.Select(p=>string.Join(",",p.Outer)).Distinct().Count(),Is.EqualTo(810));
        Assert.That(patterns.All(p=>p.Outer.All(m=>BitOperations.PopCount((uint)m) is 1 or 2)),Is.True);
    }
    [Test]
    public void EveryOuterPatternAndTargetPairHasEightDistinctLegitimateTowerClaims()
    {
        for(var p=0;p<810;++p)
        for(var pair=0;pair<12;++pair)
        {
            var s=State(p,pair);
            Assert.That(s.FirstTowers.Distinct().Count(),Is.EqualTo(8));
            Assert.That(s.FirstTowers.Order(),Is.EqualTo(s.TowerIndices.Order()));
            foreach(var role in DsrSanctity2State.Roles)
            {
                Assert.That(Vector3.Distance(s.FirstPosition(role),DsrSanctity2State.TowerPosition(s.FirstTowers[(int)role])),Is.LessThan(3));
                if(s.IsMeteorRole(role))Assert.That(s.FirstTowers[(int)role],Is.LessThan(12));
            }
            Assert.That(s.Targets.Select(s.Quadrant).Order(),Is.EqualTo(new[]{0,2}));
            Assert.That(s.Quadrants.GroupBy(q=>q).All(g=>g.Count()==2),Is.True);
            foreach(var role in DsrSanctity2State.Roles.Where(r=>!s.IsMeteorRole(r))) Assert.That(s.Quadrant(role),Is.EqualTo(DsrSanctity2State.BaseQuadrants[(int)role]));
            var n=s.FirstTowers[(int)s.Targets.Single(r=>s.Quadrant(r)==0)]%3;
            var south=s.FirstTowers[(int)s.Targets.Single(r=>s.Quadrant(r)==2)]%3;
            if(!s.Pattern.ForcedCursed)Assert.That(Math.Abs(n-south),Is.LessThan(2));
        }
    }
    [Test]
    public void AllInnerMasksProduceValidClaimsAndEverySeatCanPracticeEitherCategory()
    {
        foreach(var role in DsrSanctity2State.Roles)
        foreach(var practice in Enum.GetValues<MeteorPractice>())
        for(var seed=0;seed<30;++seed)
        {
            var o=new DsrSanctity2StateOverrides{PracticeAssignment=practice};
            var a=new DsrSanctity2State(new Rng(seed),o,role);var b=new DsrSanctity2State(new Rng(seed),o,role);
            Assert.That((a.Targets,a.FirstTowers,a.PatternIndex,a.InnerMask),Is.EqualTo((b.Targets,b.FirstTowers,b.PatternIndex,b.InnerMask)));
            if(practice!=MeteorPractice.Random)Assert.That(a.IsMeteor(role),Is.EqualTo(practice==MeteorPractice.Meteor));
        }
        for(var pattern=0;pattern<810;++pattern)
        foreach(var mask in Enumerable.Range(1,14).Where(m=>BitOperations.PopCount((uint)m)==8-DsrSanctity2State.Patterns[pattern].OutsideCount))
        {
            var s=new DsrSanctity2State(new Rng(0),new(){AdvancedMode=true,TowerPattern=pattern,InnerPattern=mask},PartyRole.MainTank);
            Assert.That(s.FirstTowers.Distinct().Count(),Is.EqualTo(8));
        }
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
    [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)]
    public void FullFragmentCompletesForEveryMeteorPair(int pair)
    {
        var scenario=new DsrSanctity2Scenario();
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,pair,new(){ScenarioFactory=()=>scenario,
            Overrides=o=>{var s=(DsrSanctity2StateOverrides)o;s.AdvancedMode=true;s.TargetPair=pair;},WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(scenario.Completed,Is.True);
        Assert.That(scenario.ResolvedMeteors,Is.EqualTo(14));Assert.That(scenario.ActiveTowerVisuals,Is.Empty);
        Assert.That(scenario.ResolvedTowers,Is.EqualTo(16));Assert.That(scenario.FailedTowers,Is.Zero);
    }
    [Test]
    public void PlannedMeteorRoutesKeepEveryCometSeparatedAndFinishInTimeAtRealSprintSpeed()
    {
        for(var p=0;p<810;++p)
        {
            var s=State(p,0);var drops=new List<Vector3>();
            foreach(var i in Enumerable.Range(0,7))foreach(var role in s.Targets)
            {
                var position=DsrSanctity2Ai.MeteorPosition(s,role,i);
                Assert.That(drops.All(prior=>Vector3.Distance(prior,position)>=5),Is.True,$"pattern {p}, drop {i}");drops.Add(position);
                if(i>0)Assert.That(Vector3.Distance(position,DsrSanctity2Ai.MeteorPosition(s,role,i-1)),Is.LessThan(7.8f*(DsrSanctity2Timing.MeteorInterval-.08f)));
            }
            foreach(var role in s.Targets)Assert.That(Vector3.Distance(DsrSanctity2Ai.MeteorPosition(s,role,6),s.FinalPosition(role)),Is.LessThan(7.8f*(DsrSanctity2Timing.SecondTowers-DsrSanctity2Timing.Meteor(6)-.08f)));
        }
    }
    [Test]
    public void ShortMeteorLegsWeaveOutsidePuddlesAndLeaveTimeToStop()
    {
        var shortSweeps = new HashSet<int>();
        for (var pattern = 0; pattern < 810; ++pattern)
        {
            var state = State(pattern,0);
            foreach (var role in state.Targets)
            {
                var shortRoute = DsrSanctity2Ai.MeteorSweep(state,role) < MathF.PI-.001f;
                var totalLength = 0f;
                if (shortRoute) shortSweeps.Add((int)MathF.Round(DsrSanctity2Ai.MeteorSweep(state,role)*180/MathF.PI));
                for (var drop = 1; drop < 7; ++drop)
                {
                    var route = DsrSanctity2Ai.MeteorLeg(state,role,drop);
                    var length = 0f;
                    for (var node = 1; node < route.Count; ++node)
                    {
                        length += Vector3.Distance(route[node-1],route[node]);
                        for (var sample = 0; sample <= 8; ++sample)
                        {
                            var position = Vector3.Lerp(route[node-1],route[node],sample/8f);
                            Assert.That(position.Length(),Is.LessThan(20));
                            for (var quadrant = 0; quadrant < 4; ++quadrant)
                            {
                                Assert.That(Vector3.Distance(position,DsrSanctity2Scenario.FirePosition(quadrant)),Is.GreaterThan(7));
                                Assert.That(Vector3.Distance(position,DsrSanctity2State.Direction(quadrant*MathF.PI/2)*10),Is.GreaterThan(7));
                            }
                        }
                    }
                    Assert.That(length/7.8f,Is.LessThan(DsrSanctity2Timing.MeteorInterval-.16f));
                    totalLength += length;
                }
                if (shortRoute)
                {
                    var sweep = MathF.Min(162*MathF.PI/180,DsrSanctity2Ai.MeteorSweep(state,role)-16*MathF.PI/180);
                    Assert.That(totalLength,Is.GreaterThan(19.8f*sweep+1));
                }
            }
        }
        Assert.That(shortSweeps.Count,Is.EqualTo(2));
    }

    [TestCase(12603u,3642u,3268u,65540u)] [TestCase(12638u,3643u,3268u,131076u)] [TestCase(12639u,3644u,3268u,196612u)]
    [TestCase(12591u,0u,629u,0u)]
    public void NewActorsUseVisibleVerifiedModelsAndEquipment(uint id,uint name,uint model,uint body)
    {
        var c=DsrActors.Config(id,default);Assert.That(c.IsVisible,Is.True);Assert.That(c.ModelCharaId,Is.EqualTo(model));
        Assert.That(c.NameId,Is.EqualTo(name));Assert.That(c.InitialModeAttributeFlags,body == 0 ? Is.Null : Is.Zero);
        if(body!=0)Assert.That(c.Appearance!.BodyModel,Is.EqualTo(body));
    }
    private static IEnumerable<TestCaseData> OuterRuns() => from pattern in Enumerable.Range(0,810) from difficulty in new[]{BotDifficulty.Easy,BotDifficulty.Hard} select new TestCaseData(pattern,difficulty);
    [TestCaseSource(nameof(OuterRuns))]
    public void FullRunClearsEveryOuterPatternIncludingAllForcedCursedCases(int pattern, BotDifficulty difficulty)
    {
        var scenario=new DsrSanctity2Scenario();
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,pattern,new(){ScenarioFactory=()=>scenario,BotDifficulty=difficulty,
            Overrides=o=>{var s=(DsrSanctity2StateOverrides)o;s.AdvancedMode=true;s.TargetPair=pattern%12;s.TowerPattern=pattern;},Probe=p=>
            {
                if(p.Crossed(1))
                {
                    var visuals=scenario.ActiveTowerVisuals;
                    Assert.That(visuals.Count,Is.EqualTo(8));
                    Assert.That(visuals.Select(t=>t.Position),Is.EquivalentTo(scenario.LastState!.TowerIndices.Select(DsrSanctity2State.TowerPosition)));
                    Assert.That(visuals.All(t=>t.Radius==3 && !t.SecondWave && MathF.Abs(t.From-DsrSanctity2Timing.FirstTowerCast)<.05f && MathF.Abs(t.Until-DsrSanctity2Timing.FirstTowers)<.001f),Is.True);
                }
                if(p.Crossed(13)) Assert.That(scenario.ActiveTowerVisuals,Is.Empty);
                if(p.Crossed(16))
                {
                    Assert.That(scenario.ActiveTowerVisuals.Select(t=>t.Position),Is.EquivalentTo(Enumerable.Range(0,8).Select(i=>DsrSanctity2State.Direction(i*MathF.PI/4)*18f)));
                    Assert.That(scenario.ActiveTowerVisuals.All(t=>t.SecondWave && t.Radius==3 && MathF.Abs(t.Until-DsrSanctity2Timing.SecondTowers)<.001f),Is.True);
                }
                for (var drop = 1; drop < 7; ++drop)
                {
                    if (!p.Crossed(DsrSanctity2Timing.Meteor(drop)-DsrSanctity2Timing.MeteorSpawnLead-.03f)) continue;
                    foreach (var role in scenario.LastState!.Targets)
                        Assert.That(p.Member(role)!.MoveDestination,Is.Null,$"pattern {pattern}, {difficulty}, stopped before drop {drop}");
                }
                for (var drop = 0; drop < 7; ++drop)
                {
                    if (!p.Crossed(DsrSanctity2Timing.Meteor(drop)-DsrSanctity2Timing.MeteorSpawnLead+.03f)) continue;
                    var state = scenario.LastState!;
                    for (var target = 0; target < 2; ++target)
                        Assert.That(Vector3.Distance(scenario.CometPositions[drop*2+target],DsrSanctity2Ai.MeteorPosition(state,state.Targets[target],drop)),Is.LessThan(.3f),$"pattern {pattern}, {difficulty}, drop {drop}");
                }
                if(p.Crossed(27)) Assert.That(scenario.ActiveTowerVisuals,Is.Empty);
            },WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(scenario.Completed,Is.True);
        Assert.That(scenario.ActiveTowerVisuals,Is.Empty);
        Assert.That(scenario.ResolvedTowers,Is.EqualTo(16));Assert.That(scenario.ResolvedMeteors,Is.EqualTo(14));
    }
    private static IEnumerable<TestCaseData> Seats()=>from role in DsrSanctity2State.Roles from practice in new[]{MeteorPractice.Meteor,MeteorPractice.NoMeteor} from cursed in new[]{false,true} select new TestCaseData(role,practice,cursed);
    [TestCaseSource(nameof(Seats))]
    public void AllSeatsClearMeteorAndNonMeteorPracticeIncludingCursedPatterns(PartyRole role,MeteorPractice practice,bool cursed)
    {
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,(int)role,new(){PlayerRole=role,ScenarioFactory=()=>new DsrSanctity2Scenario(),
            Overrides=o=>{var s=(DsrSanctity2StateOverrides)o;s.PracticeAssignment=practice;s.Cursed=cursed;},WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);
    }
    [Test]
    public void MeteorMarkersAndCommunicationUseTheSameFixedAssignmentState()
    {
        int[]? claims=null;var communication=0;
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,11,new(){ScenarioFactory=()=>new DsrSanctity2Scenario(),Probe=p=>
        {
            var s=((DsrSanctity2Scenario)p.Game.ActiveScenario!).LastState!;
            if(p.Crossed(1.7f)) foreach(var role in DsrSanctity2State.Roles)
            {
                Assert.That(p.Member(role)!.HasStatus(562),Is.EqualTo(s.IsMeteor(role)));
                Assert.That(p.Member(role)!.LastLockonVfxId,Is.EqualTo(s.IsMeteor(role)?285u:(uint?)null));
            }
            if(p.Crossed(5.4f))
            {
                claims=(int[])s.FirstTowers.Clone();++communication;
                foreach(var role in DsrSanctity2State.Roles)Assert.That(Vector3.Distance(p.Member(role)!.Position,s.CommunicationPosition(role)),Is.LessThan(.15f));
            }
            if(p.Crossed(11.6f)) Assert.That(s.FirstTowers,Is.EqualTo(claims));
        },WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(communication,Is.EqualTo(1));
    }
    [Test]
    public void MissingFirstTowerAndContestingABotBothFailWithoutBotReassignment()
    {
        foreach(var contest in new[]{false,true})
        {
            int[]? original=null;
            var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity2Scenario(),PlayerRole=PartyRole.MeleeDpsA,Probe=p=>
            {
                var s=((DsrSanctity2Scenario)p.Game.ActiveScenario!).LastState!;
                if(p.Crossed(9.5f))original=(int[])s.FirstTowers.Clone();
                if(p.Crossed(11.6f))Assert.That(s.FirstTowers,Is.EqualTo(original));
            },Takeovers=[new(11.7f,contest ? new Vector2(StateForSeed(0).FirstPosition(PartyRole.MainTank).X,StateForSeed(0).FirstPosition(PartyRole.MainTank).Z) : Vector2.Zero)],WriteArtifactsOnFailure=false});
            Assert.That(run.Deaths.Any(d=>d.ActionId==29564),Is.True,run.ToString);
        }
    }
    [Test]
    public void StayingInIceAndReusingMeteorPositionsAreLethal()
    {
        var s=StateForSeed(0);var role=s.Targets[0];
        var pair=s.PairPosition(role);
        var ice=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new(){PlayerRole=role,ScenarioFactory=()=>new DsrSanctity2Scenario(),
            Takeovers=[new(11.7f,new(pair.X,pair.Z))],WriteArtifactsOnFailure=false});
        Assert.That(ice.Deaths.Any(d=>d.Role==role&&d.ActionId==25575),Is.True,ice.ToString);
        var pos=DsrSanctity2Ai.MeteorPosition(s,role,0);
        var link=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new(){PlayerRole=role,ScenarioFactory=()=>new DsrSanctity2Scenario(),
            Takeovers=[new(13.4f,new(pos.X,pos.Z))],WriteArtifactsOnFailure=false});
        Assert.That(link.Deaths.Any(d=>d.ActionId==25578),Is.True,link.ToString);
    }
    [Test]
    public void OutsideHumanMustPressAntiKnockbackRatherThanReceivingBotImmunity()
    {
        var s=StateForSeed(0);var role=s.Targets[0];var pos=DsrSanctity2Ai.MeteorPosition(s,role,4);
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new(){PlayerRole=role,ScenarioFactory=()=>new DsrSanctity2Scenario(),
            Takeovers=[new(21.4f,new(pos.X,pos.Z))],WriteArtifactsOnFailure=false});
        Assert.That(run.Deaths.Any(d=>d.Role==role&&d.ActionId==0&&d.Time<24),Is.True,run.ToString);
    }
    [TestCase(7548u,1209)] [TestCase(7559u,160)]
    public void RealUserActionsGrantTheStatusesThatPreventTheNewKnockback(uint action,int status)
    {
        var checkedStatus=false;
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity2Scenario(),StopAt=1.2f,Probe=p=>
        {
            if(!p.Crossed(1f))return;
            var member=p.Member(PartyRole.MainTank)!;
            AntiKnockbackHandler.Apply(member,action);
            checkedStatus=member.HasStatus((ushort)status);
        },WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(checkedStatus,Is.True);
    }
    [TestCase(6f)] [TestCase(13f)] [TestCase(18f)] [TestCase(28f)]
    public void CancellationAndRepeatedExecutionReleaseActorsAndPuddles(float stop)
    {
        var reusable=new DsrSanctity2Scenario();
        for(var iteration=0;iteration<3;++iteration)
        {
            FakeGame? fake=null;
            var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,iteration,new(){ScenarioFactory=()=>reusable,StopAt=stop,
                ConfigureNative=f=>{fake=f;f.BattleCharas.PacketSpawnDelayFrames=5;},WriteArtifactsOnFailure=false});
            Assert.That(run.Passed,run.ToString);Assert.That(fake!.BattleCharas.OccupiedSlots,Is.Zero);
            Assert.That(fake.EventObjects.OccupiedSlots,Is.Zero);
            Assert.That(reusable.ActiveTowerVisuals,Is.Empty);
        }
    }
    [Test]
    public void NativeReleaseScheduleMatchesRoundedObservedReceiveTimeline()
    {
        var emitted=new Dictionary<uint,List<float>>();var seen=new Dictionary<FakeBattleChara,int>();
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity2Scenario(),Probe=p=>
        {
            foreach(var actor in p.World.Children.OfType<SimEnemy>())
                if(actor.Proxy is FakeBattleChara proxy)
                {
                    foreach(var effect in proxy.ReceivedEffects.Skip(seen.GetValueOrDefault(proxy)))
                    {if(!emitted.TryGetValue(effect.ActionId,out var list))emitted[effect.ActionId]=list=[];list.Add(p.Time);}
                    seen[proxy]=proxy.ReceivedEffects.Count;
                }
        },WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);
        foreach(var (id,time,count) in new[]{(25575u,DsrSanctity2Timing.Storm,4),(28591u,DsrSanctity2Timing.Fire,4),(28592u,DsrSanctity2Timing.Fire,1),(29564u,DsrSanctity2Timing.FirstTowers,8),(25308u,DsrSanctity2Timing.Knockback,1),(28651u,DsrSanctity2Timing.SecondTowers,8)})
        {Assert.That(emitted[id],Has.Count.EqualTo(count));Assert.That(emitted[id].All(t=>Math.Abs(t-time)<.05f),Is.True,$"release {id}");}
        Assert.That(emitted[25577],Has.Count.EqualTo(14));
        for(var i=0;i<7;++i)Assert.That(emitted[25577].Count(t=>Math.Abs(t-DsrSanctity2Timing.Meteor(i))<.05f),Is.EqualTo(2));
    }
    [Test]
    public void RegistrationIsIndependentProvisionalAndNeverCallsPartyMarking()
    {
        var scenarios=ScenarioCatalog.Create().Scenarios.ToList();var index=scenarios.FindIndex(s=>s is DsrSanctity2Scenario);
        Assert.That(scenarios[index-1],Is.TypeOf<AnoMech.Scenarios.Dsr.Sanctity1.DsrSanctity1Scenario>());
        var scenario=scenarios[index];Assert.That(Game.DisplayName(scenario),Is.EqualTo("P2 Sanctity of the Ward (Part 2)"));
        Assert.That(scenario.IsIncomplete,Is.True);Assert.That(scenario.EnablePhaseBgm,Is.True);Assert.That(scenario.Phase,Is.SameAs(DsrZone.P2));
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,0,new(){ScenarioFactory=()=>new DsrSanctity2Scenario(),WriteArtifactsOnFailure=false});
        Assert.That(run.Passed,run.ToString);Assert.That(typeof(AnoMech.Core.Native.Interfaces.Natives).GetProperty("Markings"),Is.Null);
    }
    [Test]
    public void ExactOverridesAndCursedFiltersRejectContradictionsBeforeLaunch()
    {
        var scenario=new DsrSanctity2Scenario();var o=scenario.Overrides;o.AdvancedMode=true;
        o.TowerPattern=Array.FindIndex(DsrSanctity2State.Patterns,p=>!p.ForcedCursed);o.Cursed=true;
        Assert.That(scenario.SettingsConflicts,Is.Not.Empty);
        o.Cursed=null;o.InnerPattern=0;Assert.That(scenario.SettingsConflicts,Is.Not.Empty);
        o.InnerPattern=null;o.TargetPair=12;Assert.That(scenario.SettingsConflicts,Is.Not.Empty);
        o.AdvancedMode=false;Assert.That(scenario.SettingsConflicts,Is.Empty);
    }
    private static DsrSanctity2State StateForSeed(int seed)=>new(new Rng(seed),new(),PartyRole.MeleeDpsA);
    private static DsrSanctity2State State(int pattern,int pair)=>new(new Rng(pattern+pair),new(){AdvancedMode=true,TowerPattern=pattern,TargetPair=pair},PartyRole.MeleeDpsB);
}
