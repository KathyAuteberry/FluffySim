using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.Trine;

public static class DmuTrineActions
{
    private static readonly VulnSpec MagicallyVulnerable = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private static readonly IEnemyActionEffect MagicVulnerability = ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f);

    public static readonly EnemyAction Trine = new(ActionId.Trine) { Cast = new() { AnimationLock = 2.1f } };

    public static readonly EnemyAction TrineBurst = new(ActionId.TrineBurst)
    {
        Effects = [Damage(MagicallyVulnerable, Lethal), MagicVulnerability],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "in a Trine burst",
    };

    // The sheet's rect runs ahead of Kefka; retail turns it to his left or right.
    public static readonly EnemyAction WingsLeft = new(ActionId.WingsOfDestructionLeft)
    {
        Cast = new() { AnimationLock = 2.1f, FaceTarget = false },
        Area = new() { Rotation = MathF.PI / 2f },
        Effects = [Damage(MagicallyVulnerable, Lethal)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "on the cleaved half of Wings of Destruction",
    };

    // UNVERIFIED: only the left cleave has been seen in retail.
    public static readonly EnemyAction WingsRight = WingsLeft with
    {
        ActionId = ActionId.WingsOfDestructionRight,
        Area = new() { Rotation = -MathF.PI / 2f },
    };

    public static readonly EnemyAction WingsBusterCast = new(ActionId.WingsOfDestructionBusterCast)
    {
        Cast = new() { AnimationLock = 2.1f, FaceTarget = false },
    };

    // Lands on the closest and the farthest player from Kefka.
    public static readonly EnemyAction WingsBuster = new(ActionId.WingsOfDestructionBuster)
    {
        Cast = new() { FaceTarget = false },
        Effects = [OnTarget(Damage(TankBuster)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "inside a Wings of Destruction tankbuster",
    };

    public static readonly EnemyAction UltimateEmbrace = new(ActionId.UltimateEmbrace)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(TankBuster, split: Stack(2))],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "Ultimate Embrace was not shared by two",
    };
}
