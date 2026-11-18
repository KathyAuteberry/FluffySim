using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;

namespace AnoMech.Scenarios.TopNative;

public sealed class TopNativeZone : IZone
{
    public static readonly TopNativeZone Instance = new();
    public static readonly Phase P1 = new(Instance, "P1", WeatherId.P1, Bgm.P1);
    public static readonly Phase P2 = new(Instance, "P2", WeatherId.P2, Bgm.P2,
        arenaMapEffectOverrides: new Dictionary<byte, ushort> { [MapEffect.P2Floor] = MapEffect.Shown });
    public static readonly Phase P3 = new(Instance, "P3", WeatherId.P3, Bgm.P3);

    public bool SupportsSpectator => true;
    public string DisplayName => "The Omega Protocol";
    public string Name => "The Omega Protocol (Ultimate)";
    public uint TerritoryId => 1122;
    public Vector3 Origin => new(100f, 0f, 100f);
    public DutyInitData DutyInitData => Top.TopReplayData.DutyInitData;
    // Retail zones the party in around the barrier's centre, 5 y west of its EObj.
    public Vector3 PlayerSpawn => new(0f, 0f, 15f);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } = [new("Elemental", Core.Game.WaymarkPresets.Ring(13f))];

    public void Run(SimWorld world) => world.EnforceArenaBoundary(ArenaRadius);
}
