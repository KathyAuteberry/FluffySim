using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.GravenImage2;

// From Kefka's return to the centre after Light of Judgment 1 and its Hyperdrives to the Gravitas
// puddles going off. Scenario time is pull time - 73.0: the ST holds Kefka, and the first
// Double-trouble Trap pass has 45.18s left.
public sealed class DmuGravenImage2Scenario : IMultiplayerReplayable
{
    // UNVERIFIED: a Vitrophyre whose circle reaches a puddle sets it off, i.e. both 5y radii overlap.
    private const float VitrophyreReachesPuddle = 10f;

    public string Name => "Graven Image 2";
    public IPhase Phase => DmuZone.P1;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DmuGravenImage2Ai()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    // Light of Judgment 1's track, 13.4s in.
    public ushort? Bgm => DmuConstants.Bgm.AfterFirstJudgment;
    public float BgmSecondsAtStart => 13.4f;

    public DmuGravenImage2State? LastState { get; private set; }
    public MarkedTankbusterSwap? Swap { get; private set; }

    private static readonly float[] AutoAttacks = [3.60f, 14.71f, 17.74f, 25.80f, 28.83f, 33.89f];

    private readonly DmuGravenImage2SettingsWindow settingsWindow = new();
    private SimWorld world = null!;
    private DmuGravenImage2State state = null!;
    private DmuArena arena = null!;
    private DmuDoubleTroubleTrap trap = null!;
    private readonly List<SimTether> tethers = [];
    private readonly List<(SimEnemy Image, PartyRole Role)> gravitas = [];
    private readonly List<(SimEnemy Image, PartyRole Role)> vitrophyre = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new DmuGravenImage2State(world.Rng, settingsWindow.Overrides);
        LastState = state;
        arena = new DmuArena(world);
        trap = new DmuDoubleTroubleTrap(world, arena, state.TrapPassOrder);
        tethers.Clear();
        gravitas.Clear();
        vitrophyre.Clear();
        Swap = null;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DmuGravenImage2State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () =>
        {
            arena.Spawn(Vector3.Zero, new Dictionary<Statue, (uint, uint)>
            {
                [Statues.Centre] = Beat.Resolve,
                [Statues.PulseWave] = Beat.Resolve,
                [Statues.WaveCannon] = Beat.Fire,
            });
            world.Map.AddEffect(FirstJudgmentArena, 0);
            state.Objects.Kefka = arena.Kefka;
            if (arena.Kefka is { } kefka) Swap = new MarkedTankbusterSwap(world, kefka, PartyRole.OffTank, "DmuRevoltingRuin");
            trap.Apply(state.TrapSupport, state.TrapDps, 45.18f);
        });
        foreach (var at in AutoAttacks) world.Events.Add(at, AutoAttack);
        world.Events.Add(2.74f, arena.TeleportKefkaToCentre);
        world.Events.Add(4.18f, () => arena.Kefka?.Cast(DmuActions.GravenImage));
        world.Events.Add(7.92f, () => Tether(set: 1));
        world.Events.Add(9.21f, () => DmuMysteryMagic.ShowIcons(arena.Kefka, DmuMysteryMagic.IceIcon(state.Ice)));
        world.Events.Add(9.30f, () =>
        {
            arena.Kefka?.Cast(DmuActions.BlizzardBoss);
            DmuMysteryMagic.CastIce(arena, state.Ice);
        });
        world.Events.Add(14.42f, () => Gravitas(south: true));
        world.Events.Add(18.42f, Vitrophyre);
        world.Events.Add(19.17f, RevoltingRuin);
        world.Events.Add(23.08f, () => arena.Beat(CleaveStatue(state.FirstCleaveEast), Beat.WindUp));
        world.Events.Add(24.25f, () => Swap?.FirstHitLands());
        world.Events.Add(24.37f, () => Tether(set: 2));
        world.Events.Add(27.38f, RevoltingRuinSecond);
        world.Events.Add(28.14f, () => arena.Beat(CleaveStatue(state.FirstCleaveEast), Beat.Fire));
        world.Events.Add(28.25f, () => Cleave(state.FirstCleaveEast));
        world.Events.Add(32.86f, () => Gravitas(south: false));
        world.Events.Add(36.22f, () => arena.Beat(CleaveStatue(state.SecondCleaveEast), Beat.WindUp));
        world.Events.Add(36.89f, Vitrophyre);
        world.Events.Add(40.81f, () =>
        {
            arena.Beat(Statues.Gravitas, Beat.Resolve);
            arena.Beat(Statues.Vitrophyre, Beat.Resolve);
        });
        world.Events.Add(41.25f, () => arena.Beat(CleaveStatue(state.SecondCleaveEast), Beat.Fire));
        world.Events.Add(41.30f, () => Cleave(state.SecondCleaveEast));
        world.Events.Add(44.83f, () =>
        {
            foreach (var puddle in arena.Puddles) puddle.PlayBeat(Beat.Resolve.State, Beat.Resolve.Mask);
        });
        world.Events.Add(45.18f, () => trap.Burst(next: 49.0f));
        world.Events.Add(47.82f, GravityIII);
        world.Events.Add(50.85f, () =>
        {
            foreach (var puddle in arena.Puddles) puddle.PlayBeat(4, 8);
        });
        world.Events.Add(51.3f, arena.ClearPuddles);
    }

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } m && m.IsAlive() ? m : null;

    private static Statue CleaveStatue(bool east) => east ? Statues.EastCleave : Statues.WestCleave;

    private void AutoAttack()
    {
        if (arena.Kefka is not { IsActive: true, IsCasting: false } kefka || Swap is null || Alive(Swap.Holder) is not { } tank) return;
        if (Vector2.Distance(new(kefka.Position.X, kefka.Position.Z), new(tank.Position.X, tank.Position.Z)) > 12f) return;
        kefka.Cast(DmuActions.AutoAttack, tank);
    }

    // The middle statue tethers the Gravitas group, the east statue the Vitrophyre group.
    private void Tether(int set)
    {
        foreach (var tether in tethers) tether.Despawn();
        tethers.Clear();
        gravitas.Clear();
        vitrophyre.Clear();
        arena.Beat(Statues.Gravitas, Beat.Activate);
        arena.Beat(Statues.Vitrophyre, Beat.Activate);
        int middle = 0, east = 4;
        foreach (var role in PerRole.All)
        {
            var spreads = state.SpreadsInSet(role, set);
            var image = spreads ? arena.GravenImageAt(east++, Statues.Vitrophyre) : arena.GravenImageAt(middle++, Statues.Gravitas);
            if (image is null || Alive(role) is not { } target) continue;
            tethers.Add(world.Tether(image, target, TetherId.GravenImage));
            (spreads ? vitrophyre : gravitas).Add((image, role));
        }
    }

    private void Gravitas(bool south)
    {
        var spots = new List<Vector3>();
        foreach (var (image, role) in gravitas)
            if (Alive(role) is { } target)
            {
                image.Cast(DmuActions.Gravitas, target);
                spots.Add(target.Position);
            }
        if (spots.Count == 0) return;
        var centre = spots.Aggregate(Vector3.Zero, (a, b) => a + b) / spots.Count;
        if (south) state.Objects.SouthPuddles = new(centre.X, centre.Z);
        else state.Objects.NorthPuddles = new(centre.X, centre.Z);
        world.Events.Add(south ? 0.8f : 1.1f, () =>
        {
            foreach (var spot in spots) arena.DropPuddle(spot);
        });
    }

    private void Vitrophyre()
    {
        foreach (var tether in tethers) tether.Despawn();
        tethers.Clear();
        foreach (var (image, role) in vitrophyre)
            if (Alive(role) is { } target)
                image.Cast(DmuActions.Vitrophyre(ctx =>
                {
                    var at = new Vector2(ctx.Origin.Position.X, ctx.Origin.Position.Z);
                    if (!arena.Puddles.Any(p => Vector2.Distance(at, new(p.Position.X, p.Position.Z)) < VitrophyreReachesPuddle)) return;
                    DiagnosticLog.Info($"[DmuGravenImage2] Vitrophyre on {role} at ({at.X:F1},{at.Y:F1}) reached a Gravitas puddle.");
                    world.Party.WipeAllPlayers(ActionId.GravitationalExplosion, $"{role}'s Vitrophyre reached a Gravitas puddle");
                }), target);
        vitrophyre.Clear();
    }

    private void RevoltingRuin()
    {
        if (Swap?.Mark(LockonId.Tankbuster, expected: PartyRole.OffTank) is { } marked)
            arena.Kefka?.Cast(DmuActions.RevoltingRuin, marked);
    }

    private void RevoltingRuinSecond()
    {
        if (Swap is null || arena.Kefka is not { } kefka) return;
        var target = Swap.SecondHit();
        var outcome = Swap.Outcomes.LastOrDefault();
        if (target is null) return;
        kefka.Cast(DmuActions.RevoltingRuinSecond(ctx =>
        {
            if (outcome is null || outcome.Passed) return;
            foreach (var hit in ctx.Hits) ctx.Kill(hit, $"Revolting Ruin III swap failed ({outcome.Grade})");
        }), target);
    }

    private void Cleave(bool east)
    {
        if (arena.GravenImageAt(8, Statues.Centre) is not { } image) return;
        image.SetRotation(east ? float.Pi / 2f : -float.Pi / 2f);
        image.Cast(east ? DmuActions.IntemperateWill : DmuActions.GravitationalWave);
    }

    // Each puddle soaks whoever stands in it; one nobody stands in goes off on everyone.
    // UNVERIFIED: what an unsoaked puddle does.
    private void GravityIII()
    {
        foreach (var puddle in arena.Puddles)
            arena.Helper(puddle.Position, 0f)?.Cast(DmuActions.GravityIII(ctx =>
            {
                if (ctx.Hits.Count > 0) return;
                world.Party.WipeAllPlayers(ActionId.GravityIII, "a Gravitas puddle went unsoaked");
            }));
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new DmuGravenImage2AiReplayStateMessage(s.EncodeFlags(), s.EncodeRoles()) : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not DmuGravenImage2AiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (DmuGravenImage2State.FromNetworkReplay(msg.Flags, msg.Roles) is not { } shadow) return null;
        ((IScenarioAi<DmuGravenImage2State>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }

    public void RefreshLiveHandles(object shadowState, IReadOnlyDictionary<int, SimEnemy> peerEnemies)
    {
        if (shadowState is DmuGravenImage2State shadow)
            shadow.Objects.Kefka ??= peerEnemies.Values.FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.Kefka);
    }
}
