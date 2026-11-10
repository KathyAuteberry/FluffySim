using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Tea;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Tea.P4PerfectAlexander;

namespace AnoMech.Tests;

public class ManaPerfectAlexanderTests
{
    private static IEnumerable<TestCaseData> Practices()
        => from practice in Enum.GetValues<P4Practice>()
           from seed in new[] { 3, 31 }
           select new TestCaseData(practice, seed);

    [TestCaseSource(nameof(Practices))]
    public void ManaPracticesComplete(P4Practice practice, int seed)
    {
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, seed, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP4PerfectAlexanderScenario(),
            Spectator = true,
            BotDifficulty = BotDifficulty.Hard,
            Overrides = o => ((TeaP4PerfectAlexanderStateOverrides)o).Practice = practice,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void AllRolesCompleteOrdinaryAndSpectatorPractice([Values] PartyRole role, [Values(false, true)] bool spectator)
    {
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 913, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP4PerfectAlexanderScenario(),
            PlayerRole = role,
            Spectator = spectator,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(P4Practice.TheFinalWord, true, 23.21f)]
    [TestCase(P4Practice.TheFinalWord, false, 34.32f)]
    [TestCase(P4Practice.FateCalibrationAlpha, true, 97.97f)]
    [TestCase(P4Practice.FateCalibrationAlpha, false, 102f)]
    public void MotionShowsSustainedNativeBackAndForthMovement(P4Practice practice, bool first, float check)
    {
        var samples = PerRole.All.ToDictionary(role => role, _ => new List<System.Numerics.Vector3>());
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 31, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP4PerfectAlexanderScenario(),
            Spectator = true,
            StopAt = check + 0.3f,
            Overrides = value =>
            {
                var o = (TeaP4PerfectAlexanderStateOverrides)value;
                o.Practice = practice;
                o.FinalWordMotionFirst = first;
                o.AlphaFirstMotion = first;
                o.AlphaSecondMotion = true;
            },
            Probe = p =>
            {
                if (p.Time < check - 1.95f || p.Time > check + 0.2f) return;
                foreach (var role in PerRole.All)
                    samples[role].Add(p.Member(role)!.Proxy!.Position);
            },
        });
        Assert.That(run.Passed, run.ToString);
        foreach (var (role, poses) in samples)
        {
            Assert.That(poses.Count, Is.GreaterThan(20), role.ToString());
            var travelled = poses.Zip(poses.Skip(1), System.Numerics.Vector3.Distance).Sum();
            Assert.That(travelled, Is.GreaterThan(1.5f), $"{role} emitted native movement");
            var steps = poses.Zip(poses.Skip(1), (a, b) => b - a).Where(v => v.LengthSquared() > 1e-8f).ToArray();
            Assert.That(steps, Is.Not.Empty, role.ToString());
            var reversed = new[] { System.Numerics.Vector3.UnitX, System.Numerics.Vector3.UnitZ }.Any(axis =>
                steps.Any(v => System.Numerics.Vector3.Dot(v, axis) > 0.001f)
                && steps.Any(v => System.Numerics.Vector3.Dot(v, axis) < -0.001f));
            Assert.That(reversed, Is.True, $"{role} moves both ways");
        }
    }

    [Test]
    public void OpticalHealersSpreadUnderTheBossAcrossItsCentre()
    {
        var checkedPositions = false;
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 31, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP4PerfectAlexanderScenario(),
            Spectator = true,
            StopAt = 47f,
            Overrides = value =>
            {
                var o = (TeaP4PerfectAlexanderStateOverrides)value;
                o.Practice = P4Practice.OpticalSight;
                o.OpticalSpreadFirst = true;
            },
            Probe = p =>
            {
                if (!p.Crossed(46f)) return;
                var h1 = p.Member(PartyRole.RegenHealer)!.Position;
                var h2 = p.Member(PartyRole.ShieldHealer)!.Position;
                Assert.That(h1.X, Is.EqualTo(-5f).Within(0.5f));
                Assert.That(h2.X, Is.EqualTo(5f).Within(0.5f));
                Assert.That(h1.Z, Is.EqualTo(0f).Within(0.5f));
                Assert.That(h2.Z, Is.EqualTo(0f).Within(0.5f));
                checkedPositions = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedPositions, Is.True);
    }

    [Test]
    public void OpticalStacksRollOneSupportAndOneDpsDeterministically()
    {
        for (var seed = 0; seed < 64; seed++)
        {
            var first = new TeaP4PerfectAlexanderState(new Rng(seed), SimParty.Empty, new());
            var repeated = new TeaP4PerfectAlexanderState(new Rng(seed), SimParty.Empty, new());
            Assert.That(first.OpticalStackTargets.Count(TeaP4PerfectAlexanderState.IsSupport), Is.EqualTo(1));
            Assert.That(first.OpticalStackTargets, Is.EqualTo(repeated.OpticalStackTargets));
        }
    }

    [Test]
    public void TwoSupportPlayersCannotBothRequestTheOpticalStack()
    {
        PerRole.ForceSeats = true;
        try
        {
            var settings = new TeaP4PerfectAlexanderStateOverrides();
            settings.OpticalStack[PartyRole.MainTank] = true;
            settings.OpticalStack[PartyRole.OffTank] = true;
            Assert.That(settings.Validate().Any, Is.True);
        }
        finally { PerRole.ForceSeats = false; }
    }

    [TestCase(P4Practice.CapitalPunishmentOne, 118.9f, PartyRole.MainTank)]
    [TestCase(P4Practice.CapitalPunishmentTwo, 218.9f, PartyRole.OffTank)]
    [TestCase(P4Practice.CapitalPunishmentThree, 264.6f, PartyRole.MainTank)]
    public void CapitalPunishmentKillsABotWithoutItsDefensiveStatuses(P4Practice practice, float removeAt, PartyRole holder)
    {
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 31, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP4PerfectAlexanderScenario(),
            Spectator = true,
            WriteArtifactsOnFailure = false,
            Overrides = o => ((TeaP4PerfectAlexanderStateOverrides)o).Practice = practice,
            Probe = p =>
            {
                if (!p.Crossed(removeAt)) return;
                var tank = p.Member(holder)!;
                foreach (var status in tank.ActiveStatuses.Where(s => Mitigation.ByStatusId.ContainsKey(s.StatusId)).ToList())
                    tank.RemoveStatus(status.StatusId);
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Any(d => d.Role == holder && d.ActionId == TeaConstants.ActionId.OrdainedCapitalPunishmentHit), Is.True, run.ToString);
    }

    [Test]
    public void FinalWordBeaconsUseManaWestAndEastPositions()
    {
        var scenario = new TeaP4PerfectAlexanderScenario();
        float? lightX = null;
        float? darkX = null;
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 5, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            Spectator = true,
            StopAt = 26f,
            Overrides = o => ((TeaP4PerfectAlexanderStateOverrides)o).FinalWordMotionFirst = true,
            Probe = p =>
            {
                if (!p.Crossed(25f)) return;
                lightX = p.Member(scenario.LastState!.LightBeacon)!.Position.X;
                darkX = p.Member(scenario.LastState.DarkBeacon)!.Position.X;
            },
        });
        Assert.Multiple(() =>
        {
            Assert.That(run.Passed, run.ToString);
            Assert.That(lightX, Is.LessThan(-13f));
            Assert.That(darkX, Is.GreaterThan(13f));
        });
    }
}
