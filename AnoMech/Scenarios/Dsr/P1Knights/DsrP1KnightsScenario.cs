using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dsr.P1Knights;

public sealed class DsrP1KnightsScenario : IScenario
{
    public string Name => "The Holy See — South-first / HMRT (Elemental)";
    public bool IsIncomplete => true;
    public bool EnablePhaseBgm => true;
    public IPhase Phase => DsrZone.P1;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP1KnightsAi()];
    public DsrP1KnightsState? LastState { get; private set; }
    public bool Completed { get; private set; }
    private SimWorld world = null!;
    private DsrP1KnightsState state = null!;
    private SimEnemy adelphel = null!, grinnaux = null!, charibert = null!, zephirin = null!, haurchefant = null!;
    private readonly List<SimEnemy> actors = [];
    private readonly Queue<SimEnemy> helpers = [];
    private readonly List<(SimEnemy Actor, Vector3 Spot)> portals = [];
    private readonly List<SimTether> chains = [];
    private SimEventObject? prison;
    private bool imprisoned, hallowing, interrupted;
    private float elapsed;
    private SimCharacter? heavensblazeTarget;
    private int interruptIndex;

    public void Run(SimWorld value, int? selectedAi)
    {
        world = value; state = LastState = new(world.Rng); Completed = false;
        actors.Clear(); helpers.Clear(); portals.Clear(); chains.Clear();
        imprisoned = hallowing = interrupted = false; elapsed = 0; interruptIndex = 0;
        world.Events.Add(0, Initialize);
        world.Events.Add(2.14f, () => adelphel.Cast(0x62D4));
        world.Events.Add(14.17f, () => state.ShieldBashTether = world.Tether(adelphel, End.Passable(world.Party.Get(state.ShieldBashSeed), exclusive: false), 0x54));
        world.Events.Add(14.26f, () => grinnaux.Cast(0x62DA));
        world.Events.Add(19.25f, () => DimensionHit(Dimension.Empty));
        world.Events.Add(19.29f, OpeningBash);
        world.Events.Add(19.29f, () => { heavensblazeTarget = FarthestFrom(charibert.Position); charibert.Cast(0x62DD, heavensblazeTarget); });
        world.Events.Add(24.28f, () => charibert.Cast(Instant(0x62DD, Damage(split: Distribution.Stack(7))), heavensblazeTarget));
        world.Events.Add(34.62f, () => { adelphel.SetTargetable(false); adelphel.SetModelHidden(true); grinnaux.SetPosition(Vector3.Zero); MarkSlashes(state.FirstSlashTargets); });
        world.Events.Add(34.71f, () => grinnaux.Cast(0x62D6));
        world.Events.Add(40.86f, () => Slashes(state.FirstSlashTargets));
        world.Events.Add(41.66f, () => MarkSlashes(state.SecondSlashTargets));
        world.Events.Add(47.94f, () => Slashes(state.SecondSlashTargets));
        world.Events.Add(50.71f, () => { adelphel.SetPosition(new Placement(state.AdelphelLanding, 0).Face(Vector3.Zero)); adelphel.SetModelHidden(false); adelphel.SetTargetable(true); adelphel.PlayActionTimeline(0x1E43); });
        world.Events.Add(51.86f, () => grinnaux.Cast(0x62DC));
        world.Events.Add(52.93f, () => adelphel.Cast(0x62D4));
        world.Events.Add(55.83f, () => KnockParty(true));
        ScheduleBlades();
        world.Events.Add(64.83f, Execution);
        world.Events.Add(67.74f, ClosePortals);
        world.Events.Add(67.96f, Hallowing);
        world.Events.Add(70.5f, () => adelphel.SetPosition(new Vector3(-.6f, 0, -1f)));
        world.Events.Add(76.9f, MarkSymbols);
        world.Events.Add(76.99f, () => charibert.Cast(0x62DE));
        world.Events.Add(78.02f, () => grinnaux.Cast(0x62DC));
        world.Events.Add(80.92f, ChainParty);
        world.Events.Add(81.98f, () => KnockParty(false));
        world.Events.Add(84.04f, CheckChains);
        world.Events.Add(84.61f, Flames);
        world.Events.Add(86.22f, Hallowing);
        world.Events.Add(94.19f, () => adelphel.Cast(0x62D4));
        world.Events.Add(95.21f, () => grinnaux.Cast(state.SecondDimension == Dimension.Empty ? 0x62DAu : 0x62DBu));
        world.Events.Add(100.18f, () => DimensionHit(state.SecondDimension));
        world.Events.Add(105.41f, Hallowing);
        world.Events.Add(110.08f, PreparePrison);
        world.Events.Add(112.31f, () => charibert.Cast(0x62E1));
        world.Events.Add(112.62f, () => prison = world.SpawnEventObject(new() { EObjId = 0x1EB681, Placement = new(DsrP1KnightsState.PrisonCentre, MathF.PI / 2) }));
        world.Events.Add(112.71f, () =>
        {
            foreach (var member in world.Party.ActiveMembers()) member.AddStatus(0xA66, 32.61f);
        });
        world.Events.Add(112.84f, () => charibert.Cast(0x62E4));
        world.Events.Add(114.26f, () => imprisoned = true);
        world.Events.Add(115.28f, () => prison?.EObjSetState(1, 0));
        world.Events.Add(121.39f, () =>
        {
            haurchefant = Spawn(13117, 1455, new Vector3(-19, 0, 0));
            haurchefant.PlayActionTimeline(0x1E4C);
        });
        world.Events.Add(122.28f, () => zephirin.Cast(0x62E2, haurchefant));
        world.Events.Add(122.3f, () => Spawn(11810, 11320, new Vector3(-16, 0, 0)));
        for (var i = 0; i < 7; i++) world.Events.Add(124.11f + i * 1.07f, () => zephirin.Cast(Instant(0x62E3, Damage())));
        foreach (var bait in new[] { 128.16f, 133.2f, 138.23f, 143.27f }) world.Events.Add(bait, Brightwing);
        world.Events.Add(136.82f, () => haurchefant.Despawn());
        world.Events.Add(145.32f, () => { imprisoned = false; foreach (var member in world.Party.ActiveMembers()) member.RemoveStatus(0xA66); });
        world.Events.Add(147.7f, () => charibert.SetHealthFraction(.29f));
        world.Events.Add(148.14f, () => prison?.Despawn());
        world.Events.Add(148.31f, () => charibert.Cast(Instant(0x62E4, Damage())));
        world.Events.Add(150, () => { foreach (var actor in actors) actor.Despawn(); Completed = true; });
        world.ActionResolved += ActionResolved;
        world.OnDespawn(() => world.ActionResolved -= ActionResolved);
        if (selectedAi is { } index) ((IScenarioAi<DsrP1KnightsState>)AiStrats[index]).Run(state, world);
    }

    private SimEnemy Spawn(uint id, uint name, Vector3 spot, bool targetable = false)
    {
        var actor = world.SpawnEnemy(new(id, name, Level: 90, Targetable: targetable,
            EnemyList: targetable ? EnemyListMode.Always : EnemyListMode.Never,
            IsHostile: id != 13117, Visibility: id == 9020 ? SpawnVisibility.InvisibleHelper : SpawnVisibility.Visible, Placement: new(spot, MathF.PI)))
            ?? throw new InvalidOperationException("Could not spawn Holy See actor");
        actors.Add(actor); return actor;
    }
    private void Initialize()
    {
        world.EnforceSquareArenaBoundary(22);
        adelphel = Spawn(12601, 3634, new(-.6f, 0, -1), true);
        grinnaux = Spawn(12602, 3639, Vector3.Zero, true);
        charibert = Spawn(12603, 3642, new(0, 0, -22));
        for (var i = 0; i < 24; i++) helpers.Enqueue(Spawn(9020, 3632, Vector3.Zero));
        adelphel.SetTarget(world.Party.Get(PartyRole.OffTank), false); grinnaux.SetTarget(world.Party.Get(PartyRole.MainTank), false);
    }
    private SimEnemy Helper(Placement at)
    {
        var actor = helpers.Dequeue(); actor.SetPosition(at);
        world.Events.Add(4f, () => helpers.Enqueue(actor)); return actor;
    }
    private static EnemyAction Instant(uint id, params IEnemyActionEffect[] effects) => new(id)
    { Cast = new() { CastSecondsOverride = 0, FaceTarget = false }, Effects = effects, Timing = new() { DamageDelay = 0 } };
    private SimCharacter? FarthestFrom(Vector3 spot) => world.Party.ActiveMembers().Where(m => m.IsAlive()).MaxBy(m => Vector3.DistanceSquared(m.Position, spot));
    private void OpeningBash()
    {
        var holder = state.ShieldBashTether?.B;
        state.ShieldBashTether?.Despawn();
        if (holder == null) return;
        adelphel.SetRotation(adelphel.Placement().Face(holder.Position).Rotation);
        adelphel.Cast(Instant(0x62D1, OnTarget(Damage(Severity.TankBuster.MinMit(1f) with { ForceMitigation = true })), OnOthers(Damage(Severity.Lethal))), holder);
        for (var hit = 0; hit < 6; hit++) world.Events.Add(3.03f + hit * .3f, () =>
            Helper(adelphel.Placement().Face(holder.Position)).Cast(Instant(0x62D3, OnTarget(Damage(Severity.TankBuster)), OnOthers(Damage(Severity.Lethal))) with { Area = new() { Size = MathF.PI / 4 } }, holder));
    }
    private void DimensionHit(Dimension dimension)
    {
        var action = Instant(dimension == Dimension.Empty ? 0x62DAu : 0x62DBu, Damage(Severity.Lethal));
        if (dimension == Dimension.Empty) action = action with { Area = new() { Size = 6 } };
        Helper(grinnaux.Placement()).Cast(action);
    }
    private void MarkSlashes(IReadOnlyList<PartyRole> roles)
    { foreach (var role in roles) world.Party.Get(role)?.AttachLockonVfx(0xEA); }
    private void Slashes(IReadOnlyList<PartyRole> roles)
    {
        foreach (var role in roles)
        {
            var target = world.Party.Get(role)!;
            Helper(grinnaux.Placement().Face(target.Position)).Cast(Instant(0x62D7, Damage(VulnerableTo(0xB7D)), ApplyStatus(0xB7D, 3.96f)), target);
            var direction = Vector3.Normalize(target.Position - grinnaux.Position);
            var distance = 22 / MathF.Max(MathF.Abs(direction.X), MathF.Abs(direction.Z));
            var spot = direction * distance;
            world.Events.Add(.6f, () =>
            {
                if (portals.Any(p => Vector3.Distance(p.Spot, spot) < 8)) Wipe(0x62D7, "Hyperdimensional portals too close");
                portals.Add((Spawn(13071, 3293, spot), spot));
            });
        }
        var stack = world.Party.ActiveMembers().First(m => !roles.Contains(((ISimPartyMember)m).Role));
        Helper(grinnaux.Placement().Face(stack.Position)).Cast(Instant(0x63EE, Damage(VulnerableTo(0xB7D), split: Distribution.Stack(4)), ApplyStatus(0xB7D, 3.96f)) with { Area = new() { Size = MathF.PI / 4 } }, stack);
    }
    private void KnockParty(bool resist)
    {
        Helper(grinnaux.Placement()).Cast(Instant(0x62DC, Resolve(ctx =>
        {
            foreach (var member in ctx.Hits)
                if (!resist || !world.Party.IsBotDriven(member)) ctx.Knockback(member, 16, 20, 0, 169);
        })));
    }
    private void ScheduleBlades()
    {
        var path = state.AdelphelDashPath();
        float[] times = [59.09f, 60.15f, 61.22f, 62.29f];
        float[][] fractions = [[0,.5f,1], [.33f,.67f,1], [.5f,1], [.33f,.67f,1]];
        float[][] delays = [[.24f,.51f,.91f], [.3f,.52f,.96f], [.26f,.48f], [.25f,.55f,.99f]];
        for (var i = 0; i < 4; i++)
        {
            var from = path[i]; var to = path[i + 1];
            world.Events.Add(times[i], () =>
            {
                Helper(new Placement(from, 0).Face(to)).Cast(Instant(0x62CE, Resolve(ctx =>
                {
                    var line = to - from; var length = line.Length(); var dir = line / length;
                    foreach (var member in world.Party.ActiveMembers())
                    {
                        var off = member.Position - from; var along = Vector3.Dot(off, dir);
                        if (along >= 0 && along <= length && (off - dir * along).Length() < 3) ctx.Kill(member, "In Shining Blade path");
                    }
                })));
                adelphel.Cast(Instant(0x62CE), to);
                adelphel.MoveTo(to, 60);
            });
            for (var j = 0; j < fractions[i].Length; j++)
            {
                var spot = Vector3.Lerp(from, to, fractions[i][j]);
                world.Events.Add(times[i] + delays[i][j], () =>
                {
                    var orb = Spawn(13070, 4385, spot);
                    world.Events.Add(1.17f, () => orb.Cast(new EnemyAction(0x62CF) { Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0 } }));
                    if (portals.Any(p => Vector3.Distance(p.Spot, spot) < 7)) Wipe(0x62CF, "Brightsphere detonated a portal");
                    world.Events.Add(5.16f, orb.Despawn);
                });
            }
        }
    }
    private void Execution()
    {
        var target = world.Party.Get(PartyRole.OffTank)!;
        adelphel.SetPosition(target.Position);
        Helper(new(target.Position, 0)).Cast(Instant(0x62D5, OnTarget(Damage(Severity.TankBuster.MinMit(1f) with { ForceMitigation = true })), OnOthers(Damage(Severity.Lethal))), target);
    }
    private void Hallowing()
    {
        var generation = ++interruptIndex; hallowing = true; interrupted = false;
        adelphel.Cast(new EnemyAction(0x62D0) { Cast = new() { CastSecondsOverride = 3.7f, Interruptible = true } }, grinnaux);
        var assigned = DsrP1KnightsState.HallowingInterrupters[Math.Min(generation - 1, 2)];
        world.Events.Add(1.6f, () =>
        {
            if (world.Party.Get(assigned) is { } bot && bot.IsAlive() && world.Party.IsBotDriven(bot)) Interrupt();
        });
        world.Events.Add(3.7f, () =>
        {
            if (generation != interruptIndex || interrupted) return;
            hallowing = false; adelphel.SetHealthFraction(1); grinnaux.SetHealthFraction(1);
            Wipe(0x62D0, "Missed Holiest Hallowing interrupt");
        });
    }
    private void Interrupt()
    { if (!hallowing) return; interrupted = true; hallowing = false; adelphel.CancelCast(CastCancelReason.Interrupted); }
    private void ActionResolved(SimCharacter actor, uint action, ulong target)
    { if (action is 7538 or 7551 && target == adelphel.GameObjectId) Interrupt(); }
    private void MarkSymbols()
    {
        foreach (var (role, symbol) in state.Symbols)
            world.Party.Get(role)?.AttachLockonVfx(symbol switch { ChainSymbol.Circle => 0x119u, ChainSymbol.Triangle => 0x11Au, ChainSymbol.Square => 0x11Bu, _ => 0x11Cu });
    }
    private void ChainParty()
    {
        foreach (var (a, b, _) in state.ChainPairs()) chains.Add(world.Tether(world.Party.Get(a), world.Party.Get(b), 9, 15, 0x301));
    }
    private void CheckChains()
    {
        foreach (var chain in chains.Where(c => c.IsActive))
        {
            chain.A?.Die(0x62E0, "Burning Chain not broken"); chain.B?.Die(0x62E0, "Burning Chain not broken"); chain.Despawn();
        }
    }
    private void Flames()
    {
        foreach (var member in world.Party.ActiveMembers()) Helper(new(member.Position, 0)).Cast(Instant(0x62DF, Damage(), OnOthers(Damage(Severity.Lethal))), member);
    }
    private void ClosePortals() { foreach (var portal in portals) portal.Actor.Despawn(); portals.Clear(); }
    private void PreparePrison()
    {
        adelphel.SetTargetable(false); grinnaux.SetTargetable(false); adelphel.SetModelHidden(true); grinnaux.SetModelHidden(true);
        world.Map.DirectorUpdate(0x80000016, 0, 0x14);
        charibert.SetPosition(new Placement(DsrP1KnightsState.CharibertSpot, MathF.PI)); charibert.SetTargetable(true);
        zephirin = Spawn(12592, 3633, new(22, 0, 0)); zephirin.SetRotation(-MathF.PI / 2);
    }
    private void Brightwing()
    {
        var baits = world.Party.ActiveMembers().Where(m => m.IsAlive()).OrderBy(m => Vector3.DistanceSquared(m.Position, charibert.Position)).Take(2).ToArray();
        foreach (var target in baits)
        {
            Helper(charibert.Placement().Face(target.Position)).Cast(Instant(0x6319, Damage(VulnerableTo(0x8E6)), ApplyStatus(0x8E6, 17.96f)) with { Area = new() { Size = MathF.PI / 12 } }, target);
            target.AddStatus(0xA65, 5);
            world.Events.Add(5.48f, () =>
            {
                var spot = target.Position; target.RemoveStatus(0xA65);
                Helper(new(spot, 0)).Cast(new EnemyAction(0x631A) { Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0 } });
            });
        }
    }
    private void Wipe(uint action, string cause) { foreach (var member in world.Party.ActiveMembers()) member.Die(action, cause); }
    public void Tick(float delta, float time)
    {
        elapsed = time;
        foreach (var chain in chains.Where(c => c.IsActive && c.StretchGt(30)))
        { chain.A?.RemoveStatus(0x301); chain.B?.RemoveStatus(0x301); chain.Despawn(); }
        foreach (var member in world.Party.ActiveMembers())
        {
            if (portals.Any(p => Vector3.Distance(p.Spot, member.Position) < 5)) member.Die(0x62D7, "Touched a Hyperdimensional portal");
            if (imprisoned && Vector3.Distance(member.Position, DsrP1KnightsState.PrisonCentre) > 9) member.Die(0x62E1, "Left the Planar Prison");
        }
    }
}
