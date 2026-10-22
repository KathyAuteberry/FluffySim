using System.Numerics;
using AnoMech.Core.Game.Geometry;

namespace AnoMech.Tests;

public class TimedCircleRouteTests
{
    [TestCase(0f)] [TestCase(90f)] [TestCase(180f)] [TestCase(270f)]
    public void ClockwiseDetourClearsEverySegmentAroundTwoNearbyTwisters(float rotation)
    {
        Vector2 At(float degrees, float radius = 18.5f) => new(MathF.Sin((degrees + rotation) * MathF.PI / 180) * radius, -MathF.Cos((degrees + rotation) * MathF.PI / 180) * radius);
        var start = At(210);
        var goal = At(270, 15);
        TimedCircle[] hazards = [new(start, 1.5f, 46.7f, 50.9f), new(At(245, 15), 1.5f, 46.7f, 50.9f), new(At(252, 15.5f), 1.5f, 46.7f, 50.9f)];
        var route = TimedCircleRoute.Plan(start, goal, 46f, 8.3f, hazards, true);
        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Count, Is.GreaterThan(1));
        var time = 46f;
        foreach (var next in route)
        {
            Assert.That(TimedCircleRoute.SegmentClear(start, next, time, 8.3f, hazards), Is.True);
            Assert.That(start.X * next.Y - start.Y * next.X, Is.GreaterThanOrEqualTo(-.001f));
            time += Vector2.Distance(start, next) / 8.3f;
            start = next;
        }
        Assert.That(start, Is.EqualTo(goal));
        Assert.That(time, Is.LessThan(50.9f));
    }

    [Test]
    public void UnobstructedRouteIsUnchanged()
    {
        var start = new Vector2(-18, 0);
        var goal = new Vector2(-15, -5);
        var route = TimedCircleRoute.Plan(start, goal, 47, 8.3f, [new(new(18, 0), 1.5f, 46.7f, 50.9f)], true);
        Assert.That(route, Is.EqualTo(new[] { goal }));
    }

    [Test]
    public void DestinationCannotParkOnAnInactiveTwisterThatWillArmLater()
    {
        Assert.That(TimedCircleRoute.Plan(new(-18, 0), new(-15, -5), 45.15f, 8.3f,
            [new(new(-15, -5), 1.5f, 46.7f, 50.9f)], true), Is.Null);
    }

    [Test]
    public void ExpiredTwisterNoLongerBlocksMovementOrDestination()
    {
        var goal = new Vector2(-15, -5);
        Assert.That(TimedCircleRoute.Plan(new(-18, 0), goal, 50.91f, 8.3f,
            [new(goal, 1.5f, 46.7f, 50.9f)], true), Is.EqualTo(new[] { goal }));
    }

    [Test]
    public void ActivationDuringMovementUsesOnlyDangerousPartOfSegment()
    {
        TimedCircle[] hazards = [new(Vector2.Zero, 1.5f, 1, 5)];
        Assert.That(TimedCircleRoute.SegmentClear(new(-4, 0), new(4, 0), 0, 4, hazards), Is.False);
        Assert.That(TimedCircleRoute.SegmentClear(Vector2.Zero, new(8, 0), 0, 4, hazards), Is.True);
        Assert.That(TimedCircleRoute.SegmentClear(new(-4, 0), new(4, 0), 4.8f, 4, hazards), Is.True);
        Assert.That(TimedCircleRoute.SegmentClear(new(-4, 0), new(4, 0), 5.1f, 4, hazards), Is.True);
    }
}
