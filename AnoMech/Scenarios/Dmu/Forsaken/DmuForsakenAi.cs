using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.Forsaken;

public sealed class DmuForsakenAi : IScenarioAi<DmuForsakenState>
{
    public string Name => "Yan";
    public string Group => "Mana";

    private const int FinalTower = 7;
    private const float PastMeleeFromCenter = 9.7f;
    private const float FutureMeleeFromCenter = 11f;

    private static readonly Vector2?[] OddTowerSpots =
    [
        new(5.66f, -4.66f),
        new(5.66f, -8.66f),
        new(-5.0f, -3.6f),
        new(-5.66f, -9.16f),
        new(5.66f, -10.66f),
        new(1.0f, -3.9f),
        new(-1.0f, -3.6f),
        new(-1.0f, -3.6f),
    ];

    private static readonly Vector2?[] EvenTowerSpots =
    [
        new(5.66f, -2.16f),
        new(5.05f, -9.11f),
        new(-5.66f, -2.16f),
        new(-5.05f, -9.11f),
        new(10.66f, -5.66f),
        new(3.5f, 4.9f),
        new(-3.5f, 4.9f),
        new(-10.66f, -5.66f),
    ];

    private DmuForsakenState state = null!;
    private SimWorld world = null!;
    private List<PartyRole> towerGroup = [];
    private List<PartyRole> helperGroup = [];
    private readonly Dictionary<Sign, PartyRole> finalTowerMarks = [];

    public void Run(DmuForsakenState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        finalTowerMarks.Clear();
        FormGroupsFromStackColumns();
        var ai = new AiManager(world);

        ai.Move(1f, LineUpInColumnsSouthOfKefka);
        ai.Move(10.16f, () => TowerSpots(0), jitter: 0f, arrivalTime: 22.16f);
        ai.Move(25.17f, () => TowerSpots(1), jitter: 0f, arrivalTime: 32.16f);
        ai.Move(33.3f, BaitAllThingsEnding(0, 2), arrivalTime: 37f);
        ai.Move(39.21f, () => TowerSpots(2), jitter: 0f, arrivalTime: 43.21f);
        world.Events.Add(45.5f, SelfMarkFinalTowerSpells);
        ai.Move(47.22f, () => TowerSpots(3), jitter: 0f, arrivalTime: 53.22f);
        ai.Move(54.35f, BaitAllThingsEnding(1, 4), arrivalTime: 57f);
        ai.Move(59.26f, () => TowerSpots(4), jitter: 0f, arrivalTime: 63.86f);
        ai.Move(66.4f, () => TowerSpots(5), jitter: 0f, arrivalTime: 73.27f);
        ai.Move(75.4f, BaitAllThingsEnding(2, 6), arrivalTime: 78f);
        ai.Move(81.31f, () => TowerSpots(6), jitter: 0f, arrivalTime: 85.8f);
        ai.Move(90.32f, () => TowerSpots(FinalTower), jitter: 0f, arrivalTime: 94.32f);
        world.Events.Add(97f, ClearSelfMarks);
        ai.Move(96.45f, GatherAtA, arrivalTime: 98f);
        ai.Move(101.16f, BaitLastEndAtA, arrivalTime: 105f);
    }

    private static IAiMove LineUpInColumnsSouthOfKefka() => AiMove.Create(
        new(-7.0f, 3.8f),
        new(-2.65f, 5.8f),
        new(-7.6f, 8.0f),
        new(-3.0f, 10.0f),
        new(2.65f, 5.8f),
        new(7.0f, 3.8f),
        new(3.0f, 10.0f),
        new(7.6f, 8.0f));

    private void FormGroupsFromStackColumns()
    {
        var stacks = state.Lockons.Where(pair => pair.Value == LockonId.ForsakenStack).Select(pair => pair.Key).OrderBy(r => r).ToList();
        var supportPartner = (PartyRole)(((int)stacks[0] + 2) % 4);
        var dpsPartner = (PartyRole)(((int)stacks[1] - 2) % 4 + 4);
        towerGroup = TankHealerSwapped([stacks[0], stacks[1], supportPartner, dpsPartner]);
        helperGroup = TankHealerSwapped(Enum.GetValues<PartyRole>().Where(role => !towerGroup.Contains(role)));
    }

