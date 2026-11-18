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

namespace AnoMech.Scenarios.TopNative.Pantokrator;

public sealed class TopPantokratorScenario : IMultiplayerReplayable
{
    public const float PantokratorCast = 4f;
    public const float NumbersAt = PantokratorCast + 4.96f;
    public const float FirstFlameCast = PantokratorCast + 7.11f;
    public const float FlameInterval = 2.0025f;
    public const int FlamePairs = 14;
    public const float FirstPuddle = PantokratorCast + 14.63f;
    public const float PuddleInterval = 2.002f;
    public const int PuddleWaves = 10;
    public const float FirstResolve = PantokratorCast + 17.09f;
    public const float ResolveInterval = 5.99f;
    public const float FirstWaveCannonMarkers = PantokratorCast + 38.69f;
    public const float FirstWaveCannon = PantokratorCast + 43.78f;
    public const float SecondWaveCannonMarkers = PantokratorCast + 47.72f;
    public const float SecondWaveCannon = PantokratorCast + 52.82f;
    public static readonly float[] DiffuseTimes = [43.78f, 45.86f, 47.94f, 50.02f, 52.11f];
    private static readonly float[] MissileDurations = [12f, 18f, 24f, 30f];
    private static readonly float[] BeamDurations = [24f, 30f, 12f, 18f];
    // The beam targets of each resolve: the players whose Condensed Wave Cannon runs out.
    public static readonly int[] BeamNumbers = [3, 4, 1, 2];

