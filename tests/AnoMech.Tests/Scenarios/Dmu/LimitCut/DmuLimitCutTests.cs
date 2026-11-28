using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dmu.LimitCut;
using AnoMech.Scenarios.Umad;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

public class DmuLimitCutTests
{
    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    public void EitherTankCanHoldChaos(int seed, bool mainTankOnChaos)
    {
        var run = ScenarioRun.Execute(typeof(DmuLimitCutScenario), 0, seed, new()
        {
            Overrides = o => ((DmuLimitCutStateOverrides)o).MainTankOnChaos = mainTankOnChaos,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void CyclonesWithoutATankLimitBreakKillTheStack()
        => Negative<DmuLimitCutScenario>(CasterDps)
            .At(14.0f, p => p.World.Party.LimitBreak.Set(0f))
            .ShouldKill(UmadConstants.ActionId.Cyclone, PerRole.All);

    private const ushort Rampart = 1191;
    private const ushort DarkMind = 746;
    private const ushort Oblation = 2682;
    private const ushort ShadowedVigil = 3835;

    // Share, MT first: the main tank stands on Exdeath for the first hit at ~38.6s.
    private const float BeforeFirstThunder = 38.0f;
    // The off tank has swapped in for the second hit at ~41.6s.
    private const float BeforeSecondThunder = 41.4f;

    private static void Give(ScenarioProbe p, params ushort[] statuses)
    {
        foreach (var status in statuses) p.World.Party.Player!.AddStatus(status, 20f);
    }

    // The second hit of the set on the tank that took the first.
    private static void GiveSecondHitOnTheSameTank(ScenarioProbe p, params ushort[] statuses)
    {
        p.World.Party.Player!.AddStatus(UmadConstants.StatusId.LightningResistanceDownII, 3.96f);
        Give(p, statuses);
    }

    private static ScenarioRun RunThunder(PartyRole role, float freezeAt, Action<ScenarioProbe> setUp)
        => ScenarioRun.Execute(typeof(DmuLimitCutScenario), 0, 1, new()
        {
            PlayerRole = role,
            UserActionsEnabled = true,
            Takeovers = [new Takeover(freezeAt, null)],
            Probe = p => { if (p.Crossed(freezeAt)) setUp(p); },
            WriteArtifactsOnFailure = false,
        });

    [Test]
    public void TheFirstThunderTakesRampartAndTwoShortMitigations()
    {
        var run = RunThunder(MainTank, BeforeFirstThunder, p => Give(p, Rampart, DarkMind, Oblation));
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void TheFirstThunderKillsOnRampartAndOneShortMitigation()
        => Negative<DmuLimitCutScenario>(MainTank)
            .WithMitigationChecks()
            .FreezeAt(BeforeFirstThunder)
            .At(BeforeFirstThunder, p => Give(p, Rampart, Oblation))
            .Seed(1)
            .ShouldKill(UmadConstants.ActionId.ThunderIII_Resolve, MainTank);

    [Test]
    public void TheSecondThunderOnTheSameTankTakesRampartAndAFortyPercent()
    {
        var run = RunThunder(OffTank, BeforeSecondThunder, p => GiveSecondHitOnTheSameTank(p, Rampart, ShadowedVigil));
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void TheSecondThunderOnTheSameTankKillsOnShortMitigationsAlone()
        => Negative<DmuLimitCutScenario>(OffTank)
            .WithMitigationChecks()
            .FreezeAt(BeforeSecondThunder)
            .At(BeforeSecondThunder, p => GiveSecondHitOnTheSameTank(p, Rampart, DarkMind, Oblation))
            .Seed(1)
            .ShouldKill(UmadConstants.ActionId.ThunderIII_Resolve, OffTank);
}
