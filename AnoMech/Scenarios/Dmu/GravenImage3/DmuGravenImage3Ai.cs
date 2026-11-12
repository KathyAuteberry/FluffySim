using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dmu.GravenImage3;

public sealed class DmuGravenImage3Ai : IScenarioAi<DmuGravenImage3State>
{
    public string Name => "Yan Flash";
    public string Group => "Mana";

    private static readonly Vector3 AveMariaStatue = new(-5f, 0f, -75f);
    private static readonly Vector3 IndolentWillStatue = new(5.25f, 0f, -66f);

    private DmuGravenImage3State state = null!;
    private SimWorld world = null!;

    public void Run(DmuGravenImage3State stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);
        ai.Move(0.2f, StartPositions);
        ai.Move(7.6f, () => DropSpots(early: true), arrivalTime: 14.1f);
        ai.Move(14.7f, () => DropSpots(early: false), arrivalTime: 17.2f);
        ai.Move(17.7f, TrapStacks, arrivalTime: 22.4f);
        ai.Move(24.4f, TeleportSpread, jitter: 0f);
        ai.Move(35.3f, FinalMysteryMagic, arrivalTime: 40.6f);
        world.Events.Add(41.1f, FaceTheGaze);
    }

    private static IAiMove ByRole(params (PartyRole Role, Vector2 Spot)[] spots)
    {
        var coords = new Vector2?[8];
        foreach (var (role, spot) in spots) coords[(int)role] = spot;
        return AiMove.Create(coords).NaturalOrder();
    }

    private static IAiMove StartPositions() => ByRole(
        (MainTank, new(0f, -6f)), (OffTank, new(-3f, -2f)),
        (RegenHealer, new(-3f, 3f)), (ShieldHealer, new(3f, 3f)),
        (MeleeDpsA, new(-1.5f, 2f)), (MeleeDpsB, new(1.5f, 2f)),
        (PhysRangedDps, new(-3f, 4.5f)), (CasterDps, new(3f, 4.5f)));

    private IAiMove DropSpots(bool early) => ByRole(PerRole.All.Select(role =>
    {
        var (first, second) = DmuGravenImage3State.DropSpots(state.Arrows8[role]);
        return (role, early ? first : second);
    }).ToArray());

    private bool HoldsTrap(PartyRole role)
        => world.Party.Get(role) is { } member && (member.HasStatus(DmuConstants.StatusId.DoubleTroubleTrap) || member.ShowsStatus(DmuConstants.StatusId.DoubleTroubleTrap));

    private IAiMove TrapStacks() => ByRole(PerRole.All.Select(role =>
    {
        var side = role.IsDps() ? 1f : -1f;
        return (role, new Vector2(side * (HoldsTrap(role) ? 8f : 4.5f), 0f));
    }).ToArray());

    private static Vector2 SpreadSpot(PartyRole role) => role switch
    {
        MainTank => new(-4.2f, -4.6f), OffTank => new(3.7f, -4.6f),
        MeleeDpsA => new(-4.4f, 3.7f), MeleeDpsB => new(3.4f, 3.7f),
        PhysRangedDps => new(-8.5f, -8.5f), CasterDps => new(8.5f, -8.5f),
        RegenHealer => new(-8.5f, 8.5f), _ => new(8.5f, 8.5f),
    };

    private static IAiMove TeleportSpread() => ByRole(PerRole.All.Select(role => (role, SpreadSpot(role))).ToArray());

    private static Vector2 SpreadAnchor(PartyRole role) => role switch
    {
        MainTank => new(-8f, 7.5f), OffTank => new(7.5f, 7.5f),
        MeleeDpsA => new(-8f, -7.5f), MeleeDpsB => new(7f, -7.5f),
        RegenHealer => new(0f, 18.5f), ShieldHealer => new(18.5f, 0f),
        PhysRangedDps => new(-18.5f, 0f), _ => new(0f, -18.5f),
    };

    private IAiMove FinalMysteryMagic()
    {
        bool Safe(Vector2 p) => DmuMysteryMagic.SafeFromThunder(p, state.Thunder, margin: 1.5f);
        return ByRole(PerRole.All.Select(role =>
        {
            var anchor = state.Fire.Stacks ? role.IsDps() ? new Vector2(-7.5f, -7.5f) : new Vector2(6f, 6f) : SpreadAnchor(role);
            return (role, DmuMysteryMagic.NearestSafe(anchor, Safe, reach: 19f));
        }).ToArray());
    }

    private void FaceTheGaze()
    {
        var statue = state.AveMaria ? AveMariaStatue : IndolentWillStatue;
        foreach (var role in PerRole.All)
        {
            if (world.Party.Get(role) is not { } member || !world.Party.IsBotDriven(member)) continue;
            var toward = member.Placement().Face(statue).Rotation;
            member.TurnTo(state.AveMaria ? toward : toward + MathF.PI);
        }
    }
}
