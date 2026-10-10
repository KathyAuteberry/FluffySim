using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru.DiamondDust;

internal static class FruDiamondDustActions
{
    public static readonly EnemyAction Raidwide = new(ActionId.DiamondDust) { Cast = new() { TargetsPartyForPresentation = true, AnimationLock = 2.6f }, Effects = [Damage()] };
    public static readonly EnemyAction Axe = new(ActionId.AxeKick) { Cast = new() { AnimationLock = 2.1f }, Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0f } };
    public static readonly EnemyAction Scythe = Axe with { ActionId = ActionId.ScytheKick, Area = new() { Size = 4f } };
    public static readonly EnemyAction Icicle = new(ActionId.IcicleImpact) { Cast = new() { AnimationLock = 2.1f }, Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0f } };
    public static readonly EnemyAction Stone = new(ActionId.FrigidStone) { Cast = new() { AnimationLock = 2.1f }, Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))], Timing = new() { DamageDelay = 0f } };
    public static readonly EnemyAction Cone = new(ActionId.DiamondDustHouseOfLight)
    {
        Cast = new() { AnimationTargetsCaster = true, AnimationLock = 2.1f },
        Area = new() { Size = MathF.PI / 12f },
        Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))],
        Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction NeedleCircle = new(ActionId.FrigidNeedleCircle) { Cast = new() { AnimationLock = 1.1f }, Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0f } };
    public static readonly EnemyAction NeedleCross = NeedleCircle with { ActionId = ActionId.FrigidNeedleCross };
    public static readonly EnemyAction Knockback = new(ActionId.HeavenlyStrike)
    {
        Cast = new() { TargetsPartyForPresentation = true, AnimationLock = 3.1f },
        Effects = [Damage(), EnemyActionEffects.Knockback(156u)],
        Timing = new() { DamageDelay = 0f },
    };
}
