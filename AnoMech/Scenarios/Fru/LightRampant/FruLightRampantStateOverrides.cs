using System;
using System.Numerics;

namespace AnoMech.Scenarios.Fru.LightRampant;

public sealed class FruLightRampantStateOverrides
{
    public int? PuddlePair { get; set; }
    public int? ExtraLightsteepedMask { get; set; }
    public int? WeightEdge { get; set; }
    public bool? NorthOrbFirst { get; set; }
    public bool? Spread { get; set; }
    public bool? PairTargetsDps { get; set; }

    public void Validate()
    {
        if (PuddlePair is < 0 or >= 28) throw new ArgumentOutOfRangeException(nameof(PuddlePair));
        if (WeightEdge is < 0 or >= 6) throw new ArgumentOutOfRangeException(nameof(WeightEdge));
        if (ExtraLightsteepedMask is { } mask && (mask < 0 || mask > 255 || BitOperations.PopCount((uint)mask) != 4))
            throw new ArgumentException("Exactly four extra Lightsteeped recipients are required.", nameof(ExtraLightsteepedMask));
    }
}
