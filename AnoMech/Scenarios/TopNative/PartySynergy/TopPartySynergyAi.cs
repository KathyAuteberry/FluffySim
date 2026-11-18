using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Top;

namespace AnoMech.Scenarios.TopNative.PartySynergy;

public sealed class TopPartySynergyAi : IScenarioAi<TopPartySynergyState>
{
    public string Name => "Elemental";
    public string Group => "Elemental";

    public static readonly PartyRole[] LeftLightParty = [PartyRole.RegenHealer, PartyRole.MainTank, PartyRole.MeleeDpsA, PartyRole.PhysRangedDps];
    public static readonly PartyRole[] RightLightParty = [PartyRole.ShieldHealer, PartyRole.OffTank, PartyRole.MeleeDpsB, PartyRole.CasterDps];

    private const int Cross = 0, Square = 1, Circle = 2, Triangle = 3;

    private static readonly int[] TopToBottom = [Circle, Cross, Triangle, Square];

    private static readonly Vector2[] MidLeft = [new(-11.25f, 15.5f), new(-11.25f, 5.2f), new(-11.25f, -5.2f), new(-11.25f, -15.5f)];
    private static readonly Vector2[] RemoteLeft = [new(-11f, 15.4f), new(-17.8f, 6f), new(-18.3f, -5f), new(-11f, -15.4f)];

    private TopPartySynergyState state = null!;
    private List<PartyRole> left = [];
    private List<PartyRole> right = [];
    private List<PartyRole> knockbackLeft = [];
    private List<PartyRole> knockbackRight = [];

    public void Run(TopPartySynergyState s, SimWorld world)
    {
        state = s;
        FormGroups();
        var ai = new AiManager(world);
        ai.Move(1f, LineUpByLightParty, jitter: 0.3f);
        ai.Move(11f, DodgeCloneAttacks, arrivalTime: 14.8f);
        ai.Move(16f, SpreadForGlitch, arrivalTime: 21.5f, jitter: 0.2f);
        ai.Move(23f, KnockbackSpots, arrivalTime: 28.4f, jitter: 0.2f);
        ai.Move(30f, StackSpots, arrivalTime: 33f, jitter: 0.2f);
    }

    private int Symbol(PartyRole role) => Array.IndexOf(state.Order.List, role) / 2;

    private int Rank(PartyRole role) => Array.IndexOf(TopToBottom, Symbol(role));

    private PartyRole Partner(PartyRole role) => state.Order.List.First(r => r != role && Symbol(r) == Symbol(role));

    private void FormGroups()
    {
        left = LeftLightParty.ToList();
        right = RightLightParty.ToList();
        var leftDuplicates = LowerDuplicates(left);
        var rightDuplicates = LowerDuplicates(right);
        for (var i = 0; i < leftDuplicates.Count && i < rightDuplicates.Count; i++)
        {
            left[left.IndexOf(leftDuplicates[i])] = rightDuplicates[i];
            right[right.IndexOf(rightDuplicates[i])] = leftDuplicates[i];
        }
        knockbackLeft = left.ToList();
        knockbackRight = right.ToList();
        var stacks = state.Stacks.List;
        var bothLeft = stacks.All(left.Contains);
        if (!bothLeft && !stacks.All(right.Contains)) return;
        var fartherFromEye = stacks.OrderBy(r => SpreadSpot(r, bothLeft).Y).First();
        var partner = Partner(fartherFromEye);
        var (side, other) = bothLeft ? (knockbackLeft, knockbackRight) : (knockbackRight, knockbackLeft);
        side[side.IndexOf(fartherFromEye)] = partner;
        other[other.IndexOf(partner)] = fartherFromEye;
    }

    private List<PartyRole> LowerDuplicates(List<PartyRole> group)
        => group.Where((r, i) => group.Take(i).Any(earlier => Symbol(earlier) == Symbol(r))).ToList();

    private Vector2 SpreadSpot(PartyRole role, bool onLeft)
    {
        var far = state.Glitch == GlitchType.Far;
        var rank = Rank(role);
        if (far && !onLeft && (Symbol(role) == Circle || Symbol(role) == Square)) rank = 3 - rank;
        var spot = (far ? RemoteLeft : MidLeft)[rank];
        return onLeft ? spot : spot with { X = -spot.X };
    }

    private static Vector2 Local(Vector2 eastNorth) => new(eastNorth.X, -eastNorth.Y);

    private static ISwapStep Spots(params (PartyRole Role, Vector2 Spot)[] spots)
    {
        var coords = new Vector2?[8];
        foreach (var (role, spot) in spots) coords[(int)role] = spot;
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove LineUpByLightParty()
    {
        var spots = new List<(PartyRole, Vector2)>();
        for (var i = 0; i < 4; i++)
        {
            spots.Add((LeftLightParty[i], new Vector2(-2f, -4.5f + 3f * i)));
            spots.Add((RightLightParty[i], new Vector2(2f, -4.5f + 3f * i)));
        }
        return Spots(spots.ToArray());
    }

    private IAiMove DodgeCloneAttacks()
    {
        float distance;
        if (state.AttackF == OmegaAttack.Legs)
            distance = state.AttackM == OmegaAttack.Sword ? 2.5f : -2.5f;
        else
            distance = state.AttackM == OmegaAttack.Sword ? -17f : -10f;
        return AiMove.All(new(0, -distance)).ApplyPositions(state.AttackDir.Rotate(1).Apply);
    }

    private IAiMove SpreadForGlitch()
        => Spots(left.Select(r => (r, Local(SpreadSpot(r, true))))
                .Concat(right.Select(r => (r, Local(SpreadSpot(r, false))))).ToArray())
            .ApplyPositions(state.NewNorthA.Apply);

    private Vector2 RightGroupDirection => state.Glitch == GlitchType.Far ? new(1f, 0f) : new(0f, -1f);

    private IAiMove KnockbackSpots()
        => Spots(knockbackLeft.Select(r => (r, Local(new Vector2(-3f, 0f))))
                .Concat(knockbackRight.Select(r => (r, Local(RightGroupDirection * 3f)))).ToArray())
            .ApplyPositions(state.NewNorthB.Apply);

    private IAiMove StackSpots()
    {
        var distance = state.Glitch == GlitchType.Far ? 19f : 16f;
        return Spots(knockbackLeft.Select(r => (r, Local(new Vector2(-distance, 0f))))
                .Concat(knockbackRight.Select(r => (r, Local(RightGroupDirection * distance)))).ToArray())
            .ApplyPositions(state.NewNorthB.Apply);
    }
}
