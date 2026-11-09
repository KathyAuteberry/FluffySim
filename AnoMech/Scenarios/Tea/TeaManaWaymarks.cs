using System.Collections.Generic;
using AnoMech.Core.Game;

namespace AnoMech.Scenarios.Tea;

public static class TeaManaWaymarks
{
    public static IReadOnlyList<Waymark> Layout =>
    [
        new(WaymarkSlot.A, new(0, 0, -6)),
        new(WaymarkSlot.B, new(6, 0, 0)),
        new(WaymarkSlot.C, new(0, 0, 6)),
        new(WaymarkSlot.D, new(-6, 0, 0)),
        new(WaymarkSlot.One, new(0, 0, -13.75f)),
        new(WaymarkSlot.Two, new(13.75f, 0, 0)),
        new(WaymarkSlot.Three, new(0, 0, 13.75f)),
        new(WaymarkSlot.Four, new(-13.75f, 0, 0)),
    ];
}
