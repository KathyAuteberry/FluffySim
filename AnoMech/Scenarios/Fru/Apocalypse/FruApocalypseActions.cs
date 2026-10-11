using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Fru.Apocalypse;

public static class FruApocalypseActions
{
    public static readonly EnemyAction Refrain=new(40269);
    public static readonly EnemyAction WaterCast=new(40270);
    public static readonly EnemyAction Apocalypse=new(40296);
    public static readonly EnemyAction SpiritCast=new(40288);
    public static readonly EnemyAction EruptionCast=new(40273);
    public static readonly EnemyAction DanceCast=new(40181);
    public static readonly EnemyAction Pulsar=new(40282) { Effects=[Damage()] };
    public static readonly EnemyAction Explosion=new(40297) { Cast=new(){AnimationTargetsCaster=true},Effects=[Damage(Severity.Lethal)],Timing=new(){DamageDelay=0} };
    public static readonly EnemyAction Water=new(40271) { Effects=[Damage(split:Distribution.Stack(4))],Timing=new(){DamageDelay=0} };
    public static readonly EnemyAction Eruption=new(40274) { Effects=[OnTarget(Damage()),OnOthers(Damage(Severity.Lethal))],Timing=new(){DamageDelay=0} };
    // UNVERIFIED displacement speeds; effect distances are encounter-defined.
    public static readonly EnemyAction Spirit=new(40289)
    {
        Effects=[OnTarget(Damage()),OnOthers(Push(40f,40f))],Timing=new(){DamageDelay=0},
    };
    public static readonly EnemyAction Dance=new(40182) { Timing=new(){DamageDelay=0} };
    public static readonly EnemyAction Knockback=new(40183) { Effects=[EnemyActionEffects.Push(21f,35f)],Timing=new(){DamageDelay=0} };
}
