using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.TopNative.ProgramLoop.TopProgramLoopScenario;
using static AnoMech.Scenarios.TopNative.ProgramLoop.TopProgramLoopState;

namespace AnoMech.Scenarios.TopNative.ProgramLoop;

public sealed class TopProgramLoopAi : IScenarioAi<TopProgramLoopState>
{
    public string Name => "Elemental";
    public string Group => "Elemental";

    public static readonly PartyRole[] Priority =
    [
        PartyRole.RegenHealer, PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA,
        PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.ShieldHealer,
    ];

    private const float StackRadius = 12.5f;
    private const float FrontRadius = 7f;
    private const float TetherRadius = 16.5f;
    private const float MinGrabRadius = 2f;
    private const float GrabShortOfHolder = 2f;
    private const float BetweenTowersRadius = 8f;
    private const float BetweenTowersOffset = 15f;
    private const float BehindTowerRadius = 17.5f;
    private const float BehindTowerOffset = 12f;
    private const float InterceptStep = 0.2f;
    private const float InterceptFor = 4f;

    private TopProgramLoopState state = null!;
    private AiManager ai = null!;
    private SimWorld world = null!;
    private readonly Dictionary<PartyRole, Vector2> plan = [];
    private readonly Dictionary<int, (PartyRole Role, int Source, int Side, int Destination)[]> grabPlans = [];

    public void Run(TopProgramLoopState stateParam, SimWorld world)
    {
        state = stateParam;
        this.world = world;
        ai = new AiManager(world);
        plan.Clear();
        grabPlans.Clear();
        GatherSouth();
        ThreesStepInFront();
        FirstSet();
        for (var set = 1; set < 4; set++) LaterSet(set);
    }

    private static Vector2 Polar(float bearing, float radius)
    {
        var p = TopNativeConstants.Polar(bearing, radius);
        return new Vector2(p.X, p.Z);
    }

    private static float Bearing(Vector2 p) => TopNativeConstants.BearingDegrees(new Vector3(p.X, 0, p.Y));

    private void GatherSouth()
    {
        for (var i = 0; i < Priority.Length; i++)
            Go(1f, Priority[i], Polar(176f + i, StackRadius + (i % 2) * 0.6f));
    }

    private void ThreesStepInFront()
    {
        var threes = state.Numbers.WithNumber(3, Priority);
        for (var i = 0; i < threes.Count; i++) Go(NumbersAt + 0.3f, threes[i], Polar(i == 0 ? 174f : 186f, FrontRadius));
    }

    private IReadOnlyList<PartyRole> TowerTakers(int set) => state.Numbers.WithNumber(set + 1, Priority);
    private IReadOnlyList<PartyRole> TetherTakers(int set) => state.Numbers.WithNumber((set + 2) % 4 + 1, Priority);
    private IReadOnlyList<int> TowerCardinals(int set) => state.TowerSets[set].Select(Cardinal).ToArray();
    private IReadOnlyList<int> EmptyCardinals(int set) => Enumerable.Range(0, 4).Where(c => !TowerCardinals(set).Contains(c)).ToArray();

    private static Vector2 TowerSpot(int spot) => Polar(SpotBearings[spot], SpotRadius);
    private static Vector2 TetherSpot(int cardinal) => Polar(CardinalBearing(cardinal), TetherRadius);
    private Vector2[] WaitingSpots(int set, int spot)
    {
        var cardinal = Cardinal(spot);
        var side = MathF.Sign(SpotBearings[spot] - CardinalBearing(cardinal) is var d && d > 180 ? d - 360 : d);
        var bearing = CardinalBearing(cardinal) - 9f * side;
        return [Polar(bearing, 9.5f), Polar(bearing, 15.5f)];
    }

    private void FirstSet()
    {
        var spots = state.TowerSets[0];
        var start = TowerSpawnTimes[0] + 0.4f;
        var takers = TowerTakers(0);
        for (var i = 0; i < 2; i++) Route(start, takers[i], TowerSpot(spots[i]));
        AssignWaiting(0, TowerTakers(1).ToList(), (role, spot) => Route(start, role, spot));
        WaitBetweenTowersForTether(1, start);

        var holders = TetherTakers(0);
        var empties = EmptyCardinals(0);
        for (var i = 0; i < 2; i++)
        {
            var role = holders[i];
            var to = TetherSpot(empties[i]);
            ai.Move(start + 6.8f, () => AiMove.Single(role, to), jitter: 0f, arrivalTime: TowerTimes[0] - 0.5f);
            plan[role] = to;
        }
    }

    private void LaterSet(int set)
    {
        var previous = TowerTimes[set - 1];
        var grabbers = grabPlans[set].Select(g => g.Role).ToArray();
        foreach (var (role, source, _, destination) in grabPlans[set])
        {
            var to = TetherSpot(destination);
            for (var t = 0.1f; t <= InterceptFor; t += InterceptStep)
                ai.Move(previous + BlasterAfterTower + t, () => AiMove.Single(role, InterceptOrCarry(role, source, to, grabbers)), jitter: 0f);
            plan[role] = to;
        }

        var start = previous + BlasterAfterTower + 0.25f;
        var takers = TowerTakers(set);
        var spots = state.TowerSets[set];
        for (var i = 0; i < 2; i++) Route(start, takers[i], TowerSpot(spots[i]));
        var oldHolders = TetherTakers(set - 1);
        var waiting = oldHolders.ToList();
        if (set < 3) WaitBetweenTowersForTether(set + 1, start);
        else waiting.AddRange(TowerTakers(set - 1));
        AssignWaiting(set, waiting, (role, spot) => Route(oldHolders.Contains(role) ? previous + BlasterAfterTower + 0.5f : start, role, spot));
    }

