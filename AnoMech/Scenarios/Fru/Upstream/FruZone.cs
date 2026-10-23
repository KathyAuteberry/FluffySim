using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.Upstream.FruConstants;

namespace AnoMech.Scenarios.Fru.Upstream;

public sealed class FruZone : IZone
{
    public bool SupportsSpectator => true;
    public static readonly FruZone Instance = new();
    public static readonly Phase P2 = new(Instance, "P2", WeatherId.Usurper, BgmId.Usurper);
    public static readonly Phase P3 = new(Instance, "P3", WeatherId.Oracle, BgmId.Oracle,
        arenaMapEffectOverrides: new Dictionary<byte, ushort>
        {
            [MapEffect.P2Platform] = 0x4,
            [MapEffect.OracleStage] = 0x1,
            [MapEffect.OracleFloor] = 0x1,
        });
    public static readonly Phase P4 = new(Instance, "P4", WeatherId.Oracle, BgmId.OracleAndUsurper,
        arenaMapEffectOverrides: new Dictionary<byte, ushort>
        {
            [MapEffect.P2Platform] = 0x4,
            [MapEffect.OracleStage] = 0x1,
            [MapEffect.OracleFloor] = 0x1,
            [MapEffect.FragmentOfFate] = 0x1,
        });
    public static readonly Phase P5 = global::AnoMech.Scenarios.Fru.FruZone.P5;

    public string DisplayName => "Futures Rewritten (Upstream)";
    public string Name => "Futures Rewritten (Upstream)";
    public uint TerritoryId => 1238;
    public Vector3 Origin => new(100f, 0f, 100f);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new WaymarkLayout("Inner Circle", InnerCircleWaymarks)];

    public EntranceBarrier EntranceBarrier => new(2007457, 0xA58C08, 0xA58C07, new Vector3(0f, 0f, 15f));

    public IReadOnlyList<ushort> ArenaMapEffectSnapshot { get; } =
    [
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 1, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
    ];

    public void Run(SimWorld world) => world.EnforceArenaBoundary(Geometry.ArenaRadius);

}
