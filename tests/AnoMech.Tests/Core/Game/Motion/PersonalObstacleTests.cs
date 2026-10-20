using System.Numerics;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Motion;

namespace AnoMech.Tests;

public class PersonalObstacleTests
{
    private sealed class Walker(ObstacleField obstacles) : INavigated
    {
        public Vector3 Position { get; set; } = new(-5, 0, 0);
        public float Rotation { get; private set; }
        public float HitboxRadius => .5f;
        public ObstacleField Obstacles => obstacles;
        public bool TurnsAlongPath => true;
        public void Step(Vector3 next, Vector3 glide, float speed, bool arrived) => Position = next;
        public void Halt() { }
        public void Turn(float rotation) => Rotation = rotation;
    }

    [TestCase(30)] [TestCase(60)] [TestCase(120)]
    public void OnlyTheWalkerWithTheKeepOutFieldDetoursAroundThePuddle(int fps)
    {
        var shared = new ObstacleField();
        var forbidden = new ObstacleField();
        forbidden.Add(new CircleObstacle(Vector2.Zero, 1.6f));
        var avoiding = new Walker(shared);
        var owner = new Walker(shared);
        var a = new Navigator(avoiding) { MovementObstacles = forbidden };
        var b = new Navigator(owner);
        a.MoveTo(new(5, 0, 0), 6, null);
        b.MoveTo(new(5, 0, 0), 6, null);
        var closestAvoiding = 5f;
        var closestOwner = 5f;
        for (var i = 0; i < fps * 4; i++)
        {
            a.Tick(1f / fps, false);
            b.Tick(1f / fps, false);
            closestAvoiding = Math.Min(closestAvoiding, avoiding.Position.Length());
            closestOwner = Math.Min(closestOwner, owner.Position.Length());
        }
        Assert.That(closestAvoiding, Is.GreaterThan(1.6f));
        Assert.That(closestOwner, Is.LessThan(.1f));
        Assert.That(Vector3.Distance(avoiding.Position, new(5, 0, 0)), Is.LessThan(.1f));
        Assert.That(shared.All, Is.Empty);
        a.MovementObstacles = null;
        avoiding.Position = new(-5, 0, 0);
        a.MoveTo(new(5, 0, 0), 6, null);
        closestAvoiding = 5;
        for (var i = 0; i < fps * 2; i++)
        {
            a.Tick(1f / fps, false);
            closestAvoiding = Math.Min(closestAvoiding, avoiding.Position.Length());
        }
        Assert.That(closestAvoiding, Is.LessThan(.1f));
    }
}
