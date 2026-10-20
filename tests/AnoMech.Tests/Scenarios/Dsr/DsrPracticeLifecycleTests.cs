#pragma warning disable CA1416
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.Strength;
using AnoMech.Scenarios.Dsr.Strength2;
using AnoMech.Scenarios.Dsr.Sanctity1;
using AnoMech.Scenarios.Dsr.Sanctity2;

namespace AnoMech.Tests;

public class DsrPracticeLifecycleTests
{
    private static IEnumerable<TestCaseData> Runs()
    {
        Type[] scenarios = [typeof(DsrStrengthScenario), typeof(DsrStrength2Scenario),
            typeof(DsrSanctity1Scenario), typeof(DsrSanctity2Scenario)];
        return from scenario in scenarios
               from role in Enum.GetValues<PartyRole>()
               from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
               select new TestCaseData(scenario, role, difficulty);
    }

    [TestCaseSource(nameof(Runs))]
    public void EverySeatClearsWithDelayedNativeActorsAndReleasesThem(Type type, PartyRole role, BotDifficulty difficulty)
    {
        FakeGame? native = null;
        var run = ScenarioRun.Execute(type, 0, 2300 + (int)role * 13, new()
        {
            PlayerRole = role, BotDifficulty = difficulty,
            ShowSimulatedParty = (int)role % 2 == 0,
            ConfigureNative = f =>
            {
                native = f;
                f.BattleCharas.PacketSpawnDelayFrames = 5;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
    }
}
