using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Upstream.P5FulgentBlade;

namespace AnoMech.Tests;

public class FruAkhMornTests
{
    public static IEnumerable<TestCaseData> AkhMornRoles()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        foreach (var difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard })
            yield return new TestCaseData(role, difficulty);
    }

    [TestCaseSource(nameof(AkhMornRoles))]
    public void FulgentAkhMornClearsWithManaLightParties(PartyRole role, BotDifficulty difficulty)
    {
        for (var seed = 0; seed < 8; seed++)
        {
            var run = ScenarioRun.Execute(typeof(FruP5FulgentBladeScenario), 0, seed,
                new ScenarioRunOptions { PlayerRole = role, BotDifficulty = difficulty });
            Assert.That(run.Passed, run.ToString);
        }
    }

    [Test]
    public void LeavingYourLightPartyFailsAkhMorn()
    {
        var run = ScenarioRun.Execute(typeof(FruP5FulgentBladeScenario), 0, 0, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.MeleeDpsA,
            Takeovers = [new Takeover(33f, new(0, 0))],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.ActionId is 40311 or 40312), Is.True, run.ToString);
    }

}