    private static List<PartyRole> TankHealerSwapped(IEnumerable<PartyRole> roles)
    {
        var list = roles.OrderBy(r => r).ToList();
        (list[0], list[1]) = (list[1], list[0]);
        return list;
    }

    private static bool TowerGroupSoaks(int tower) => tower is < 3 or FinalTower;

    private PartyRole Soaker(uint lockon, int tower, int order)
        => (TowerGroupSoaks(tower) ? towerGroup : helperGroup).Where(role => state.Lockons[role] == lockon).Skip(order).First();

    private PartyRole Helper(int tower, int order) => (TowerGroupSoaks(tower) ? helperGroup : towerGroup)[order];

    private void SelfMarkFinalTowerSpells()
    {
        MarkPair(LockonId.ForsakenCone, state.FinalConeMarksSwapped, Sign.Bind1, Sign.Bind2);
        MarkPair(LockonId.ForsakenChariot, state.FinalAoeMarksSwapped, Sign.Ignore1, Sign.Ignore2);
    }

    private void MarkPair(uint lockon, bool swapped, Sign first, Sign second)
    {
        var holders = towerGroup.Where(role => state.Lockons[role] == lockon).OrderBy(r => r).ToList();
        if (holders.Count != 2) return;
        if (swapped) holders.Reverse();
        foreach (var (sign, role) in new[] { (first, holders[0]), (second, holders[1]) })
        {
            finalTowerMarks[sign] = role;
            if (world.Party.Get(role) is { } member) world.SoftMarkers.Set(sign, member);
        }
    }

    private void ClearSelfMarks() => world.SoftMarkers.Clear();

    private IAiMove TowerSpots(int tower) => tower % 2 == 1 ? EvenTower(tower) : OddTower(tower);

    private IAiMove OddTower(int tower) => AiMove.Create(OddTowerSpots)
        .Assignments([
            Soaker(LockonId.ForsakenStack, tower, 0),
            Soaker(LockonId.ForsakenCone, tower, 0),
            Soaker(LockonId.ForsakenStack, tower, 1),
            Soaker(LockonId.ForsakenChariot, tower, 0),
            Helper(tower, 0),
            Helper(tower, 1),
            Helper(tower, 2),
            Helper(tower, 3),
        ])
        .ApplyPositions(state.NewNorthAt(tower).Apply);

    private IAiMove EvenTower(int tower) => AiMove.Create(EvenTowerSpots)
        .Assignments([
            FinalTowerMarkOr(Sign.Bind1, () => Soaker(LockonId.ForsakenCone, tower, 0), tower),
            FinalTowerMarkOr(Sign.Ignore1, () => Soaker(LockonId.ForsakenChariot, tower, 0), tower),
            FinalTowerMarkOr(Sign.Bind2, () => Soaker(LockonId.ForsakenCone, tower, 1), tower),
            FinalTowerMarkOr(Sign.Ignore2, () => Soaker(LockonId.ForsakenChariot, tower, 1), tower),
            Helper(tower, 0),
            Helper(tower, 1),
            Helper(tower, 2),
            Helper(tower, 3),
        ])
        .ApplyPositions(state.NewNorthAt(tower).Apply);

    private PartyRole FinalTowerMarkOr(Sign mark, Func<PartyRole> byPriority, int tower)
        => tower == FinalTower && finalTowerMarks.Count == 4 ? finalTowerMarks[mark] : byPriority();

    private Func<IAiMove> BaitAllThingsEnding(int end, int northIndex)
    {
        var distance = state.EndAttacks[end] == EndAttack.PastsEnd ? -PastMeleeFromCenter : FutureMeleeFromCenter;
        return () => AiMove.All(new(0, distance)).ApplyPositions(state.NewNorthAt(northIndex).Apply);
    }

    private static IAiMove GatherAtA() => AiMove.All(new(0, -PastMeleeFromCenter)).ApplyPositions(Direction.N.Apply);

    private IAiMove BaitLastEndAtA()
    {
        var distance = state.EndAttacks[3] == EndAttack.PastsEnd ? -PastMeleeFromCenter : FutureMeleeFromCenter;
        return AiMove.All(new(0, distance)).ApplyPositions(Direction.N.Apply);
    }
}
