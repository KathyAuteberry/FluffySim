using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.Forsaken;

// Shapes and timings follow upstream's UMAD Forsaken; none of them has been checked against a
// retail capture yet.
public static class DmuForsakenActions
{
    public const float AllThingsEndingHalfCone = MathF.PI / 2;

    internal static readonly VulnSpec MagicallyVulnerable = VulnerableTo(StatusId.MagicVulnerabilityUp);
    internal static readonly IEnemyActionEffect LongMagicVulnerability = ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f);

    public static readonly EnemyAction Forsaken = new(ActionId.Forsaken)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(MagicallyVulnerable)],
        Timing = new() { DamageDelay = 1.91f },
    };

    public static readonly EnemyAction LightOfJudgment = new(ActionId.ForsakenLightOfJudgment)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(MagicallyVulnerable.VulnerableTo(StatusId.SpellsTrouble))],
        Timing = new() { DamageDelay = 1.1f },
    };

    private static readonly EnemyAction TheRiverOfLight = new(ActionId.TheRiverOfLight)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(MagicallyVulnerable, Lethal)],
        DeathExplanation = "a tower not taken by exactly two",
    };

    public static readonly EnemyAction ThePathOfLight = new(ActionId.ThePathOfLight)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [FollowUp(TheRiverOfLight, ctx => ctx.Hits.Count != 2)],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction Spelldriver = new(ActionId.Spelldriver)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(MagicallyVulnerable, split: Stack(3)), LongMagicVulnerability],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction Spellscatter = new(ActionId.Spellscatter)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(MagicallyVulnerable), LongMagicVulnerability],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction Spellwave = new(ActionId.Spellwave)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = MathF.PI / 4, ExcludeCaster = true },
        Effects = [Damage(MagicallyVulnerable), LongMagicVulnerability],
        Timing = new() { DamageDelay = 0.62f },
    };
}
