using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;
using Actions = AnoMech.Scenarios.TopNative.TopNativeActions;

namespace AnoMech.Scenarios.TopNative.Transition;

public sealed class TopTransitionScenario : IMultiplayerReplayable
{
    public const float HelloWorldCast = 35f;
    public const float DebuffsAt = HelloWorldCast - 27.3f;
    public const float DebuffDuration = 19f;
    public static readonly float[] WaveRepeaterCasts = [HelloWorldCast - 27.23f, HelloWorldCast - 19.09f];
    public static readonly float[] RingOffsets = [7.07f, 9.12f, 11.18f];
    public static readonly float[] ArmsCasts = [HelloWorldCast - 12.16f, HelloWorldCast - 9.66f];
    public static readonly float[] ArmsReveal = [HelloWorldCast - 24.19f, HelloWorldCast - 21.19f];
    public const float ArmsRadius = 14f;

    public string Name => "Transition";
    public IPhase Phase => TopNativeZone.P3;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new TopTransitionAi()];
    public bool HasPerPlayerSettings => true;
    public void DrawPerPlayerSettings() => settings.DrawPerPlayer();
    public void DrawSettings() => settings.Draw();
    public object SettingsOverrides => settings.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settings.Overrides.Validate().Problems;
    public TopTransitionState? LastState { get; private set; }

    private readonly TopTransitionSettingsWindow settings = new();
    private SimWorld world = null!;
    private TopTransitionState state = null!;
    private SimEnemy? omega;
    private readonly List<SimEnemy?> helpers = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = LastState = new TopTransitionState(world.Rng, world.Party, settings.Overrides);
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<TopTransitionState>)AiStrats[idx]).Run(state, world);

        omega = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaP3, NameId: BNpcNameId.OmegaP3, Level: Level,
            Targetable: false, EnemyList: EnemyListMode.OnlyWhenVisible, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.OmegaP3,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        helpers.Clear();
        for (var i = 0; i < 10; i++) helpers.Add(SpawnHelper());
        world.Events.Add(HelloWorldCast - 27.32f, () => world.Map.AddEffect(0x00200010, 1));
        world.Events.Add(HelloWorldCast - 18.24f, () => world.Map.AddEffect(MapEffect.Hide, MapEffect.P2Floor));
        world.Events.Add(HelloWorldCast - 18.24f, () => world.Map.AddEffect(0x00020004, 11));
        world.Events.Add(HelloWorldCast - 16.23f, () => world.Map.AddEffect(0x04000004, 1));
        world.Events.Add(DebuffsAt, DealDebuffs);
        for (var wave = 0; wave < 2; wave++) WaveRepeater(wave);
        for (var set = 0; set < 2; set++) Arms(set);
        world.Events.Add(DebuffsAt + DebuffDuration + 0.07f, ResolveDebuffs);
        world.Events.Add(HelloWorldCast - 4.16f, () => omega?.SetTargetable(true));
        world.Events.Add(HelloWorldCast, () => omega?.Cast(ActionId.HelloWorld, animationLock: 0.6f));
    }

    public void Tick(float delta, float elapsed) { }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.OmegaP3,
        Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private void DealDebuffs()
    {
        foreach (var (role, debuff) in state.Debuffs)
        {
            if (debuff == SniperDebuff.None) continue;
            world.Party.Get(role)?.AddStatus(debuff == SniperDebuff.Sniper ? StatusId.SniperCannonFodder : StatusId.HighPoweredSniperCannonFodder, DebuffDuration);
        }
    }

    private void WaveRepeater(int wave)
    {
        var helper = helpers[wave];
        var at = WaveRepeaterCasts[wave];
        world.Events.Add(at, () => helper?.Cast(Actions.WaveRepeater1, Vector3.Zero));
        EnemyActionRing(at + RingOffsets[0], helper, Actions.WaveRepeater2);
        EnemyActionRing(at + RingOffsets[1], helper, Actions.WaveRepeater3);
        EnemyActionRing(at + RingOffsets[2], helper, Actions.WaveRepeater4);
    }

    private void EnemyActionRing(float at, SimEnemy? helper, Core.EnemyActions.EnemyAction ring)
        => world.Events.Add(at, () => helper?.Cast(ring));

    private void Arms(int set)
    {
        for (var i = 0; i < 3; i++)
        {
            var bearing = set == 0 ? state.FirstArmsBearing(i) : state.SecondArmsBearing(i);
            var position = Polar(bearing, ArmsRadius);
            var left = (i + set) % 2 == 0;
            SimEnemy? arm = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: left ? BNpcBaseId.LeftArmUnit : BNpcBaseId.RightArmUnit,
                NameId: left ? BNpcNameId.LeftArmUnit : BNpcNameId.RightArmUnit, Level: Level, Targetable: false,
                EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.HiddenUntilShown,
                LayoutId: left ? LayoutId.LeftArmUnit : LayoutId.RightArmUnit, Placement: new Placement(position, 0f).Face(Vector3.Zero)));
            world.Events.Add(ArmsReveal[set], () => arm?.PlayActionTimeline(ActionTimelineId.WarpEnd));
            world.Events.Add(ArmsCasts[set], () => arm?.Cast(Actions.ColossalBlow));
            world.Events.Add(ArmsCasts[set] + 2.5f, () => arm?.PlayActionTimeline(ActionTimelineId.WarpStart));
            world.Events.Add(ArmsCasts[set] + 4.7f, () => arm?.Despawn());
        }
    }

    private void ResolveDebuffs()
    {
        var i = 2;
        foreach (var (role, debuff) in state.Debuffs.OrderBy(kv => kv.Key))
        {
            if (debuff == SniperDebuff.None || world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            var helper = helpers[i++];
            helper?.SetPosition(member.Position);
            helper?.Cast(debuff == SniperDebuff.Sniper ? Actions.SniperCannon : Actions.HighPoweredSniperCannon, member);
        }
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new TopTransitionAiReplayStateMessage(new Dictionary<PartyRole, SniperDebuff>(s.Debuffs), s.FirstArmsNorth) : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not TopTransitionAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        var shadow = TopTransitionState.FromNetworkReplay(msg.Debuffs, msg.FirstArmsNorth);
        ((IScenarioAi<TopTransitionState>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }
}
