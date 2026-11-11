using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dmu;

public sealed class DmuZone : IZone
{
    public static readonly DmuZone Instance = new();

    // 77 between pulls; a pull turns it to 78 over 4s.
    public static readonly Phase P1 = new(Instance, "P1", 78, DmuConstants.Bgm.Opening);

    public bool SupportsSpectator => true;
    public string DisplayName => "Dancing Mad";
    public string Name => "Dancing Mad (Ultimate)";
    public uint TerritoryId => 1363;
    public Vector3 Origin => new(100f, 0f, 100f);
    public DutyInitData DutyInitData => DmuReplayData.DutyInitData;

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
    [
        new("Mana (Yan Flash)", [
            new(WaymarkSlot.A, new(0f, 0f, -12.5f)),
            new(WaymarkSlot.B, new(13.8f, 0f, 0f)),
            new(WaymarkSlot.C, new(0f, 0f, 13.8f)),
            new(WaymarkSlot.D, new(-13.7f, 0f, 0f)),
            new(WaymarkSlot.One, new(-9.5f, 0f, -9.5f)),
            new(WaymarkSlot.Two, new(9.4f, 0f, -9.6f)),
            new(WaymarkSlot.Three, new(9.5f, 0f, 9.4f)),
            new(WaymarkSlot.Four, new(-9.5f, 0f, 9.5f)),
        ]),
    ];

    // What retail's director sends at every pull reset.
    public void Run(SimWorld world)
    {
        world.Map.DirectorUpdate(0x80000028U, 0x4U);
        world.Map.DirectorUpdate(0x8000000DU, 0x1U);
    }
}
