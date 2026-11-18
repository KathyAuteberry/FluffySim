using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;

namespace AnoMech.Scenarios.TopNative;

public static class TopNativeActions
{
    public static readonly RuinSpec ComeRuin = new((2, StatusId.TwiceComeRuin), (3, StatusId.ThriceComeRuin));

    public static readonly VulnSpec MagicVulns = VulnSpec.None
        .VulnerableTo(StatusId.MagicVulnerabilityUp)
        .VulnerableTo(StatusId.VulnerabilityUp);

    public static readonly EnemyAction AutoAttackP1 = new(ActionId.AutoAttackP1)
    {
        Cast = new() { AnimationLock = 0.1f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.5f },
    };

    // -- Program Loop --

    public static readonly EnemyAction StorageViolationObliteration = new(ActionId.StorageViolationObliteration)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 1.1f },
        Timing = new() { DamageDelay = 0.67f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "tower unfilled",
    };

    public static readonly EnemyAction StorageViolation = new(ActionId.StorageViolation)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 1.1f },
        Timing = new() { DamageDelay = 0.63f },
        Effects =
        [
            Damage(MagicVulns.VulnerableTo(StatusId.HpPenalty)),
            ApplyRuin(ComeRuin, 2, 11f),
            RemoveStatus(StatusId.FirstInLine), RemoveStatus(StatusId.SecondInLine),
            RemoveStatus(StatusId.ThirdInLine), RemoveStatus(StatusId.FourthInLine),
            FollowUp(StorageViolationObliteration, ctx => ctx.Hits.Count == 0),
        ],
        DeathExplanation = "tower taken with Twice-come Ruin or HP Penalty",
    };

    public static readonly EnemyAction BlasterAoe = new(ActionId.BlasterAoe)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 1.1f },
        Timing = new() { DamageDelay = 0.64f },
        Effects =
        [
            Damage(MagicVulns),
            ApplyRuin(ComeRuin, 2, 11f),
            OnTarget(ApplyStatus(StatusId.HpPenalty, 9f, delayToEffectResult: true)),
        ],
        DeathExplanation = "Blaster: tether explosion hit a second time",
    };

    // -- Pantokrator --

    // UNVERIFIED: no retail sample of anyone standing in a puddle; a hit is treated as a magic
    // vulnerability that the next beam or missile turns lethal.
    public static readonly EnemyAction BallisticImpact = new(ActionId.BallisticImpact)
    {
        Cast = new() { AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(MagicVulns), ApplyStatus(StatusId.MagicVulnerabilityUp, 2f)],
        DeathExplanation = "Ballistic Impact puddle",
    };

    // UNVERIFIED: treated as lethal; no retail sample of a Flame Thrower hit was checked.
    public static readonly EnemyAction FlameThrowerFirst = FlameThrower(ActionId.FlameThrowerFirst);
    public static readonly EnemyAction FlameThrowerRest = FlameThrower(ActionId.FlameThrowerRest);

    private static EnemyAction FlameThrower(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 0.1f, FaceTarget = false },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "Flame Thrower",
    };

    public static readonly EnemyAction GuidedMissileKyrios = new(ActionId.GuidedMissileKyrios)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(MagicVulns), ApplyStatus(StatusId.MagicVulnerabilityUp, 2f)],
        DeathExplanation = "Guided Missile Kyrios overlapped another hit",
    };

    public static readonly EnemyAction CondensedWaveCannonKyrios = new(ActionId.CondensedWaveCannonKyrios)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(MagicVulns, split: Stack(3)), ApplyStatus(StatusId.MagicVulnerabilityUp, 2f)],
        DeathExplanation = "Condensed Wave Cannon Kyrios needs three in the beam",
    };

    public static readonly EnemyAction DiffuseWaveCannonKyrios = new(ActionId.DiffuseWaveCannonKyrios)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Area = new() { Size = System.MathF.PI / 3f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(TankBuster)],
        DeathExplanation = "Diffuse Wave Cannon Kyrios is for the two farthest, the tanks",
    };

    public static readonly EnemyAction WaveCannonKyrios = new(ActionId.WaveCannonKyrios)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(MagicVulns), ApplyStatus(StatusId.MagicVulnerabilityUp, 10f)],
        DeathExplanation = "Wave Cannon Kyrios hit twice",
    };

    // -- Limitless Synergy --

    public static readonly EnemyAction OptimizedSagittariusArrow = new(ActionId.OptimizedSagittariusArrow)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "Optimized Sagittarius Arrow",
    };

    // UNVERIFIED cone width: retail never hit anyone but its tank; 30° each side fits the
    // tightest retail tank angle that still missed the party in the middle.
    public static readonly EnemyAction OptimizedBladedanceAoe = new(ActionId.OptimizedBladedanceAoe)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Area = new() { Size = System.MathF.PI / 6f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(TankBuster)],
        DeathExplanation = "Optimized Bladedance is a tank buster",
    };

    public static readonly EnemyAction BeyondDefenseAoe = new(ActionId.BeyondDefenseAoe)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [OnOthers(Damage(Lethal)), OnTarget(Damage()), OnTarget(ApplyRuin(ComeRuin, 2, 7f))],
        DeathExplanation = "Beyond Defense jumps on one player only",
    };

    public static readonly EnemyAction PilePitch = new(ActionId.PilePitch)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(MagicVulns, split: Stack(3)), ApplyRuin(ComeRuin, 2, 7f)],
        DeathExplanation = "Pile Pitch with Twice-come Ruin from Beyond Defense",
    };

    public static readonly EnemyAction OptimizedMeteorAoe = new(ActionId.OptimizedMeteorAoe)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage()],
    };

    public static readonly EnemyAction LaserShowerHurried = new(ActionId.LaserShower)
    {
        Cast = new() { CastSecondsOverride = 3f, AnimationLock = 0.3f },
    };

    public static readonly EnemyAction CosmoMemoryAoe = new(ActionId.CosmoMemoryAoe)
    {
        Cast = new() { AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage()],
    };

    // -- P3 transition --

    public static readonly EnemyAction WaveRepeater1 = new(ActionId.WaveRepeater1)
    {
        Cast = new() { AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.5f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "Wave Repeater",
    };

    public static readonly EnemyAction WaveRepeater2 = WaveRepeaterRing(ActionId.WaveRepeater2, 6f);
    public static readonly EnemyAction WaveRepeater3 = WaveRepeaterRing(ActionId.WaveRepeater3, 12f);
    public static readonly EnemyAction WaveRepeater4 = WaveRepeaterRing(ActionId.WaveRepeater4, 18f);

    private static EnemyAction WaveRepeaterRing(uint actionId, float inner) => new(actionId)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Area = new() { Size = inner },
        Timing = new() { DamageDelay = 0.5f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "Wave Repeater",
    };

    public static readonly EnemyAction ColossalBlow = new(ActionId.ColossalBlow)
    {
        Cast = new() { AnimationLock = 0.6f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "Colossal Blow",
    };

    public static readonly EnemyAction SniperCannon = new(ActionId.SniperCannon)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [OnTarget(Damage()), OnOthers(Damage(Lethal))],
        DeathExplanation = "Sniper Cannon is a spread",
    };

    public static readonly EnemyAction HighPoweredSniperCannon = new(ActionId.HighPoweredSniperCannon)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(MagicVulns, split: Exactly(2)), ApplyStatus(StatusId.MagicVulnerabilityUp, 2f)],
        DeathExplanation = "High-powered Sniper Cannon is a two-player stack",
    };

    // -- Hello World --

    // Two stacks a break: a second break within the second kills.
    public static readonly EnemyAction Patch = new(ActionId.Patch)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.5f },
        Effects = [Damage(), ApplyRuin(ComeRuin, 3, 1f), ApplyRuin(ComeRuin, 3, 1f)],
        DeathExplanation = "two Hello World tethers broke within a second of each other",
    };

    public static readonly EnemyAction PatchFail = new(ActionId.PatchFail)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.5f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "a Hello World tether ran out unbroken",
    };

    // -- Oversampled Wave Cannon --

    public static readonly EnemyAction OversampledWaveCannonAoe = new(ActionId.OversampledWaveCannonAoe)
    {
        Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects = [Damage(MagicVulns), ApplyStatus(StatusId.MagicVulnerabilityUp, 5f), ApplyRuin(ComeRuin, 2, 7f)],
        DeathExplanation = "hit by two monitors",
    };
}
