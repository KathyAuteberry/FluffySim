using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.Fru.Upstream.FruConstants;

namespace AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

internal static class FruP4CrystallizeTimeActions
{
    public static readonly EnemyAction MornAfah = new(40250)
    {
        Effects = [Damage(split: Stack(8))],
        Timing = new() { DamageDelay = 0.6f },
    };

    public static readonly EnemyAction HallowedWingsKnockback = Wings(true);

    public static EnemyAction Wings(bool requireMitigation) => FruActions.HallowedWingsKnockback with
    {
        Effects =
        [
            OnFront(4, new WingsDamage(requireMitigation), ApplyStatus(StatusId.MagicVulnerabilityUp, 6f, delayToEffectResult: true)),
            OnBehindFront(4, Damage()),
            new WingsKnockback(),
        ],
    };

    private sealed class WingsDamage(bool requireMitigation) : IEnemyActionEffect
    {
        public void Apply(EnemyActionContext ctx, IReadOnlyList<SimCharacter> hits)
        {
            // UNVERIFIED: the vulnerable-tank threshold is a practice approximation.
            Damage(VulnerableTo(StatusId.MagicVulnerabilityUp, requiredMitigation: requireMitigation ? 0.56f : 0f), severity: Normal with { ForceMitigation = requireMitigation }, split: WildCharge(front: 1, frontHit: TankBuster with { ForceMitigation = requireMitigation }))
                .Apply(ctx, hits.Where(DamageCheck.IsTank).ToList());
            Damage(VulnerableTo(StatusId.MagicVulnerabilityUp)).Apply(ctx, hits.Where(h => !DamageCheck.IsTank(h)).ToList());
            if (hits.FirstOrDefault() is { } first && !DamageCheck.IsTank(first)) ctx.Kill(first, "tank must lead Hallowed Wings");
        }
    }

    private sealed class WingsKnockback : IEnemyActionEffect
    {
        public void Apply(EnemyActionContext ctx, IReadOnlyList<SimCharacter> hits)
            => Knockback(KnockbackId.HallowedWings, 1.36f)
                .Apply(ctx, hits.Where(h => !h.HasStatus(1209) && !h.HasStatus(160)
                    && !(ReferenceEquals(h, ctx.Party.Player) && Plugin.Game.World.PracticeAntiKnockback.Active)).ToList());
    }
}
