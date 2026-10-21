using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.EnemyActions;

// Owned by one mechanic/run, so partially spent shields cannot leak into a restart.
internal sealed class DamageBudget
{
    private readonly Dictionary<SimStatus, (int Version, float Remaining)> shields = [];

    public Result Apply(EnemyActionContext ctx, SimCharacter target, float damage, float suppliedTaken = 1f, uint? practiceMaxHp = null)
    {
        var statuses = target.ActiveStatusSnapshot.Select(s => s.StatusId).ToArray();
        var taken = Mitigation.DamageTaken(statuses, DamageKind.Magic) * suppliedTaken;
        var reduced = (uint)MathF.Ceiling(damage * taken);
        var remaining = (float)reduced;
        var nativeMaxHp = target.Proxy?.MaxHealth ?? 0;
        var bonusHp = statuses.Sum(id => Mitigation.ByStatusId.TryGetValue(id, out var m) ? m.MaxHp : 0f);
        var maxHp = (uint)((practiceMaxHp ?? nativeMaxHp) * (1f + bonusHp));
        var hp = nativeMaxHp > 0 ? (uint)((ulong)(target.Proxy?.Health ?? 0) * maxHp / nativeMaxHp) : 0;
        var shieldBefore = 0f;
        var shieldAfter = 0f;
        foreach (var status in target.ActiveStatuses)
        {
            var id = status.StatusId;
            if (!Mitigation.ByStatusId.TryGetValue(id, out var mitigation) || !mitigation.IsShield) continue;
            var shield = shields.TryGetValue(status, out var stored) && stored.Version == status.ApplicationVersion
                ? stored.Remaining : maxHp * Mitigation.ShieldFraction([id], maxHp);
            shieldBefore += shield;
            var absorbed = MathF.Min(remaining, shield);
            remaining -= absorbed;
            shieldAfter += shield - absorbed;
            shields[status] = (status.ApplicationVersion, shield - absorbed);
            ctx.UpdateShieldAtImpact(status, maxHp > 0 ? shields[status].Remaining / maxHp : 0);
        }
        var hpDamage = (uint)MathF.Ceiling(remaining);
        var after = hpDamage >= hp ? 0 : hp - hpDamage;
        ctx.ShowDamage(target, reduced, DamageKind.Magic);
        ctx.SetRemainingHealth(target, maxHp > 0 ? (uint)((ulong)after * nativeMaxHp / maxHp) : 0);
        if (after == 0) ctx.Kill(target, $"damage exceeded effective HP ({hpDamage} damage to {hp} HP)");
        return new(reduced, hpDamage, hp, after, maxHp, shieldBefore, shieldAfter, taken, statuses);
    }

    public readonly record struct Result(uint Damage, uint HpDamage, uint HpBefore, uint HpAfter, uint MaxHp, float ShieldBefore, float ShieldAfter, float Taken, ushort[] Statuses);
}
