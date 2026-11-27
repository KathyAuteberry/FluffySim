using System;
using System.Collections.Generic;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.Elements;

public static class DmuElementsActions
{
    private const float VulnerabilitySeconds = 3.96f;
    private const float InnerDonutRadius = 4f;
    private static readonly VulnSpec MagicallyVulnerable = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private static readonly IEnemyActionEffect MagicVulnerability = ApplyStatus(StatusId.MagicVulnerabilityUp, VulnerabilitySeconds);

    public static readonly EnemyAction DefinitionOfInsanity = new(ActionId.DefinitionOfInsanity) { Cast = new() { AnimationLock = 2.1f } };
    public static readonly EnemyAction DecisiveBattleChaos = new(ActionId.DecisiveBattleChaos) { Cast = new() { AnimationLock = 2.1f } };
    public static readonly EnemyAction DecisiveBattleExdeath = new(ActionId.DecisiveBattleExdeath) { Cast = new() { AnimationLock = 2.1f } };
    public static readonly EnemyAction StatueGlance = new(ActionId.StatueGlance) { Cast = new() { AnimationLock = 2.1f } };

    public static readonly EnemyAction ChaosAutoAttack = new(ActionId.ChaosAutoAttack)
    {
        Cast = new() { AnimationLock = 0.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.5f },
    };

    public static readonly EnemyAction ExdeathAutoAttack = ChaosAutoAttack with { ActionId = ActionId.ExdeathAutoAttack };

    public static readonly EnemyAction BowelsOfAgony = new(ActionId.BowelsOfAgony)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(MagicallyVulnerable)],
        Timing = new() { DamageDelay = 0.6f },
    };

    public static readonly EnemyAction ThunderIIICircle = new(ActionId.ThunderIIICircle)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(MagicallyVulnerable, Lethal)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "inside Exdeath's Thunder III",
    };

    public static readonly EnemyAction ThunderIIIBusterCast = new(ActionId.ThunderIIIBusterCast) { Cast = new() { AnimationLock = 2.1f } };

    // ISSIS: the Exdeath tank invulns both hits.
    public static readonly EnemyAction ThunderIIIBuster = new(ActionId.ThunderIIIBuster)
    {
        Cast = new() { FaceTarget = false },
        Effects =
        [
            OnTarget(Damage(DmuP3Actions.InvulnRequired), ApplyStatus(StatusId.LightningResistanceDownII, VulnerabilitySeconds)),
            OnOthers(Damage(Lethal)),
        ],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "inside Exdeath's Thunder III tankbuster",
    };

    // The holder's own burst when Entropy or Dynamic Fluid runs out. UNVERIFIED that it clears no wind.
    public static readonly EnemyAction StrayFlames = new(ActionId.StrayFlames)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(MagicallyVulnerable), MagicVulnerability],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "hit by Stray Flames while magic vulnerable",
    };

    public static readonly EnemyAction StraySpray = StrayFlames with
    {
        ActionId = ActionId.StraySpray,
        Area = new() { Size = InnerDonutRadius },
        DeathExplanation = "hit by Stray Spray while magic vulnerable",
    };

    // The crystals fire at the two players nearest them: a wind they catch is cleared and blows its
    // carrier away from the shot. UNVERIFIED: the knockback distances and the shot's own target keeping its wind.
    public static readonly EnemyAction Inferno = new(ActionId.Inferno)
    {
        Cast = new() { FaceTarget = false },
        Area = new() { Size = InnerDonutRadius },
        Effects = [Damage(MagicallyVulnerable), MagicVulnerability, new WindExposure()],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "hit by a crystal's Inferno while magic vulnerable",
    };

    public static readonly EnemyAction Tsunami = Inferno with
    {
        ActionId = ActionId.Tsunami,
        Area = new(),
        DeathExplanation = "hit by a crystal's Tsunami while magic vulnerable",
    };

    public static readonly EnemyAction Cyclone = new(ActionId.Cyclone)
    {
        Cast = new() { FaceTarget = false },
        Effects =
        [
            Damage(VulnerableTo(StatusId.WindResistanceDownII), split: Stack(2)),
            ApplyStatus(StatusId.WindResistanceDownII, 0.96f),
            RemoveStatus(StatusId.Headwind, atImpact: true),
            RemoveStatus(StatusId.Tailwind, atImpact: true),
        ],
        Timing = new() { DamageDelay = 0.63f },
        DeathExplanation = "a Cyclone needs two players, and only one at a time",
    };

    public static readonly EnemyAction LatitudinalImplosion = new(ActionId.LatitudinalImplosion) { Cast = new() { AnimationLock = 2.1f } };
    public static readonly EnemyAction LongitudinalImplosion = new(ActionId.LongitudinalImplosion) { Cast = new() { AnimationLock = 2.1f } };

    public static readonly EnemyAction Shockwave = new(ActionId.Shockwave)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = MathF.PI / 4f },
        Effects = [Damage(MagicallyVulnerable, Lethal)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "in an Implosion shockwave",
    };

    public static bool HasWind(SimCharacter who) => who.HasStatus(StatusId.Headwind) || who.HasStatus(StatusId.Tailwind);

    // The status text: a head-on wind doubles the knockback, a rear wind halves it.
    public static float WindKnockback(SimCharacter who, System.Numerics.Vector3 from)
    {
        var facing = who.Placement();
        var faces = facing.IsLookingAt(from);
        var backTurned = facing.IsLookingAwayFrom(from);
        if (who.HasStatus(StatusId.Headwind)) return backTurned ? 10f : faces ? 40f : 20f;
        return faces ? 10f : backTurned ? 40f : 20f;
    }

    private sealed class WindExposure : IEnemyActionEffect
    {
        public void Apply(EnemyActionContext ctx, IReadOnlyList<SimCharacter> hits)
        {
            foreach (var target in hits)
            {
                if (ReferenceEquals(target, ctx.Target) || ctx.IsKilled(target) || !target.IsAlive() || !HasWind(target)) continue;
                ctx.Knockback(target, WindKnockback(target, ctx.Caster.Position), 20f);
                ctx.RemoveStatusAtImpact(target, StatusId.Headwind);
                ctx.RemoveStatusAtImpact(target, StatusId.Tailwind);
            }
        }
    }
}
