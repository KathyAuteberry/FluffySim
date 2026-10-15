using System.Numerics;

namespace AnoMech.Scenarios.Fru.LightRampant;

public static class FruLightRampantRules
{
    // BossMod's danger boundary; packet positions do not establish the exact server cutoff.
    public const float ActiveChainMinimum = 25f;
    public const float PuddleRadius = 6f;
    public const float PuddleSpawnDelay = 0.83f;
    // UNVERIFIED: practice grace from impact; the exact server hazard cutoff is unmeasured.
    public const float PuddleHazardActivationDelay = 1.5f;
    // UNVERIFIED: exact server Bleed cutoff; native objects survive their fade.
    public const float PuddleLifetime = 10f;
    public const float PuddleVisualLifetime = 12.85f;
    // UNVERIFIED: the precise cone edge has not been measured.
    public const float HouseOfLightHalfAngle = System.MathF.PI / 6f;
    public static bool ChainFails(Vector3 a, Vector3 b, bool aAlive, bool bAlive) =>
        !aAlive || !bAlive || Vector2.DistanceSquared(new(a.X, a.Z), new(b.X, b.Z)) < ActiveChainMinimum * ActiveChainMinimum;
    public static bool MustSoakCenter(int lightsteeped) => lightsteeped == 2;
    public static bool LightsteepedOverloads(int lightsteeped) => lightsteeped >= 5;
}
