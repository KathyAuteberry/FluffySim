using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;

namespace AnoMech.Scenarios.Tea.P2BjCc;

internal static class TeaP2BjCcActions
{
    public static readonly EnemyAction HiddenMine = TeaActions.HiddenMine with
    {
        Effects = [new MineDamage()],
    };

    private sealed class MineDamage : IEnemyActionEffect
    {
        // UNVERIFIED practice threshold: retained until captured hits establish the pre-mitigation damage.
        private const float RequiredMitigation = 0.36f;

        public void Apply(EnemyActionContext ctx, IReadOnlyList<SimCharacter> hits)
        {
            var kind = DamageKinds.FromAttackType(Natives.Data.Action(ctx.Action.ActionId)?.AttackType ?? 0);
            foreach (var target in hits)
            {
                var effective = Mitigation.Effective(target.ActiveStatusSnapshot.Select(s => s.StatusId), kind, target.Proxy?.MaxHealth ?? 0);
                var survives = DamageCheck.IsTank(target) && effective >= RequiredMitigation - 0.0005f;
                ctx.ShowDamage(target, survives ? 80_000u : 120_000u, kind);
                if (!survives) ctx.Kill(target, "Hidden Mine: tank mitigation required");
                Mitigation.SpendShields(target);
            }
        }
    }
}
