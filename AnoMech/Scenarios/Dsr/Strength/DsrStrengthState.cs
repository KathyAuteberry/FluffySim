using System;
using System.Linq;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr.Strength;

public sealed class DsrStrengthStateOverrides
{
    public int? SafeAxis { get; set; }
    public int? KnightSides { get; set; }
    public int? HeavyCardinal { get; set; }
    public int? AdditionalPattern { get; set; }
}

public sealed class DsrStrengthState
{
    public int SafeAxis { get; }
    public int KnightSides { get; }
    public int HeavyCardinal { get; }
    public int AdditionalPattern { get; }
    public int[] KnightClocks { get; }
    public int MainGroupClock => SafeAxis < 2 ? SafeAxis : SafeAxis + 4;
    public Vector3 HeavyPosition => Direction(HeavyCardinal * 2) * 7f;

    public DsrStrengthState(Rng rng, DsrStrengthStateOverrides overrides)
    {
        SafeAxis = Roll(rng, overrides.SafeAxis, 4);
        KnightSides = Roll(rng, overrides.KnightSides, 8);
        HeavyCardinal = Roll(rng, overrides.HeavyCardinal, 4);
        AdditionalPattern = Roll(rng, overrides.AdditionalPattern, 8);
        var axes = Enumerable.Range(0, 4).Where(a => a != SafeAxis).Select((a, i) => a + ((KnightSides >> i & 1) * 4));
        KnightClocks = rng.Shuffle(axes).ToArray();
    }

    public static Vector3 Direction(int clock) => new(MathF.Sin(clock * MathF.PI / 4), 0, -MathF.Cos(clock * MathF.PI / 4));
    private static int Roll(Rng rng, int? value, int count) => value is { } v
        ? v >= 0 && v < count ? v : throw new ArgumentOutOfRangeException(nameof(value)) : rng.Next(count);
}
