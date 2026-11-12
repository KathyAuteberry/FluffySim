using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.GravenImage3;

// From Tele-trouncing to the last Mystery Magic, after Light of Judgment 2 and its Hyperdrives.
// Scenario time is pull time - 145.0: the MT holds Kefka and the second trap pass has 22.81s left.
public sealed class DmuGravenImage3Scenario : IMultiplayerReplayable
{
    // Tele-trouncing's own radius.
    private const float ArrowTrigger = 2f;
    // Measured from the paddle's centre, wherever the rider stepped in.
    private const float ArrowTeleport = 6f;
    // UNVERIFIED: a lower bound; retail warps come at least this long after the rider is first seen inside.
    private const float ArrowEntryDelay = 0.3f;
    // UNVERIFIED: two paddles this close cancel each other.
    private const float ArrowsCollide = 4f;
    private const float BindSeconds = 1f;
    // A confused rider whose chain ends (lands on no paddle) stays put this long after the bind.
    private const float ChainEndHold = 1.6f;
    private const float ArrowObstacleRadius = 2.2f;
    // A confused player on an armed paddle waits for it, not for the chase: re-held every frame.
    private const float PaddleHoldsChase = 0.25f;
    private const float ConfusedSeconds = 6f;
    private const float WillStatusDelay = 0.63f;

    public string Name => "Graven Image 3";
    public IPhase Phase => DmuZone.P1;
    public bool SupportsSolo => true;
    // Off until a peer's confused chase holds up: peers reach their spots by movement input and
    // ride the paddles with network delay, and the bots' chase then catches its partner.
    public bool SupportsMultiplayer => false;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DmuGravenImage3Ai()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    // Light of Judgment 2's track, 17.6s in.
    public ushort? Bgm => DmuConstants.Bgm.AfterSecondJudgment;
    public float BgmSecondsAtStart => 17.6f;

    public DmuGravenImage3State? LastState { get; private set; }
    public int ArrowsDropped { get; private set; }
    public int ArrowsResolved { get; private set; }
    public int ArrowsLost { get; private set; }
    public IReadOnlyDictionary<PartyRole, int> Rides => rides;
    private readonly Dictionary<PartyRole, int> rides = [];
    public IReadOnlyList<(PartyRole Role, float Time, float Length)> Warps => warps;
    private readonly List<(PartyRole Role, float Time, float Length)> warps = [];

    private static readonly float[] AutoAttacks = [3.2f, 6.2f, 9.2f, 12.3f];

    private sealed class Paddle(SimEventObject? visual, Vector2 at, Arrow arrow, CircleObstacle obstacle)
    {
        public SimEventObject? Visual = visual;
        public Vector2 At = at;
        public Arrow Arrow = arrow;
        public CircleObstacle Obstacle = obstacle;
        public bool Armed;
        public readonly Dictionary<SimCharacter, float> EnteredAt = [];
    }

