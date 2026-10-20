using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr;

public sealed class DsrZone : IZone
{
    public static readonly DsrZone Instance = new();
    public static readonly Phase P2 = new(Instance, "P2 King Thordan", 45, PhaseMusic.Heroes, init: InitializeP2);
    // UNVERIFIED: P3 weather and director layout need native-client confirmation.
    public static readonly Phase P3 = new(Instance, "P3 Nidhogg", 3, 922, init: world =>
    {
        for (byte slot = 0; slot < 9; slot++) world.Map.AddEffect(0x00040004, slot);
        world.Map.DirectorUpdate(0x80000016, 0, 0x48);
        world.Map.DirectorUpdate(0x8000001F, 758);
        world.Map.AddEffect(0x00020001, 8);
        var map = world.Map;
        world.OnDespawn(() => map.AddEffect(0x00040004, 8));
    });
    public static readonly Phase P1 = new(Instance, "P1 The Holy See", 2, 920, init: world =>
    {
        world.EnforceSquareArenaBoundary(22f);
        world.PlaceWaymarks(DsrP1Constants.Phase1Waymarks);
        for (byte slot = 0; slot < 9; slot++) world.Map.AddEffect(0x00040004, slot);
        world.Map.DirectorUpdate(0x80000016, 0, 1);
        world.Map.DirectorUpdate(0x8000001F, 758);
    });
    public string DisplayName => "Dragonsong's Reprise";
    public string Name => "Dragonsong's Reprise (Ultimate)";
    public Core.Map.EntranceBarrier EntranceBarrier => new(2007457, 0x875BE2, 0x875BE0, new(0f, 0f, 18f));
    public Vector3 PlayerSpawn => new(0f, 0f, 16f);
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

    private static void InitializeP2(SimWorld world)
    {
        for (byte slot = 0; slot < 9; slot++) world.Map.AddEffect(0x00040004, slot);
    }

}
