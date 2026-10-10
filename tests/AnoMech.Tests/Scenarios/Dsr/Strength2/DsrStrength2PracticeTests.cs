using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.Strength2;
using Newtonsoft.Json;

namespace AnoMech.Tests;

public class DsrStrength2PracticeTests
{
    private static IEnumerable<TestCaseData> Choices() => from role in DsrStrength2State.NonTanks
        from assignment in Enum.GetValues<DsrStrength2PracticeAssignment>() select new TestCaseData(role, assignment);

    [TestCaseSource(nameof(Choices))]
    public void PracticeSelectsOnlyExistingValidPatterns(PartyRole role, DsrStrength2PracticeAssignment assignment)
    {
        var candidates = DsrStrength2State.PracticePatterns(role, assignment);
        Assert.That(candidates.Count, Is.EqualTo(assignment switch
        {
            DsrStrength2PracticeAssignment.Random => 120,
            DsrStrength2PracticeAssignment.SkywardLeapAny or DsrStrength2PracticeAssignment.Unmarked => 60,
            _ => 20,
        }));
        for (var seed = 0; seed < 100; ++seed)
        {
            var o = new DsrStrength2StateOverrides { PracticeAssignment = assignment };
            var state = new DsrStrength2State(new Rng(seed), o, role);
            var repeated = new DsrStrength2State(new Rng(seed), o, role);
            Assert.That(state.Leaps, Is.EqualTo(repeated.Leaps));
            Assert.That(state.TowerCommunicators, Is.EqualTo(repeated.TowerCommunicators));
            Assert.That(candidates.Any(p => DsrStrength2State.Orders[p.Order].Select(i => DsrStrength2State.LeapTrios[p.Trio][i]).SequenceEqual(state.Leaps)), Is.True);
            Assert.That(state.Leaps.Concat(state.TowerCommunicators).Distinct().Count(), Is.EqualTo(6));
            var index = Array.IndexOf(state.Leaps, role);
            switch (assignment)
            {
                case DsrStrength2PracticeAssignment.SkywardLeapAny: Assert.That(index, Is.GreaterThanOrEqualTo(0)); break;
                case DsrStrength2PracticeAssignment.SkywardLeapWest: Assert.That(index, Is.EqualTo(0)); break;
                case DsrStrength2PracticeAssignment.SkywardLeapEast: Assert.That(index, Is.EqualTo(1)); break;
                case DsrStrength2PracticeAssignment.SkywardLeapSouth: Assert.That(index, Is.EqualTo(2)); break;
                case DsrStrength2PracticeAssignment.Unmarked: Assert.That(index, Is.EqualTo(-1)); break;
            }
        }
    }

