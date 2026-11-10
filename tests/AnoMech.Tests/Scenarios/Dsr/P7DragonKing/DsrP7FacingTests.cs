using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P7DragonKing;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrP7Constants;
using Scenario = AnoMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingScenario;
using State = AnoMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingState;

namespace AnoMech.Tests;

public class DsrP7FacingTests
{
    [TestCase(0f)]
    [TestCase(.7f)]
    [TestCase(1.5707964f)]
    [TestCase(3.1415927f)]
    [TestCase(-1.5707964f)]
    public void EveryExaflareRayAndTowerRotatesWithItsSelectedFrame(float facing)
    {
        var state = new State(new Rng(0));
        var original = Enum.GetValues<EdgeSpot>().SelectMany(spot => state.ExaflareExplosions(0, spot)).ToArray();
        state.SnapshotExaflareFacing(0, facing);
        state.SnapshotAkhMornFacing(0, facing);
        var rotated = Enum.GetValues<EdgeSpot>().SelectMany(spot => state.ExaflareExplosions(0, spot)).ToArray();
        Assert.That(rotated, Has.Length.EqualTo(original.Length));
        for (var i = 0; i < original.Length; i++)
        {
            Assert.That(Vector2.Distance(rotated[i].At, state.ExaflarePoint(0, original[i].At)), Is.LessThan(.001f));
            Assert.That(rotated[i].Delay, Is.EqualTo(original[i].Delay));
        }
        foreach (var spot in Enum.GetValues<EdgeSpot>())
            Assert.That(state.AkhMornPoint(0, State.Spot(spot)), Is.EqualTo(state.ExaflarePoint(0, State.Spot(spot))));
        Assert.That(state.ExaflarePoint(1, State.Spot(EdgeSpot.Tanks)), Is.EqualTo(State.Spot(EdgeSpot.Tanks)));
    }

    [Test]
    public void BotsSetUpNorthBeforeEveryExaflareAndAkhMornCast()
    {
        var checkedCasts = 0;
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 9, new()
        {
            Spectator = true,
            Probe = p =>
            {
                foreach (var at in Scenario.ExaflareCasts.Concat(Scenario.AkhMornCasts))
                {
                    if (!p.Crossed(at + .05f)) continue;
                    var boss = Boss(p.World);
                    Assert.That(boss.Rotation, Is.EqualTo(MathF.PI).Within(.02f));
                    Assert.That(boss.Position, Is.EqualTo(Vector3.Zero));
                    checkedCasts++;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedCasts, Is.EqualTo(6));
    }

    [Test]
    public void ExaflareCastLocksFacingAndTelegraphsUntilReleaseThenTracksTheTankAgain()
    {
        var checkedCast = false;
        var checkedRelease = false;
        var scenario = new Scenario();
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 9, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = MainTank,
            StopAt = 24.1f,
            Overrides = value => ((DsrP7DragonKingStateOverrides)value).ForcedElement = AscalonElement.Flames,
            Takeovers = [new(14f, new(10f, 0f)), new(18f, new(-19f, 0f))],
            Probe = p =>
            {
                if (p.Crossed(20f))
                {
                    var boss = Boss(p.World);
                    Assert.That(boss.Rotation, Is.EqualTo(MathF.PI / 2f).Within(.02f));
                    Assert.That(boss.Proxy!.Rotation, Is.EqualTo(MathF.PI / 2f).Within(.02f));
                    var casters = p.World.Children.OfType<SimEnemy>()
                        .Where(e => e.Casting.ActionId == ActionId.ExaflaresEdgeFirst && e.Casting.IsBusy).ToArray();
                    Assert.That(casters, Has.Length.EqualTo(3));
                    var expected = Enum.GetValues<EdgeSpot>().Select(spot =>
                    {
                        var at = scenario.LastState!.ExaflarePoint(0, State.Spot(spot));
                        return p.World.Coordinates.ToGlobal(new Vector3(at.X, 0f, at.Y));
                    }).ToArray();
                    foreach (var caster in casters)
                    {
                        var cast = ((FakeBattleChara)caster.Proxy!).ReceivedCasts.Last();
                        Assert.That(expected.Any(at => Vector3.Distance(cast.Position, at) < .01f), Is.True);
                    }
                    Assert.That(scenario.LastState!.ExaflarePoint(0, State.Spot(EdgeSpot.Tanks)).X, Is.EqualTo(-8f).Within(.01f));
                    checkedCast = true;
                }
                if (p.Crossed(22.9f))
                    Assert.That(Boss(p.World).Rotation, Is.EqualTo(-MathF.PI / 2f).Within(.02f));
                if (!p.Crossed(24f)) return;
                Assert.That(MathF.Abs(MathF.IEEERemainder(Boss(p.World).Proxy!.Rotation + MathF.PI / 2f, MathF.Tau)), Is.LessThan(.02f));
                Assert.That(scenario.LastState!.ExaflarePoint(0, State.Spot(EdgeSpot.Tanks)).X, Is.EqualTo(-8f).Within(.01f));
                checkedRelease = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedCast && checkedRelease, Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FirstExplosionUsesTheSameRotatedCenterAsItsTelegraph(bool insideRotatedCircle)
    {
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 9, new()
        {
            PlayerRole = CasterDps,
            StopAt = Scenario.ExaflareFirstHits[0] + .05f,
            WriteArtifactsOnFailure = false,
            Takeovers =
            [
                new(15.5f, new(10f, 0f), Bot: MainTank),
                new(22.5f, insideRotatedCircle ? new(-8f, 0f) : new(0f, 12f)),
            ],
            Probe = p =>
            {
                if (p.Crossed(Scenario.ExaflareCasts[0] + .05f))
                    Assert.That(Boss(p.World).Rotation, Is.EqualTo(MathF.PI / 2f).Within(.02f));
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Any(d => d.Role == CasterDps && d.ActionId == ActionId.ExaflaresEdgeFirst), Is.EqualTo(insideRotatedCircle), run.ToString);
    }

    [Test]
    public void ProvokeChangesTheFacingTargetWithoutMovingTheBoss()
    {
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 9, new()
        {
            StopAt = 16f,
            Takeovers = [new(15f, new(-10f, 0f), Bot: OffTank)],
            Probe = p =>
            {
                if (p.Crossed(15.1f))
                {
                    var boss = Boss(p.World);
                    p.World.NotifyActionResolved(p.Member(OffTank)!, 7533, boss.GameObjectId.ObjectId);
                }
                if (!p.Crossed(15.5f)) return;
                Assert.That(Boss(p.World).Rotation, Is.EqualTo(-MathF.PI / 2f).Within(.02f));
                Assert.That(Boss(p.World).Position, Is.EqualTo(Vector3.Zero));
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    private static SimEnemy Boss(SimWorld world)
        => world.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == BNpcBaseId.DragonKingThordan);
}
