using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.TopNative.Pantokrator.TopPantokratorScenario;

namespace AnoMech.Scenarios.TopNative.Pantokrator;

public sealed class TopPantokratorAi : IScenarioAi<TopPantokratorState>
{
    public string Name => "Elemental (HaruGlory)";
    public string Group => "Elemental";

    public static readonly PartyRole[] Priority =
    [
        PartyRole.RegenHealer, PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA,
        PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.ShieldHealer,
    ];

    private const float NorthWest = 315f;
    private const float StackRadius = 12f;
    private const float StackLeadOverFlame = 101f;
    private const float WaitNearCenterRadius = 3f;
    private const float ReactToNumbers = 0.8f;
    private static readonly float[] StackDepth = [-0.6f, -0.2f, 0.2f, 0.6f];

    private static readonly Dictionary<PartyRole, (float Bearing, float Radius)> WaveCannonSpots = new()
    {
        [PartyRole.MainTank] = (358f, 13.6f),
        [PartyRole.OffTank] = (2f, 13.6f),
        [PartyRole.RegenHealer] = (267f, 9.5f),
        [PartyRole.MeleeDpsA] = (234f, 9.8f),
        [PartyRole.PhysRangedDps] = (203f, 9.7f),
        [PartyRole.CasterDps] = (160f, 9f),
        [PartyRole.MeleeDpsB] = (122f, 9f),
        [PartyRole.ShieldHealer] = (77f, 10.6f),
    };

    private TopPantokratorState state = null!;
    private AiManager ai = null!;
    private readonly Dictionary<PartyRole, int> groupOf = [];
    private readonly Dictionary<PartyRole, int> rankInGroup = [];
    private readonly float[] firstStackBearing = new float[2];

    public static Dictionary<PartyRole, int> Groups(TopNativeNumbers numbers)
    {
        var groups = new Dictionary<PartyRole, int>();
        var northWest = new HashSet<int>();
        foreach (var role in Priority)
            groups[role] = northWest.Add(numbers[role]) ? 0 : 1;
        return groups;
    }

    public void Run(TopPantokratorState stateParam, SimWorld world)
    {
        state = stateParam;
        ai = new AiManager(world);
        groupOf.Clear();
        rankInGroup.Clear();
        foreach (var (role, group) in Groups(state.Numbers)) groupOf[role] = group;
        var nextRank = new int[2];
        foreach (var role in Priority) rankInGroup[role] = nextRank[groupOf[role]]++;
        ChooseStartingSectors();

        for (var i = 0; i < Priority.Length; i++) Go(0.5f, Priority[i], Polar(NorthWest + 45f * i, WaitNearCenterRadius));
        foreach (var role in Priority)
        {
            var number = state.Numbers[role];
            var cycleStart = 3 * number - 2;
            Go(NumbersAt + ReactToNumbers, role, number == 1 ? OnesWaitNearCenter(role) : Stack(role, 0));
            for (var drop = 0; drop <= 10; drop++)
            {
                if (drop >= cycleStart - 2 && drop <= cycleStart + 2) continue;
                var next = drop + 1;
                Go(Drop(drop) + 0.6f, role, Stack(role, next));
            }
            MissileCycle(role, number, cycleStart);
            var spot = WaveCannonSpots[role];
            Go(FirstPuddle + 22.62f, role, Polar(spot.Bearing, spot.Radius));
        }
    }

    private void ChooseStartingSectors()
    {
        var a0 = state.FirstFlameBearing;
        var hitNorthWest = Distance(NorthWest, a0) < 30f || Distance(NorthWest, a0 + 180f) < 30f;
        float[] sectorStarts = [a0 + 30f, a0 + 210f];
        int northWestSector;
        if (!hitNorthWest)
            northWestSector = Clockwise(sectorStarts[0], NorthWest) < 120f ? 0 : 1;
        else
            northWestSector = Clockwise(NorthWest, sectorStarts[0]) < Clockwise(NorthWest, sectorStarts[1]) ? 0 : 1;
        var lead = a0 + state.Rotation * StackLeadOverFlame;
        float[] candidates = [lead, lead + 180f];
        var inFirstSector = candidates.First(c => Clockwise(sectorStarts[0], c) < 120f);
        var inSecondSector = candidates.First(c => c != inFirstSector);
        firstStackBearing[0] = northWestSector == 0 ? inFirstSector : inSecondSector;
        firstStackBearing[1] = northWestSector == 0 ? inSecondSector : inFirstSector;
    }

    private void MissileCycle(PartyRole role, int number, int front)
    {
        if (number > 1) Go(Drop(front - 2) + 0.55f, role, Offset(role, front - 1, 41f, 6f));
        Go(Drop(front - 1) + 0.1f, role, Offset(role, front, 88f, 11.5f));
        var resolve = FirstResolve + ResolveInterval * (number - 1);
        if (number == 4)
        {
            Go(resolve + 0.1f, role, Stack(role, 11));
            return;
        }
        Go(resolve + 0.1f, role, Offset(role, front + 1, 61f, 17.5f));
        Go(Drop(front + 1) + 0.3f, role, Offset(role, front + 2, 11f, 16.5f));
        Go(Drop(front + 2) + 0.75f, role, Stack(role, front + 3));
    }

    private static float Drop(int index) => FirstPuddle + PuddleInterval * index;

    private float StackBearing(PartyRole role, int drop) => firstStackBearing[groupOf[role]] + state.Rotation * 30f * drop;

    private Vector2 Stack(PartyRole role, int drop) => Polar(StackBearing(role, drop), StackRadius + StackDepth[rankInGroup[role]]);

    private Vector2 Offset(PartyRole role, int drop, float ahead, float radius)
        => Polar(StackBearing(role, drop) + state.Rotation * ahead, radius);

    private Vector2 OnesWaitNearCenter(PartyRole role) => Offset(role, 0, 36f, 3.5f);

    private void Go(float at, PartyRole role, Vector2 to) => ai.Move(at, () => AiMove.Single(role, to), jitter: 0f);

    private static Vector2 Polar(float bearing, float radius)
    {
        var p = TopNativeConstants.Polar(bearing, radius);
        return new Vector2(p.X, p.Z);
    }

    private static float Clockwise(float from, float to) => ((to - from) % 360f + 360f) % 360f;

    private static float Distance(float a, float b) => MathF.Min(Clockwise(a, b), Clockwise(b, a));
}
