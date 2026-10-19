using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.MirrorMirror;

namespace AnoMech.Tests;

public class FruMirrorMirrorTests
{
    private static IEnumerable<TestCaseData> Patterns()
        => from blue in Enumerable.Range(0, 8)
           from red in Enumerable.Range(0, 8).Where(r => r != blue && (r + 2) % 8 != blue)
           from pairs in new[] { false, true }
           from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
           select new TestCaseData(blue, red, pairs, difficulty);
    [TestCaseSource(nameof(Patterns))]
    public void ManaMirrorsAndBanishSurvive(int blue, int red, bool pairs, BotDifficulty difficulty)
    {
        var run = ScenarioRun.Execute(typeof(FruMirrorMirrorScenario), 0, blue * 8 + red, new()
        {
            ScenarioFactory = () => new FruMirrorMirrorScenario(),
            PlayerRole = (PartyRole)((blue + red) % 8), BotDifficulty = difficulty, WriteArtifactsOnFailure = false,
            Overrides = o =>
            {
                var s = (FruMirrorMirrorStateOverrides)o;
                s.Blue = blue; s.Red = red; s.Pairs = pairs;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }
}
