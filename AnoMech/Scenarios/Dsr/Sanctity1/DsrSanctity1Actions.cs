using System;
using System.Linq;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dsr.Sanctity1;

public static class DsrSanctity1Actions
{
    public static readonly EnemyAction Sanctity = Visual(25569, 3.1f);
    public static readonly EnemyAction GazeCast = Visual(25552, 2.1f);
    public static readonly EnemyAction Gaze = GazeAction(25553);
    public static readonly EnemyAction Glory = GazeAction(25554);
    public static readonly EnemyAction Flare = new(25295)
    {
        Cast = new() { AnimationTargetsCaster = true, TargetsCasterForPresentation = true, AnimationLock = 2.1f },
        Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = .3f },
    };
    public static readonly EnemyAction Blade = new(25570)
    {
        Area = new() { AdjustTargets = (_, _) => [] },
        Cast = new() { TargetsCasterForPresentation = true, AnimationLock = 1.6f },
    };
    public static readonly EnemyAction Sever = new(25571)
    {
        Cast = new() { AnimationLock = 1.6f, PresentationTargets = (_, target, world) => world.Party.ActiveMembers()
            .Where(m => target != null && System.Numerics.Vector3.Distance(m.Position, target.Position) <= 6f).ToArray() },
        Timing = new() { DamageDelay = 0f },
    };
    public static float DistanceDamageScale(float distance) => 40f / Math.Clamp(distance, 1f, 40f);
    public static EnemyAction SeverAtDistance(float distance) => Sever with { Effects = [new SeverDamage(distance)] };
    private sealed record SeverDamage(float Distance) : IEnemyActionEffect
    {
        public void Apply(EnemyActionContext ctx, System.Collections.Generic.IReadOnlyList<SimCharacter> hits)
        {
            // UNVERIFIED: the cutoff and displayed falloff are practice approximations, not the game's damage formula.
            var reason = ctx.Hits.Count < 4 ? $"{ctx.Hits.Count}/4 players in Sacred Sever" : Distance < 30f ? "Sacred Sever jump too short" : null;
            var severity = reason == null ? Severity.Normal : Severity.Lethal;
            var kind = DamageKinds.FromAttackType(Natives.Data.Action(ctx.Action.ActionId)?.AttackType ?? 0);
            foreach (var target in ctx.Hits)
            {
                var cause = DamageCheck.LethalCause(target, VulnSpec.None, severity, kind, ctx.Party);
                if (cause != null) ctx.Kill(target, reason ?? cause);
                var amount = cause != null ? 120000u : (uint)(40000f * DistanceDamageScale(Distance) * 4 / ctx.Hits.Count);
                ctx.ShowDamage(target, amount, kind);
            }
        }
    }
    private static EnemyAction GazeAction(uint id) => new(id)
    {
        Cast = new() { AnimationTargetsCaster = true, TargetsPartyForPresentation = true, AnimationLock = 1.1f },
        Effects = [EnemyActionEffects.Gaze(true)], Timing = new() { DamageDelay = 0f },
    };
    private static EnemyAction Visual(uint id, float animationLock) => new(id) { Cast = new() { TargetsCasterForPresentation = true, AnimationTargetsCaster = true, AnimationLock = animationLock } };
}
