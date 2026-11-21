using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dmu.Trine;

public sealed class DmuTrineAi : IScenarioAi<DmuTrineState>
{
    public string Name => "Yan Flash";
    public string Group => "Mana";

    private const float TankCounterClockwiseFrom = 315f;
    private const float BurstClearance = 6.6f;
    private const float BusterClearance = 8.5f;
    private const float OffTankRadius = 18.5f;
    private const float MaxMainTankTurn = 60f;
    private const float ArenaReach = 19f;

    private DmuTrineState state = null!;

    public void Run(DmuTrineState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        ai.Move(0.2f, NorthTankSouthParty);
        ai.Move(9.4f, WingsSafeHalfBesideKefka, arrivalTime: 11.5f);
        ai.Move(18.95f, FirstTrineSpots, arrivalTime: 22.3f);
        ai.Move(24.0f, OffTankJoinsMainTankForEmbrace, arrivalTime: 26.8f);
    }

    private static IAiMove NorthTankSouthParty() => AiMove.ByRole(
        (MainTank, new(0f, -6.5f)), (OffTank, new(-1.5f, 2.5f)),
        (RegenHealer, new(-1f, 3.5f)), (ShieldHealer, new(1f, 3.5f)),
        (MeleeDpsA, new(-1.5f, 2f)), (MeleeDpsB, new(1.5f, 2f)),
        (PhysRangedDps, new(-0.5f, 4f)), (CasterDps, new(0.5f, 4f)));

    private IAiMove WingsSafeHalfBesideKefka()
    {
        var east = state.WingsCleaveLeft ? 1f : -1f;
        return AiMove.ByRole(
            (MainTank, new(east * 2f, -2.4f)), (OffTank, new(east * 2.6f, -1f)),
            (RegenHealer, new(east * 1.6f, 0.6f)), (ShieldHealer, new(east * 2.6f, 0.6f)),
            (MeleeDpsA, new(east * 1.6f, -0.6f)), (MeleeDpsB, new(east * 2.6f, -0.2f)),
            (PhysRangedDps, new(east * 1.6f, 1.8f)), (CasterDps, new(east * 2.6f, 1.8f)));
    }

    private TrineTriangle PartyTriangle() => state.FirstWaveTriangles.MinBy(t => DmuTrineLayout.Bearing(t.Centre))!;

    private TrineTriangle TankTriangle()
    {
        var party = PartyTriangle();
        return state.FirstWaveTriangles.Where(t => t != party)
            .MinBy(t => (TankCounterClockwiseFrom - DmuTrineLayout.Bearing(t.Centre) + 360f) % 360f)!;
    }

    private IReadOnlyList<Vector2> LaterBursts()
        => [.. state.SecondWaveTriangle.Corners, .. state.ThirdWaveTriangles.SelectMany(t => t.Corners)];

    private IAiMove FirstTrineSpots()
    {
        var party = PartyTriangle().Centre;
        var tankDirection = Vector2.Normalize(TankTriangle().Centre);
        var offTank = tankDirection * OffTankRadius;
        var mainTank = ClosestSafeToKefka(tankDirection, party, offTank);
        return AiMove.ByRole(PerRole.All.Select(role => (role, role switch
        {
            MainTank => mainTank,
            OffTank => offTank,
            _ => party,
        })).ToArray());
    }

    private Vector2 ClosestSafeToKefka(Vector2 direction, Vector2 party, Vector2 offTank)
    {
        var bursts = LaterBursts();
        var heading = MathF.Atan2(direction.X, direction.Y);
        for (var radius = 4f; radius <= ArenaReach; radius += 0.25f)
        {
            var best = (Spot: (Vector2?)null, Turn: float.MaxValue);
            for (var turn = -MaxMainTankTurn; turn <= MaxMainTankTurn; turn += 2.5f)
            {
                var angle = heading + turn * MathF.PI / 180f;
                var spot = new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * radius;
                if (bursts.Any(b => Vector2.Distance(b, spot) < BurstClearance)) continue;
                if (Vector2.Distance(spot, party) < BusterClearance || Vector2.Distance(spot, offTank) < BusterClearance) continue;
                if (MathF.Abs(turn) < best.Turn) best = (spot, MathF.Abs(turn));
            }
            if (best.Spot is { } found) return found;
        }
        return direction * 10f;
    }

    private IAiMove OffTankJoinsMainTankForEmbrace()
    {
        var party = PartyTriangle().Centre;
        var mainTank = ClosestSafeToKefka(Vector2.Normalize(TankTriangle().Centre), party, Vector2.Normalize(TankTriangle().Centre) * OffTankRadius);
        return AiMove.ByRole((OffTank, mainTank + Vector2.Normalize(mainTank) * 1f));
    }
}
