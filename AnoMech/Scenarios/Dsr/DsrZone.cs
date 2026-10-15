using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr;

public sealed class DsrZone : IZone
{
    public static readonly DsrZone Instance = new();
    public static readonly Phase P2 = new(Instance, "P2 King Thordan", 45, PhaseMusic.Heroes, init: InitializeP2, clientSetup: HoldP2Layers);
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

    private static void HoldP2Layers(SimWorld world)
    {
        foreach (ushort layer in new ushort[] { 0x0F9D, 0x139F, 0xEE3C, 0xEE3F, 0xEE40, 0xF2DC, 0xFF52 })
            world.Map.HoldLayerActive(layer, false);
        foreach (ushort layer in new ushort[] { 0x160C, 0xEE79, 0xEE7A })
            world.Map.HoldLayerActive(layer, true);
        foreach (uint instance in new uint[] { 0x833316, 0x833317, 0x833318, 0x833319, 0x83331A, 0x833321, 0x833324 })
            world.Map.HoldInstanceActive(0xEE3E, instance, false);
        world.Map.HoldInstanceActive(0xF2E0, 0x8A4AF3, false);
        world.Map.HoldInstanceActive(0xF2E0, 0x8A4AF4, false);
    }
}
