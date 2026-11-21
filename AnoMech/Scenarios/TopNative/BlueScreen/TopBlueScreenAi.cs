using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.TopNative.BlueScreen.TopBlueScreenScenario;

namespace AnoMech.Scenarios.TopNative.BlueScreen;

public sealed class TopBlueScreenAi : IScenarioAi<TopBlueScreenState>
{
    public string Name => "Elemental";
    public string Group => "Elemental";

    public static readonly PartyRole[] EastNorthToSouth = [PartyRole.OffTank, PartyRole.CasterDps, PartyRole.ShieldHealer, PartyRole.MeleeDpsB];
    public static readonly PartyRole[] WestNorthToSouth = [PartyRole.MainTank, PartyRole.PhysRangedDps, PartyRole.RegenHealer, PartyRole.MeleeDpsA];
    private static readonly float[] EastSpreadBearings = [67f, 90f, 115f, 135f];
    private static readonly float[] WestSpreadBearings = [293f, 270f, 245f, 225f];
    private const float SpreadRadius = 13.7f;
    private const float EastStackBearing = 163f;
    private const float WestStackBearing = 200f;
    private const float StackRadius = 13.4f;
    private static readonly float[] StackDepth = [-0.9f, -0.3f, 0.3f, 0.9f];
    private const float InsideRingsRadius = 9.5f;
    private const float DetourRadius = 8.5f;
    private const float AvoidCentreWithin = 6.5f;

    private AiManager ai = null!;
    private readonly Dictionary<PartyRole, Vector2> plan = [];

    public void Run(TopBlueScreenState state, SimWorld world)
    {
        ai = new AiManager(world);
        plan.Clear();
        foreach (var role in EastNorthToSouth.Concat(WestNorthToSouth)) Go(0.5f, role, SpreadSpot(role));

        for (var wave = 0; wave < TopBlueScreenState.Waves; wave++)
        {
            var east = StacksEast(state.Markers[wave]);
            var afterProteans = LinesAt[wave] + ProteansAfterLines + 0.1f;
            if (wave == 1)
            {
                GatherInsideTheRings(afterProteans, east);
                GatherAtStacks(WaveRepeaterCasts[0] + RingOffsets[1] + 0.15f, east);
            }
            else
            {
                GatherAtStacks(afterProteans, east);
            }
            if (wave < 2) ReturnToSpreads(StacksAt[wave] + 0.2f);
            else GatherInsideTheRings(WaveRepeaterCasts[1] + RingOffsets[0] + 0.15f, east);
        }
    }

    private static Vector2 SpreadSpot(PartyRole role)
    {
        var east = Array.IndexOf(EastNorthToSouth, role);
        return Polar(east >= 0 ? EastSpreadBearings[east] : WestSpreadBearings[Array.IndexOf(WestNorthToSouth, role)], SpreadRadius);
    }

    private static HashSet<PartyRole> StacksEast(IReadOnlyList<PartyRole> markers)
    {
        var east = EastNorthToSouth.ToHashSet();
        var markedEast = EastNorthToSouth.Where(markers.Contains).ToArray();
        var markedWest = WestNorthToSouth.Where(markers.Contains).ToArray();
        if (markedEast.Length == 2)
        {
            east.Remove(markedEast[^1]);
            east.Add(WestNorthToSouth[^1]);
        }
        else if (markedWest.Length == 2)
        {
            east.Add(markedWest[^1]);
            east.Remove(EastNorthToSouth[^1]);
        }
        return east;
    }

    private static IEnumerable<(PartyRole Role, float Bearing, int Rank)> StackPlaces(HashSet<PartyRole> east)
    {
        var order = EastNorthToSouth.Concat(WestNorthToSouth).ToArray();
        var eastRank = 0;
        var westRank = 0;
        foreach (var role in order)
            yield return east.Contains(role) ? (role, EastStackBearing, eastRank++) : (role, WestStackBearing, westRank++);
    }

    private void GatherAtStacks(float at, HashSet<PartyRole> east)
    {
        foreach (var (role, bearing, rank) in StackPlaces(east)) Go(at, role, Polar(bearing, StackRadius + StackDepth[rank]));
    }

    private void GatherInsideTheRings(float at, HashSet<PartyRole> east)
    {
        foreach (var (role, bearing, _) in StackPlaces(east)) Go(at, role, Polar(bearing, InsideRingsRadius));
    }

    private void ReturnToSpreads(float at)
    {
        foreach (var role in EastNorthToSouth.Concat(WestNorthToSouth)) Go(at, role, SpreadSpot(role));
    }

    private void Go(float at, PartyRole role, Vector2 to)
    {
        if (plan.TryGetValue(role, out var from) && DistanceFromCentre(from, to) < AvoidCentreWithin)
        {
            var detour = Polar(Bisector(Bearing(from), Bearing(to)), DetourRadius);
            Move(at, role, detour);
            at += Vector2.Distance(from, detour) / AiManager.RunSpeed + 0.05f;
        }
        Move(at, role, to);
        plan[role] = to;
    }

    private void Move(float at, PartyRole role, Vector2 to) => ai.Move(at, () => AiMove.Single(role, to), jitter: 0f);

    private static float DistanceFromCentre(Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(-Vector2.Dot(a, ab) / MathF.Max(ab.LengthSquared(), 1e-4f), 0f, 1f);
        return (a + ab * t).Length();
    }

    private static float Bisector(float a, float b)
    {
        var delta = ((b - a) % 360f + 540f) % 360f - 180f;
        return a + delta / 2f;
    }

    private static float Bearing(Vector2 p) => TopNativeConstants.BearingDegrees(new Vector3(p.X, 0f, p.Y));

    private static Vector2 Polar(float bearing, float radius)
    {
        var p = TopNativeConstants.Polar(bearing, radius);
        return new Vector2(p.X, p.Z);
    }
}
