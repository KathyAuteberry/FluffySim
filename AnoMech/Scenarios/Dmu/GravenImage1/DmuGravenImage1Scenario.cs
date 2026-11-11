using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.GravenImage1;

// From the pull to Mystery Magic 2: Revolting Ruin III's swap, Graven Image's knockback and
// Mystery Magic, Wave Cannon's towers, and the first Double-trouble Trap. Scenario time is pull
// time (InCombat).
public sealed class DmuGravenImage1Scenario : IMultiplayerReplayable
{
    public string Name => "Graven Image 1";
    public IPhase Phase => DmuZone.P1;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DmuGravenImage1Ai()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    // Retail plays the between-pulls track until Revolting Ruin III.
    public ushort? Bgm => DmuConstants.Bgm.BetweenPulls;

    public DmuGravenImage1State? LastState { get; private set; }
    public MarkedTankbusterSwap? Swap { get; private set; }

    private static readonly float[] AutoAttacks = [2.96f, 5.99f, 9.01f, 17.06f, 20.09f, 23.12f, 31.21f, 39.26f, 45.33f, 48.35f];

    private readonly DmuGravenImage1SettingsWindow settingsWindow = new();
    private SimWorld world = null!;
    private DmuGravenImage1State state = null!;
    private DmuArena arena = null!;
    private DmuDoubleTroubleTrap trap = null!;
    private readonly List<SimTether> tethers = [];
    private readonly List<(SimEnemy Image, SimCharacter Target)> pulseWaves = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new DmuGravenImage1State(world.Rng, settingsWindow.Overrides);
        LastState = state;
        arena = new DmuArena(world);
        trap = new DmuDoubleTroubleTrap(world, arena, state.TrapPassOrder);
        tethers.Clear();
        pulseWaves.Clear();
        Swap = null;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DmuGravenImage1State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () =>
        {
            arena.Spawn(new Vector3(0f, 0f, -0.4f));
            state.Objects.Kefka = arena.Kefka;
            if (arena.Kefka is { } kefka) Swap = new MarkedTankbusterSwap(world, kefka, PartyRole.MainTank, "DmuRevoltingRuin");
        });
        foreach (var at in AutoAttacks) world.Events.Add(at, AutoAttack);
        world.Events.Add(10.05f, () => arena.Beat(Statues.Centre, Beat.Activate));
        world.Events.Add(10.14f, RevoltingRuin);
        world.Events.Add(15.21f, () => Swap?.FirstHitLands());
        world.Events.Add(18.34f, RevoltingRuinSecond);
        world.Events.Add(24.52f, arena.TeleportKefkaToCentre);
        world.Events.Add(26.12f, () => arena.Kefka?.Cast(DmuActions.GravenImage));
        world.Events.Add(29.86f, TetherPulseWave);
        world.Events.Add(32.18f, () =>
        {
            DmuMysteryMagic.ShowIcons(arena.Kefka, DmuMysteryMagic.IceIcon(state.Ice1), DmuMysteryMagic.FireIcon(state.Fire1));
            DmuMysteryMagic.MarkPlayers(world.Party, state.Fire1);
        });
        world.Events.Add(32.27f, () =>
        {
            arena.Kefka?.Cast(DmuActions.MysteryMagic);
            DmuMysteryMagic.CastIce(arena, state.Ice1);
        });
        world.Events.Add(34.83f, () => arena.Beat(Statues.PulseWave, Beat.Resolve));
        world.Events.Add(34.91f, PulseWave);
        world.Events.Add(37.16f, () => arena.Beat(Statues.WaveCannon, Beat.WindUp));
        world.Events.Add(38.05f, () => DmuMysteryMagic.ResolveFire(arena, world.Party, state.Fire1));
        world.Events.Add(41.42f, () => arena.Kefka?.Cast(DmuActions.DoubleTroubleTrap));
        world.Events.Add(42.18f, () => arena.Beat(Statues.WaveCannon, Beat.Fire));
        world.Events.Add(42.27f, WaveCannon);
        world.Events.Add(42.91f, Towers);
        world.Events.Add(44.41f, () => trap.Apply(state.TrapSupport, state.TrapDps, 5.0f));
        world.Events.Add(48.41f, () => DmuMysteryMagic.ShowIcons(arena.Kefka, DmuMysteryMagic.IceIcon(state.Ice2), DmuMysteryMagic.ThunderIcon(state.Thunder2)));
        world.Events.Add(48.49f, () =>
        {
            arena.Kefka?.Cast(DmuActions.MysteryMagic);
            DmuMysteryMagic.CastIce(arena, state.Ice2);
            DmuMysteryMagic.CastThunder(arena, state.Thunder2);
        });
        world.Events.Add(49.51f, () => trap.Burst(next: 68.0f));
        world.Events.Add(55.0f, () => DiagnosticLog.Info("[DmuGravenImage1] End after Mystery Magic 2."));
    }

    public void RunInstanceEvents(SimWorld instanceWorld)
        => instanceWorld.Events.Add(10.09f, () => instanceWorld.SwitchBgm(DmuConstants.Bgm.Opening));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } m && m.IsAlive() ? m : null;

    private void AutoAttack()
    {
        if (arena.Kefka is not { IsActive: true, IsCasting: false } kefka || Swap is null || Alive(Swap.Holder) is not { } tank) return;
        if (Vector2.Distance(new(kefka.Position.X, kefka.Position.Z), new(tank.Position.X, tank.Position.Z)) > 12f) return;
        kefka.Cast(DmuActions.AutoAttack, tank);
    }

    private void RevoltingRuin()
    {
        if (Swap?.Mark(LockonId.Tankbuster, expected: PartyRole.MainTank) is { } marked)
            arena.Kefka?.Cast(DmuActions.RevoltingRuin, marked);
    }

    // The second hit goes to enmity #2; a missed swap fails the scenario (owner's call).
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

    private IEnumerable<PartyRole> TetheredRoles => state.DpsTethered ? DmuGravenImage1State.Dps : DmuGravenImage1State.Supports;

    private void TetherPulseWave()
    {
        arena.Beat(Statues.PulseWave, Beat.Activate);
        var index = 0;
        foreach (var role in TetheredRoles)
        {
            if (Alive(role) is not { } target || arena.GravenImageAt(index++, Statues.PulseWave) is not { } image) continue;
            tethers.Add(world.Tether(image, target, GravenTether));
            pulseWaves.Add((image, target));
        }
    }

    private void PulseWave()
    {
        foreach (var tether in tethers) tether.Despawn();
        tethers.Clear();
        foreach (var (image, target) in pulseWaves)
            if (target.IsAlive()) image.Cast(DmuActions.PulseWave, target);
        pulseWaves.Clear();
    }

    private readonly List<Vector3> towerSpots = [];

    private void WaveCannon()
    {
        towerSpots.Clear();
        var index = 0;
        foreach (var role in state.WaveCannonTargets)
            if (Alive(role) is { } target && arena.GravenImageAt(index++, Statues.WaveCannon) is { } image)
                image.Cast(DmuActions.WaveCannon, target);
        world.Events.Add(0.6f, () =>
        {
            foreach (var role in state.WaveCannonTargets)
                if (Alive(role) is { } target) towerSpots.Add(target.Position with { Y = 0f });
        });
    }

    private void Towers()
    {
        foreach (var spot in towerSpots)
            arena.Helper(spot, 0f)?.Cast(DmuActions.Explosion(ctx =>
            {
                if (ctx.Hits.Count > 0) return;
                DiagnosticLog.Info($"[DmuGravenImage1] Tower at ({spot.X:F1},{spot.Z:F1}) unsoaked.");
                world.Party.WipeAllPlayers(ActionId.UnmitigatedExplosion, "a Wave Cannon tower went unsoaked");
            }));
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new DmuGravenImage1AiReplayStateMessage(s.EncodeFlags(), s.EncodeRoles()) : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not DmuGravenImage1AiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (DmuGravenImage1State.FromNetworkReplay(msg.Flags, msg.Roles) is not { } shadow) return null;
        ((IScenarioAi<DmuGravenImage1State>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }

    public void RefreshLiveHandles(object shadowState, IReadOnlyDictionary<int, SimEnemy> peerEnemies)
    {
        if (shadowState is DmuGravenImage1State shadow)
            shadow.Objects.Kefka ??= peerEnemies.Values.FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.Kefka);
    }
}
