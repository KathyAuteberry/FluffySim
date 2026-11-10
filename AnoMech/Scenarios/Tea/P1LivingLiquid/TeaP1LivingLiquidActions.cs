using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;

namespace AnoMech.Scenarios.Tea.P1LivingLiquid;

internal static class TeaP1LivingLiquidActions
{
    public static readonly EnemyAction UnequalHandOfPain = TeaActions.HandOfPain with
    {
        Effects = [new UnequalHandDamage()],
    };

    private sealed class UnequalHandDamage : IEnemyActionEffect
    {
        // UNVERIFIED practice threshold: the captured post-mitigation hits do not reveal the raw HP-gap formula.
        private const float RequiredMitigation = 0.35f;

        public void Apply(EnemyActionContext ctx, IReadOnlyList<SimCharacter> hits)
        {
            var sourceTaken = Mitigation.DamageTaken(ctx.Caster.ActiveStatusSnapshot.Select(s => s.StatusId), DamageKind.Magic);
            foreach (var target in hits)
            {
                var effective = 1f - sourceTaken * (1f - Mitigation.Effective(target.ActiveStatusSnapshot.Select(s => s.StatusId), DamageKind.Magic, target.Proxy?.MaxHealth ?? 0));
                var survives = effective >= RequiredMitigation - 0.0005f;
                ctx.ShowDamage(target, survives ? 40_000u : 120_000u, DamageKind.Magic);
                if (!survives) ctx.Kill(target, "Hand of Pain: insufficient mitigation and barrier for unequal boss HP");
                Mitigation.SpendShields(target);
            }
        }
    }
}
