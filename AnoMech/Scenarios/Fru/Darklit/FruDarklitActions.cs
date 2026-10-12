using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Fru.Darklit;

public static class FruDarklitActions
{
    public static readonly EnemyAction Darklit = new(40239) { Effects = [Damage()], Cast = new() { TargetsPartyForPresentation = true } };
    public static readonly EnemyAction OracleDarklit = new(40301);
    public static readonly EnemyAction Path = new(40187);
    public static readonly EnemyAction SpiritCast = new(40288);
    public static readonly EnemyAction Spirit = new(40289)
    {
        Effects = [OnTarget(Damage()), OnOthers(Push(40f, 40f))],
        Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Water = new(40271)
    {
        Effects = [Damage(split: Distribution.Stack(4))], Timing = new() { DamageDelay = 0 },
        Cast = new() { TargetsPartyForPresentation = true },
    };
    public static readonly EnemyAction SomberCast = new(40283);
    public static readonly EnemyAction SomberFirst = new(40284) { Timing = new() { DamageDelay = 0 } };
    public static readonly EnemyAction SomberSecond = new(40285) { Timing = new() { DamageDelay = 0 } };
}
