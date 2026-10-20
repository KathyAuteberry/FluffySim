using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dsr.P6DoubleDragons;

public sealed class DsrP6DoubleDragonsScenario : IScenario
{
    private readonly bool secondHalf;
    public DsrP6DoubleDragonsScenario() : this(false) { }
    public DsrP6DoubleDragonsScenario(bool secondHalf) => this.secondHalf = secondHalf;
    public string DisplayNameOverride => secondHalf ? "P6 Hallowed Wings 2 to Cauterize" : "P6 Wyrmsbreath 1 to Wroth Flames";
    public string Name => secondHalf ? "Hallowed Wings 2 to Cauterize (Elemental 5-1)" : "Wyrmsbreath to Wroth Flames (Elemental)";
    public bool IsIncomplete => true;
    public bool EnablePhaseBgm => true;
    public IPhase Phase => DsrZone.P6;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP6DoubleDragonsAi()];
    private readonly DsrP6DoubleDragonsStateOverrides overrides = new();
    public object SettingsOverrides => overrides;
    public void DrawSettings()
    {
        var difference = overrides.InitialHpDifference;
        if (Dalamud.Bindings.ImGui.ImGui.SliderFloat("Initial HP difference", ref difference, -.1f, .1f, "%.3f")) overrides.InitialHpDifference = difference;
    }
    public DsrP6DoubleDragonsState? LastState { get; private set; }
    public bool Completed { get; private set; }
    public const float TethersOut = 14.65f;
    public const float BreathsLand = 21.83f;
    public const float VowApplied = 29.0f;
    public const float AkhAfahCast = 32.17f;
    public const float AkhAfahLands = 40.36f;
    public const float HallowedWingsCast = 45.98f;
    public const float NidhoggCauterizeCast = 49.53f;
    public const float HallowedWingsLands = 54.26f;
    public static readonly float[] VowPasses = [62.97f, 97.03f, 131.06f, 165.10f];
    public const float WrothFlames = 66.08f;
    public static readonly float[] OrbSpawns = [70.37f, 72.39f, 75.58f];
    public static readonly float[] FlameBlastCasts = [77.69f, 79.69f, 82.68f];
    public const float FlameBlastDelay = 4.99f;
    public const float AkhMornCast = 71.38f;
    public static readonly float[] AkhMornHits = [79.36f, 81.01f, 82.57f, 84.13f];
    public const float HraesvelgrCauterizeCast = 74.46f;
    public const float HotWingCast = 85.78f;
    public const float HotWingLands = 92.26f;
    public const float FlamesResolve = 93.27f;
    public const float FirstHalfEnd = 100.0f;
    public const float SecondHalfStart = 99.0f;
    public const float SecondAkhAfahCast = 101.37f;
    public const float SecondAkhAfahLands = 109.56f;
    public const float SecondHallowedWingsCast = 114.05f;
    public const float SecondHotWingCast = 116.05f;
    public const float SecondHallowedWingsLands = 122.34f;
    public const float SecondHotWingLands = 122.52f;
    public const float SecondTethersOut = 135.00f;
    public const float SecondBreathsLand = 142.17f;
    public const float ElementsTurn = 154.17f;
    public const float CauterizeCast = 149.70f;
    public const float CauterizeLands = 154.70f;
    public const float TouchdownLands = 161.69f;
    public const float RevengeOfTheHorde = 162.94f;
    public const float SecondHalfEnd = 169.0f;


    private SimWorld world = null!;
    private DsrP6DoubleDragonsState state = null!;
    private SimEnemy nidhogg = null!, hraesvelgr = null!;
    private readonly List<SimEnemy> actors = [];
    private readonly Queue<SimEnemy> helpers = [];
    private readonly List<(SimCharacter Member, SimEnemy Dragon, bool Fire, SimTether Tether)> tethers = [];
    private readonly List<(SimEventObject Object, float ActiveAt)> pools = [];
    // UNVERIFIED: puddle activation and bleed cadence await a native capture.
    private readonly PeriodicExposure<SimCharacter> puddleExposure = new(1f, 1f);
    private readonly Dictionary<SimCharacter, Vector3> pyretic = [];
    private readonly List<SimEnemy>[] orbs = [[], [], []];
    private SimCharacter? vow;
    private float elapsed;
    private float nidhoggHp, hraesvelgrHp;
    private SimTether? hpLink;
    private float Start => secondHalf ? SecondHalfStart : 0;
    private float End => secondHalf ? SecondHalfEnd : FirstHalfEnd;
    private static readonly Vector3 NidhoggHome = new(-22, 0, 0), HraesvelgrHome = new(22, 0, 0);
    // UNVERIFIED: breath tether threshold and cone half-angle require native measurements.
    public const float BreathTautDistance = 21f;
    private const float BreathHalfAngle = MathF.PI / 30f;

    public void Run(SimWorld value, int? selectedAi)
    {
        world = value; elapsed = 0; Completed = false; hpLink = null;
        nidhoggHp = .7f; hraesvelgrHp = .7f + overrides.InitialHpDifference;
        actors.Clear(); helpers.Clear(); tethers.Clear(); pools.Clear(); pyretic.Clear(); puddleExposure.Clear();
        foreach (var set in orbs) set.Clear();
        state = LastState = new(world.Rng, overrides) { StartTime = Start, EndTime = End };
        world.Events.Add(0, Initialize);
        world.Events.Add(.4f, RefreshHp);
        foreach (var at in new[] { 8.47f, 11.58f, 24.82f, 27.94f, 59.80f, 62.92f, 66.03f, 85.87f, 88.94f, 92.02f, 95.09f, 98.21f })
            At(at, () => Auto(nidhogg, PartyRole.MainTank, 0x6D2A));
        foreach (var at in new[] { 8.51f, 11.63f, 24.86f, 27.94f, 59.84f, 62.96f, 95.14f, 98.26f })
            At(at, () => Auto(hraesvelgr, PartyRole.OffTank, 0x6D22));
        for (var i = 0; i < 2; i++)
        {
            var index = i;
            At(i == 0 ? TethersOut : SecondTethersOut, () => TetherBreaths(index));
            At(i == 0 ? 14.74f : 135.08f, () => CastBreaths(index));
            At(i == 0 ? BreathsLand : SecondBreathsLand, () => ResolveBreaths(index));
            At(i == 0 ? AkhAfahCast : SecondAkhAfahCast, () => { nidhogg.Cast(0x6D43); hraesvelgr.Cast(0x6D41); });
            At(i == 0 ? AkhAfahLands : SecondAkhAfahLands, () => Afah(index));
            At(i == 0 ? HallowedWingsCast : SecondHallowedWingsCast, () => hraesvelgr.Cast(WingsId(index)));
            At(i == 0 ? HallowedWingsLands : SecondHallowedWingsLands, () => Wings(index));
            At(i == 0 ? HotWingCast : SecondHotWingCast, () => nidhogg.Cast(state.HotWingAt(index) ? 0x6D2Bu : 0x6D2Du));
            At(i == 0 ? HotWingLands : SecondHotWingLands, () => Hot(index));
        }
        At(VowApplied, () => GiveVow(world.Party.Get(state.VowTarget)!, VowPasses[0]));
        for (var i = 0; i < VowPasses.Length; i++) { var pass = i; At(VowPasses[i], () => PassVow(pass)); }
        At(47.5f, () => Fly(nidhogg, state.NidhoggDiveStart));
        At(NidhoggCauterizeCast, () => nidhogg.Cast(0x6D3E));
        At(54.53f, () => Dive(nidhogg, 0x6D3E));
        At(58, () => Home(nidhogg, NidhoggHome));
        At(WrothFlames, () => nidhogg.Cast(0x6D45));
        At(70.09f, () =>
        {
            foreach (var role in state.Spreads) world.Party.Get(role)?.AddStatus(0xAC6, 23.4f);
            foreach (var role in state.Stacks) world.Party.Get(role)?.AddStatus(0xAC7, 23.4f);
        });
        for (var i = 0; i < 3; i++)
        {
            var set = i;
            At(OrbSpawns[i], () => { foreach (var spot in state.OrbSet(set)) orbs[set].Add(Spawn(13238, 3459, spot)); });
            At(FlameBlastCasts[i], () => { foreach (var orb in orbs[set]) orb.Cast(0x6729); });
            At(FlameBlastCasts[i] + FlameBlastDelay, () => FlameBlasts(set));
            At(new[] { 94f, 96.1f, 99.3f }[i], () => { foreach (var orb in orbs[set]) orb.Despawn(); });
        }
        At(AkhMornCast, () => nidhogg.Cast(0x6D46, world.Party.Get(state.AkhMornTarget)));
        for (var i = 0; i < 4; i++) { var hit = i; At(AkhMornHits[i], () => AkhMorn(hit)); }
        At(73.5f, () => Fly(hraesvelgr, state.HraesvelgrDiveStart));
        At(HraesvelgrCauterizeCast, () => hraesvelgr.Cast(0x6D3F));
        At(79.45f, () => Dive(hraesvelgr, 0x6D3F));
        At(85, () => Home(hraesvelgr, HraesvelgrHome));
        At(FlamesResolve, Wroth);
        At(134.85f, () => world.Map.DirectorUpdate(0x80000016, 1, 0x3C));
        At(148.5f, () => { Fly(nidhogg, state.NidhoggCauterizeStart); Fly(hraesvelgr, state.HraesvelgrCauterizeStart); });
        At(CauterizeCast, () => { nidhogg.Cast(0x6D3E); hraesvelgr.Cast(0x6D3F); });
        At(ElementsTurn, TurnElements);
        At(CauterizeLands, CleanseDives);
        At(156, () =>
        {
            foreach (var member in world.Party.ActiveMembers().Where(m => m.HasStatus(0x3C0) || m.HasStatus(0xD98)))
                member.Die(0x6D3E, "Did not cleanse the elemental debuff with the opposite dragon");
        });
        At(159.5f, () => { Home(nidhogg, Vector3.Zero); Home(hraesvelgr, new(0, 0, 25)); });
        At(160.8f, () => { nidhogg.Cast(0x6D40); hraesvelgr.Cast(0x6D40); });
        At(TouchdownLands, () =>
        {
            foreach (var member in world.Party.ActiveMembers())
                if (Vector3.Distance(member.Position, nidhogg.Position) < 10 || Vector3.Distance(member.Position, hraesvelgr.Position) < 10)
                    member.Die(0x70E7, "Too close to Touchdown");
        });
        At(RevengeOfTheHorde, () => { nidhogg.Cast(0x6D21); hraesvelgr.Cast(0x6D21); });
        At(End, () => { foreach (var actor in actors) actor.Despawn(); foreach (var pool in pools) pool.Object.Despawn(); Completed = true; });
        world.ActionResolved += OnAction;
        world.OnDespawn(() => { world.ActionResolved -= OnAction; puddleExposure.Clear(); });
        if (selectedAi is { } ai) ((IScenarioAi<DsrP6DoubleDragonsState>)AiStrats[ai]).Run(state, world);
    }

    private void At(float time, Action action) { if (time >= Start && time <= End) world.Events.Add(time - Start, action); }
    private SimEnemy Spawn(uint id, uint name, Vector3 spot, bool targetable = false)
    {
        var actor = world.SpawnEnemy(new(id, name, Level: 90, Targetable: targetable,
            EnemyList: targetable ? EnemyListMode.Always : EnemyListMode.Never,
            Visibility: id == 9020 ? SpawnVisibility.InvisibleHelper : SpawnVisibility.Visible, Placement: new(spot, 0)))
            ?? throw new InvalidOperationException("Could not spawn Double Dragons actor");
        actors.Add(actor); return actor;
    }
    private void Initialize()
    {
        world.EnforceSquareArenaBoundary(22f);
        nidhogg = Spawn(12612, 3458, NidhoggHome, true); hraesvelgr = Spawn(12613, 4954, HraesvelgrHome, true);
        Home(nidhogg, NidhoggHome); Home(hraesvelgr, HraesvelgrHome);
        for (var i = 0; i < 24; i++) helpers.Enqueue(Spawn(9020, 3458, Vector3.Zero));
        if (!secondHalf) return;
        GiveVow(world.Party.Get(PartyRole.OffTank)!, VowPasses[2]);
        world.Party.Get(PartyRole.MainTank)?.AddStatus(0xB51, VowPasses[1] + 100 - Start);
        world.Party.Get(state.VowTarget)?.AddStatus(0xB51, VowPasses[0] + 100 - Start);
    }
    private SimEnemy Helper(Placement placement)
    {
        var helper = helpers.Dequeue(); helper.SetPosition(placement);
        world.Events.Add(1.5f, () => helpers.Enqueue(helper)); return helper;
    }
    private static EnemyAction Instant(uint id, params IEnemyActionEffect[] effects) => new(id)
    {
        Cast = new() { CastSecondsOverride = 0, FaceTarget = false }, Effects = effects, Timing = new() { DamageDelay = 0 },
    };
    private void Auto(SimEnemy dragon, PartyRole role, uint id) => dragon.Cast(Instant(id, OnTarget(Damage())), world.Party.Get(role));
    private static void Home(SimEnemy dragon, Vector3 position)
    {
        dragon.SetPosition(new Placement(position, position.X < 0 ? MathF.PI / 2 : -MathF.PI / 2)); dragon.SetModelHidden(false); dragon.SetTargetable(true);
    }
    private static void Fly(SimEnemy dragon, Vector3 position)
    {
        dragon.SetTargetable(false); dragon.SetPosition(new Placement(position, position.Z < 0 ? 0 : MathF.PI));
    }
    private void Dive(SimEnemy dragon, uint id) => Helper(dragon.Placement()).Cast(Instant(id, Damage(Severity.Lethal)));
    private void TetherBreaths(int index)
    {
        foreach (var member in world.Party.ActiveMembers())
        {
            var role = ((ISimPartyMember)member).Role;
            if (role.IsTank()) continue;
            var fire = state.NidhoggTethersAt(index).Contains(role); var dragon = fire ? nidhogg : hraesvelgr;
            tethers.Add((member, dragon, fire, world.Tether(member, dragon, fire ? (ushort)0xC2 : (ushort)0xC3)));
        }
    }
    private void CastBreaths(int index)
    {
        var glow = state.GlowAt(index);
        nidhogg.Cast(DsrP6DoubleDragonsState.NidhoggGlows(glow) ? 0x6D33u : 0x6D32u);
        hraesvelgr.Cast(DsrP6DoubleDragonsState.HraesvelgrGlows(glow) ? 0x6D35u : 0x6D34u);
    }
    private static bool InCone(Vector3 from, Vector3 toward, Vector3 point, float angle)
    {
        var direction = Vector3.Normalize(toward - from); var offset = point - from;
        return Vector3.Dot(direction, Vector3.Normalize(offset)) >= MathF.Cos(angle);
    }
    private void ResolveBreaths(int index)
    {
        var counts = world.Party.ActiveMembers().ToDictionary(m => m, _ => new int[2]);
        foreach (var tether in tethers)
        {
            var pose = tether.Dragon.Placement().Face(tether.Member.Position);
            Helper(pose).Cast(Instant(tether.Fire ? 0x6D36u : 0x6D37u));
            foreach (var member in counts.Keys)
                if (InCone(pose.Position, tether.Member.Position, member.Position, BreathHalfAngle)) counts[member][tether.Fire ? 0 : 1]++;
            if (Vector3.Distance(tether.Member.Position, tether.Dragon.Position) < BreathTautDistance)
                tether.Member.Die(tether.Fire ? 0x6D36u : 0x6D37u, "Breath tether was too short");
            tether.Tether.Despawn();
        }
        tethers.Clear();
        foreach (var (member, hits) in counts)
        {
            if (hits[0] > 1 || hits[1] > 1 || index == 0 && hits[0] != hits[1]) member.Die(0x6D36, "Incorrect breath overlap");
            if (index == 1 && hits[0] != hits[1]) member.AddStatus(hits[0] > hits[1] ? (ushort)0xB52 : (ushort)0xB53, 12.05f);
            if (member.Position.Length() > 20) member.Die(0x6D38, "Outside Swirling Blizzard safe area");
        }
        var glow = state.GlowAt(index);
        foreach (var (dragon, glows, id, tank) in new[] {
            (nidhogg, DsrP6DoubleDragonsState.NidhoggGlows(glow), 0x6D3Bu, PartyRole.MainTank),
            (hraesvelgr, DsrP6DoubleDragonsState.HraesvelgrGlows(glow), 0x6D3Cu, PartyRole.OffTank) })
        {
            if (glows && glow != BreathGlow.Both)
            {
                foreach (var member in counts.Keys)
                    if (InCone(dragon.Position, Vector3.Zero, member.Position, MathF.PI / 12)) member.Die(id, "In glowing dragon's breath");
                Helper(dragon.Placement().Face(Vector3.Zero)).Cast(Instant(id));
            }
            else
            {
                var target = world.Party.Get(tank)!;
                var radius = glows ? 6f : 15f;
                var caught = counts.Keys.Where(m => Vector3.Distance(m.Position, target.Position) < radius).ToList();
                Helper(new(target.Position, 0)).Cast(Instant(glows ? (dragon == nidhogg ? 0x6D39u : 0x6D3Au) : 0x6D3Du,
                    Resolve(ctx => { foreach (var m in caught) if (!((ISimPartyMember)m).Role.IsTank() || glows && caught.Count != 2) ctx.Kill(m, "Incorrect tank breath separation"); }), Damage(Severity.TankBuster)));
            }
        }
    }
    private void Afah(int index)
    {
        if (MathF.Abs(nidhoggHp - hraesvelgrHp) > .03f)
        {
            hpLink ??= world.Tether(nidhogg, hraesvelgr, 2);
            foreach (var member in world.Party.ActiveMembers()) member.Die(0x6D44, "Dragon HP difference exceeded 3%");
        }
        foreach (var (dragon, role, id) in new[] { (nidhogg, state.NidhoggStackTarget(index), 0x6D44u), (hraesvelgr, state.HraesvelgrStackTarget(index), 0x6D42u) })
            dragon.Cast(Instant(id, Damage(split: Distribution.Stack(4))), world.Party.Get(role));
    }
    private uint WingsId(int index) => (state.WingsNorthAt(index), state.PlumesNearAt(index)) switch
    { (true, true) => 0x6D26, (true, false) => 0x6D27, (false, true) => 0x6D23, _ => 0x6D24 };
    private void Wings(int index)
    {
        var nearest = world.Party.ActiveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, hraesvelgr.Position));
        var tanks = (state.PlumesNearAt(index) ? nearest : nearest.Reverse()).Take(2).ToList();
        foreach (var member in world.Party.ActiveMembers())
            if (state.WingsNorthAt(index) ? member.Position.Z < 0 : member.Position.Z > 0) member.Die(WingsId(index), "In Hallowed Wings half arena");
        Helper(new(new Vector3(22, 0, state.WingsNorthAt(index) ? -11 : 11), -MathF.PI / 2)).Cast(Instant(state.WingsNorthAt(index) ? 0x6D28u : 0x6D25u));
        foreach (var target in tanks) Helper(new(target.Position, 0)).Cast(Instant(0x6D29, Damage(Severity.TankBuster), OnOthers(Damage(Severity.Lethal))), target);
    }
    private void GiveVow(SimCharacter member, float until)
    {
        vow = member; member.AddStatus(0xB50, until - (elapsed + Start) + .1f);
    }
    private void PassVow(int pass)
    {
        if (vow == null || !vow.IsAlive()) { FailVow(); return; }
        var caught = world.Party.ActiveMembers().Where(m => m.IsAlive() && m != vow && Vector3.Distance(m.Position, vow.Position) < 5).ToList();
        Helper(new(vow.Position, 0)).Cast(Instant(0x6D31));
        if (caught.Count != 1 || caught[0].HasStatus(0xB51)) { FailVow(); return; }
        vow.RemoveStatus(0xB50); vow.AddStatus(0xB51, 100);
        GiveVow(caught[0], pass < 3 ? VowPasses[pass + 1] : End + 1);
    }
    private void FailVow() { foreach (var member in world.Party.ActiveMembers()) member.Die(0x6D31, "Mortal Vow requires exactly one eligible receiver"); }
    private void Hot(int index)
    {
        var wing = state.HotWingAt(index);
        foreach (var z in wing ? new[] { -14.5f, 14.5f } : new[] { 0f })
            Helper(new(new Vector3(-22, 0, z), MathF.PI / 2)).Cast(Instant(wing ? 0x6D2Cu : 0x6D2Eu, Damage(Severity.Lethal)));
    }
    private void FlameBlasts(int set)
    {
        foreach (var orb in orbs[set])
        {
            foreach (var member in world.Party.ActiveMembers())
                if (MathF.Abs(member.Position.X - orb.Position.X) <= 3 || MathF.Abs(member.Position.Z - orb.Position.Z) <= 3) member.Die(0x6729, "In Flame Blast cross");
        }
    }
    private void AkhMorn(int hit)
    {
        var target = world.Party.Get(state.AkhMornTarget)!; var spot = target.Position;
        Helper(new(spot, 0)).Cast(Instant(hit == 0 ? 0x6D46u : 0x6D47u, Damage(split: Distribution.Stack(8)) ), target);
        if (world.SpawnEventObject(new() { EObjId = 0x1EB683, Placement = new(spot, 0) }) is { } pool)
            pools.Add((pool, elapsed + 1.1f));
    }
    private void Wroth()
    {
        foreach (var role in state.Spreads)
        {
            var target = world.Party.Get(role)!;
            Helper(new(target.Position, 0)).Cast(Instant(0x742B, Damage(), OnOthers(Damage(Severity.Lethal))), target);
            target.RemoveStatus(0xAC6);
        }
        foreach (var role in state.Stacks)
        {
            var target = world.Party.Get(role)!;
            Helper(new(target.Position, 0)).Cast(Instant(0x742C, Damage(split: Distribution.Stack(2))), target);
            target.RemoveStatus(0xAC7);
        }
    }
    private void TurnElements()
    {
        foreach (var member in world.Party.ActiveMembers())
        {
            if (member.HasStatus(0xB52)) { member.RemoveStatus(0xB52); member.AddStatus(0x3C0, 30); pyretic[member] = member.Position; }
            if (member.HasStatus(0xB53)) { member.RemoveStatus(0xB53); member.AddStatus(0xD98, 30); }
        }
    }
    private void CleanseDives()
    {
        foreach (var (dragon, id, cleanses) in new[] { (nidhogg, 0x6D3Eu, (ushort)0xD98), (hraesvelgr, 0x6D3Fu, (ushort)0x3C0) })
        {
            var pose = dragon.Placement();
            Helper(pose).Cast(Instant(id, Resolve(ctx =>
            {
                foreach (var member in ctx.Hits)
                {
                    if (((ISimPartyMember)member).Role.IsTank()) continue;
                    if (!member.HasStatus(cleanses)) ctx.Kill(member, "Wrong elemental dragon");
                    else { member.RemoveStatus(cleanses); pyretic.Remove(member); }
                }
            }), OnNearest(Damage(Severity.TankBuster.MinMit(1f) with { ForceMitigation = true }))));
        }
    }
    private void RefreshHp()
    { nidhogg.SetHealthFraction(nidhoggHp); hraesvelgr.SetHealthFraction(hraesvelgrHp); }
    private void OnAction(SimCharacter actor, uint id, ulong target)
    {
        if (actor.HasStatus(0x3C0)) actor.Die(id, "Acted during Pyretic");
        if (target == nidhogg.GameObjectId) nidhoggHp = MathF.Max(.01f, nidhoggHp - .002f);
        if (target == hraesvelgr.GameObjectId) hraesvelgrHp = MathF.Max(.01f, hraesvelgrHp - .002f);
        if (target == nidhogg.GameObjectId || target == hraesvelgr.GameObjectId) RefreshHp();
    }
    public void Tick(float delta, float time)
    {
        elapsed = time;
        if (nidhogg == null || hraesvelgr == null) return;
        if (MathF.Abs(nidhoggHp - hraesvelgrHp) > .03f) hpLink ??= world.Tether(nidhogg, hraesvelgr, 2);
        else { hpLink?.Despawn(); hpLink = null; }
        for (var i = 0; i < tethers.Count; i++)
        {
            var tether = tethers[i];
            var id = Vector3.Distance(tether.Member.Position, tether.Dragon.Position) >= BreathTautDistance ? (tether.Fire ? (ushort)0xC2 : (ushort)0xC3) : (ushort)0xC4;
            if (tether.Tether.TetherId == id) continue;
            tether.Tether.Despawn();
            tethers[i] = (tether.Member, tether.Dragon, tether.Fire, world.Tether(tether.Member, tether.Dragon, id));
        }
        foreach (var (member, spot) in pyretic)
            if (member.HasStatus(0x3C0) && Vector3.Distance(member.Position, spot) > 1) member.Die(0x6D3E, "Moved during Pyretic");
        foreach (var member in world.Party.ActiveMembers())
        {
            var inside = pools.Any(p => p.Object.IsAlive && elapsed >= p.ActiveAt && Vector3.DistanceSquared(member.Position, p.Object.Position) < 36);
            var ticks = puddleExposure.Advance(member, inside, elapsed);
            if (!inside) member.RemoveStatus(0xB87);
            else if (ticks > 0)
            {
                member.AddStatus(0xB87, 1.1f);
                if (ticks >= 3) member.Die(0x6D46, "Remained in Akh Morn puddles through lethal bleed exposure");
            }
        }
    }
}
