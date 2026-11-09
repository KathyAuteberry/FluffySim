using System.Numerics;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Motion;

namespace AnoMech.Tests;

public class NavigatorTests
{
    private sealed class Walker : INavigated
    {
        public Vector3 Position { get; private set; }
        public float Rotation { get; private set; }
        public float HitboxRadius => .5f;
        public ObstacleField Obstacles { get; } = new();
        public bool TurnsAlongPath => true;
        public int HaltCount { get; private set; }
        public void Step(Vector3 next, Vector3 glide, float speed, bool arrived) => Position = next;
        public void Halt() => HaltCount++;
        public void Turn(float rotation) => Rotation = rotation;
    }

    [Test]
    public void FullHeavyHoldsTheWalkAndResumesWhenItLifts()
    {
        FakeGame.Install();
        var walker = new Walker();
        var navigator = new Navigator(walker);
        navigator.MoveTo(new(10, 0, 0), 5f, null);
        var speedFactor = MovementSpeed.Factor([(1796, 100)]);

        navigator.Tick(1f, false, speedFactor);

        Assert.That(walker.Position, Is.EqualTo(Vector3.Zero));
        Assert.That(walker.HaltCount, Is.EqualTo(1));
        Assert.That(navigator.Destination, Is.EqualTo(new Vector3(10, 0, 0)));

        navigator.Tick(1f, false);
        Assert.That(walker.Position, Is.EqualTo(new Vector3(5, 0, 0)));
        navigator.Tick(1f, false);
        Assert.That(walker.Position, Is.EqualTo(new Vector3(10, 0, 0)));
        Assert.That(navigator.HasOrders, Is.False);
    }

    [Test]
    public void ZeroDurationTickDoesNotCompleteAnOrder()
    {
        var walker = new Walker();
        var navigator = new Navigator(walker);
        navigator.MoveTo(new(10, 0, 0), 5f, null);

        navigator.Tick(0f, false);

        Assert.That(walker.Position, Is.EqualTo(Vector3.Zero));
        Assert.That(navigator.HasOrders, Is.True);
    }
}
