using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu;

public static class DmuActions
{
    private static readonly VulnSpec MagicallyVulnerable = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private static readonly IEnemyActionEffect BriefMagicVulnerability = ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f);

    // UNVERIFIED: 90°; the sheet has no angle.
    private static readonly AreaSpec RuinCone = new() { Size = MathF.PI / 4f };
    // UNVERIFIED: a quadrant; the sheet has no angle.
    private static readonly AreaSpec Quadrant = new() { Size = MathF.PI / 4f };

    public static readonly EnemyAction AutoAttack = new(ActionId.AutoAttack)
    {
        Cast = new() { AnimationLock = 0.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.5f },
    };

    public static readonly EnemyAction RevoltingRuin = new(ActionId.RevoltingRuin)
    {
        Cast = new() { AnimationLock = 2.1f },
        Area = RuinCone,
        Effects = [OnTarget(Damage(TankBuster)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "in Revolting Ruin III's cone",
    };

    public static EnemyAction RevoltingRuinSecond(Action<EnemyActionContext> grade) => new(ActionId.RevoltingRuinSecond)
    {
        Cast = new() { AnimationLock = 2.1f, CastSecondsOverride = 0f },
        Area = RuinCone,
        Effects = [Resolve(grade), OnTarget(Damage(TankBuster)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "in Revolting Ruin III's cone",
    };

    public static readonly EnemyAction GravenImage = new(ActionId.GravenImage) { Cast = new() { AnimationLock = 2.1f } };
    public static readonly EnemyAction MysteryMagic = new(ActionId.MysteryMagic) { Cast = new() { AnimationLock = 2.1f } };
    public static readonly EnemyAction DoubleTroubleTrap = new(ActionId.DoubleTroubleTrap) { Cast = new() { AnimationLock = 2.1f } };

    public static readonly EnemyAction BlizzardShown = new(ActionId.BlizzardShown)
    {
        Area = Quadrant,
        Effects = [Damage(Lethal)],
        DeathExplanation = "in Blizzard III Blowout",
    };

    public static readonly EnemyAction BlizzardHidden = BlizzardShown with { ActionId = ActionId.BlizzardHidden, DeathExplanation = "in the real Blizzard III Blowout: Kefka lied" };

    // Omened and harmless: the lie's decoy.
    public static readonly EnemyAction BlizzardFake = new(ActionId.BlizzardFake) { Area = Quadrant };

    public static readonly EnemyAction ThunderShown = new(ActionId.ThunderShown)
    {
        Effects = [Damage(Lethal)],
        DeathExplanation = "in Thrumming Thunder III",
    };

    public static readonly EnemyAction ThunderHidden = ThunderShown with { ActionId = ActionId.ThunderHidden, DeathExplanation = "in the real Thrumming Thunder III: Kefka lied" };

    public static readonly EnemyAction ThunderFake = new(ActionId.ThunderFake);

    public static readonly EnemyAction FlagrantFireSpread = new(ActionId.FlagrantFireSpread)
    {
        Cast = new() { CastSecondsOverride = 0f, FaceTarget = false },
        Effects = [Damage(MagicallyVulnerable), BriefMagicVulnerability],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "Flagrant Fire III spreads overlapped",
    };

    // UNVERIFIED: the soakers it needs; each role group's four take one.
    public static readonly EnemyAction FlagrantFireStack = new(ActionId.FlagrantFireStack)
    {
        Cast = new() { CastSecondsOverride = 0f, FaceTarget = false },
        Effects = [Damage(MagicallyVulnerable, split: Distribution.Stack(4)), BriefMagicVulnerability],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "Flagrant Fire III stack",
    };

    public static readonly EnemyAction PulseWave = new(ActionId.PulseWave)
    {
        Cast = new() { CastSecondsOverride = 0f, FaceTarget = false },
        Effects = [Damage(Harmless), Knockback(KnockbackId.PulseWave)],
        Timing = new() { DamageDelay = 0.5f },
    };

    // A clipped player takes the beam's vulnerability too, and can no longer soak a tower.
    public static readonly EnemyAction WaveCannon = new(ActionId.WaveCannon)
    {
        Cast = new() { CastSecondsOverride = 0f },
        Effects = [Damage(MagicallyVulnerable), ApplyStatus(StatusId.MagicVulnerabilityUp, 3.96f)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "hit by two Wave Cannons",
    };

    public static EnemyAction Explosion(Action<EnemyActionContext> unsoaked) => new(ActionId.Explosion)
    {
        Effects = [Damage(MagicallyVulnerable), BriefMagicVulnerability, Resolve(unsoaked)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "soaked a tower under Magic Vulnerability Up",
    };

    // The holder is spared. UNVERIFIED: fewer than three others in it kills them.
    public static EnemyAction DoubleTroubleTrapBurst(Action<EnemyActionContext> pass) => new(ActionId.DoubleTroubleTrapBurst)
    {
        Cast = new() { CastSecondsOverride = 0f, FaceTarget = false },
        Area = new() { ExcludeCaster = true },
        Effects = [Damage(MagicallyVulnerable, split: Distribution.Stack(3)), BriefMagicVulnerability, Knockback(KnockbackId.DoubleTroubleTrap), Resolve(pass)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "Double-trouble Trap",
    };

    // Every Gravitas lands on the whole south stack at once, so it carries no vulnerability.
    public static readonly EnemyAction Gravitas = new(ActionId.Gravitas)
    {
        Cast = new() { CastSecondsOverride = 0f, FaceTarget = false },
        Effects = [Damage(split: Distribution.Stack(2))],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "Gravitas stack",
    };

    public static EnemyAction Vitrophyre(Action<EnemyActionContext> onPuddles) => new(ActionId.Vitrophyre)
    {
        Cast = new() { CastSecondsOverride = 0f, FaceTarget = false },
        Effects = [Resolve(onPuddles), OnTarget(Damage()), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "clipped by Vitrophyre",
    };

    // A half-room cleave from the centre, toward the side its statue showed.
    public static readonly EnemyAction GravitationalWave = new(ActionId.GravitationalWave)
    {
        Cast = new() { CastSecondsOverride = 0f, FaceTarget = false },
        Area = new() { Size = MathF.PI / 2f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "in the west half-room cleave",
    };

    public static readonly EnemyAction IntemperateWill = GravitationalWave with { ActionId = ActionId.IntemperateWill, DeathExplanation = "in the east half-room cleave" };

    public static EnemyAction GravityIII(Action<EnemyActionContext> unsoaked) => new(ActionId.GravityIII)
    {
        Cast = new() { CastSecondsOverride = 0f, FaceTarget = false },
        Effects = [Damage(), Resolve(unsoaked)],
        Timing = new() { DamageDelay = 0.6f },
    };

    public static readonly EnemyAction BlizzardBoss = new(ActionId.BlizzardBoss) { Cast = new() { AnimationLock = 2.1f } };
}
