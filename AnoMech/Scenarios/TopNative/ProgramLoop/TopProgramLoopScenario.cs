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

namespace AnoMech.Scenarios.TopNative.ProgramLoop;

public sealed class TopProgramLoopScenario : IMultiplayerReplayable
{
    public const float ProgramLoopCast = 10.4f;
    public const float NumbersAt = ProgramLoopCast + 4.74f;
    public const float TethersAt = ProgramLoopCast + 10.06f;
    public static readonly float[] TowerSpawnTimes = [ProgramLoopCast + 7.94f, ProgramLoopCast + 17.08f, ProgramLoopCast + 25.96f, ProgramLoopCast + 34.90f];
    public static readonly float[] TowerTimes = [ProgramLoopCast + 18.16f, ProgramLoopCast + 27.19f, ProgramLoopCast + 36.17f, ProgramLoopCast + 45.16f];
    public const float BlasterAfterTower = 0.13f;
    private static readonly float[] LooperDurations = [16f, 25f, 34f, 43f];

    public string Name => "Program Loop";
    public IPhase Phase => TopNativeZone.P1;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new TopProgramLoopAi()];
    public void DrawSettings() => settings.Draw();
    public bool HasPerPlayerSettings => true;
    public void DrawPerPlayerSettings() => settings.DrawPerPlayer();
    public object SettingsOverrides => settings.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settings.Overrides.Validate().Problems;
    public TopProgramLoopState? LastState { get; private set; }

    private readonly TopProgramLoopSettingsWindow settings = new();
    private SimWorld world = null!;
    private TopProgramLoopState state = null!;
    private SimEnemy? omega;
    private readonly SimTether?[] tethers = new SimTether?[2];
    private readonly SimEnemy?[] towerHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] blasterHelpers = new SimEnemy?[2];
    private int tetherNumber;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = LastState = new TopProgramLoopState(world.Rng, world.Party, settings.Overrides);
        Array.Clear(tethers);
        tetherNumber = TetherNumber(0);
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<TopProgramLoopState>)AiStrats[idx]).Run(state, world);

        SpawnOmega();
        world.Events.Add(ProgramLoopCast, () => omega?.Cast(ActionId.ProgramLoop, animationLock: 0.6f));
        world.Events.Add(NumbersAt, DealNumbers);
        world.Events.Add(ProgramLoopCast + 5.9f, () => world.Map.AddEffect(MapEffect.Show, MapEffect.P1Floor));
        for (var k = 0; k < 4; k++) Towers(k);
        world.Events.Add(TethersAt, SpawnTethers);
        world.Events.Add(ProgramLoopCast + 10.15f, () => omega?.Cast(ActionId.Blaster, animationLock: 0.2f));
        for (var k = 0; k < 4; k++) Blaster(k);
        world.Events.Add(TowerTimes[3] + 0.5f, () => { foreach (var t in tethers) t?.Despawn(); });
    }

    public void Tick(float delta, float elapsed) { }

    private void SpawnOmega()
    {
        omega = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaP1, NameId: BNpcNameId.Omega, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.OmegaP1,
            Placement: new Placement(Vector3.Zero, 0f)));
        for (var i = 0; i < 2; i++)
        {
            towerHelpers[i] = SpawnHelper();
            blasterHelpers[i] = SpawnHelper();
        }
        for (var t = 1.0f; t < TowerTimes[3] + 2f; t += 3.04f)
            world.Events.Add(t, () => omega?.Cast(Actions.AutoAttackP1, world.Party.Get(PartyRole.MainTank)));
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.Omega,
        Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(new Vector3(0f, 0f, -10f), 0f)));

    private void DealNumbers()
    {
        foreach (var (role, number) in state.Numbers.ByRole)
        {
            if (world.Party.Get(role) is not { } member) continue;
            member.AddStatus(StatusId.Looper, LooperDurations[number - 1]);
            member.AddStatus(StatusId.InLine[number - 1]);
        }
    }

    private void Towers(int set)
    {
        var spots = state.TowerSets[set];
        for (var i = 0; i < 2; i++)
        {
            var spot = spots[i];
            var position = Polar(TopProgramLoopState.SpotBearings[spot], TopProgramLoopState.SpotRadius);
            var index = (uint)(set * 2 + i);
            SimEventObject? timer = null;
            SimTower? tower = null;
            var helper = towerHelpers[i];
            world.Events.Add(TowerSpawnTimes[set], () =>
            {
                timer = world.SpawnEventObject(TowerConfig(EObjId.TowerTimer, position, spot, 0x40 + index) with { TimelineState = 2 });
                tower = world.SpawnTower(TowerConfig(EObjId.TowerSolo, position, spot, 0x48 + index), [0x8, 0x10], EObjId.TowerStateUnk, 3f);
            });
            world.Events.Add(TowerTimes[set] - 0.1f, () => helper?.SetPosition(position));
            world.Events.Add(TowerTimes[set], () => helper?.Cast(Actions.StorageViolation));
            world.Events.Add(TowerTimes[set] + 0.05f, () => { timer?.Despawn(); tower?.Despawn(); });
        }
    }

    private static EventObjectSpawnConfig TowerConfig(uint eObjId, Vector3 position, int spot, uint arg) => new()
    {
        EObjId = eObjId,
        Placement = new Placement(position, 0f),
        LayoutId = TopProgramLoopState.SpotLayoutIds[spot],
        EventId = EObjId.TowerEventId,
        TargetableStatus = 5,
        Arg2 = arg << 16 | 3,
    };

    // The 3s stand in front to take it; after each explosion the tethers stay on their holders
    // until someone steps between them and Omega.
    private void SpawnTethers()
    {
        if (omega == null) return;
        var closest = world.Party.ActiveMembers().Where(m => m.IsAlive() && MayTakeTether(m))
            .OrderBy(m => Vector3.DistanceSquared(m.Position, omega.Position)).Take(2).ToList();
        for (var i = 0; i < 2; i++)
            tethers[i] = world.Tether(new TopProgramLoopTetherEnd(closest.Count > i ? closest[i] : null, MayTakeTether), omega, TetherId.Blaster);
    }

    public static int TetherNumber(int set) => (set + 2) % 4 + 1;

    private bool MayTakeTether(SimCharacter member)
        => !world.Party.IsBotDriven(member) || member is ISimPartyMember m && state.Numbers[m.Role] == tetherNumber;

    private void Blaster(int set)
    {
        var at = TowerTimes[set] + BlasterAfterTower;
        if (set > 0)
            world.Events.Add(TowerTimes[set] + 0.07f, () => omega?.Cast(set == 3 ? ActionId.BlasterLast : ActionId.BlasterRepeat, animationLock: 0.2f));
        for (var i = 0; i < 2; i++)
        {
            var index = i;
            world.Events.Add(at, () =>
            {
                if (tethers[index]?.A is not { } holder || !holder.IsAlive()) return;
                blasterHelpers[index]?.SetPosition(omega?.Position ?? Vector3.Zero);
                blasterHelpers[index]?.Cast(Actions.BlasterAoe, holder);
                if (set < 3) tetherNumber = TetherNumber(set + 1);
            });
        }
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new TopProgramLoopAiReplayStateMessage(new Dictionary<PartyRole, int>(s.Numbers.ByRole), s.EncodeTowers()) : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not TopProgramLoopAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        var shadow = TopProgramLoopState.FromNetworkReplay(msg.Numbers, msg.Towers);
        ((IScenarioAi<TopProgramLoopState>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }
}
