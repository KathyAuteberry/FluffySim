using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using AnoMech.Core.Game;
namespace AnoMech.Scenarios.Dsr;
public static class DsrP1Constants
{
    public static class Geometry
    {
        public const float ArenaHalfWidth = 22f;

        public const float ThordanArenaRadius = 21.5f;
    }

    public static IReadOnlyList<Waymark> Phase1Waymarks =>
    [
        new(WaymarkSlot.A, new Vector3(-6.669f, 0f, 3.162f)),
        new(WaymarkSlot.B, new Vector3(-3.186f, 0f, 6.708f)),
        new(WaymarkSlot.C, new Vector3(3.22f, 0f, 6.667f)),
        new(WaymarkSlot.D, new Vector3(6.599f, 0f, 3.18f)),
        new(WaymarkSlot.One, new Vector3(-6.862f, 0f, -3.595f)),
        new(WaymarkSlot.Two, new Vector3(-3.159f, 0f, -6.721f)),
        new(WaymarkSlot.Three, new Vector3(3.441f, 0f, -6.881f)),
        new(WaymarkSlot.Four, new Vector3(6.664f, 0f, -3.598f)),
    ];

    public static Vector3 Phase1Waymark(WaymarkSlot slot) => Phase1Waymarks.First(w => w.Slot == slot).Offset;

    // Compass degrees: 0 = north (-Z), 90 = east (+X).
    public static Vector3 AtBearing(float bearingDegrees, float radius)
    {
        var rad = bearingDegrees * MathF.PI / 180f;
        return new Vector3(radius * MathF.Sin(rad), 0f, -radius * MathF.Cos(rad));
    }

}
