using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru;

public sealed class FruZone : IZone
{
    public bool SupportsSpectator => true;
    public static readonly FruZone Instance = new();
    public static readonly Phase P1 = new(Instance, "P1", WeatherId.Fatebreaker, Bgm.Fatebreaker, clientSetup: HoldP1Layers);
    public static readonly Phase P2 = new(Instance, "P2", WeatherId.Usurper, Bgm.Usurper, clientSetup: HoldP2Layers,
        arenaMapEffectOverrides: new Dictionary<byte, ushort> { [MapEffect.P2Platform] = MapEffect.Shown });
    public static readonly Phase P3 = new(Instance, "P3", WeatherId.Oracle, Bgm.Oracle, clientSetup: HoldLaterLayers,
        arenaMapEffectOverrides: OracleStage());
    public static readonly Phase P4 = new(Instance, "P4", WeatherId.Oracle, Bgm.OracleAndUsurper, clientSetup: HoldLaterLayers,
        arenaMapEffectOverrides: OracleStage());
    public static readonly Phase P5 = new(Instance, "P5", WeatherId.Pandora, Bgm.Pandora, clientSetup: HoldLaterLayers,
        arenaMapEffectOverrides: new Dictionary<byte, ushort> { [MapEffect.PandoraArena] = MapEffect.Shown });
    public string DisplayName => "Futures Rewritten";
    public string Name => "Futures Rewritten (Ultimate)";
    public Core.Map.EntranceBarrier EntranceBarrier => new(2007457, 0xA58C08, 0xA58C07, new(0f, 0f, 15f));
    public Vector3 PlayerSpawn => new(0f, 0f, 16f);
    public uint TerritoryId => 1238;
    public Vector3 Origin => new(100f, 0f, 100f);
    public byte Level => 100;

    // Retail sends every slot hidden at zone-in and on each reset.
    public IReadOnlyList<ushort> ArenaMapEffectSnapshot { get; } = Enumerable.Repeat(MapEffect.Hidden, 54).ToArray();

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

    private static Dictionary<byte, ushort> OracleStage() => new()
    {
        [MapEffect.OracleStage] = MapEffect.Shown,
        [MapEffect.OracleFloor] = MapEffect.Shown,
    };

    private static void HoldP1Layers(SimWorld world)
    {
        for (ushort key = 0x5F88; key <= 0x5F94; key++) world.Map.HoldLayerActive(key, true);
        world.Map.HoldLayerActive(0x5F9F, true);
        world.Map.HoldLayerActive(0x5FC9, false);
        world.Map.HoldLayerActive(0x5FCA, false);
        world.Map.HoldLayerActive(0x5FCB, false);
    }

    private static void HoldP2Layers(SimWorld world)
    {
        for (ushort key = 0x5F88; key <= 0x5F94; key++) world.Map.HoldLayerActive(key, false);
        world.Map.HoldLayerActive(0x5F9F, false);
        world.Map.HoldLayerActive(0x5FC9, true);
    }

    private static void HoldLaterLayers(SimWorld world)
    {
        for (ushort key = 0x5F88; key <= 0x5F94; key++) world.Map.HoldLayerActive(key, false);
        world.Map.HoldLayerActive(0x5F9F, false);
        world.Map.HoldLayerActive(0x5FC9, false);
    }
}
