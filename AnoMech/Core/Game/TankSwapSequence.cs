using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Core.Game;

public sealed class TankSwapSequence
{
    public sealed record Snapshot(float Time, uint Action, PartyRole Target, PartyRole[] Recipients, ushort[] TargetStatuses);
    public List<Snapshot> Snapshots { get; } = [];
    public PartyRole Holder { get; private set; } = PartyRole.MainTank;
    private readonly SimWorld world;
    private readonly SimEnemy boss;
    private readonly uint heel, cleave;
    private readonly ushort vulnerability;
    private bool acceptingSwap;
    // UNVERIFIED: a minimum practice budget, not a captured lethal-damage threshold.
    private const float MinimumMitigation = .2f;

    public TankSwapSequence(SimWorld world, SimEnemy boss, uint heel, uint cleave, ushort vulnerability)
    {
        this.world = world; this.boss = boss; this.heel = heel; this.cleave = cleave; this.vulnerability = vulnerability;
        world.ActionResolved += ActionResolved;
        world.OnDespawn(() => world.ActionResolved -= ActionResolved);
        boss.SetTarget(world.Party.Get(Holder), false);
    }

    public void Heel()
    {
        var previous = Holder;
        Holder = PartyRole.MainTank;
        // Heel captures MT now; Provoke during its bar must change only later attacks.
        acceptingSwap = true;
        DiagnosticLog.Info($"[TankSwap] event=heel-locked time={world.Events.Elapsed:F2} target=MainTank before={previous} after={Holder} window=open");
        var target = world.Party.Get(Holder)!;
        boss.SetTarget(target, false);
        MitigateBot(target);
        world.Announce("Heavenly Heel: MT mitigate. ST Provoke during the cast or after the hit, then mitigate three cleaves.");
        boss.Cast(new EnemyAction(heel)
        {
            Effects = [Damage(Severity.TankBuster.MinMit(MinimumMitigation) with { ForceMitigation = true }), ApplyStatus(vulnerability, 15), Resolve(ctx =>
            {
                Record(heel, target, ctx.Hits);
                LogFailures(ctx);
                world.Announce(Holder == PartyRole.OffTank
                    ? "ST has Thordan. MT move out of the frontal cleave."
                    : "ST: Provoke Thordan now. MT move out of the frontal cleave.");
                var offTank = world.Party.Get(PartyRole.OffTank);
                if (offTank != null && world.Party.IsBotDriven(offTank))
                {
                    MitigateBot(offTank);
                    world.NotifyActionResolved(offTank, 7533, boss.GameObjectId.ObjectId);
                }
            })],
            Timing = new() { DamageDelay = 0 },
        }, target);
    }

    private void MitigateBot(SimCharacter tank)
    {
        if (!ReferenceEquals(tank, world.Party.Player) && world.Party.IsBotDriven(tank)) tank.AddStatus(1191, 20);
    }

    private void ActionResolved(SimCharacter actor, uint action, ulong target)
    {
        if (action != 7533) return;
        var reason = !acceptingSwap ? "before-heel-cast"
            : target != boss.GameObjectId.ObjectId ? "wrong-boss"
            : actor.Dead ? "actor-dead"
            : actor is not ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank } ? "not-a-tank" : "accepted";
        var before = Holder;
        if (reason == "accepted") Holder = ((ISimPartyMember)actor).Role;
        DiagnosticLog.Info($"[TankSwap] event=provoke time={world.Events.Elapsed:F2} target=0x{target:X} boss=0x{boss.GameObjectId.ObjectId:X} before={before} after={Holder} result={reason}");
        if (reason != "accepted") return;
        boss.SetTarget(actor, false);
        world.Announce(Holder == PartyRole.OffTank
            ? "ST has Thordan. Hold him away from the party for all three cleaves."
            : "MT reclaimed Thordan. ST must Provoke before the next cleave.");
    }

    public void Cleave()
    {
        var target = world.Party.Get(Holder)!;
        boss.SetTarget(target, false);
        boss.SetRotation(boss.Placement().Face(target.Position).Rotation);
        boss.Cast(new EnemyAction(cleave)
        {
            Cast = new() { CastSecondsOverride = 0, FaceTarget = false },
            Timing = new() { DamageDelay = 0 },
            Effects = [Resolve(ctx =>
            {
                Record(cleave, target, ctx.Hits);
                if (Holder != PartyRole.OffTank)
                {
                    DiagnosticLog.Info($"[TankSwap] event=failure reason=missing-swap holder={Holder} time={world.Events.Elapsed:F2}");
                    foreach (var hit in ctx.Hits) ctx.Kill(hit, "missing MT→ST Provoke swap");
                }
            }), Damage(VulnerableTo(vulnerability, 1), Severity.TankBuster.MinMit(MinimumMitigation) with { ForceMitigation = true }), Resolve(LogFailures)],
        }, target);
    }

    private static void LogFailures(EnemyActionContext ctx)
    {
        foreach (var outcome in ctx.Outcomes.Where(o => o.Killed))
            DiagnosticLog.Info($"[TankSwap] event=damage-failure action={ctx.Action.ActionId} recipient={(outcome.Target as ISimPartyMember)?.Role} reason={outcome.Explanation}");
    }

    private void Record(uint action, SimCharacter target, IReadOnlyList<SimCharacter> hits)
    {
        var statuses = target.ActiveStatusSnapshot.Select(s => s.StatusId).ToArray();
        var recipients = hits.OfType<ISimPartyMember>().Select(m => m.Role).ToArray();
        var actualTarget = ((ISimPartyMember)target).Role;
        Snapshots.Add(new(world.Events.Elapsed, action, actualTarget, recipients, statuses));
        DiagnosticLog.Info($"[Tank swap] t={world.Events.Elapsed:F2} action={action} target={actualTarget}@{target.Position} holder={Holder} recipients=[{string.Join(",", recipients)}] statuses=[{string.Join(",", statuses)}] vuln={target.HasStatus(vulnerability)}");
    }

}
