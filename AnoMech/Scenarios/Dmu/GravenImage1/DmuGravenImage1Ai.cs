using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dmu.GravenImage1;
public sealed class DmuGravenImage1Ai : IScenarioAi<DmuGravenImage1State>
{
    public string Name => "Yan Flash";
    public string Group => "Mana";

    private static readonly Vector2 PulseWaveStatue = new(0f, -44f);
    private const float PulseWaveDistance = 13f;
    private const float ArenaReach = 18f;

    private DmuGravenImage1State state = null!;
    private SimWorld world = null!;

    public void Run(DmuGravenImage1State stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);
        ai.Move(0.2f, PullPositions);
        ai.UseAction(11.84f, OffTank, Enmity.ProvokeActionId, () => state.Objects.Kefka);
        ai.Move(19.0f, OffTankTakesNorth);
        ai.Move(30.0f, BeforeMysteryMagic);
        ai.Move(32.4f, MysteryMagicApproach, arrivalTime: 34.7f);
        ai.Move(35.5f, MysteryMagicSpots, sprint: true);
        ai.Move(38.6f, WaveCannonLine, arrivalTime: 42.0f);
        ai.Move(42.5f, TowerSoaks, arrivalTime: 45.4f);
        ai.Move(46.2f, TrapStacks, arrivalTime: 49.2f);
        ai.Move(50.3f, MysteryMagic2Dodge, sprint: true);
    }

    private static IAiMove ByRole(params (PartyRole Role, Vector2 Spot)[] spots)
    {
        var coords = new Vector2?[8];
        foreach (var (role, spot) in spots) coords[(int)role] = spot;
        return AiMove.Create(coords).NaturalOrder();
    }

    private static IAiMove PullPositions() => ByRole(
        (MainTank, new(0f, -6.5f)), (OffTank, new(-5f, 0.5f)),
        (RegenHealer, new(-3f, 6f)), (ShieldHealer, new(3f, 6f)),
        (MeleeDpsA, new(-2f, 3f)), (MeleeDpsB, new(2f, 3f)),
        (PhysRangedDps, new(-5f, 8f)), (CasterDps, new(5f, 8f)));

    private static IAiMove OffTankTakesNorth() => ByRole((OffTank, new(0f, -6.5f)), (MainTank, new(-6f, -1f)));

    private bool Tethered(PartyRole role) => role.IsDps() == state.DpsTethered;
    private IAiMove BeforeMysteryMagic() => ByRole(PerRole.All.Select(role =>
        (role, new Vector2(role.IsDps() ? 4f : -4f, Tethered(role) ? -10f : 0f))).ToArray());
    private Vector2 FinalSpot(PartyRole role)
    {
        var safeNorthwest = state.Ice1.RealSouthwestNortheast;
        if (state.Fire1.Stacks)
            return role.IsDps()
                ? safeNorthwest ? new(7.5f, 6f) : new(6.5f, -5.5f)
                : safeNorthwest ? new(-4.5f, -5.5f) : new(-4f, 5.5f);
        return (role, safeNorthwest) switch
        {
            (MainTank, true) => new(-2f, -10f), (MainTank, false) => new(-1f, 9.5f),
            (OffTank, true) => new(-7.5f, -1.5f), (OffTank, false) => new(-8f, 1f),
            (RegenHealer, true) => new(-11.5f, -10f), (RegenHealer, false) => new(-10.5f, 11f),
            (ShieldHealer, true) => new(-18.5f, -1.5f), (ShieldHealer, false) => new(-18f, 2.5f),
            (MeleeDpsA, true) => new(2f, 8.5f), (MeleeDpsA, false) => new(1f, -9.5f),
            (MeleeDpsB, true) => new(8.5f, 1.5f), (MeleeDpsB, false) => new(10.5f, -1f),
            (PhysRangedDps, true) => new(11.5f, 11.5f), (PhysRangedDps, false) => new(10f, -10f),
            (CasterDps, true) => new(18.5f, 1.5f), _ => new(18f, -2f),
        };
    }
    private static Vector2 BeforeKnockback(Vector2 final)
    {
        var away = Vector2.Normalize(final - PulseWaveStatue);
        var start = final - away * PulseWaveDistance;
        return start.Length() > ArenaReach ? Vector2.Normalize(start) * ArenaReach : start;
    }

    private IAiMove MysteryMagicApproach() => ByRole(PerRole.All.Select(role =>
        (role, Tethered(role) ? BeforeKnockback(FinalSpot(role)) : FinalSpot(role))).ToArray());

    private IAiMove MysteryMagicSpots() => ByRole(PerRole.All.Select(role => (role, FinalSpot(role))).ToArray());

    private static readonly (PartyRole Role, float X)[] Line =
    [
        (ShieldHealer, -19.5f), (RegenHealer, -14f), (OffTank, -9f), (MainTank, -4f),
        (MeleeDpsA, 3.5f), (MeleeDpsB, 8.5f), (PhysRangedDps, 13.5f), (CasterDps, 18.5f),
    ];

    private static IAiMove WaveCannonLine() => ByRole(Line.Select(l => (l.Role, new Vector2(l.X, 0f))).ToArray());
    private IAiMove TowerSoaks()
    {
        var spots = new List<(PartyRole, Vector2)>();
        foreach (var dps in new[] { false, true })
        {
            var side = Line.Where(l => l.Role.IsDps() == dps).ToList();
            var beamed = side.Where(l => state.WaveCannonTargets.Contains(l.Role)).OrderBy(l => l.X).ToList();
            var free = side.Where(l => !state.WaveCannonTargets.Contains(l.Role)).OrderBy(l => l.X).ToList();
            for (var i = 0; i < free.Count && i < beamed.Count; i++) spots.Add((free[i].Role, new(beamed[i].X, 0f)));
            foreach (var b in beamed) spots.Add((b.Role, new(b.X, 6f)));
        }
        return ByRole(spots.ToArray());
    }
    private IAiMove TrapStacks() => ByRole(PerRole.All.Select(role =>
    {
        var east = role.IsDps() ? 1f : -1f;
        var holder = role == state.TrapSupport || role == state.TrapDps;
        return (role, new Vector2(east * (holder ? 7.5f : 5f), 0f));
    }).ToArray());

    private IAiMove MysteryMagic2Dodge()
    {
        bool Safe(Vector2 p) => DmuMysteryMagic.SafeFromIce(p, state.Ice2) && DmuMysteryMagic.SafeFromThunder(p, state.Thunder2, margin: 1.5f)
                                && Math.Abs(p.X) > 1.5f && Math.Abs(p.Y) > 1.5f;
        return ByRole(PerRole.All.Select(role =>
        {
            var at = world.Party.Get(role) is { } m ? new Vector2(m.Position.X, m.Position.Z) : Vector2.Zero;
            return (role, DmuMysteryMagic.NearestSafe(at, Safe));
        }).ToArray());
    }
}