    private readonly DmuGravenImage3SettingsWindow settingsWindow = new();
    private SimWorld world = null!;
    private DmuGravenImage3State state = null!;
    private DmuArena arena = null!;
    private DmuDoubleTroubleTrap trap = null!;
    private readonly List<Paddle> paddles = [];
    private readonly List<SimTether> tethers = [];
    private bool confusionRunning;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new DmuGravenImage3State(world.Rng, settingsWindow.Overrides);
        LastState = state;
        arena = new DmuArena(world);
        trap = new DmuDoubleTroubleTrap(world, arena, PerRole.All);
        paddles.Clear();
        tethers.Clear();
        confusionRunning = false;
        ArrowsDropped = ArrowsResolved = ArrowsLost = 0;
        rides.Clear();
        warps.Clear();

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DmuGravenImage3State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () =>
        {
            arena.Spawn(Vector3.Zero, new Dictionary<Statue, (uint, uint)>
            {
                [Statues.Centre] = Beat.WindUp,
                [Statues.PulseWave] = Beat.Resolve,
                [Statues.WaveCannon] = Beat.Fire,
                [Statues.Gravitas] = Beat.Resolve,
                [Statues.Vitrophyre] = Beat.Resolve,
                [Statues.EastCleave] = Beat.Fire,
            });
            world.Map.AddEffect(FirstJudgmentArena, 0);
            world.Map.AddEffect(SecondJudgmentArena, 0);
            state.Objects.Kefka = arena.Kefka;
            if (arena.Kefka is { } kefka && Alive(PartyRole.MainTank) is { } tank)
            {
                kefka.SetTarget(tank, false);
                kefka.KeepFacing(tank);
            }
            trap.Apply(state.TrapSupport, state.TrapDps, 22.81f);
        });
        foreach (var at in AutoAttacks) world.Events.Add(at, AutoAttack);
        world.Events.Add(1.59f, () => arena.Kefka?.Cast(DmuActions.TeleTrouncing));
        world.Events.Add(7.34f, ApplyTelePortents);
        world.Events.Add(14.41f, () => DropPaddles(early: true));
        world.Events.Add(15.66f, () => arena.Kefka?.Cast(DmuActions.GravenImage));
        world.Events.Add(17.39f, () => DropPaddles(early: false));
        world.Events.Add(19.41f, TetherWills);
        world.Events.Add(20.78f, () => arena.Kefka?.Cast(DmuActions.StatueGlance));
        world.Events.Add(22.81f, () => trap.Burst(next: null));
        world.Events.Add(28.37f, () =>
        {
            arena.Beat(Statues.Confusion, Beat.Resolve);
            arena.Beat(Statues.Sleep, Beat.Resolve);
        });
        world.Events.Add(28.45f, Wills);
        world.Events.Add(28.45f + WillStatusDelay, StartConfusion);
        world.Events.Add(28.45f + WillStatusDelay + ConfusedSeconds, EndConfusion);
        world.Events.Add(31.66f, () => arena.Beat(GazeStatue, Beat.WindUp));
        world.Events.Add(32.77f, () => arena.Kefka?.Cast(ActionId.StatueGlanceEnd));
        world.Events.Add(34.86f, arena.TeleportKefkaToCentre);
        world.Events.Add(36.31f, () =>
        {
            DmuMysteryMagic.ShowIcons(arena.Kefka, DmuMysteryMagic.FireIcon(state.Fire), DmuMysteryMagic.ThunderIcon(state.Thunder));
            DmuMysteryMagic.MarkPlayers(world.Party, state.Fire);
        });
        world.Events.Add(36.41f, () =>
        {
            arena.Kefka?.Cast(DmuActions.MysteryMagic);
            DmuMysteryMagic.CastThunder(arena, state.Thunder);
        });
        world.Events.Add(41.48f, () => arena.Beat(GazeStatue, Beat.Fire));
        world.Events.Add(41.58f, Gaze);
        world.Events.Add(42.18f, () => DmuMysteryMagic.ResolveFire(arena, world.Party, state.Fire));
        world.Events.Add(44.0f, Verdict);
    }

    public void Tick(float delta, float elapsed)
    {
        foreach (var paddle in paddles.ToList())
        {
            var near = world.Party.ActiveMembers().Where(m => m.IsAlive() && Flat(m.Position) is var p && Vector2.Distance(p, paddle.At) <= ArrowTrigger).ToList();
            if (!paddle.Armed)
            {
                if (near.Count == 0) paddle.Armed = true;
                continue;
            }
            foreach (var gone in paddle.EnteredAt.Keys.Except(near).ToList()) paddle.EnteredAt.Remove(gone);
            foreach (var member in near) paddle.EnteredAt.TryAdd(member, elapsed);
            foreach (var waiting in near.Where(m => m.ActorControl.Confused)) waiting.HoldConfusedChase(PaddleHoldsChase);
            var rider = near.FirstOrDefault(m => !m.HasStatus(StatusId.Bind) && !m.HasStatus(StatusId.Sleep) && elapsed - paddle.EnteredAt[m] >= ArrowEntryDelay);
            if (rider is not null) Ride(paddle, rider);
        }
    }

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } m && m.IsAlive() ? m : null;

    private static Vector2 Flat(Vector3 v) => new(v.X, v.Z);

    private Statue GazeStatue => state.AveMaria ? Statues.AveMaria : Statues.IndolentWill;

    private void AutoAttack()
    {
        if (arena.Kefka is not { IsActive: true, IsCasting: false } kefka || Alive(PartyRole.MainTank) is not { } tank) return;
        if (Vector2.Distance(Flat(kefka.Position), Flat(tank.Position)) > 12f) return;
        kefka.Cast(DmuActions.AutoAttack, tank);
    }

    private void ApplyTelePortents()
    {
        var early = arena.Helper(Vector3.Zero, 0f);
        var late = arena.Helper(Vector3.Zero, 0f);
        foreach (var (role, pair) in state.Arrows8)
        {
            if (Alive(role) is not { } member) continue;
            var earlyStatus = StatusFor(pair, pair.Early, pair.FirstExpiresFirst);
            var lateStatus = StatusFor(pair, pair.Late, !pair.FirstExpiresFirst);
            member.AddStatus(earlyStatus, 7f, sourceObject: early?.GameObjectId ?? default);
            member.AddStatus(lateStatus, 10f, sourceObject: late?.GameObjectId ?? default);
        }
    }

    // A turned pair's second arrow shows the second status set.
    private static ushort StatusFor(ArrowPair pair, Arrow arrow, bool isFirst)
        => (pair.Identical || isFirst ? StatusId.TelePortent : StatusId.TelePortentTurned)[DmuGravenImage3State.Index(arrow)];

    private void DropPaddles(bool early)
    {
        foreach (var (role, pair) in state.Arrows8)
        {
            if (Alive(role) is not { } member) continue;
            var arrow = early ? pair.Early : pair.Late;
            var at = Flat(member.Position);
            arena.Helper(member.Position, 0f)?.Cast(DmuActions.TeleTrouncingDrop, member);
            var clash = paddles.Where(p => Vector2.Distance(p.At, at) < ArrowsCollide).ToList();
            ArrowsDropped++;
            if (clash.Count > 0)
            {
                foreach (var p in clash) Remove(p, lost: true);
                ArrowsLost++;
                Mistake($"{role}'s Tele-portent landed within {ArrowsCollide:F0}y of another; both vanished");
                continue;
            }
            var visual = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.TelePortent,
                EventId = EventId.StatueDirector,
                TargetableStatus = 5,
                Arg2 = TelePortentArg2Base + ((uint)(ArrowsDropped - 1) << 16),
                Placement = new Placement(new Vector3(at.X, 0f, at.Y), DmuGravenImage3State.Facing(arrow)),
            });
            var obstacle = new CircleObstacle(at, ArrowObstacleRadius);
            if (!confusionRunning) world.Obstacles.Add(obstacle);
            paddles.Add(new Paddle(visual, at, arrow, obstacle));
        }
    }

    private void Ride(Paddle paddle, SimCharacter rider)
    {
        var to = paddle.At + DmuGravenImage3State.Direction(paddle.Arrow) * ArrowTeleport;
        Remove(paddle, lost: false);
        ArrowsResolved++;
        if (rider is ISimPartyMember { Role: var role })
        {
            rides[role] = rides.GetValueOrDefault(role) + 1;
            warps.Add((role, world.Events.Elapsed, Vector2.Distance(paddle.At, to)));
        }
        // A carry is the client's own move, so a player's in-flight chase can't walk them off it.
        rider.CarryTo(new Vector3(to.X, 0f, to.Y));
        rider.AddStatus(StatusId.Bind, BindSeconds);
        var chains = paddles.Any(p => p.Armed && Vector2.Distance(p.At, to) <= ArrowTrigger);
        rider.HoldConfusedChase(chains ? BindSeconds : BindSeconds + ChainEndHold);
        DiagnosticLog.Info($"[DmuGravenImage3] {(rider as ISimPartyMember)?.Role} rode a {paddle.Arrow} paddle at ({paddle.At.X:F1},{paddle.At.Y:F1}) to ({to.X:F1},{to.Y:F1}); {ArrowsResolved}/{ArrowsDropped}.");
    }

    private void Remove(Paddle paddle, bool lost)
    {
        paddle.Visual?.Despawn();
        world.Obstacles.Remove(paddle.Obstacle);
        paddles.Remove(paddle);
        if (lost) ArrowsLost++;
    }

    // One group to the confusion statue, the other to the sleep statue.
    private void TetherWills()
    {
        arena.Beat(Statues.Confusion, Beat.Activate);
        arena.Beat(Statues.Sleep, Beat.Activate);
        int confusion = 0, sleep = 4;
        foreach (var role in PerRole.All)
        {
            var confused = role.IsDps() == state.DpsConfused;
            var image = confused ? arena.GravenImageAt(confusion++, Statues.Confusion) : arena.GravenImageAt(sleep++, Statues.Sleep);
            if (image is not null && Alive(role) is { } target) tethers.Add(world.Tether(image, target, TetherId.GravenImage));
        }
    }

    private void Wills()
    {
        foreach (var tether in tethers) tether.Despawn();
        tethers.Clear();
        int confusion = 0, sleep = 4;
        foreach (var role in PerRole.All)
        {
            var confused = role.IsDps() == state.DpsConfused;
            var image = confused ? arena.GravenImageAt(confusion++, Statues.Confusion) : arena.GravenImageAt(sleep++, Statues.Sleep);
            if (image is null || Alive(role) is not { } target) continue;
            image.Cast(confused ? DmuActions.IndulgentWill : DmuActions.IdyllicWill, target);
        }
    }

    // The chase walks into the paddles, so bots stop routing around them.
    private void StartConfusion()
    {
        confusionRunning = true;
        foreach (var paddle in paddles) world.Obstacles.Remove(paddle.Obstacle);
        foreach (var role in PerRole.All.Where(r => r.IsDps() == state.DpsConfused))
            if (Alive(role) is { } member)
            {
                member.AddStatus(StatusId.Confused, ConfusedSeconds);
                member.ActorControl.SetConfused(true);
            }
    }

    private void EndConfusion()
    {
        confusionRunning = false;
        foreach (var member in world.Party.ActiveMembers().ToList())
            if (member.ActorControl.Confused) member.ActorControl.SetConfused(false);
        foreach (var paddle in paddles) world.Obstacles.Add(paddle.Obstacle);
    }

    private void Gaze()
    {
        if (arena.GravenImageAt(8, GazeStatue) is not { } image) return;
        image.Cast(DmuActions.Gaze(lookAway: !state.AveMaria, (ctx, target) =>
        {
            target.AddStatus(StatusId.DamageDown, 180f);
            Mistake($"{(target as ISimPartyMember)?.Role} {(state.AveMaria ? "looked away from Ave Maria" : "looked at Indolent Will")}");
        }));
    }

    // Every paddle has to be ridden before P1 ends, or the run would never see P3.
    private void Verdict()
    {
        var left = paddles.Count;
        DiagnosticLog.Info($"[DmuGravenImage3] Tele-portents: dropped={ArrowsDropped} resolved={ArrowsResolved} lost={ArrowsLost} left={left}.");
        foreach (var paddle in paddles.ToList()) Remove(paddle, lost: false);
        if (ArrowsResolved == 16) world.Announce("All 16 Tele-portents resolved.");
        else Mistake($"{ArrowsResolved}/16 Tele-portents resolved: P3 would stay locked");
    }

    private void Mistake(string explanation)
    {
        world.Announce(explanation);
        Plugin.Game?.RecordMechanicMistake(ActionId.TeleTrouncing, explanation);
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new DmuGravenImage3AiReplayStateMessage(s.EncodeFlags(), s.EncodeRoles(), s.EncodeArrows()) : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not DmuGravenImage3AiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (DmuGravenImage3State.FromNetworkReplay(msg.Flags, msg.Roles, msg.Arrows) is not { } shadow) return null;
        ((IScenarioAi<DmuGravenImage3State>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }

    public void RefreshLiveHandles(object shadowState, IReadOnlyDictionary<int, SimEnemy> peerEnemies)
    {
        if (shadowState is DmuGravenImage3State shadow)
            shadow.Objects.Kefka ??= peerEnemies.Values.FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.Kefka);
    }
}
