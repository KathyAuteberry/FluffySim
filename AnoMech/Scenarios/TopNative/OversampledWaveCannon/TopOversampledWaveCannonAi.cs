using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.TopNative.OversampledWaveCannon.TopOversampledWaveCannonScenario;

namespace AnoMech.Scenarios.TopNative.OversampledWaveCannon;

public sealed class TopOversampledWaveCannonAi : IScenarioAi<TopOversampledWaveCannonState>
{
    public string Name => "Elemental (Astoh)";
    public string Group => "Elemental";

    public static readonly PartyRole[] NorthToSouth =
    [
        PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MainTank, PartyRole.OffTank,
        PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps,
    ];

    private static readonly Vector2[] OthersWhenOmegaCleavesWest = [new(1f, -16f), new(-1.5f, 0f), new(-15f, 0f), new(1f, 9.5f), new(1f, 17.5f)];
    private static readonly Vector2[] MonitorsWhenOmegaCleavesWest = [new(10f, -12f), new(11f, -3f), new(11f, 6f)];
    private static readonly Vector2[] FireWhenOmegaCleavesWest = [new(0.9f, 0.1f), new(0.2f, -0.98f), new(0f, 1f)];

    private TopOversampledWaveCannonState state = null!;

    public void Run(TopOversampledWaveCannonState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        ai.Move(0.5f, LineUpNorthToSouth, jitter: 0f);
        ai.Move(MonitorsAt + 0.6f, AstohSpots, jitter: 0f);
        foreach (var at in new[] { WaveCannonCast + 5.5f, WaveCannonCast + 8.5f })
            world.Events.Add(at, () => FaceMonitorsAtTheirTargets(world));
    }

    private float Mirror => state.OmegaCleavesEast ? -1f : 1f;

    private static IAiMove LineUpNorthToSouth()
    {
        var coords = new Vector2?[8];
        for (var i = 0; i < NorthToSouth.Length; i++) coords[(int)NorthToSouth[i]] = new Vector2(0f, -17.5f + 5f * i);
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove AstohSpots()
    {
        var coords = new Vector2?[8];
        var others = NorthToSouth.Where(r => !state.Monitors.Contains(r)).ToArray();
        var monitors = NorthToSouth.Where(state.Monitors.Contains).ToArray();
        for (var i = 0; i < others.Length; i++) coords[(int)others[i]] = Mirrored(OthersWhenOmegaCleavesWest[i]);
        for (var i = 0; i < monitors.Length; i++) coords[(int)monitors[i]] = Mirrored(MonitorsWhenOmegaCleavesWest[i]);
        return AiMove.Create(coords).NaturalOrder();
    }

    private Vector2 Mirrored(Vector2 v) => v with { X = v.X * Mirror };

    private void FaceMonitorsAtTheirTargets(SimWorld world)
    {
        var monitors = NorthToSouth.Where(state.Monitors.Contains).ToArray();
        for (var i = 0; i < monitors.Length; i++)
        {
            if (world.Party.Get(monitors[i]) is not { } member || !world.Party.IsBotDriven(member)) continue;
            var fire = Mirrored(FireWhenOmegaCleavesWest[i]);
            var right = state.MonitorRight[monitors[i]] ? fire : -fire;
            member.TurnTo(MathF.Atan2(right.Y, -right.X));
        }
    }
}
