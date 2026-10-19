using System;
using System.Linq;
using System.Numerics;

namespace AnoMech.Scenarios.Fru.MirrorMirror;

public sealed class FruMirrorMirrorStateOverrides
{
    public int? Blue { get; set; }
    public int? Red { get; set; }
    public bool? Pairs { get; set; }
    public bool ShowMirrorMarkers { get; set; }
}

public sealed class FruMirrorMirrorState
{
    public int Blue { get; }
    public int Red { get; }
    public bool Pairs { get; }
    public int OtherRed => (Red + 2) % 8;
    public FruMirrorMirrorState(Rng rng, FruMirrorMirrorStateOverrides settings)
    {
        Blue = settings.Blue ?? rng.Next(8);
        if (Blue is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(settings.Blue));
        var valid = Enumerable.Range(0, 8).Where(i => i != Blue && (i + 2) % 8 != Blue).ToArray();
        Red = settings.Red ?? valid[rng.Next(valid.Length)];
        if (!valid.Contains(Red)) throw new ArgumentOutOfRangeException(nameof(settings.Red));
        Pairs = settings.Pairs ?? rng.NextBool();
    }
    public static Vector3 Point(float octant, float radius)
        => new(MathF.Sin(octant * MathF.PI / 4f) * radius, 0f, -MathF.Cos(octant * MathF.PI / 4f) * radius);
    public static byte MirrorSlot(int octant) => (byte)(octant % 8 + 1);
}
