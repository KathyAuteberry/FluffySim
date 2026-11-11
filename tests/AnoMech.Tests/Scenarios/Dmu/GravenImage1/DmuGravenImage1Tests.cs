using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Dmu;
using AnoMech.Scenarios.Dmu.GravenImage1;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Tests;

public class DmuGravenImage1Tests
{
    // Cast at 10.14, first hit 15.21, second 18.34.
    // The human's Provoke takes the real input route (LocalProvokeHandler, ActionResolved); the
    // bot route (SimEnemy.Provoked) is the one the strategy runs use.
    [TestCase(10.80f, MarkedTankbusterSwap.Grade.OnTime, true, false)]
    [TestCase(13.80f, MarkedTankbusterSwap.Grade.OnTime, true, false)]
    [TestCase(16.50f, MarkedTankbusterSwap.Grade.Late, true, false)]
    [TestCase(8.00f, MarkedTankbusterSwap.Grade.Early, false, false)]
    [TestCase(null, MarkedTankbusterSwap.Grade.Missing, false, false)]
    [TestCase(11.50f, MarkedTankbusterSwap.Grade.OnTime, true, true)]
    public void OffTankProvokeIsGradedAgainstTheCastBar(float? provokeAt, MarkedTankbusterSwap.Grade grade, bool passes, bool botRoute)
    {
        var scenario = new DmuGravenImage1Scenario();
        var run = ScenarioRun.Execute(typeof(DmuGravenImage1Scenario), 0, 1, new()
        {
            PlayerRole = PartyRole.OffTank, ScenarioFactory = () => scenario, StopAt = 21f,
            Takeovers = [new(1f, new Vector2(-5f, 0.5f))],
            Probe = p =>
            {
                if (provokeAt is not { } at || !p.Crossed(at) || scenario.LastState?.Objects.Kefka is not { } kefka) return;
                if (botRoute) JobActions.ApplyEffects(p.Member(PartyRole.OffTank)!, Enmity.ProvokeActionId, (ulong)kefka.GameObjectId, Random.Shared);
                else new LocalProvokeHandler().OnAction(ActionType.Action, Enmity.ProvokeActionId, kefka.GameObjectId.ObjectId);
            },
            WriteArtifactsOnFailure = false,
        });
        var outcome = scenario.Swap!.Outcomes.Single();
        Assert.That(outcome.Grade, Is.EqualTo(grade), run.ToString);
        Assert.That(run.Passed, Is.EqualTo(passes), run.ToString);
        if (!passes) Assert.That(run.Deaths.Any(d => d.Cause.Contains("swap failed")), run.ToString);
    }

    private static IEnumerable<TestCaseData> ForcedVariants()
    {
        for (var bits = 0; bits < 64; bits++)
            yield return new TestCaseData(bits).SetName($"DmuGravenImage1 variant {bits:B6}");
    }

    [TestCaseSource(nameof(ForcedVariants))]
    public void EveryForcedVariantClearsWithTheYanStrategy(int bits)
    {
        var run = ScenarioRun.Execute(typeof(DmuGravenImage1Scenario), 0, 1000 + bits, new()
        {
            Overrides = o =>
            {
                var overrides = (DmuGravenImage1StateOverrides)o;
                overrides.DpsTethered = (bits & 1) != 0;
                overrides.Ice1Lie = (bits & 2) != 0;
                overrides.Fire1Lie = (bits & 4) != 0;
                overrides.Fire1StackMarkers = (bits & 8) != 0;
                overrides.Ice2Lie = (bits & 16) != 0;
                overrides.Thunder2Lie = (bits & 32) != 0;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void StandingInTheUnshownConeOfALieKills()
    {
        var scenario = new DmuGravenImage1Scenario();
        var run = ScenarioRun.Execute(typeof(DmuGravenImage1Scenario), 0, 3, new()
        {
            PlayerRole = PartyRole.CasterDps, ScenarioFactory = () => scenario, StopAt = 38f,
            Overrides = o => ((DmuGravenImage1StateOverrides)o).Ice1Lie = true,
            Takeovers = [new(36f, null)],
            Probe = p =>
            {
                if (!p.Crossed(36.5f)) return;
                var realSouthwest = scenario.LastState!.Ice1.RealSouthwestNortheast;
                p.Member(PartyRole.CasterDps)!.SetPosition(new Vector3(-8f, 0f, realSouthwest ? 8f : -8f));
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.CasterDps && d.Cause.Contains("Kefka lied")), run.ToString);
    }

    [Test]
    public void AnUnsoakedTowerWipesTheParty()
    {
        var scenario = new DmuGravenImage1Scenario();
        var run = ScenarioRun.Execute(typeof(DmuGravenImage1Scenario), 0, 5, new()
        {
            PlayerRole = PartyRole.MainTank, ScenarioFactory = () => scenario, StopAt = 47f,
            Overrides = o => ((DmuGravenImage1StateOverrides)o).DpsTethered = true,
            Takeovers = [new(42.3f, null)],
            Probe = p =>
            {
                if (!p.Crossed(42.4f)) return;
                if (!scenario.LastState!.WaveCannonTargets.Contains(PartyRole.MainTank))
                    p.Member(PartyRole.MainTank)!.SetPosition(new Vector3(-4f, 0f, 12f));
            },
            WriteArtifactsOnFailure = false,
        });
        Assume.That(scenario.LastState!.WaveCannonTargets.Contains(PartyRole.MainTank), Is.False, "seed beams the MT; pick another");
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("unsoaked")), run.ToString);
    }

    [Test]
    public void FireResolvesTheMarkersOnTruthAndTheOppositeOnALie()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new FireRoll(Lie: false, StackMarkers: true, PartyRole.RegenHealer, PartyRole.MeleeDpsA).Stacks, Is.True);
            Assert.That(new FireRoll(Lie: false, StackMarkers: false, PartyRole.RegenHealer, PartyRole.MeleeDpsA).Stacks, Is.False);
            Assert.That(new FireRoll(Lie: true, StackMarkers: true, PartyRole.RegenHealer, PartyRole.MeleeDpsA).Stacks, Is.False);
            Assert.That(new FireRoll(Lie: true, StackMarkers: false, PartyRole.RegenHealer, PartyRole.MeleeDpsA).Stacks, Is.True);
        });
    }

    [Test]
    public void IceLieFiresTheUnshownDiagonal()
    {
        var lie = new IceRoll(Lie: true, ShownSouthwestNortheast: true);
        Assert.That(DmuMysteryMagic.SafeFromIce(new Vector2(-5f, 5f), lie), Is.True, "south-west was shown, so it is safe");
        Assert.That(DmuMysteryMagic.SafeFromIce(new Vector2(-5f, -5f), lie), Is.False, "north-west takes the real cone");
    }

    [Test]
    public void ThunderLieFiresTheOtherPairOfTheSameOrientation()
    {
        var truth = new ThunderRoll(Lie: false, RunsSoutheast: true, ShownFirstPair: true);
        var lie = truth with { Lie = true };
        var onShownLine = new Vector2(-3.55f, -24.76f) + 10f * Vector2.Normalize(new Vector2(1f, 1f));
        Assert.That(DmuMysteryMagic.SafeFromThunder(onShownLine, truth), Is.False);
        Assert.That(DmuMysteryMagic.SafeFromThunder(onShownLine, lie), Is.True);
    }
}
