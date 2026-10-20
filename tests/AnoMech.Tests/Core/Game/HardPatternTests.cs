#pragma warning disable CA1416
using AnoMech.Core.Game.Ai;
using AnoMech.Scenarios.Fru.Darklit;
using AnoMech.Scenarios.Dsr.Sanctity2;

namespace AnoMech.Tests;

public class HardPatternTests
{
    public static IEnumerable<TestCaseData> DarklitPatterns() => Enumerable.Range(0,1152).Select(i=>new TestCaseData(i));
    public static IEnumerable<TestCaseData> MeteorPatterns() => Enumerable.Range(0,810).Select(i=>new TestCaseData(i));

    [TestCaseSource(nameof(DarklitPatterns))]
    [Category("Extended")]
    public void HardInvisibleDarklitAllAssignmentGraphs(int index)
    {
        var run=ScenarioRun.Execute(typeof(FruDarklitScenario),0,index,new()
        {
            BotDifficulty=BotDifficulty.Hard,ShowSimulatedParty=false,
            Overrides=o=>{var x=(FruDarklitStateOverrides)o;x.Advanced=true;x.ExactPattern=index;},
            WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,run.ToString);
    }
    [TestCaseSource(nameof(MeteorPatterns))]
    [Category("Extended")]
    public void HardInvisibleMeteorsAllOuterPatterns(int index)
    {
        var run=ScenarioRun.Execute(typeof(DsrSanctity2Scenario),0,index,new()
        {
            BotDifficulty=BotDifficulty.Hard,ShowSimulatedParty=false,
            Overrides=o=>{var x=(DsrSanctity2StateOverrides)o;x.AdvancedMode=true;x.TowerPattern=index;},
            WriteArtifactsOnFailure=false,
        });
        Assert.That(run.Passed,run.ToString);
    }
}
