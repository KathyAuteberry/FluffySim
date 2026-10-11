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
        Effects = [Damage(split: Distribution.Stack(2))], Timing = new() { DamageDelay = 0 },
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
        Effects = [EnemyActionEffects.Knockback((_,target) => target.HasStatus(160) || target.HasStatus(1209) ? null : 16f, 25f)],
        Timing = new() { DamageDelay = .3f },
    };
    private static EnemyAction Tower(uint id) => new(id)
    {
        Cast = new() { AnimationTargetsCaster = true }, Effects = [Damage()], Timing = new() { DamageDelay = .3f },
    };
}

// UNVERIFIED absolute alignment; inter-resolution intervals are independently established,
// and the comet cadence and puddle lifecycle come from the executable reference simulator.
public static class DsrSanctity2Timing
{
    public const float Storm = 8.5f;
    public const float FirstTowers = 12.7f;
    public const float FirstMeteor = 16.1f;
    public const float MeteorInterval = 1.43f;
    public const float Knockback = 23.1f;
    public const float SecondTowers = 26.1f;
    public const float Complete = 29f;
    public const float PuddleActivation = 3f;
    public const float PuddleLifetime = 14f;
    public static float Meteor(int index) => FirstMeteor+index*MeteorInterval;
}