    private void WaitBetweenTowersForTether(int set, float at)
    {
        var oldCardinals = EmptyCardinals(set - 1);
        var newCardinals = EmptyCardinals(set);
        var grabbers = TetherTakers(set);
        var swapped = Sweep(oldCardinals[0], newCardinals[1]) + Sweep(oldCardinals[1], newCardinals[0])
                      < Sweep(oldCardinals[0], newCardinals[0]) + Sweep(oldCardinals[1], newCardinals[1]);
        var grabs = new (PartyRole, int, int, int)[2];
        for (var i = 0; i < 2; i++)
        {
            var role = grabbers[i];
            var source = oldCardinals[swapped ? 1 - i : i];
            var side = SideCardinal(source, oldCardinals[swapped ? i : 1 - i], plan[role]);
            grabs[i] = (role, source, side, newCardinals[i]);
            Route(at, role, IdleSpotForGrabber(set - 1, source, side));
        }
        grabPlans[set] = grabs;
    }

    private Vector2 IdleSpotForGrabber(int towerSet, int source, int side)
    {
        var towers = TowerCardinals(towerSet);
        if ((towers[0] - towers[1] + 4) % 4 == 2)
            return Polar(CardinalBearing(side) + BehindTowerOffset * MathF.Sign(AngleDelta(CardinalBearing(side), CardinalBearing(source))), BehindTowerRadius);
        var between = CardinalBearing(towers[0]) + AngleDelta(CardinalBearing(towers[0]), CardinalBearing(towers[1])) / 2f;
        return Polar(between + BetweenTowersOffset * MathF.Sign(AngleDelta(between, CardinalBearing(side))), BetweenTowersRadius);
    }

    private Vector2? InterceptOrCarry(PartyRole role, int source, Vector2 destination, PartyRole[] grabbers)
    {
        if (world.Party.Get(role) is not { } me) return null;
        if (me.HasTetherInSlot0(TopNativeConstants.TetherId.Blaster)) return destination;
        var holder = world.Party.ActiveMembers()
            .Where(m => m.HasTetherInSlot0(TopNativeConstants.TetherId.Blaster) && m is ISimPartyMember pm && !grabbers.Contains(pm.Role))
            .Select(m => new Vector2(m.Position.X, m.Position.Z))
            .OrderBy(p => MathF.Abs(AngleDelta(Bearing(p), CardinalBearing(source))))
            .Cast<Vector2?>().FirstOrDefault();
        if (holder is not { } h || h.Length() < MinGrabRadius + 0.5f) return destination;
        var along = Vector2.Normalize(h);
        var mine = new Vector2(me.Position.X, me.Position.Z);
        var reach = Math.Clamp(Vector2.Dot(mine, along), MinGrabRadius, MathF.Max(MinGrabRadius, h.Length() - GrabShortOfHolder));
        return along * reach;
    }

    private static int Sweep(int from, int to) => Math.Min((from - to + 4) % 4, (to - from + 4) % 4);
    private static int SideCardinal(int source, int otherTether, Vector2 from)
    {
        var candidates = new[] { (source + 1) % 4, (source + 3) % 4 }.Where(c => c != otherTether).ToArray();
        return candidates.OrderBy(c => MathF.Abs(AngleDelta(Bearing(from), CardinalBearing(c)))).First();
    }

    private void AssignWaiting(int set, List<PartyRole> waiting, Action<PartyRole, Vector2> route)
    {
        var spots = state.TowerSets[set];
        var free = new List<Vector2>[] { WaitingSpots(set, spots[0]).ToList(), WaitingSpots(set, spots[1]).ToList() };
        foreach (var role in Priority.Where(waiting.Contains))
        {
            var from = plan.TryGetValue(role, out var p) ? p : Polar(180f, StackRadius);
            var pick = Enumerable.Range(0, 2).Where(i => free[i].Count > 0)
                .OrderBy(i => MathF.Abs(AngleDelta(Bearing(from), CardinalBearing(Cardinal(spots[i]))))).First();
            var spot = free[pick][0];
            free[pick].RemoveAt(0);
            route(role, spot);
        }
    }

    private void Route(float start, PartyRole role, Vector2 to)
    {
        ai.Move(start, () => AiMove.Single(role, to), jitter: 0f);
        plan[role] = to;
    }

    private void Go(float at, PartyRole role, Vector2 to)
    {
        ai.Move(at, () => AiMove.Single(role, to), jitter: 0f);
        plan[role] = to;
    }

    private static float AngleDelta(float from, float to) => ((to - from) % 360f + 540f) % 360f - 180f;
}
