using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr;

public sealed class DsrZone : IZone
{
    public static readonly DsrZone Instance = new();
    public static readonly Phase P2 = new(Instance, "P2 King Thordan", 45, PhaseMusic.Heroes);
    public string Name => "Dragonsong's Reprise (Ultimate)";
    public Core.Map.EntranceBarrier EntranceBarrier => new(0, 0, 0, new(0f, 0f, 16f));
    public uint TerritoryId => 968;
    public Vector3 Origin => new(100f, 0f, 100f);
    public byte Level => 90;
    public ushort ItemLevel => 605;
    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } = [new("Tuufless P2 onwards", [
        new(WaymarkSlot.A, new(0f, 0f, -13f)),
        new(WaymarkSlot.B, new(13f, 0f, 0f)),
        new(WaymarkSlot.C, new(0f, 0f, 13f)),
        new(WaymarkSlot.D, new(-13f, 0f, 0f)),
        new(WaymarkSlot.One, new(9.192f, 0f, -9.193f)),
        new(WaymarkSlot.Two, new(9.192f, 0f, 9.192f)),
        new(WaymarkSlot.Three, new(-9.193f, 0f, 9.192f)),
        new(WaymarkSlot.Four, new(-9.193f, 0f, -9.193f)),
    ])];
    public void Run(SimWorld world) => world.EnforceArenaBoundary(21f);
}
