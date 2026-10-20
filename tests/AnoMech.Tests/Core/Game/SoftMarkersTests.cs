using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios.Fru.UltimateRelativity;

namespace AnoMech.Tests;

public class SoftMarkersTests
{
    [TestCase(Sign.Attack1, 61201u)]
    [TestCase(Sign.Attack2, 61202u)]
    [TestCase(Sign.Attack3, 61203u)]
    [TestCase(Sign.Attack4, 61204u)]
    [TestCase(Sign.Ignore1, 61221u)]
    [TestCase(Sign.Ignore2, 61222u)]
    [TestCase(Sign.Bind3, 61213u)]
    [TestCase(Sign.Bind1, 61211u)]
    [TestCase(Sign.Bind2, 61212u)]
    public void UsesGameMarkerIcons(Sign sign, uint icon) => Assert.That(SoftMarkers.IconId(sign), Is.EqualTo(icon));

    [Test]
    public void MarkersFollowActorsAndClearOnLeaveWithoutNativeMarkingCalls()
    {
        SoftMarkers? markers = null;
        var checkedPosition = false;
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 4, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(),
            StopAt = 25f,
            Probe = p =>
            {
                if (!p.Crossed(24f)) return;
                markers = p.World.SoftMarkers;
                var state = ((FruUltimateRelativityScenario)p.Game.ActiveScenario!).LastState!;
                var role = state.Roles.Single(r => state.Marker(r) == Sign.Attack1);
                var member = p.Member(role)!;
                Assert.That(markers.Snapshot(p.World.ScenarioOrigin).Single(m => m.Sign == Sign.Attack1).Position,
                    Is.EqualTo(member.Position + p.World.ScenarioOrigin + new Vector3(0f, 2.8f, 0f)));
                markers.Set(Sign.Attack1, member);
                Assert.That(markers.Count, Is.EqualTo(8));
                checkedPosition = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedPosition, Is.True);
        Assert.That(markers!.Count, Is.Zero);
        Assert.That(typeof(Natives).GetProperty("Markings"), Is.Null);
    }

    [Test]
    public void DestroyedActorsLoseTheirLocalMarker()
    {
        var inspected = false;
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 1, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(), StopAt = 16f,
            Probe = p =>
            {
                if (!p.Crossed(15f)) return;
                var boss = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Single(a => a.SpawnConfig.BNpcBaseId == 17831);
                p.World.SoftMarkers.Set(Sign.Attack1, boss);
                boss.Despawn();
                p.World.SoftMarkers.RemoveInactive();
                Assert.That(p.World.SoftMarkers.Count, Is.EqualTo(7));
                Assert.That(p.World.SoftMarkers.Snapshot(p.World.ScenarioOrigin).Any(m => m.Sign == Sign.Attack1), Is.False);
                inspected = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(inspected, Is.True);
    }

}
