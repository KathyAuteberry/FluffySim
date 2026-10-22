using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dsr.P3Nidhogg;

public static class DsrP3NidhoggActions
{
    public const ushort FireResistanceDown = 2902;
    public const ushort PhysicalVulnerability = 2940;
    public static readonly EnemyAction FinalChorus = Hit(26376);
    public static readonly EnemyAction DiveFromGrace = Hit(26381);
    public static readonly EnemyAction AutoAttack = new(26416)
    {
        Effects = [OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))],
        Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Eye = new(26388)
    {
        Effects = [Damage(split: Distribution.Stack(4))],
        Timing = new() { DamageDelay = 0 },
    };
    public static readonly EnemyAction Geirskogul = Hit(26378, Severity.Lethal) with
    {
        Cast = new() { OmenDelay = 3.5f },
    };
    public static readonly EnemyAction Drachenlance = Hit(26380, Severity.Lethal) with
    {
        Area = new() { Size = MathF.PI / 4 },
    };
    public static readonly EnemyAction GnashingWheel = Hit(26389, Severity.Lethal);
    public static readonly EnemyAction LashingWheel = Hit(26390, Severity.Lethal) with { Area = new() { Size = 8f } };
    public static readonly EnemyAction TowerFailure = Hit(26395, Severity.Lethal);
    public static readonly EnemyAction SoulTether = new(26396)
    {
        Effects = [Damage(Severity.TankBuster.MinMit(1f) with { ForceMitigation = true })],
        Timing = new() { DamageDelay = 0 },
    };
    public static EnemyAction Dive(DiveArrow arrow) => new((uint)(arrow switch { DiveArrow.Up => 26383, DiveArrow.Down => 26384, _ => 26382 }))
    {
        Cast = new() { FaceTarget = false, AnimationLock = 1.1f, RequireAnimationReady = true },
        Effects = [Damage(VulnerableTo(FireResistanceDown)), ApplyStatus(FireResistanceDown, 11.96f), ApplyStatus(PhysicalVulnerability, 2.96f)],
        Timing = new() { DamageDelay = 0 },
    };
    public static EnemyAction Hit(uint id, Severity? severity = null) => new(id)
    {
        Effects = [Damage(severity)],
        Timing = new() { DamageDelay = 0 },
    };
}
