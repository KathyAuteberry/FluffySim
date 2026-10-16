using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dsr;

public static class DsrActions
{
    public static readonly EnemyAction Strength = Visual(25555, 3.1f);
    public static readonly EnemyAction LightningCast = Visual(25548, 2.1f);
    public static readonly EnemyAction HeavyCast = Visual(25557, 3.1f);
    public static readonly EnemyAction MercyCast = Visual(25544, 2.1f);
    public static readonly EnemyAction Spiral = new(25556)
    {
        Cast = new() { AnimationLock = 4.1f, AnimationTargetsCaster = true, OmenDelay = 5.0f },
        Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = .3f },
    };
    public static readonly EnemyAction Lightning = new(25549)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))],
        Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Mercy = new(25545)
    {
        Cast = new() { AnimationLock = 1.1f, AnimationTargetsCaster = true },
        Area = new() { Size = MathF.PI / 12 },
        Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = .3f },
    };
    public static EnemyAction Heavy(int pulse) => new((uint)(25557 + pulse))
    {
        Cast = new() { AnimationLock = 1.9f, AnimationTargetsCaster = true },
        Area = new() { Size = pulse == 1 ? null : (pulse - 1) * 6f },
        Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = pulse == 1 ? .3f : 0 },
    };
    private static EnemyAction Visual(uint id, float animationLock) => new(id)
    {
        Cast = new() { AnimationLock = animationLock, AnimationTargetsCaster = true, TargetsCasterForPresentation = true },
    };
}
