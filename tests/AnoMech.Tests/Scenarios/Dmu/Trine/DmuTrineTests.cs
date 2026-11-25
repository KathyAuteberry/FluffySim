using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dmu.Trine;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Tests;

public class DmuTrineTests
{
    private const float BustersLand = 23.37f;

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void TheTanksAreClosestAndFarthestWhenTheBustersLand(int seed)
    {
        PartyRole[] byDistance = [];
        var run = ScenarioRun.Execute(typeof(DmuTrineScenario), 0, seed, new()
        {
            Probe = p =>
            {
                if (!p.Crossed(BustersLand - 0.05f)) return;
                byDistance = PerRole.All.OrderBy(r => new Vector2(p.Member(r)!.Position.X, p.Member(r)!.Position.Z).Length()).ToArray();
            },
            WriteArtifactsOnFailure = false,
        });

        Assert.That(run.Passed, run.ToString);
        Assert.That(byDistance.First(), Is.EqualTo(MainTank));
        Assert.That(byDistance.Last(), Is.EqualTo(OffTank));
    }

    [TestCase(false, 210f, 1)]
    [TestCase(false, 210f, 2)]
    [TestCase(false, 210f, 3)]
    [TestCase(true, 270f, 1)]
    [TestCase(true, 270f, 2)]
    [TestCase(true, 270f, 3)]
    public void TheMainTankHeadsSquareOffTheFirstCentreSideCounterClockwiseFromNorthWest(bool centrePointsEast,
        float expectedBearing, int seed)
    {
        var bearing = float.NaN;
        var run = ScenarioRun.Execute(typeof(DmuTrineScenario), 0, seed, new()
        {
            Overrides = o => ((DmuTrineStateOverrides)o).CentrePointsEast = centrePointsEast,
            Probe = p =>
            {
                if (!p.Crossed(BustersLand - 0.05f)) return;
                var position = p.Member(MainTank)!.Position;
                bearing = DmuTrineLayout.Bearing(new Vector2(position.X, position.Z));
            },
            WriteArtifactsOnFailure = false,
        });

        Assert.That(run.Passed, run.ToString);
        Assert.That(bearing, Is.EqualTo(expectedBearing).Within(60f));
        Assert.That((315f - bearing + 360f) % 360f, Is.LessThanOrEqualTo(180f));
    }

    [Test]
    public void TheLayoutIsSixAlternatingTrianglesAroundTheCentre()
    {
        Assert.That(DmuTrineLayout.Outer.Select(t => t.PointsEast), Is.EqualTo(new[] { true, false, true, false, true, false }));
        foreach (var triangle in DmuTrineLayout.Outer)
        {
            Assert.That(triangle.Centre.Length(), Is.EqualTo(15.275f).Within(0.01f));
            var corners = triangle.Corners;
            Assert.That(Vector2.Distance(corners[0], corners[1]), Is.EqualTo(TrineTriangle.Side).Within(0.001f));
            Assert.That(Vector2.Distance(corners[1], corners[2]), Is.EqualTo(TrineTriangle.Side).Within(0.001f));
        }
    }
}
