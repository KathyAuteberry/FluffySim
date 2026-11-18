using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.TopNative.LimitlessSynergy.TopLimitlessSynergyScenario;

namespace AnoMech.Scenarios.TopNative.LimitlessSynergy;

public sealed class TopLimitlessSynergyAi : IScenarioAi<TopLimitlessSynergyState>
{
    public string Name => "Elemental";
    public string Group => "Elemental";

    private static readonly Vector2 TrueSouthStack = new(0f, 6.5f);
    private const float ArrowClearance = 6.2f;
    private const float TetherLead = 56f;

    private TopLimitlessSynergyState state = null!;
    private SimWorld world = null!;
    private AiManager ai = null!;

    public void Run(TopLimitlessSynergyState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        ai = new AiManager(world);
        ai.Move(0.5f, () => AiMove.All(TrueSouthStack), jitter: 0.3f);
        TanksTakeTethersClockwise(PartyRole.MainTank, state.BladedanceNorthBearing);
        TanksTakeTethersClockwise(PartyRole.OffTank, state.BladedanceSouthBearing);
        ai.Move(SagittariusCast + 1.0f, () => NonTanks(ClearOfArrow()), jitter: 0.2f);
        ai.Move(BladedanceHit + 0.6f, TanksTowardShieldPartyInMiddle, jitter: 0.2f);
        world.Events.Add(BeyondDefenseJump + 0.15f, JumpedTankLeavesOtherTankStacks);
    }

    private static Vector2 Polar(float bearing, float radius)
    {
        var p = TopNativeConstants.Polar(bearing, radius);
        return new Vector2(p.X, p.Z);
    }

    private void TanksTakeTethersClockwise(PartyRole tank, float cloneBearing)
    {
        var clone = Polar(cloneBearing, CloneRadius);
        var grab = TrueSouthStack + (clone - TrueSouthStack) * 0.45f;
        var spot = TetherSpot(clone, cloneBearing);
        ai.Move(TethersAt + 0.3f, () => AiMove.Single(tank, grab), jitter: 0f);
        ai.Move(SagittariusCast + 0.5f, () => AiMove.Single(tank, spot), jitter: 0f);
    }

    private Vector2 TetherSpot(Vector2 clone, float cloneBearing)
    {
        var party = ClearOfArrow();
        foreach (var lead in new[] { TetherLead, 46f, 66f, 40f, 72f, 34f, 78f })
        foreach (var radius in new[] { 19f, 17f })
        {
            var spot = Polar(cloneBearing + lead, radius);
            if (ArrowDistance(spot) >= 6.5f && ConeMisses(clone, spot, party) && ConeMisses(clone, spot, Vector2.Zero)) return spot;
        }
        return Polar(cloneBearing + TetherLead, 19f);
    }

    private static bool ConeMisses(Vector2 clone, Vector2 tank, Vector2 point)
    {
        var axis = Vector2.Normalize(tank - clone);
        var toPoint = Vector2.Normalize(point - clone);
        return Vector2.Dot(axis, toPoint) < MathF.Cos(MathF.PI / 180f * 38f);
    }

    private float ArrowDistance(Vector2 point)
    {
        var origin = Polar(state.SagittariusBearing, CloneRadius);
        var axis = Vector2.Normalize(TrueSouthStack - origin);
        var rel = point - origin;
        return Vector2.Dot(rel, axis) < 0 ? float.MaxValue : MathF.Abs(rel.X * axis.Y - rel.Y * axis.X);
    }

    private Vector2 ClearOfArrow()
    {
        var origin = Polar(state.SagittariusBearing, CloneRadius);
        var axis = Vector2.Normalize(TrueSouthStack - origin);
        var normal = new Vector2(-axis.Y, axis.X);
        var offset = Vector2.Dot(-origin, normal);
        var away = offset >= 0 ? normal : -normal;
        return away * MathF.Max(0f, ArrowClearance - MathF.Abs(offset));
    }

    private static IAiMove NonTanks(Vector2 spot)
    {
        var coords = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>().Where(r => !r.IsTank())) coords[(int)role] = spot;
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove TanksTowardShieldPartyInMiddle()
    {
        var coords = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>()) coords[(int)role] = Vector2.Zero;
        coords[(int)PartyRole.MainTank] = Polar(state.BeyondDefenseBearing - 20f, 9f);
        coords[(int)PartyRole.OffTank] = Polar(state.BeyondDefenseBearing + 20f, 9f);
        return AiMove.Create(coords).NaturalOrder();
    }

    private void JumpedTankLeavesOtherTankStacks()
    {
        foreach (var tank in new[] { PartyRole.MainTank, PartyRole.OffTank })
        {
            if (world.Party.Get(tank) is not { } member || !member.IsAlive()) continue;
            var hit = member.HasStatus(TopNativeConstants.StatusId.TwiceComeRuin);
            var side = tank == PartyRole.MainTank ? -1f : 1f;
            var to = hit ? Polar(state.BeyondDefenseBearing + side * 100f, 10f) : Vector2.Zero;
            ai.Move(0.05f, () => AiMove.Single(tank, to), jitter: 0f);
        }
    }
}
