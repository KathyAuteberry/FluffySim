using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

public enum AscalonElement
{
    Flames,
    Ice,
}

public enum EdgeSpot
{
    Party,
    SecondParty,
    Tanks,
}

public readonly record struct Explosion(Vector2 At, float Delay);

public sealed class DsrP7DragonKingState
{
    public const int Cycles = 3;
    public const int GigaflareCycles = 2;
    public const float SpotRadius = 8f;
    public const float GigaflareRadius = 14f;
    public const float ExaflareRadius = 6f;
    public const float ExaflareStep = 7f;
    public static readonly float[] ExaflareDelays = [0f, 1.92f, 3.79f, 5.65f, 7.52f, 9.39f];
    public static readonly Vector2 SouthOfTheExaflares = new(0f, 13.5f);
    public static readonly Vector2 ExaflareDarkTankSpot = new(-4f, 13.5f);
    public static readonly Vector2 ExaflareLightTankSpot = new(4f, 13.5f);
    public const float BaitStepInDelay = 6.0f;

    private static readonly Vector2[] Spots = [new(-6.93f, -4f), new(6.93f, -4f), new(0f, 8f)];

    public IReadOnlyList<IReadOnlyList<int>> ExaflareHeadings { get; }
    public IReadOnlyList<AscalonElement> Elements { get; }
    public IReadOnlyList<IReadOnlyList<int>> GigaflareOrder { get; }

    public DsrP7DragonKingState(Rng rng, DsrP7DragonKingStateOverrides? overrides = null)
    {
        overrides ??= new DsrP7DragonKingStateOverrides();
        var headings = Enumerable.Range(0, Cycles).Select(_ => (IReadOnlyList<int>)[rng.Next(8), rng.Next(8), rng.Next(8)]).ToList();
        ExaflareHeadings = overrides.ExaflareHeadings ?? headings;
        var elements = Enumerable.Range(0, Cycles * 3 - 1).Select(_ => rng.NextBool() ? AscalonElement.Flames : AscalonElement.Ice).ToList();
        Elements = overrides.Elements ?? (overrides.ForcedElement is { } forced ? Enumerable.Repeat(forced, elements.Count).ToList() : elements);
        var order = Enumerable.Range(0, GigaflareCycles).Select(_ => (IReadOnlyList<int>)rng.Shuffle(0, 1, 2).ToList()).ToList();
        GigaflareOrder = overrides.GigaflareOrder ?? order;
    }

    public AscalonElement ExaflareElement(int cycle) => Elements[cycle * 3];
    public AscalonElement AkhMornElement(int cycle) => Elements[cycle * 3 + 1];
    public AscalonElement GigaflareElement(int cycle) => Elements[cycle * 3 + 2];

    public static Vector2 Spot(EdgeSpot spot) => Spots[(int)spot];

    public Vector2 GigaflareSpot(int cycle, int blast) => -Spot((EdgeSpot)GigaflareOrder[cycle][blast]) * (GigaflareRadius / SpotRadius);

    public float ExaflareRotation(int cycle, EdgeSpot spot) => HeadingRotation(ExaflareHeadings[cycle][(int)spot]);

    public IEnumerable<Explosion> ExaflareExplosions(int cycle, EdgeSpot spot)
        => Explosions(Spot(spot), ExaflareRotation(cycle, spot));

    private static float HeadingRotation(int heading) => heading * MathF.PI / 4f;

    private static IEnumerable<Explosion> Explosions(Vector2 start, float rotation)
    {
        yield return new Explosion(start, 0f);
        foreach (var turn in new[] { 0f, MathF.PI / 2f, -MathF.PI / 2f })
        {
            var dir = new Vector2(MathF.Sin(rotation + turn), MathF.Cos(rotation + turn));
            for (var k = 1; k < ExaflareDelays.Length; k++)
                yield return new Explosion(start + dir * (ExaflareStep * k), ExaflareDelays[k]);
        }
    }
}
