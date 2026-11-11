using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dmu;
using AnoMech.Scenarios.Dmu.GravenImage3;

namespace AnoMech.Tests;

public class DmuGravenImage3Tests
{
    private static IEnumerable<TestCaseData> ForcedVariants()
    {
        for (var bits = 0; bits < 64; bits++)
            yield return new TestCaseData(bits).SetName($"DmuGravenImage3 variant {bits:B6}");
    }

    // The bots chain every confused player through all 16 paddles and live through the chase.
    [TestCaseSource(nameof(ForcedVariants))]
    public void EveryForcedVariantClearsAndResolvesEveryPaddle(int bits)
    {
        var scenario = new DmuGravenImage3Scenario();
        var run = ScenarioRun.Execute(typeof(DmuGravenImage3Scenario), 0, 3000 + bits, new()
        {
            ScenarioFactory = () => scenario,
            Overrides = o =>
            {
                var overrides = (DmuGravenImage3StateOverrides)o;
                overrides.DpsIdentical = (bits & 1) != 0;
                overrides.DpsConfused = (bits & 2) != 0;
                overrides.AveMaria = (bits & 4) != 0;
                overrides.FireLie = (bits & 8) != 0;
                overrides.FireStackMarkers = (bits & 16) != 0;
                overrides.ThunderLie = (bits & 32) != 0;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That((scenario.ArrowsDropped, scenario.ArrowsLost, scenario.ArrowsResolved), Is.EqualTo((16, 0, 16)), run.ToString);
    }

    // A confused local player rides their whole chain: the paddle, not the chase, decides when
    // they move on, and the carry is their own client's move.
    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.RegenHealer)]
    public void AConfusedPlayerRidesEveryPaddleOfTheirChain(PartyRole human)
    {
        var scenario = new DmuGravenImage3Scenario();
        var run = ScenarioRun.Execute(typeof(DmuGravenImage3Scenario), 0, 12, new()
        {
            PlayerRole = human, ScenarioFactory = () => scenario,
            Overrides = o => ((DmuGravenImage3StateOverrides)o).DpsConfused = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Rides.GetValueOrDefault(human), Is.EqualTo(4), run.ToString);
        Assert.That(scenario.ArrowsResolved, Is.EqualTo(16), run.ToString);
    }

    [Test]
    public void ATurnedPairGoesToTheQuadrantItsArrowsName()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DmuGravenImage3State.DropSpots(new(Arrow.West, Arrow.North, true)), Is.EqualTo((new Vector2(-6f, -13f), new Vector2(-6f, -7f))));
            Assert.That(DmuGravenImage3State.DropSpots(new(Arrow.West, Arrow.North, false)), Is.EqualTo((new Vector2(-6f, -7f), new Vector2(-6f, -13f))));
            Assert.That(DmuGravenImage3State.DropSpots(new(Arrow.North, Arrow.East, true)).Early, Is.EqualTo(new Vector2(13f, -6.5f)));
            Assert.That(DmuGravenImage3State.DropSpots(new(Arrow.East, Arrow.South, true)).Early, Is.EqualTo(new Vector2(6f, 12.5f)));
            Assert.That(DmuGravenImage3State.DropSpots(new(Arrow.South, Arrow.West, true)).Early, Is.EqualTo(new Vector2(-13f, 6f)));
            Assert.That(DmuGravenImage3State.DropSpots(new(Arrow.South, Arrow.South, true)).Early.X, Is.EqualTo(-13f), "a pair of south arrows goes north-west");
        });
    }

    [Test]
    public void TurnedArrowsAreAlwaysAQuarterTurnClockwise()
    {
        var state = new DmuGravenImage3State(new Rng(5), new DmuGravenImage3StateOverrides());
        foreach (var pair in state.Arrows8.Values.Where(p => !p.Identical))
            Assert.That(pair.Second, Is.EqualTo(DmuGravenImage3State.Clockwise(pair.First)));
    }

    [Test]
    public void FacingTheWrongWayForAveMariaGivesDamageDown()
    {
        var damageDown = false;
        var run = ScenarioRun.Execute(typeof(DmuGravenImage3Scenario), 0, 6, new()
        {
            PlayerRole = PartyRole.CasterDps, StopAt = 43f,
            Overrides = o => ((DmuGravenImage3StateOverrides)o).AveMaria = true,
            Takeovers = [new(41.2f, null, Facing: 0f)],
            Probe = p =>
            {
                if (p.Crossed(42.5f)) damageDown = p.Member(PartyRole.CasterDps)!.HasStatus(DmuConstants.StatusId.DamageDown);
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(damageDown, Is.True, run.ToString);
    }
}
