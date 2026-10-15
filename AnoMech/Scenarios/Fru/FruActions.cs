using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Fru;

public static class FruActions
{
    public static readonly EnemyAction LightRampant = new(FruConstants.ActionId.LightRampant)
    {
        Effects = [Damage()],
    };
    public static readonly EnemyAction LuminousHammer = new(FruConstants.ActionId.LuminousHammer)
    {
        Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))],
        Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction Burst = new(FruConstants.ActionId.Burst)
    {
        Effects = [Damage(Severity.Lethal)],
        Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction BanishSpread = new(FruConstants.ActionId.BanishSpreadHit)
    {
        Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))],
        Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction BanishPairs = new(FruConstants.ActionId.BanishPairsHit)
    {
        Effects = [Damage(split: Distribution.Stack(2))],
        Timing = new() { DamageDelay = 0f },
    };
    public static readonly EnemyAction HouseOfLight = new(FruConstants.ActionId.HouseOfLightHit)
    {
        Area = new() { Size = global::AnoMech.Scenarios.Fru.LightRampant.FruLightRampantRules.HouseOfLightHalfAngle },
        Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))],
        Timing = new() { DamageDelay = 0f },
    };
}
