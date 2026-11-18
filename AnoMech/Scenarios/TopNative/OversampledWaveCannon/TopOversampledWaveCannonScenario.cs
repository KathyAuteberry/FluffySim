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

namespace AnoMech.Scenarios.TopNative.OversampledWaveCannon;

public sealed class TopOversampledWaveCannonScenario : IMultiplayerReplayable
{
    public const float MonitorsAt = 7.91f;
    public const float WaveCannonCast = 8f;
    // From the cast, as retail fires it; the bar ends 0.3s earlier.
    public const float FiresAfterCast = 10.07f;
    public const float IonEffluxCast = WaveCannonCast + 16.10f;

    public string Name => "Oversampled Wave Cannon";
    public IPhase Phase => TopNativeZone.P3;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new TopOversampledWaveCannonAi()];
    public void DrawSettings() => settings.Draw();
    public bool HasPerPlayerSettings => true;
    public void DrawPerPlayerSettings() => settings.DrawPerPlayer();
    public object SettingsOverrides => settings.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settings.Overrides.Validate().Problems;
    public TopOversampledWaveCannonState? LastState { get; private set; }

    private readonly TopOversampledWaveCannonSettingsWindow settings = new();
    private SimWorld world = null!;
    private TopOversampledWaveCannonState state = null!;
    private SimEnemy? omega;
    private readonly List<SimEnemy?> helpers = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = LastState = new TopOversampledWaveCannonState(world.Rng, world.Party, settings.Overrides);
        helpers.Clear();
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<TopOversampledWaveCannonState>)AiStrats[idx]).Run(state, world);

        omega = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaP3, NameId: BNpcNameId.OmegaP3, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.OmegaP3,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        for (var i = 0; i < 8; i++) helpers.Add(SpawnHelper());
        world.Events.Add(MonitorsAt, () =>
        {
            foreach (var role in state.Monitors)
                world.Party.Get(role)?.AddStatus(state.MonitorRight[role] ? StatusId.MonitorRight : StatusId.MonitorLeft);
        });
        world.Events.Add(WaveCannonCast, () => omega?.Cast(state.OmegaCleavesEast ? ActionId.OversampledWaveCannonR : ActionId.OversampledWaveCannonL, animationLock: 0.6f));
        world.Events.Add(WaveCannonCast + FiresAfterCast, Fire);
        world.Events.Add(IonEffluxCast, () => omega?.Cast(ActionId.IonEfflux, animationLock: 0.6f));
    }

    public void Tick(float delta, float elapsed) { }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.OmegaP3,
        Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    // Each monitor takes two players on its side; with fewer there, random others make up the two.
    private void Fire()
    {
        var targets = new List<SimCharacter>();
        targets.AddRange(world.Party.Find.OnSideN(world.Rng, new Placement(Vector3.Zero, MathF.PI), state.OmegaCleavesEast ? -1 : 1, 2));
        foreach (var role in state.Monitors)
        {
            if (world.Party.Get(role) is not { } monitor || !monitor.IsAlive()) continue;
            monitor.RemoveStatus(StatusId.MonitorRight);
            monitor.RemoveStatus(StatusId.MonitorLeft);
            targets.AddRange(world.Party.Find.OnSideN(world.Rng, monitor.Placement(), state.MonitorRight[role] ? -1 : 1, 2, monitor));
        }
        for (var i = 0; i < targets.Count; i++)
        {
            helpers[i % helpers.Count]?.SetPosition(targets[i].Position);
            helpers[i % helpers.Count]?.Cast(Actions.OversampledWaveCannonAoe, targets[i]);
        }
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new TopOversampledWaveCannonAiReplayStateMessage(s.OmegaCleavesEast, new Dictionary<PartyRole, bool>(s.MonitorRight)) : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not TopOversampledWaveCannonAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        var shadow = TopOversampledWaveCannonState.FromNetworkReplay(msg.OmegaCleavesEast, msg.MonitorRight);
        ((IScenarioAi<TopOversampledWaveCannonState>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }
}
