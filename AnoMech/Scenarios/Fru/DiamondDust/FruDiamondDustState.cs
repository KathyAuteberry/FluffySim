using System;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.DiamondDust;

public sealed class FruDiamondDustStateOverrides
{
    public bool? Stillness { get; set; }
    public int? GazeSector { get; set; }
    public bool? Axe { get; set; }
    public bool? StonesOnSupports { get; set; }
    public int? FirstIcicleAxis { get; set; }
    public int? OracleSector { get; set; }
}

public sealed class FruDiamondDustState
{
    public PartyRole RayTarget { get; }
    public bool Stillness { get; }
    public int GazeSector { get; }
    public bool Axe { get; }
    public bool StonesOnSupports { get; }
    public int FirstIcicleAxis { get; }
    public int OracleSector { get; }
    public FruDiamondDustState(Rng rng, FruDiamondDustStateOverrides settings)
    {
        Axe = settings.Axe ?? rng.NextBool();
        StonesOnSupports = settings.StonesOnSupports ?? rng.NextBool();
        FirstIcicleAxis = settings.FirstIcicleAxis ?? rng.Next(4);
        OracleSector = settings.OracleSector ?? rng.Next(8);
        Stillness = settings.Stillness ?? rng.NextBool();
        GazeSector = settings.GazeSector ?? rng.Next(8);
        RayTarget = (PartyRole)rng.Next(8);
        if (GazeSector is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(settings.GazeSector));
        if (FirstIcicleAxis is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(settings.FirstIcicleAxis));
        if (OracleSector is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(settings.OracleSector));
    }
    public bool HasStone(PartyRole role) => !role.IsDps() == StonesOnSupports;
    public static Vector3 At(int sector, float radius)
    {
        var angle = sector * MathF.PI / 4f;
        return new(MathF.Sin(angle) * radius, 0f, -MathF.Cos(angle) * radius);
    }
    public Vector3 IciclePosition(int wave, int index) => wave switch
    {
        0 => At(FirstIcicleAxis + index * 4, 16f),
        1 => At((FirstIcicleAxis & 1) + 1 + index * 2, 16f),
        2 => At(FirstIcicleAxis + 2 + index * 4, 16f),
        _ => throw new ArgumentOutOfRangeException(nameof(wave)),
    };
}
