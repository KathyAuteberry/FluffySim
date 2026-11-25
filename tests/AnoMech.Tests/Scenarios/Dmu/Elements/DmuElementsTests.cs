using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dmu;
using AnoMech.Scenarios.Dmu.Elements;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Tests;

public class DmuElementsTests
{
    private const float AfterHeroes = 13f;
    private const float BeforeFirstShot = 51.5f;
    private const float AfterFirstCyclone = 54.2f;
    private const float End = 85f;

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void YanSplitsTheHeroesByRole(int seed)
    {
        PartyRole[] epic = [];
        var run = ScenarioRun.Execute(typeof(DmuElementsScenario), 0, seed, new()
        {
            Probe = p =>
            {
                if (!p.Crossed(AfterHeroes)) return;
                epic = PerRole.All.Where(r => p.Member(r)!.HasStatus(DmuConstants.StatusId.EpicHero)).ToArray();
            },
            WriteArtifactsOnFailure = false,
        });

        Assert.That(run.Passed, run.ToString);
        Assert.That(epic, Is.EquivalentTo(new[] { OffTank, ShieldHealer, MeleeDpsA, MeleeDpsB }));
    }

    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(3, false)]
    [TestCase(4, true)]
    [TestCase(5, true)]
    [TestCase(6, true)]
    public void Tlb3KeepsEveryWindForVacuumWave(int seed, bool mainTankOnChaos)
    {
        var windsAtEnd = 0;
        var run = ScenarioRun.Execute(typeof(DmuElementsScenario), 0, seed, new()
        {
            Overrides = o => ((DmuElementsStateOverrides)o).MainTankOnChaos = mainTankOnChaos,
            Probe = p =>
            {
                if (!p.Crossed(End)) return;
                windsAtEnd = PerRole.All.Count(r => DmuElementsActions.HasWind(p.Member(r)!));
            },
            WriteArtifactsOnFailure = false,
        });

        Assert.That(run.Passed, run.ToString);
        Assert.That(windsAtEnd, Is.EqualTo(8));
    }

    [Test]
    public void AWindCaughtByATsunamiClearsAndCallsACyclone()
    {
        var stillWindy = true;
        var cycloned = false;
        var run = ScenarioRun.Execute(typeof(DmuElementsScenario), 0, 3, new()
        {
            Overrides = o => ((DmuElementsStateOverrides)o).FireFirst = false,
            Probe = p =>
            {
                if (p.Crossed(BeforeFirstShot)) DragBesideTheCrystalTarget(p, 3f);
                if (!p.Crossed(AfterFirstCyclone)) return;
                stillWindy = DmuElementsActions.HasWind(p.Member(MeleeDpsA)!);
                cycloned = PerRole.All.Any(r => p.Member(r)!.HasStatus(DmuConstants.StatusId.WindResistanceDownII));
            },
            WriteArtifactsOnFailure = false,
        });

        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(stillWindy, Is.False, "the Tsunami should have cleared the wind");
        Assert.That(cycloned || run.Deaths.Any(d => d.ActionId == DmuConstants.ActionId.Cyclone), Is.True, "a cleared wind should call a Cyclone");
    }

    [Test]
    public void TwoOverlappingInfernosKill()
    {
        var run = ScenarioRun.Execute(typeof(DmuElementsScenario), 0, 3, new()
        {
            Overrides = o => ((DmuElementsStateOverrides)o).FireFirst = true,
            Probe = p =>
            {
                if (p.Crossed(BeforeFirstShot)) DragBesideTheCrystalTarget(p, 6f);
            },
            WriteArtifactsOnFailure = false,
        });

        Assert.That(run.Deaths.Any(d => d.Role == MeleeDpsA && d.ActionId == DmuConstants.ActionId.Inferno), run.ToString);
    }

    private static void DragBesideTheCrystalTarget(ScenarioProbe p, float distance)
    {
        var state = ((DmuElementsScenario)p.Game.ActiveScenario!).LastState!;
        var crystal = DmuElementsCorners.Crystal(state.Corner(state.First));
        var target = PerRole.All.Select(r => p.Member(r)!)
            .OrderBy(m => Vector2.DistanceSquared(new(m.Position.X, m.Position.Z), crystal)).First();
        var at = new Vector2(target.Position.X, target.Position.Z);
        var spot = at + Vector2.Normalize(at - crystal) * distance;
        p.Member(MeleeDpsA)!.SetPosition(new Vector3(spot.X, 0f, spot.Y));
    }
}
