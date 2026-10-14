using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Fru.FallOfFaith;

public static class FruFallOfFaithTiming
{
    public static readonly float[] Reveal = [3, 6.1f, 8.6f, 11.1f];
    public static readonly float[] Fetters = [12.7f, 15.72f, 18.22f, 20.75f];
    public static readonly float[] Charge = [14.77f, 17.8f, 20.30f, 22.8f];
    public static readonly float[] Impact = [16.1f, 19.13f, 21.62f, 24.15f];
    public static readonly float[] Swap = [16.76f, 19.78f];
    public const float LightningConeDelay = .312f;
    public const float FettersDuration = 4;
    public const float VulnerabilityDuration = 4;
    public const float End = 29;
}

public static class FruFallOfFaithActions
{
    public const ushort Prey = 1051;
    public const ushort Fetters = 2304;
    public const ushort MagicVulnerability = 2941;
    public const ushort MarkOfMortality = 4372;
    public static readonly EnemyAction PrimaryLightning = new(40142)
    {
        Effects = [Damage(VulnerableTo(MagicVulnerability)), ApplyStatus(MagicVulnerability, 4)],
        Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction LightningCone = new(40143)
    {
        Area = new() { Size = System.MathF.PI / 3 },
        Cast = new() { AnimationTargetsCaster = true, TargetsCasterForPresentation = true },
        Effects = [Damage(VulnerableTo(MagicVulnerability)), ApplyStatus(MagicVulnerability, 4)],
        Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction FireCone = new(40156)
    {
        Area = new() { Size = System.MathF.PI / 4 },
        Cast = new() { AnimationTargetsCaster = true, TargetsCasterForPresentation = true },
        Effects = [Damage(VulnerableTo(MagicVulnerability)), ApplyStatus(MagicVulnerability, 4)],
        Timing = new() { DamageDelay = 0 },
    };
}
