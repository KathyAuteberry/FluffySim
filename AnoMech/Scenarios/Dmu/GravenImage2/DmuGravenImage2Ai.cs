using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dmu.GravenImage2;

public sealed class DmuGravenImage2Ai : IScenarioAi<DmuGravenImage2State>
{
    public string Name => "Yan Flash";
    public string Group => "Mana";

    private const float StackOffAxis = 0.8f;
    private const float StackDepth = 9.2f;
    private const float PassDistance = 13f;
    private const float PassHolderBehind = 1.5f;
    private static readonly Vector2 TowardNorthPuddles = Vector2.Normalize(new(0.5f, -0.866f));
    private static readonly Vector2 TowardSouthPuddles = -TowardNorthPuddles;

    private DmuGravenImage2State state = null!;
    private SimWorld world = null!;

    public void Run(DmuGravenImage2State stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);
        ai.Move(0.2f, StartPositions);
        ai.Move(9.5f, SouthStack, arrivalTime: 14.0f);
        ai.Move(14.7f, () => SetSpots(1), arrivalTime: 18.1f);
        ai.Move(18.8f, BehindKefkaForRevoltingRuin);
        ai.UseAction(22.67f, MainTank, Enmity.ProvokeActionId, () => state.Objects.Kefka);
        ai.Move(23.3f, () => SafeHalf(state.FirstCleaveEast, tanking: OffTank), arrivalTime: 27.6f);
        ai.Move(28.5f, NorthStack, arrivalTime: 32.5f);
        ai.Move(33.1f, () => SetSpots(2), arrivalTime: 36.6f);
        ai.Move(37.2f, () => SafeHalf(state.SecondCleaveEast, tanking: null), arrivalTime: 41.0f);
        ai.Move(41.5f, TrapPass, arrivalTime: 44.9f);
        ai.Move(46.7f, IntoPuddles, sprint: true);
    }

    private static IAiMove ByRole(params (PartyRole Role, Vector2 Spot)[] spots)
    {
        var coords = new Vector2?[8];
        foreach (var (role, spot) in spots) coords[(int)role] = spot;
        return AiMove.Create(coords).NaturalOrder();
    }

    private static IAiMove StartPositions() => ByRole(
        (OffTank, new(0f, -7f)), (MainTank, new(-4f, 1f)),
        (RegenHealer, new(-3f, 5f)), (ShieldHealer, new(3f, 5f)),
        (MeleeDpsA, new(-2f, 3f)), (MeleeDpsB, new(2f, 3f)),
        (PhysRangedDps, new(-4f, 6f)), (CasterDps, new(4f, 6f)));

    private Vector2 SouthStackSpot => new(state.Ice.RealSouthwestNortheast ? StackOffAxis : -StackOffAxis, StackDepth);
    private static readonly Vector2 NorthStackSpot = new(0f, -StackDepth);

    private IAiMove SouthStack() => ByRole(PerRole.All.Select(role => (role, SouthStackSpot)).ToArray());

    private static IAiMove NorthStack() => ByRole(PerRole.All.Select(role => (role, NorthStackSpot)).ToArray());

    private IAiMove SetSpots(int set) => ByRole(PerRole.All.Select(role =>
        (role, state.SpreadsInSet(role, set) ? SpreadSlot(role, set) : Vector2.Zero)).ToArray());

    private static Vector2 SpreadSlot(PartyRole role, int set)
    {
        var mirror = set == 1 ? 1f : -1f;
        return role switch
        {
            MainTank or MeleeDpsA => new(-10f, 3f * mirror),
            OffTank or MeleeDpsB => new(10f, 3f * mirror),
            RegenHealer or PhysRangedDps => new(-10f, 16f * mirror),
            _ => new(10f, 16f * mirror),
        };
    }

    private static IAiMove BehindKefkaForRevoltingRuin() => ByRole(PerRole.All.Select(role =>
        (role, role == OffTank ? new Vector2(0f, -7f) : role == MainTank ? new Vector2(-3f, 2.5f) : new Vector2(0f, 3f))).ToArray());

    private static IAiMove SafeHalf(bool cleaveEast, PartyRole? tanking)
    {
        var side = cleaveEast ? -1f : 1f;
        return ByRole(PerRole.All.Select(role =>
            (role, role == tanking ? new Vector2(side * 2.5f, -6.5f) : new Vector2(side * 5f, role.IsDps() ? 1.5f : 0f))).ToArray());
    }

    private Vector2 SouthPuddles => state.Objects.SouthPuddles ?? SouthStackSpot;
    private Vector2 NorthPuddles => state.Objects.NorthPuddles ?? NorthStackSpot;

    private bool HoldsTrap(PartyRole role)
        => world.Party.Get(role) is { } member && (member.HasStatus(DmuConstants.StatusId.DoubleTroubleTrap) || member.ShowsStatus(DmuConstants.StatusId.DoubleTroubleTrap));

    private IAiMove TrapPass() => ByRole(PerRole.All.Select(role =>
    {
        var (puddles, toward) = role.IsDps() ? (SouthPuddles, TowardSouthPuddles) : (NorthPuddles, TowardNorthPuddles);
        var group = puddles - toward * PassDistance;
        return (role, HoldsTrap(role) ? group - toward * PassHolderBehind : group);
    }).ToArray());

    private IAiMove IntoPuddles() => ByRole(PerRole.All.Select(role => (role, role.IsDps() ? SouthPuddles : NorthPuddles)).ToArray());
}
