using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.Trine;

// One telegraph: an equilateral triangle of side 10 whose corners each burst in a 6 y circle.
public sealed record TrineTriangle(Vector2 Centre, bool PointsEast, uint LayoutId)
{
    public const float Side = 10f;
    private static readonly float Circumradius = Side / MathF.Sqrt(3f);

    public uint EObjId => PointsEast ? DmuConstants.EObjId.TrinePointingEast : DmuConstants.EObjId.TrinePointingWest;

    public IReadOnlyList<Vector2> Corners
    {
        get
        {
            var east = PointsEast ? 1f : -1f;
            return
            [
                Centre + new Vector2(east * Circumradius, 0f),
                Centre + new Vector2(-east * Circumradius / 2f, Side / 2f),
                Centre + new Vector2(-east * Circumradius / 2f, -Side / 2f),
            ];
        }
    }
}

// Retail always lays the same hexagon: six outer triangles around a centre one, the outer ones
// alternating direction. Only the waves and the centre triangle's direction change.
public static class DmuTrineLayout
{
    private static readonly float H = TrineTriangle.Side * MathF.Sqrt(3f) / 2f;

    // Clockwise from north.
    public static readonly TrineTriangle[] Outer =
    [
        new(new(H / 3f, -15f), true, LayoutId.TrineNorth),
        new(new(5f * H / 3f, -5f), false, LayoutId.TrineEast),
        new(new(4f * H / 3f, 10f), true, LayoutId.TrineSoutheast),
        new(new(-H / 3f, 15f), false, LayoutId.TrineSouth),
        new(new(-5f * H / 3f, 5f), true, LayoutId.TrineWest),
        new(new(-4f * H / 3f, -10f), false, LayoutId.TrineNorthwest),
    ];

    public static TrineTriangle Centre(bool pointsEast) => new(Vector2.Zero, pointsEast, LayoutId.TrineCentre);

    // Clockwise from north, in degrees [0, 360).
    public static float Bearing(Vector2 p) => (MathF.Atan2(p.X, -p.Y) * 180f / MathF.PI + 360f) % 360f;
}

public sealed class DmuTrineState
{
    public const int OuterCount = 6;

    public bool CentrePointsEast { get; }
    // Indices into DmuTrineLayout.Outer; the third wave also bursts the centre triangle.
    public IReadOnlyList<int> FirstWave { get; }
    public int SecondWave { get; }
    public IReadOnlyList<int> ThirdWaveOuter { get; }
    // Wings of Destruction's cleaved half, from Kefka's facing.
    public bool WingsCleaveLeft { get; }

    public IReadOnlyList<TrineTriangle> FirstWaveTriangles => FirstWave.Select(i => DmuTrineLayout.Outer[i]).ToList();
    public TrineTriangle SecondWaveTriangle => DmuTrineLayout.Outer[SecondWave];
    public IReadOnlyList<TrineTriangle> ThirdWaveTriangles => [.. ThirdWaveOuter.Select(i => DmuTrineLayout.Outer[i]), DmuTrineLayout.Centre(CentrePointsEast)];

    // UNVERIFIED: any combination of waves; three retail pulls showed three different ones.
    public DmuTrineState(Rng rng, DmuTrineStateOverrides overrides)
    {
        CentrePointsEast = overrides.CentrePointsEast ?? rng.NextBool();
        var order = rng.Shuffle(Enumerable.Range(0, OuterCount)).ToList();
        SecondWave = order[0];
        ThirdWaveOuter = order.Skip(1).Take(2).OrderBy(i => i).ToList();
        FirstWave = order.Skip(3).OrderBy(i => i).ToList();
        WingsCleaveLeft = overrides.WingsCleaveLeft ?? rng.NextBool();
        DiagnosticLog.Info($"[DmuTrine] Roll: first=[{string.Join(",", FirstWave)}] second={SecondWave} "
                           + $"third=[{string.Join(",", ThirdWaveOuter)}]+centre({(CentrePointsEast ? "east" : "west")}) wings={(WingsCleaveLeft ? "left" : "right")}");
    }
}

// Null leaves a roll random.
public sealed class DmuTrineStateOverrides
{
    public bool? CentrePointsEast { get; set; }
    public bool? WingsCleaveLeft { get; set; }
}
