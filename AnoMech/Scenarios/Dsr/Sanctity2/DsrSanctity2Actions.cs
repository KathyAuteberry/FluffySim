using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dsr.Sanctity2;

public static class DsrSanctity2Actions
{
    public static readonly EnemyAction StormCast = new(25574);
    public static readonly EnemyAction FireCast = new(28590);
    public static readonly EnemyAction FirstTowerCast = new(29563);
    public static readonly EnemyAction SecondTowerCast = new(28650);
    public static readonly EnemyAction CometCast = new(25576);
    public static readonly EnemyAction Storm = new(25575)
    {
        Effects = [Damage(split: Distribution.Stack(2)), ApplyStatus(2903,3)], Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Fire = new(28591)
    {
        Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = .3f },
    };
    public static readonly EnemyAction Donut = new(28592)
    {
        Area = new() { Size = 15 }, Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = .3f },
    };
    public static readonly EnemyAction FirstTower = Tower(29564);
    public static readonly EnemyAction SecondTower = Tower(28651);
    public static readonly EnemyAction Comet = new(25577)
    {
        Cast = new() { AnimationTargetsCaster = true }, Effects = [Damage()], Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Impact = new(25578)
    {
        Cast = new() { TargetsPartyForPresentation = true }, Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Knockback = new(25308)
    {
        Effects = [EnemyActionEffects.Push((ctx,target) => target == ctx.Party.Player && Plugin.Game.World.PracticeAntiKnockback.Active || target.HasStatus(160) || target.HasStatus(1209) ? null : 16f, 25f)],
        Timing = new() { DamageDelay = .3f },
    };
    private static EnemyAction Tower(uint id) => new(id)
    {
        Cast = new() { AnimationTargetsCaster = true }, Effects = [Damage()], Timing = new() { DamageDelay = .3f },
    };
}

// T=0 is the first Meteor Prey marker. Receive times are rounded to encounter-scale precision.
public static class DsrSanctity2Timing
{
    public const float FirstTowerCast = .05f;
    public const float CometCast = .09f;
    public const float FireCast = .13f;
    public const float StormCast = .18f;
    public const float Fire = 7.63f;
    public const float FirePresentation = 8.15f;
    public const float IcePresentation = 8.3f;
    public const float Storm = 7.79f;
    public const float FirstTowers = 12.05f;
    public const float FirstMeteor = 13.45f;
    public const float MeteorInterval = 1.43f;
    // Actor snapshots precede effects by 0.52–0.92s; exact server capture time is not recorded.
    public const float MeteorSpawnLead = .7f;
    public const float SecondTowerCast = 14.4f;
    public const float KnockbackCast = 18.44f;
    public const float Knockback = 22.44f;
    public const float SecondTowers = 25.4f;
    public const float PuddleEnd = 25.1f;
    public const float Complete = 28f;
    // UNVERIFIED activation: Frostbite is observed by T+10.09; actor presence alone is insufficient.
    public const float IceActivation = 9.9f;
    // UNVERIFIED exact cutoff; puddles must be traversable for the knockback.
    public const float PuddleSafe = Knockback - .3f;
    public static float Meteor(int index) => FirstMeteor+index*MeteorInterval;
}
