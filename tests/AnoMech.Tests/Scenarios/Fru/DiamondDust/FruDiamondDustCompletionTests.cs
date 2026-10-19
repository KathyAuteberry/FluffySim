using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.DiamondDust;

namespace AnoMech.Tests;

public class FruDiamondDustCompletionTests
{
    private static IEnumerable<TestCaseData> Patterns()
        => from axis in Enumerable.Range(0, 4)
           from mirror in Enumerable.Range(0, 8)
           from still in new[] { false, true }
           from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
           select new TestCaseData(axis, mirror, still, difficulty);
    [TestCaseSource(nameof(Patterns))]
    public void CompleteManaRoutesClear(int axis, int mirror, bool still, BotDifficulty difficulty)
    {
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, axis * 8 + mirror, new()
        {
            ScenarioFactory = () => new FruDiamondDustScenario(),
            PlayerRole = (PartyRole)((axis * 8 + mirror) % 8), BotDifficulty = difficulty,
            WriteArtifactsOnFailure = false,
            Overrides = o =>
            {
                var s = (FruDiamondDustStateOverrides)o;
                s.FirstIcicleAxis = axis; s.OracleSector = mirror; s.Stillness = still;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }
}
