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

namespace AnoMech.Scenarios.TopNative.BlueScreen;

public sealed class TopBlueScreenScenario : IMultiplayerReplayable
{
    public const float WaveCannonCast = 11.35f;
    public static readonly float[] MarkersAt = [13.86f, 23.98f, 34.20f];
    public static readonly float[] LinesAt = [16.40f, 26.57f, 36.78f];
    public const float ProteansAfterLines = 0.54f;
    public static readonly float[] StacksAt = [21.86f, 32.00f, 42.23f];
    public static readonly float[] WaveRepeaterCasts = [19.36f, 36.78f];
    // From the cast; the centre circle resolves with the cast itself.
    public static readonly float[] RingOffsets = [7.07f, 9.12f, 11.18f];
    public const float BlueScreenCast = 49.03f;

    public string Name => "Blue Screen";
    public IPhase Phase => TopNativeZone.P4;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new TopBlueScreenAi()];
    public void DrawSettings() => settings.Draw();
    public bool HasPerPlayerSettings => true;
    public void DrawPerPlayerSettings() => settings.DrawPerPlayer();
    public object SettingsOverrides => settings.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settings.Overrides.Validate().Problems;
    public TopBlueScreenState? LastState { get; private set; }

    private readonly TopBlueScreenSettingsWindow settings = new();
    private SimWorld world = null!;
    private TopBlueScreenState state = null!;
    private SimEnemy? omega;
    private readonly List<SimEnemy?> lineHelpers = [];
    private readonly List<SimEnemy?> proteanHelpers = [];
    private readonly List<SimEnemy?> stackHelpers = [];
    private readonly List<SimEnemy?> repeaterHelpers = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = LastState = new TopBlueScreenState(world.Rng, world.Party, settings.Overrides);
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<TopBlueScreenState>)AiStrats[idx]).Run(state, world);

        SpawnActors();
        world.Events.Add(WaveCannonCast, () => omega?.Cast(ActionId.WaveCannonP4, animationLock: 0.6f));
        for (var wave = 0; wave < TopBlueScreenState.Waves; wave++)
        {
            Markers(wave);
            Lines(wave);
            Proteans(wave);
            Stacks(wave);
        }
        for (var i = 0; i < WaveRepeaterCasts.Length; i++) WaveRepeater(i);
        world.Events.Add(BlueScreenCast, () => omega?.Cast(ActionId.BlueScreen, animationLock: 0.6f));
    }

    public void Tick(float delta, float elapsed) { }

    private void SpawnActors()
    {
        omega = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaP3, NameId: BNpcNameId.OmegaP3, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.OmegaP3,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        lineHelpers.Clear();
        proteanHelpers.Clear();
        stackHelpers.Clear();
        repeaterHelpers.Clear();
        for (var i = 0; i < 8; i++) lineHelpers.Add(SpawnHelper());
        for (var i = 0; i < 8; i++) proteanHelpers.Add(SpawnHelper());
        for (var i = 0; i < 2; i++) stackHelpers.Add(SpawnHelper());
        for (var i = 0; i < 2; i++) repeaterHelpers.Add(SpawnHelper());
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.OmegaP3,
        Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private List<SimCharacter> Alive() => world.Party.ActiveMembers().Where(m => m.IsAlive()).Take(8).ToList();

    private void Markers(int wave) => world.Events.Add(MarkersAt[wave], () =>
    {
        var markers = state.Markers[wave];
        for (var i = 0; i < markers.Count; i++)
            if (world.Party.Get(markers[i]) is { } member && member.IsAlive())
                stackHelpers[i]?.Cast(Actions.WaveCannonP4StackMarker, member);
    });

    // Each line keeps the bearing its player had when it was cast and fires 5.3 s later.
    private void Lines(int wave) => world.Events.Add(LinesAt[wave], () =>
    {
        var members = Alive();
        for (var i = 0; i < members.Count; i++)
        {
            lineHelpers[i]?.SetPosition(new Placement(Vector3.Zero, 0f).Face(members[i].Position));
            lineHelpers[i]?.Cast(Actions.WaveCannonP4Line);
        }
    });

    private void Proteans(int wave) => world.Events.Add(LinesAt[wave] + ProteansAfterLines, () =>
    {
        var members = Alive();
        for (var i = 0; i < members.Count; i++)
        {
            proteanHelpers[i]?.SetPosition(Vector3.Zero);
            proteanHelpers[i]?.Cast(Actions.WaveCannonP4Protean, members[i]);
        }
    });

    private void Stacks(int wave) => world.Events.Add(StacksAt[wave], () =>
    {
        var markers = state.Markers[wave];
        for (var i = 0; i < markers.Count; i++)
        {
            if (world.Party.Get(markers[i]) is not { } member || !member.IsAlive()) continue;
            stackHelpers[i]?.SetPosition(new Placement(Vector3.Zero, 0f).Face(member.Position));
            stackHelpers[i]?.Cast(Actions.WaveCannonP4Stack, member);
        }
    });

    private void WaveRepeater(int index)
    {
        var helper = repeaterHelpers[index];
        var at = WaveRepeaterCasts[index];
        world.Events.Add(at, () => helper?.Cast(Actions.WaveRepeater1, Vector3.Zero));
        world.Events.Add(at + RingOffsets[0], () => helper?.Cast(Actions.WaveRepeater2));
        world.Events.Add(at + RingOffsets[1], () => helper?.Cast(Actions.WaveRepeater3));
        world.Events.Add(at + RingOffsets[2], () => helper?.Cast(Actions.WaveRepeater4));
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new TopBlueScreenAiReplayStateMessage(s.EncodeMarkers()) : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not TopBlueScreenAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        var shadow = TopBlueScreenState.FromNetworkReplay(msg.Markers);
        ((IScenarioAi<TopBlueScreenState>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }
}
