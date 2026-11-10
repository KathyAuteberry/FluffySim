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