    public string Name => "Pantokrator";
    public IPhase Phase => TopNativeZone.P1;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new TopPantokratorAi()];
    public void DrawSettings() => settings.Draw();
    public bool HasPerPlayerSettings => true;
    public void DrawPerPlayerSettings() => settings.DrawPerPlayer();
    public object SettingsOverrides => settings.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settings.Overrides.Validate().Problems;
    public TopPantokratorState? LastState { get; private set; }

    private readonly TopPantokratorSettingsWindow settings = new();
    private SimWorld world = null!;
    private TopPantokratorState state = null!;
    private SimEnemy? omega;
    private readonly List<SimEnemy?> flameHelpers = [];
    private readonly List<SimEnemy?> puddleHelpers = [];
    private readonly List<SimEnemy?> resolveHelpers = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = LastState = new TopPantokratorState(world.Rng, world.Party, settings.Overrides);
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<TopPantokratorState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.Map.AddEffect(MapEffect.Show, MapEffect.P1Floor));
        SpawnActors();
        world.Events.Add(PantokratorCast, () => omega?.Cast(ActionId.Pantokrator, animationLock: 0.6f));
        world.Events.Add(NumbersAt, DealNumbers);
        for (var k = 0; k < FlamePairs; k++) Flames(k);
        for (var i = 0; i < PuddleWaves; i++) Puddles(i);
        for (var r = 0; r < 4; r++) Resolve(r);
        WaveCannons();
        world.Events.Add(PantokratorCast + 57.84f, () => omega?.SetTargetable(false));
    }

    public void Tick(float delta, float elapsed) { }

    private void SpawnActors()
    {
        omega = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaP1, NameId: BNpcNameId.Omega, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.OmegaP1,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        flameHelpers.Clear();
        puddleHelpers.Clear();
        resolveHelpers.Clear();
        for (var i = 0; i < 6; i++) flameHelpers.Add(SpawnHelper());
        for (var i = 0; i < 16; i++) puddleHelpers.Add(SpawnHelper());
        for (var i = 0; i < 8; i++) resolveHelpers.Add(SpawnHelper());
        for (var t = 1.0f; t < PantokratorCast + 57f; t += 3.04f)
            world.Events.Add(t, () => omega?.Cast(Actions.AutoAttackP1, world.Party.Get(PartyRole.MainTank)));
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.Omega,
        Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private void DealNumbers()
    {
        foreach (var (role, number) in state.Numbers.ByRole)
        {
            if (world.Party.Get(role) is not { } member) continue;
            member.AddStatus(StatusId.InLine[number - 1]);
            member.AddStatus(StatusId.GuidedMissileIncoming[number - 1], MissileDurations[number - 1]);
            member.AddStatus(StatusId.CondensedWaveCannon[number - 1], BeamDurations[number - 1]);
        }
    }

    private void Flames(int k)
    {
        var at = FirstFlameCast + FlameInterval * k;
        var action = k == 0 ? Actions.FlameThrowerFirst : Actions.FlameThrowerRest;
        for (var side = 0; side < 2; side++)
        {
            var helper = flameHelpers[(k % 3) * 2 + side];
            var bearing = state.FlameBearing(k) + side * 180f;
            world.Events.Add(at - 0.05f, () => helper?.SetPosition(new Placement(Vector3.Zero, FacingBearing(bearing))));
            world.Events.Add(at, () => helper?.Cast(action));
        }
    }

    private void Puddles(int wave)
    {
        var at = FirstPuddle + PuddleInterval * wave;
        world.Events.Add(at, () =>
        {
            var members = world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList();
            for (var i = 0; i < members.Count && i < 8; i++)
                puddleHelpers[(wave % 2) * 8 + i]?.Cast(Actions.BallisticImpact, members[i].Position);
        });
    }

    private void Resolve(int round)
    {
        var at = FirstResolve + ResolveInterval * round;
        var missile = round + 1;
        world.Events.Add(at, () =>
        {
            var helper = 0;
            foreach (var (role, number) in state.Numbers.ByRole.OrderBy(kv => kv.Key))
            {
                if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
                if (number == missile)
                {
                    resolveHelpers[helper]?.SetPosition(member.Position);
                    resolveHelpers[helper++]?.Cast(Actions.GuidedMissileKyrios, member);
                    member.RemoveStatus(StatusId.InLine[number - 1]);
                }
                else if (number == BeamNumbers[round])
                {
                    resolveHelpers[helper]?.SetPosition(new Placement(Vector3.Zero, 0f).Face(member.Position));
                    resolveHelpers[helper++]?.Cast(Actions.CondensedWaveCannonKyrios, member);
                }
            }
        });
    }

    private void WaveCannons()
    {
        Markers(FirstWaveCannonMarkers, state.FirstWaveCannon);
        Lines(FirstWaveCannon, state.FirstWaveCannon);
        Markers(SecondWaveCannonMarkers, state.SecondWaveCannon);
        Lines(SecondWaveCannon, state.SecondWaveCannon);
        foreach (var t in DiffuseTimes)
            world.Events.Add(PantokratorCast + t, () =>
            {
                if (omega == null) return;
                var farthest = world.Party.Find.FarestN(omega.Position, 2);
                for (var i = 0; i < farthest.Count; i++)
                {
                    resolveHelpers[6 + i]?.SetPosition(new Placement(Vector3.Zero, 0f).Face(farthest[i].Position));
                    resolveHelpers[6 + i]?.Cast(Actions.DiffuseWaveCannonKyrios, farthest[i]);
                }
            });
    }

    private void Markers(float at, IReadOnlyList<PartyRole> roles)
        => world.Events.Add(at, () => { foreach (var role in roles) world.Party.Get(role)?.AttachLockonVfx(LockonId.WaveCannonKyrios); });

    private void Lines(float at, IReadOnlyList<PartyRole> roles)
        => world.Events.Add(at, () =>
        {
            for (var i = 0; i < roles.Count; i++)
            {
                if (world.Party.Get(roles[i]) is not { } member || !member.IsAlive()) continue;
                resolveHelpers[i]?.SetPosition(new Placement(Vector3.Zero, 0f).Face(member.Position));
                resolveHelpers[i]?.Cast(Actions.WaveCannonKyrios, member);
            }
        });

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s
            ? new TopPantokratorAiReplayStateMessage(new Dictionary<PartyRole, int>(s.Numbers.ByRole), s.FirstFlameBearing, s.Rotation, s.FirstWaveCannon.ToArray())
            : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not TopPantokratorAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        var shadow = TopPantokratorState.FromNetworkReplay(msg.Numbers, msg.FirstFlameBearing, msg.Rotation, msg.FirstWaveCannon);
        ((IScenarioAi<TopPantokratorState>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }
}
