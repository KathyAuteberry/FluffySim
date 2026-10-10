using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dsr.Strength2;

public static class DsrStrength2Actions
{
    public static readonly EnemyAction CollapseCast = Visual(25563, 3.1f);
    public static readonly EnemyAction TowerCast = Visual(25566, 3.1f);
    public static readonly EnemyAction RageCast = Visual(25550, 3.1f);
    public static readonly EnemyAction BladeCast = Visual(25298, 5.1f);
    public static readonly EnemyAction BashSecondary = Visual(25579, 1.1f);
    public static readonly EnemyAction Leap = new(25565)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))],
        Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Rage = new(25551)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(split: Distribution.Stack(5))],
        Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Bash = new(25297)
    {
        Cast = new() { AnimationLock = 1.6f },
        Effects = [OnTarget(Damage(Severity.TankBuster)), OnOthers(Damage(Severity.Lethal)), OnTarget(ApplyStatus(3165, 6f))],
        Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Blade = new(25299)
    {
        Cast = new() { AnimationLock = .5f, AnimationTargetsCaster = true },
        Area = new() { Size = MathF.PI / 4 },
        Effects = [Damage(Severity.TankBuster)], Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Collapse = new(25564)
    {
        Cast = new() { AnimationLock = 1.1f, AnimationTargetsCaster = true },
        Area = new() { AdjustTargets = (ctx, _) => ctx.Party.Find.InsideCircle(ctx.Origin.Position, 9f) },
        Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = .3f },
    };
    public static readonly EnemyAction Tower = new(25567)
    {
        Cast = new() { AnimationLock = 2.1f, AnimationTargetsCaster = true },
        Effects = [Damage()], Timing = new() { DamageDelay = .3f },
    };
    public static readonly EnemyAction TowerFailure = new(25568)
    {
        Cast = new() { AnimationLock = 1.1f, TargetsPartyForPresentation = true },
        Area = new() { AdjustTargets = (ctx, _) => ctx.Party.Find.InsideCircle(ctx.Origin.Position, 100f) },
        Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0 },
    };
    private static EnemyAction Visual(uint id, float duration) => new(id)
    {
        Cast = new() { AnimationLock = duration, AnimationTargetsCaster = true, TargetsCasterForPresentation = true },
    };
}
