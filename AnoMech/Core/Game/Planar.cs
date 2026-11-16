using System;
using System.Numerics;

namespace AnoMech.Core.Game;

public static class Planar
{
    public static Vector2 Flat(Vector3 v) => new(v.X, v.Z);

    public static Vector2 Forward(float rotation) => new(MathF.Sin(rotation), MathF.Cos(rotation));

    public static Vector3 Direction(float clockwiseFromNorth) => new(MathF.Sin(clockwiseFromNorth), 0, -MathF.Cos(clockwiseFromNorth));

    public static Vector3 AtBearing(float degrees, float radius)
    {
        var radians = degrees * MathF.PI / 180f;
        return new Vector3(radius * MathF.Sin(radians), 0f, -radius * MathF.Cos(radians));
    }

    public static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
    {
        var leg = to - from;
        var lengthSquared = leg.LengthSquared();
        var t = lengthSquared < 1e-6f ? 0f : Math.Clamp(Vector2.Dot(point - from, leg) / lengthSquared, 0f, 1f);
        return Vector2.Distance(from + leg * t, point);
    }
}
