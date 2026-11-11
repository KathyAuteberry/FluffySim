using System.Collections.Generic;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.Game;

// A two-hit tankbuster whose first hit follows the tank marked at cast start and whose second goes
// to whoever is second on enmity when it lands (DMU Revolting Ruin III). One tank takes both only
// if the other Provokes in between, which this tracks and grades. Enmity is the two tanks' order
// alone: Provoke puts its user first and the previous holder second. A local player's Provoke
// arrives as ActionResolved (LocalProvokeHandler), a bot's or a peer's as SimEnemy.Provoked, so
// both are heard.
public sealed class MarkedTankbusterSwap
{
    public enum Grade { OnTime, Late, Early, Missing }

    public sealed record Outcome(Grade Grade, PartyRole Marked, PartyRole SecondHit, float? ProvokeAt, float CastAt, float FirstHitAt)
    {
        public bool Passed => Grade is Grade.OnTime or Grade.Late;
    }

    public PartyRole Holder { get; private set; }
    public PartyRole Second => Other(Holder);
    public IReadOnlyList<Outcome> Outcomes => outcomes;

    private readonly SimWorld world;
    private readonly SimEnemy boss;
    private readonly string tag;
    private readonly List<Outcome> outcomes = [];
    private PartyRole? marked;
    private PartyRole expectedMarked;
    private float castAt, firstHitAt;
    private float? provokeAt;
    private bool firstHitLanded;

    public MarkedTankbusterSwap(SimWorld world, SimEnemy boss, PartyRole holder, string tag)
    {
        this.world = world;
        this.boss = boss;
        this.tag = tag;
        Holder = holder;
        boss.Provoked += OnProvoked;
        world.ActionResolved += OnActionResolved;
        world.OnDespawn(() =>
        {
            boss.Provoked -= OnProvoked;
            world.ActionResolved -= OnActionResolved;
        });
        FaceHolder();
    }

    // Cast start: locks the first hit on enmity #1 and shows it the tank marker. `expected` is who the
    // strategy wants marked; anyone else means the swap was started too early.
    public SimCharacter? Mark(uint lockonId, PartyRole expected)
    {
        marked = Holder;
        expectedMarked = expected;
        castAt = world.Events.Elapsed;
        provokeAt = null;
        firstHitLanded = false;
        var target = world.Party.Get(Holder);
        target?.ActorControl.HeadMarker(lockonId);
        DiagnosticLog.Info($"[{tag}] event=mark time={castAt:F2} marked={Holder} expected={expected}");
        return target;
    }

    public SimCharacter? FirstHitTarget => marked is { } role ? world.Party.Get(role) : null;

    public void FirstHitLands()
    {
        firstHitLanded = true;
        firstHitAt = world.Events.Elapsed;
    }

    // The second hit's target, enmity #2 now; grades the swap.
    public SimCharacter? SecondHit()
    {
        if (marked is not { } markedRole) return world.Party.Get(Second);
        var recipient = Second;
        var grade = markedRole != expectedMarked ? Grade.Early
            : recipient != markedRole ? Grade.Missing
            : provokeAt is { } at && at <= firstHitAt ? Grade.OnTime
            : Grade.Late;
        var outcome = new Outcome(grade, markedRole, recipient, provokeAt, castAt, firstHitAt);
        outcomes.Add(outcome);
        DiagnosticLog.Info($"[{tag}] event=graded grade={grade} marked={markedRole} secondHit={recipient} provokeAfterCast={(provokeAt - castAt)?.ToString("F2") ?? "none"} firstHitAfterCast={firstHitAt - castAt:F2}");
        world.Announce(grade switch
        {
            Grade.OnTime => $"Provoke on time ({provokeAt - castAt:F1}s into the cast): {markedRole} took both hits.",
            Grade.Late => $"Late Provoke ({provokeAt - castAt:F1}s, after the first hit): it works in game, but Provoke during the cast bar.",
            Grade.Early => $"Provoked before the cast: the marker went to {markedRole}, not {expectedMarked}.",
            _ => $"No Provoke during the cast: the second hit went to {recipient}.",
        });
        marked = null;
        return world.Party.Get(recipient);
    }

    private void OnActionResolved(SimCharacter actor, uint action, ulong target)
    {
        if (action == Enmity.ProvokeActionId && target == boss.GameObjectId.ObjectId) OnProvoked(actor);
    }

    private void OnProvoked(SimCharacter by)
    {
        if (by is not ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank } member || by.Dead) return;
        if (member.Role == Holder) return;
        var before = Holder;
        Holder = member.Role;
        if (marked is not null && member.Role != marked && provokeAt is null) provokeAt = world.Events.Elapsed;
        DiagnosticLog.Info($"[{tag}] event=provoke time={world.Events.Elapsed:F2} by={member.Role} before={before} after={Holder} window={(marked is null ? "closed" : firstHitLanded ? "after-first-hit" : "cast")}");
        FaceHolder();
    }

    private void FaceHolder()
    {
        if (world.Party.Get(Holder) is not { } tank) return;
        boss.SetTarget(tank, false);
        boss.KeepFacing(tank);
    }

    private static PartyRole Other(PartyRole tank) => tank == PartyRole.MainTank ? PartyRole.OffTank : PartyRole.MainTank;
}
