using AnoMech.Core;
using System;
using System.Collections.Generic;
using AnoMech.Core.Game.Party;
using AnoMech.Core.UserActions;
using AnoMech.Core.Native.Interfaces;
using System.Linq;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Scenarios.Fru.Upstream.FruConstants;

namespace AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

internal static class FruP4CrystallizeTimeActions
{
    public static readonly EnemyAction MornAfah = new(40250)
    {
        Effects = [Damage(split: Stack(8))],
        Timing = new() { DamageDelay = 0.6f },
    };

    public static EnemyAction HallowedWingsKnockback => Wings(true);

    public static EnemyAction Wings(bool requireMitigation, DamageBudget? budget = null) => FruActions.HallowedWingsKnockback with
    {
        Effects =
        [
            new WingsDamage(requireMitigation, budget ?? new DamageBudget()),
            OnFront(4, ApplyStatus(StatusId.MagicVulnerabilityUp, 6f, delayToEffectResult: true)),
            new WingsKnockback(),
        ],
    };

    private sealed class WingsDamage(bool requireMitigation, DamageBudget budget) : IEnemyActionEffect
    {
        private static readonly float[] DamageByRank = [160000, 70000, 55000, 55000, 40000, 15000, 10000, 10000];

        public void Apply(EnemyActionContext ctx, IReadOnlyList<SimCharacter> hits)
        {
            for (var i = 0; i < hits.Count; i++)
            {
                var target = hits[i];
                var role = (target as ISimPartyMember)?.Role;
                var tank = DamageCheck.IsTank(target);
                var roleModifier = tank ? .64f : role is PartyRole.CasterDps or PartyRole.RegenHealer or PartyRole.ShieldHealer ? .94f : 1f;
                var vulnerable = target.HasStatus(StatusId.MagicVulnerabilityUp);
                var raw = DamageByRank[Math.Min(i, 7)] * roleModifier * (vulnerable ? 4f : 1f);
                // Normal practice supplies the tank cooldown plan; strict practice reads actual statuses.
                var hasFortyPercent = target.ActiveStatusSnapshot.Any(s => s.StatusId is 3829 or 3832 or 3835 or 3838);
                var suppliedTankTaken = tank && !hasFortyPercent && (!requireMitigation || !Natives.UserActions.Enabled || ctx.Party.IsBotDriven(target)) ? .6f : 1f;
                if (requireMitigation && tank && vulnerable && i < 4 && Natives.UserActions.Enabled && !ctx.Party.IsBotDriven(target)
                    && Mitigation.Effective(target.ActiveStatusSnapshot.Select(s => s.StatusId), DamageKind.Magic, target.Proxy?.MaxHealth ?? 0) < .56f - .0005f)
                    ctx.Kill(target, "not enough mitigation for strict tank practice");
                // Doppel HP is a fixed native placeholder, so use the encounter's reference gear for bots.
                uint? practiceMaxHp = ctx.Party.IsBotDriven(target) ? tank ? 225000u : roleModifier < 1f ? 142000u : 157000u : null;
                var result = budget.Apply(ctx, target, raw, suppliedTankTaken, practiceMaxHp);
                if (i == 0 && !tank) ctx.Kill(target, "tank must lead Hallowed Wings");
                DiagnosticLog.Info($"[CT Wings] source={ctx.Origin.Position} rank={i + 1} role={role} position={target.Position} vuln={vulnerable} antiKB={target.HasStatus(1209) || target.HasStatus(160) || ReferenceEquals(target, ctx.Party.Player) && Plugin.Game.World.PracticeAntiKnockback.Active} base={DamageByRank[Math.Min(i, 7)]} roleModifier={roleModifier} raw={raw} taken={result.Taken} statuses=[{string.Join(",", target.ActiveStatusSnapshot.Select(s => $"{s.StatusId}@{s.RemainingTime:F2}s"))}] damage={result.Damage} shieldBefore={result.ShieldBefore} shieldAfter={result.ShieldAfter} shieldAbsorbed={result.Damage - result.HpDamage} maxHp={result.MaxHp} hpBefore={result.HpBefore} hpAfter={result.HpAfter} failure={(ctx.Outcomes.FirstOrDefault(o => o.Target == target)?.Explanation ?? "none")}.");
            }
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
