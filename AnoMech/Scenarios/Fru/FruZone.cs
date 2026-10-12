using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru;

public sealed class FruZone : IZone
{
    public static readonly FruZone Instance = new();
    public static readonly Phase P2 = new(Instance, "P2", 35, PhaseMusic.FootstepsInTheSnow, init: InitializeP2, clientSetup: HoldP2Layers);
    public static readonly Phase P3 = new(Instance, "P3", 106, PhaseMusic.TheExtreme, init: InitializeP3, clientSetup: HoldP3Layers);
    public static readonly Phase P4 = new(Instance, "P4", 105, PhaseMusic.PromisesToKeep, init: InitializeP4, clientSetup: HoldP4Layers);
    public string DisplayName => "Futures Rewritten";
    public string Name => "Futures Rewritten (Ultimate)";
    public uint TerritoryId => 1238;
    public Vector3 Origin => new(100f, 0f, 100f);
    public byte Level => 100;
    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new("FRU (JP)", [
            new(WaymarkSlot.A, new(0f, 0f, -10f)),
            new(WaymarkSlot.B, new(10f, 0f, 0f)),
            new(WaymarkSlot.C, new(0f, 0f, 10f)),
            new(WaymarkSlot.D, new(-10f, 0f, 0f)),
            new(WaymarkSlot.One, new(7.071f, 0f, -7.071f)),
            new(WaymarkSlot.Two, new(7.071f, 0f, 7.071f)),
            new(WaymarkSlot.Three, new(-7.071f, 0f, 7.071f)),
            new(WaymarkSlot.Four, new(-7.071f, 0f, -7.071f)),
        ])];
    public void Run(SimWorld world) => world.EnforceArenaBoundary(20f);

    private static void InitializeP2(SimWorld world)
        => InitializeP2Arena(world);

    private static void InitializeP2Arena(SimWorld world)
    {
        for (byte index = 0; index < 54; index++)
            world.Map.AddEffect(0x00040004, index);
        world.Map.AddEffect(0x00020001, 0x17);
    }

    private static void InitializeP3(SimWorld world)
    {
        for (byte index = 0; index < 54; index++)
            world.Map.AddEffect(0x00040004, index);
        world.Map.AddEffect(0x00080004, 0x17);
        world.Map.AddEffect(0x00020001, 0x28);
        world.Map.AddEffect(0x00020001, 0x29);
        world.Map.AddEffect(0x00200010, 0x28);
    }

    private static void HoldP3Layers(SimWorld world)
    {
        for (ushort key = 0x5F88; key <= 0x5F94; key++)
            world.Map.HoldLayerActive(key, false);
        world.Map.HoldLayerActive(0x5F9F, false);
        world.Map.HoldLayerActive(0x5FC9, false);
    }

    private static void HoldP2Layers(SimWorld world)
    {
        for (ushort key = 0x5F88; key <= 0x5F94; key++)
            world.Map.HoldLayerActive(key, false);
        world.Map.HoldLayerActive(0x5F9F, false);
        world.Map.HoldLayerActive(0x5FC9, true);
    }
    private static void InitializeP4(SimWorld world)
    {
        for (byte index = 0; index < 54; index++) world.Map.AddEffect(0x00040004, index);
        world.Map.AddEffect(0x00020001, 0x28);
        world.Map.AddEffect(0x00020001, 0x29);
    }

    private static void HoldP4Layers(SimWorld world)
    {
        for (ushort key = 0x5F88; key <= 0x5F94; key++) world.Map.HoldLayerActive(key, false);
        world.Map.HoldLayerActive(0x5F9F, false);
        world.Map.HoldLayerActive(0x5FC9, false);
    }

}
