#pragma warning disable CA1416
using AnoMech.Scenarios.Dsr.P5Wrath;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.Ai;
namespace AnoMech.Tests;
public class DsrP5WrathTests
{
    [Test]
    public void BoundedAssignmentsAndEverySeatClear()
    {
        for (var seed = 0; seed < 24; seed++)
        {
            var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, seed + 100, new()
            { PlayerRole = (PartyRole)(seed % 8), BotDifficulty = seed % 2 == 0 ? BotDifficulty.Easy : BotDifficulty.Hard });
            Assert.That(run.Passed, run.ToString);
        }
    }

    [TestCase(0f)] [TestCase(24f)] [TestCase(38f)] [TestCase(56f)] [TestCase(85f)]
    public void RestartReusesTheScenarioAndReleasesNativeObjects(float stop)
    {
        var scenario = new DsrP5WrathScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame native = null!;
            var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, repeat, new()
            {
                ScenarioFactory = () => scenario, StopAt = stop,
                ConfigureNative = f => { native = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(native.BattleCharas.OccupiedSlots, Is.Zero);
            Assert.That(native.EventObjects.OccupiedSlots, Is.Zero);
        }
    }

    private static IEnumerable<TestCaseData> Seats() => from role in Enum.GetValues<PartyRole>()
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard } select new TestCaseData(role, difficulty);

    [TestCaseSource(nameof(Seats))]
    public void EachSeatAndDifficultyClears(PartyRole role, BotDifficulty difficulty)
    {
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 900 + (int)role, new()
        { PlayerRole = role, BotDifficulty = difficulty });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(24f)] [TestCase(38f)] [TestCase(55f)]
    public void PauseFreezesTheMechanicClockThenBotsFinish(float at)
    {
        FakeGame native = null!;
        var paused = false;
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 47, new()
        {
            ConfigureNative = f => native = f,
            Probe = p =>
            {
                if (paused || !p.Crossed(at)) return;
                paused = true; var time = p.Time;
                p.Game.Paused = true;
                for (var frame = 0; frame < 120; frame++) native.Frame(1f / 60f, p.Game.Tick);
                Assert.That(p.Time, Is.EqualTo(time)); p.Game.Paused = false;
            },
        });
        Assert.That(paused, Is.True); Assert.That(run.Passed, run.ToString);
    }

    [TestCase(false)] [TestCase(true)]
    public void StationaryHumanTriggersTwisterUnlessGodMode(bool godMode)
    {
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.CasterDps, GodMode = godMode, StopAt = 47,
            Takeovers = [new(44.9f, null)], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == AnoMech.Scenarios.Dsr.DsrP5Constants.ActionId.Twister), Is.EqualTo(!godMode), run.ToString);
    }

    [Test]
    public void HumanTankNeedsInvulnerabilityForHeelCombo()
    {
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, UserActionsEnabled = true,
            Takeovers = [new(77f, null)], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == AnoMech.Scenarios.Dsr.DsrP5Constants.ActionId.AscalonsMight), Is.True, run.ToString);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void ElementalWrathClears(int seed)
    {
        var scenario = new DsrP5WrathScenario();
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, seed, new() { ScenarioFactory = () => scenario });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Completed, Is.True);
    }
}
