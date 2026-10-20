#pragma warning disable CA1416
using AnoMech.Scenarios.Dsr.P6DoubleDragons;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Map;
using System.Numerics;
namespace AnoMech.Tests;
public class DsrP6DoubleDragonsTests
{
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void FirstHalfClears(int seed)
    {
        var run = ScenarioRun.Execute(typeof(DsrP6DoubleDragonsScenario), 0, seed);
        Assert.That(run.Passed, run.ToString);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void SecondHalfClears(int seed)
    {
        var run = ScenarioRun.Execute(typeof(DsrP6CauterizeScenario), 0, seed);
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(false)] [TestCase(true)]
    public void BoundedAssignmentsClear(bool second)
    {
        var type = second ? typeof(DsrP6CauterizeScenario) : typeof(DsrP6DoubleDragonsScenario);
        for (var seed = 10; seed < 34; seed++)
        {
            var run = ScenarioRun.Execute(type, 0, seed, new() { ShowSimulatedParty = seed % 2 == 0 });
            Assert.That(run.Passed, run.ToString);
        }
    }
    private static IEnumerable<TestCaseData> Seats() =>
        from second in new[] { false, true }
        from role in Enum.GetValues<PartyRole>()
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
        select new TestCaseData(second, role, difficulty);
    [TestCaseSource(nameof(Seats))]
    public void EverySeatClears(bool second, PartyRole role, BotDifficulty difficulty)
    {
        var type = second ? typeof(DsrP6CauterizeScenario) : typeof(DsrP6DoubleDragonsScenario);
        FakeGame? fake = null;
        var run = ScenarioRun.Execute(type, 0, 100 + (int)role, new()
        {
            PlayerRole = role, BotDifficulty = difficulty,
            ConfigureNative = f => { fake = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
    }
    [TestCase(false, 20f)] [TestCase(false, 80f)] [TestCase(false, 94f)]
    [TestCase(true, 35f)] [TestCase(true, 55f)] [TestCase(true, 70f)]
    public void RepeatedEntryCleansUp(bool second, float stop)
    {
        var type = second ? typeof(DsrP6CauterizeScenario) : typeof(DsrP6DoubleDragonsScenario);
        AnoMech.Scenarios.IScenario scenario = second ? new DsrP6CauterizeScenario() : new DsrP6DoubleDragonsScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame? fake = null;
            var run = ScenarioRun.Execute(type, 0, repeat, new()
            {
                ScenarioFactory = () => scenario, StopAt = stop,
                ConfigureNative = f => fake = f,
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
        }
    }
    [TestCase(21.9f, 21.9f, false)] [TestCase(22.1f, 0f, true)] [TestCase(0f, -22.1f, true)]
    public void SquareBoundaryIncludesCorners(float x, float z, bool outside)
    {
        var boundary = new SimArenaBoundary(null!, 22f, "wall", square: true);
        Assert.That(boundary.IsOutside(new(x, 0, z)), Is.EqualTo(outside));
    }

    [Test]
    public void ImbalancedDragonHpFailsAkhAfah()
    {
        var run = ScenarioRun.Execute(typeof(DsrP6DoubleDragonsScenario), 0, 0, new()
        {
            Overrides = o => ((DsrP6DoubleDragonsStateOverrides)o).InitialHpDifference = .05f,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("3%")), Is.True, run.ToString);
    }
    [Test]
    public void MissingVowReceiverFails()
    {
        var run = ScenarioRun.Execute(typeof(DsrP6DoubleDragonsScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, Takeovers = [new(60, new(0, -12))],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("eligible receiver")), Is.True, run.ToString);
    }
    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.OffTank)]
    public void HumanTankNeedsInvulnerabilityForClosingDive(PartyRole role)
    {
        var run = ScenarioRun.Execute(typeof(DsrP6CauterizeScenario), 0, 0, new()
        {
            PlayerRole = role, UserActionsEnabled = true,
            Takeovers = [new(54.9f, role == PartyRole.MainTank ? new(-8, -20) : new(8, -20))],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == role && d.ActionId is 0x6D3E or 0x6D3F), Is.True, run.ToString);
    }
    [TestCase(false)] [TestCase(true)]
    public void PausePreservesClock(bool second)
    {
        var paused = false;
        var type = second ? typeof(DsrP6CauterizeScenario) : typeof(DsrP6DoubleDragonsScenario);
        var run = ScenarioRun.Execute(type, 0, 0, new()
        {
            Probe = p =>
            {
                if (!p.Crossed(19)) return;
                var before = p.Time; p.Game.Paused = true;
                for (var i = 0; i < 30; i++) p.Game.Tick(1f / 60);
                Assert.That(p.Time, Is.EqualTo(before));
                p.Game.Paused = false; paused = true;
            },
        });
        Assert.That(run.Passed, run.ToString); Assert.That(paused, Is.True);
    }
}
