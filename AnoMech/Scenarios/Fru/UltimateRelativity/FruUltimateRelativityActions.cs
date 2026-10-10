using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Fru.UltimateRelativity;

public static class FruUltimateRelativityActions
{
    public static readonly EnemyAction Relativity = new(40266)
    {
        Cast = new() { TargetsPartyForPresentation = true, AnimationLock = 4.1f }, Effects = [Damage()],
    };
    public static readonly EnemyAction Speed = new(40293) { Cast = new() { TargetsCasterForPresentation = true, AnimationLock = 3.1f } };
    public static readonly EnemyAction Hourglass = new(40291) { Cast = new() { TargetsCasterForPresentation = true, AnimationLock = 0.2f } };
    public static readonly EnemyAction ShellVisual = new(40286) { Cast = new() { TargetsCasterForPresentation = true, AnimationLock = 0.2f } };
    private static readonly CastSpec TargetImpact = new() { AnimationLock = 1.1f };
    private static readonly CastSpec StackImpact = new()
    {
        AnimationLock = 1.1f,
        PresentationTargets = (_, target, world) => world.Party.ActiveMembers()
            .Where(m => target != null && Vector3.Distance(m.Position, target.Position) <= 6f).ToArray(),
    };
    public static readonly EnemyAction Fire = new(40276)
    {
        Cast = TargetImpact, Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))], Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction Stack = new(40277)
    {
        Cast = StackImpact, Effects = [Damage(split: Distribution.Stack(5))], Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction Ice = new(40279)
    {
        // UNVERIFIED: the donut inner radius is provisional; the outer radius comes from the action sheet.
        Cast = new() { AnimationLock = 1.1f, PresentationTargets = (_, _, _) => [] },
        Area = new() { Size = 3f }, Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction LaserFirst = new(40235)
    {
        Cast = new() { AnimationTargetsCaster = true, AnimationLock = 2.1f }, Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))], Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction LaserRepeat = new(40292)
    {
        Cast = new() { AnimationLock = 1.1f }, Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction Eruption = new(40274)
    {
        Cast = TargetImpact, Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))], Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction Water = new(40271)
    {
        Cast = StackImpact, Effects = [Damage(split: Distribution.Stack(4))], Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction Eye = new(40278)
    {
        Cast = new()
        {
            AnimationTargetsCaster = true, AnimationLock = 1.1f,
            PresentationTargets = (_, target, world) => world.Party.ActiveMembers().Where(m => m != target).ToArray(),
        },
        Area = new() { AdjustTargets = (ctx, hits) => hits.Where(m => m != ctx.Target).ToArray() },
        Effects = [Gaze(true)], Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction Shell = new(40287)
    {
        Cast = StackImpact with { AnimationLock = 3.1f }, Effects = [Damage(split: Distribution.Stack(8))], Timing = new() { DamageDelay = 0f },
    };
}
