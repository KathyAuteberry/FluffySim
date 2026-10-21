#pragma warning disable CA1416
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P1Knights;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.Ai;
namespace AnoMech.Tests;
public class DsrP1KnightsTests
{
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void ElementalClears(int seed)
    {
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, seed);
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void BoundedAssignmentsClear()
    {
        for (var seed = 10; seed < 34; seed++)
        {
            var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, seed, new() { ShowSimulatedParty = seed % 2 == 0 });
            Assert.That(run.Passed, run.ToString);
        }
    }
    private static IEnumerable<TestCaseData> Seats() =>
        from role in Enum.GetValues<PartyRole>()
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
        select new TestCaseData(role, difficulty);
    [TestCaseSource(nameof(Seats))]
    public void EverySeatClears(PartyRole role, BotDifficulty difficulty)
    {
        FakeGame? fake = null;
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 100 + (int)role, new()
        {
            PlayerRole = role, BotDifficulty = difficulty,
            ConfigureNative = f => { fake = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
    }
    [TestCase(18f)] [TestCase(60f)] [TestCase(82f)] [TestCase(125f)] [TestCase(149f)]
    public void RestartCleansUp(float stop)
    {
        var scenario = new DsrP1KnightsScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame? fake = null;
            var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, repeat, new()
            { ScenarioFactory = () => scenario, StopAt = stop, ConfigureNative = f => fake = f });
            Assert.That(run.Passed, run.ToString); Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
        }
    }
    [TestCase(PartyRole.OffTank, 68f)] [TestCase(PartyRole.PhysRangedDps, 87f)]
    public void HumanAssignedInterruptMustBeUsed(PartyRole role, float takeover)
    {
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 0, new()
        {
            PlayerRole = role, UserActionsEnabled = true, Takeovers = [new(takeover, null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("interrupt")), Is.True, run.ToString);
    }
    [TestCase(PartyRole.MainTank, 18.9f)] [TestCase(PartyRole.OffTank, 64.7f)]
    public void HumanTankNeedsInvulnerability(PartyRole role, float takeover)
    {
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 0, new()
        {
            PlayerRole = role, UserActionsEnabled = true, Takeovers = [new(takeover, null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == role && d.ActionId is 0x62D1 or 0x62D5), Is.True, run.ToString);
    }
    [Test]
    public void CirclePriorityIsDpsOrderWestToEast()
    {
        for (var seed = 0; seed < 24; seed++)
        {
            var state = new DsrP1KnightsState(new Rng(seed));
            var pair = state.Symbols.Where(p => p.Value == ChainSymbol.Circle).Select(p => p.Key).Order().ToArray();
            Assert.That(state.ChainBearing(pair[0]), Is.EqualTo(270));
            Assert.That(state.ChainBearing(pair[1]), Is.EqualTo(90));
        }
    }

    [Test]
    public void RemovingTheOmenDoesNotRemoveDimensionDamage()
    {
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 0, new()
        {
            StopAt = 19.28f,
            Takeovers = [new(19f, new System.Numerics.Vector2(10, 0))],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == 0x62DA), Is.True);
    }

    [Test]
    public void DimensionCastRetainsCastBarWithoutAnAdvanceFloorOmen()
    {
        var checkedCast = false;
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 0, new()
        {
            StopAt = 20,
            Probe = p =>
            {
                if (!p.Crossed(15)) return;
                var actor = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Single(e => e.BNpcBaseId == 12602);
                var cast = ((FakeBattleChara)actor.Proxy!).ReceivedCasts.Last(c => c.ActionId == 0x62DA);
                Assert.That(cast.CastTime, Is.GreaterThan(0));
                Assert.That(cast.OmenDelay, Is.GreaterThan(cast.CastTime));
                checkedCast = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedCast, Is.True);
    }

    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.CasterDps)]
    public void PrisonPhysicallyPullsAStationaryHumanAndBotsBeforeBoundaryActivation(PartyRole role)
    {
        FakeGame? native = null;
        var checkedPull = false;
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 0, new()
        {
            PlayerRole = role, StopAt = 115,
            ConfigureNative = f => native = f,
            Takeovers = [new(110.2f, new System.Numerics.Vector2(0, -10))],
            Probe = p =>
            {
                if (!p.Crossed(114.1f)) return;
                Assert.That(native!.Timeline.Rows.Any(r => r["kind"] == "ac_self" && r["id"] == "0xF1"), Is.True);
                foreach (var member in p.World.Party.ActiveMembers())
                    Assert.That(System.Numerics.Vector3.Distance(member.Position, DsrP1KnightsState.PrisonCentre), Is.LessThan(7));
                checkedPull = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedPull, Is.True);
    }

    [Test]
    public void HallowingUsesInterruptibleNativeCastBar()
    {
        var checkedBar = false;
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 0, new()
        {
            StopAt = 69,
            Probe = p =>
            {
                if (!p.Crossed(68.2f)) return;
                var actor = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Single(e => e.BNpcBaseId == 12601);
                var cast = ((FakeBattleChara)actor.Proxy!).ReceivedCasts.Last(c => c.ActionId == 0x62D0);
                Assert.That(cast.Interruptible, Is.True); checkedBar = true;
            },
        });
        Assert.That(run.Passed, run.ToString); Assert.That(checkedBar, Is.True);
    }
}
