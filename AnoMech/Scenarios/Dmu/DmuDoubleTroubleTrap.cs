using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu;

// Double-trouble Trap: one support and one DPS carry it through all of P1. As it runs out it bursts
// on its holder, a stack that knocks the others back, and passes to one of those it hit in the
// holder's own role group. `passOrder` is the run's roll of who takes it first when hit.
public sealed class DmuDoubleTroubleTrap(SimWorld world, DmuArena arena, IReadOnlyList<PartyRole> passOrder)
{
    public IReadOnlyList<PartyRole> Holders => holders;
    private readonly List<PartyRole> holders = [];

    public void Apply(PartyRole support, PartyRole dps, float duration)
    {
        holders.Clear();
        foreach (var role in new[] { support, dps })
            if (world.Party.Get(role) is { } member && member.IsAlive())
            {
                member.AddStatus(StatusId.DoubleTroubleTrap, duration);
                holders.Add(role);
            }
        DiagnosticLog.Info($"[DmuTrap] holders={string.Join(",", holders)} duration={duration:F2}");
    }

    // `next` is the passed debuff's duration; null at the last burst, which passes nothing.
    public void Burst(float? next)
    {
        var bursting = holders.ToList();
        holders.Clear();
        foreach (var role in bursting)
        {
            if (world.Party.Get(role) is not { } holder || !holder.IsAlive()) continue;
            holder.RemoveStatus(StatusId.DoubleTroubleTrap);
            arena.Helper(holder.Position, holder.Rotation)?.Cast(DmuActions.DoubleTroubleTrapBurst(ctx => Pass(ctx, role, next)));
        }
    }

    private void Pass(EnemyActionContext ctx, PartyRole from, float? next)
    {
        var dps = from.IsDps();
        var hit = ctx.Hits.OfType<ISimPartyMember>().Where(m => m.Role.IsDps() == dps && !ctx.IsKilled((SimCharacter)m)).Select(m => m.Role).ToHashSet();
        DiagnosticLog.Info($"[DmuTrap] burst holder={from} hit=[{string.Join(",", ctx.Hits.OfType<ISimPartyMember>().Select(m => m.Role))}]");
        if (next is not { } duration) return;
        var candidates = passOrder.Where(hit.Contains).ToList();
        if (candidates.Count == 0 || world.Party.Get(candidates[0]) is not { } member) return;
        var to = candidates[0];
        member.AddStatus(StatusId.DoubleTroubleTrap, duration);
        holders.Add(to);
        DiagnosticLog.Info($"[DmuTrap] passed {from} -> {to} duration={duration:F2}");
    }
}