    [TestCaseSource(nameof(Choices))]
    public void FullPracticeRunClearsWithUnchangedBots(PartyRole role, DsrStrength2PracticeAssignment assignment)
    {
        var run = ScenarioRun.Execute(typeof(DsrStrength2Scenario), 0, 42, new()
        {
            PlayerRole = role,
            Overrides = o => ((DsrStrength2StateOverrides)o).PracticeAssignment = assignment,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.OffTank)]
    public void TankNeverReceivesBlueEvenWithSavedNonTankPracticeChoice(PartyRole role)
    {
        foreach (var choice in Enum.GetValues<DsrStrength2PracticeAssignment>())
        {
            var state = new DsrStrength2State(new Rng(10), new() { PracticeAssignment = choice }, role);
            Assert.That(state.Leaps, Does.Not.Contain(role));
            Assert.That(state.Leaps, Is.EqualTo(new DsrStrength2State(new Rng(10), new()).Leaps));
        }
    }

    [Test]
    public void RandomPreservesLegacyRngDraws()
    {
        for (var seed = 0; seed < 100; ++seed)
        {
            var rng = new Rng(seed);
            var state = new DsrStrength2State(rng, new(), PartyRole.MeleeDpsB);
            var expected = new Rng(seed);
            Assert.That(state.KnightPattern, Is.EqualTo(expected.Next(8)));
            var trio = DsrStrength2State.LeapTrios[expected.Next(20)];
            Assert.That(state.Leaps, Is.EqualTo(DsrStrength2State.Orders[expected.Next(6)].Select(i => trio[i])));
            var unmarked = DsrStrength2State.NonTanks.Except(state.Leaps).ToArray();
            Assert.That(state.TowerCommunicators, Is.EqualTo(DsrStrength2State.Orders[expected.Next(6)].Select(i => unmarked[i])));
            var tetherOrder = DsrStrength2State.Orders[expected.Next(6)].Select(i => unmarked[i]).ToArray();
            Assert.That(state.InitialTetherTargets, Is.EqualTo(tetherOrder[..2]));
            Assert.That(state.RageTarget, Is.EqualTo(tetherOrder[2]));
            Assert.That(rng.Next(10000), Is.EqualTo(expected.Next(10000)));
        }
    }

    [Test]
    public void LegacyJsonAndModeSwitchPreserveExactOverrides()
    {
        var o = JsonConvert.DeserializeObject<DsrStrength2StateOverrides>("{\"KnightPattern\":7,\"LeapTrio\":3,\"LeapOrder\":4,\"TowerCommunication\":5,\"TetherTargets\":2}")!;
        Assert.That(o.UsesAdvancedPatterns, Is.True);
        var original = new DsrStrength2State(new Rng(1), o);
        o.PracticeAssignment = DsrStrength2PracticeAssignment.Unmarked;
        Assert.That(new DsrStrength2State(new Rng(1), o, PartyRole.RegenHealer).Leaps, Is.EqualTo(original.Leaps));
        o.AdvancedMode = false;
        Assert.That(new DsrStrength2State(new Rng(1), o, PartyRole.RegenHealer).Leaps, Does.Not.Contain(PartyRole.RegenHealer));
        o.AdvancedMode = true;
        var roundTrip = JsonConvert.DeserializeObject<DsrStrength2StateOverrides>(JsonConvert.SerializeObject(o))!;
        var restored = new DsrStrength2State(new Rng(1), roundTrip);
        Assert.That(restored.Leaps, Is.EqualTo(original.Leaps));
        Assert.That(restored.TowerCommunicators, Is.EqualTo(original.TowerCommunicators));
        Assert.That(restored.InitialTetherTargets, Is.EqualTo(original.InitialTetherTargets));
        Assert.That(restored.KnightPattern, Is.EqualTo(7));
    }
    [TestCase(false)] [TestCase(true)]
    public void QuickUnmarkedRequiresReadingFixedBotClaims(bool contest)
    {
        var human = PartyRole.MeleeDpsB;
        var o = new DsrStrength2StateOverrides { PracticeAssignment = DsrStrength2PracticeAssignment.Unmarked };
        var expected = new DsrStrength2State(new Rng(42), o, human);
        var bots = expected.TowerCommunicators.Where(r => r != human).ToArray();
        var destination = expected.TowerPosition(contest ? bots[0] : human);
        var observed = 0;
        var run = ScenarioRun.Execute(typeof(DsrStrength2Scenario), 0, 42, new()
        {
            PlayerRole = human,
            Overrides = overrides => ((DsrStrength2StateOverrides)overrides).PracticeAssignment = DsrStrength2PracticeAssignment.Unmarked,
            Takeovers = [new(8.6f, new(expected.CommunicationPosition(bots[0]).X, expected.CommunicationPosition(bots[0]).Z)),
                new(11.5f, new(expected.StackPosition.X, expected.StackPosition.Z)), new(14.9f, new(destination.X, destination.Z))],
            Probe = p =>
            {
                if (p.Crossed(9.8f) || p.Crossed(10.4f))
                {
                    observed++;
                    var actual = ((DsrStrength2Scenario)p.Game.ActiveScenario!).LastState!;
                    Assert.That(actual.TowerCommunicators, Is.EqualTo(expected.TowerCommunicators));
                    foreach (var bot in bots)
                        Assert.That(System.Numerics.Vector3.Distance(p.World.Party.Get(bot)!.Position, expected.CommunicationPosition(bot)), Is.LessThan(.05f));
                }
                if (p.Crossed(15.5f))
                    foreach (var bot in bots)
                        Assert.That(System.Numerics.Vector3.Distance(p.World.Party.Get(bot)!.Position, expected.TowerPosition(bot)), Is.LessThan(.05f));
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(observed, Is.EqualTo(2));
        Assert.That(run.Passed, Is.EqualTo(!contest), run.ToString);
    }

}
