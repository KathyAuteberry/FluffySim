using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dsr.P4Eyes.DsrP4EyesConstants;

namespace AnoMech.Scenarios.Dsr.P4Eyes;

public sealed class DsrP4EyesAi : IScenarioAi<DsrP4EyesState>
{
    public string Name => "T/H red, DPS blue";
    public string Group => "Elemental";

    public enum Corner { NorthWest, SouthWest, SouthEast, NorthEast }

    private const float Step = 0.5f;
    private static readonly Vector2 AlphinaudStack = new(0f, 8.8f);
    private static readonly Vector2 Estinien = Vector2.Zero;
    private static readonly Vector2 BlueEye = new(-10f, 0f);
    private static readonly PartyRole[] Swappers = [PartyRole.OffTank, PartyRole.ShieldHealer, PartyRole.MainTank, PartyRole.RegenHealer];
    private static readonly Corner[] GroupOneSnake = [Corner.NorthWest, Corner.SouthWest, Corner.SouthEast, Corner.NorthEast];
    private static readonly Corner[] GroupTwoSnake = [Corner.NorthEast, Corner.SouthEast, Corner.SouthWest, Corner.NorthWest];

    private static readonly Dictionary<PartyRole, Vector2> EyeSpread = new()
    {
        [PartyRole.OffTank] = new(6.5f, 2.3f),
        [PartyRole.ShieldHealer] = new(6.5f, -1.7f),
        [PartyRole.CasterDps] = new(5f, -5.6f),
        [PartyRole.PhysRangedDps] = new(5f, 6.6f),
        [PartyRole.MainTank] = new(-5.9f, -1.8f),
        [PartyRole.RegenHealer] = new(-5.9f, 2.3f),
        [PartyRole.MeleeDpsA] = new(-3.3f, -5.6f),
        [PartyRole.MeleeDpsB] = new(-4.2f, 6.6f),
    };

    private static readonly Dictionary<PartyRole, Vector2> GildedOrbSpot = new()
    {
        [PartyRole.OffTank] = new(14.4f, -1f),
        [PartyRole.ShieldHealer] = new(14.4f, 1f),
        [PartyRole.MainTank] = new(-14.4f, -1f),
        [PartyRole.RegenHealer] = new(-14.4f, 1f),
    };

    private static readonly Dictionary<PartyRole, PartyRole> PassPartner = new()
    {
        [PartyRole.OffTank] = PartyRole.CasterDps,
        [PartyRole.ShieldHealer] = PartyRole.PhysRangedDps,
        [PartyRole.MainTank] = PartyRole.MeleeDpsA,
        [PartyRole.RegenHealer] = PartyRole.MeleeDpsB,
    };

    private static readonly Dictionary<PartyRole, Vector2> AzureOrb = new()
    {
        [PartyRole.CasterDps] = new(10f, -7f),
        [PartyRole.PhysRangedDps] = new(10f, 7f),
        [PartyRole.MeleeDpsA] = new(-10f, -7f),
        [PartyRole.MeleeDpsB] = new(-10f, 7f),
    };

    private static readonly Dictionary<PartyRole, Corner> HomeCorner = new()
    {
        [PartyRole.CasterDps] = Corner.NorthEast,
        [PartyRole.PhysRangedDps] = Corner.SouthEast,
        [PartyRole.MeleeDpsA] = Corner.NorthWest,
        [PartyRole.MeleeDpsB] = Corner.SouthWest,
    };

    private static readonly Dictionary<PartyRole, Vector2> BlueEyeHuddle = new()
    {
        [PartyRole.OffTank] = BlueEye + new Vector2(-1f, -0.6f),
        [PartyRole.ShieldHealer] = BlueEye + new Vector2(1f, -0.6f),
        [PartyRole.MainTank] = BlueEye + new Vector2(-1f, 0.6f),
        [PartyRole.RegenHealer] = BlueEye + new Vector2(1f, 0.6f),
        [PartyRole.CasterDps] = BlueEye + new Vector2(0.6f, -1.2f),
        [PartyRole.PhysRangedDps] = BlueEye + new Vector2(0.6f, 1.2f),
        [PartyRole.MeleeDpsA] = BlueEye + new Vector2(-0.6f, -1.2f),
        [PartyRole.MeleeDpsB] = BlueEye + new Vector2(-0.6f, 1.2f),
    };

    private const float AzureWaitDistance = 3.1f;
    private const float AzurePopDistance = 2.6f;

    private DsrP4EyesState state = null!;
    private SimWorld world = null!;
    private readonly Dictionary<PartyRole, Corner>[] swapPlans = [[], [], [], []];
    private readonly Dictionary<PartyRole, Corner> firstDiveCorners = [];

    public void Run(DsrP4EyesState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        foreach (var plan in swapPlans) plan.Clear();
        firstDiveCorners.Clear();
        foreach (var (role, spot) in GildedOrbSpot)
            AnoMech.Core.DiagnosticLog.Info($"[Eyes assignment] {role} yellow pair at={spot}; centre handoff to={PassPartner[role]}");
        foreach (var (role, spot) in AzureOrb)
            AnoMech.Core.DiagnosticLog.Info($"[Eyes assignment] {role} blue orb at={spot}");
        var ai = new AiManager(world);

        ScheduleRescue(ai);
        ai.Move(0.3f, () => AiMove.Create(Stacked(AlphinaudStack)).NaturalOrder());
        ai.Move(18.0f, () => AiMove.Create(SupportsToRedEyeDpsToBlueEye()).NaturalOrder(), jitter: 0f);
        Every(ai, 35.5f, 40.5f, TradeWrongColoursOnEstinienThenPreposition);
        Every(ai, 41.2f, 46.5f, SupportsHandRedToDpsAfterGildedPop);
        Every(ai, 47.0f, 58.5f, DpsToBoxCornersAfterAzurePop);
        for (var dive = 0; dive < DsrP4EyesState.Dives - 1; dive++)
        {
            var d = dive;
            world.Events.Add(DsrP4EyesScenario.DiveTimes[d] + 0.2f, () => PlanSwaps(d));
            Every(ai, DsrP4EyesScenario.DiveTimes[d] + 0.3f, DsrP4EyesScenario.DiveTimes[d] + 4.8f, () => SwapIntoDivedCorners(d));
        }
        world.Events.Add(DsrP4EyesScenario.DiveTimes[0] + 0.2f, RememberFirstDiveCorners);
    }

    private static void ScheduleRescue(AiManager ai)
    {
        var pairs = new[]
        {
            new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer },
            new[] { PartyRole.MeleeDpsA, PartyRole.MeleeDpsB },
            new[] { PartyRole.PhysRangedDps, PartyRole.CasterDps },
            new[] { PartyRole.MainTank, PartyRole.OffTank },
        };
        ai.Move(110f, () => AiMove.Create(Stacked(new(-11, 1))).NaturalOrder(), jitter: 0, sprint: true);
        for (var i = 0; i < pairs.Length; i++)
        {
            var pair = pairs[i];
            var at = 92f + DsrP4RescueSequence.BrightwingTimes[i];
            var drop = i == 1 ? new Vector2(-16.5f, 2.5f) : new Vector2(-5, 0);
            var dropDelay = i == 3 ? 3f : .4f;
            ai.Move(at - 3, () => AiMove.Create(RescuePair(pair, new(-11, 7))).NaturalOrder(), jitter: 0, sprint: true);
            ai.Move(at + dropDelay, () => AiMove.Create(RescuePair(pair, drop)).NaturalOrder(), jitter: 0, sprint: true);
            ai.Move(at + 6, () => AiMove.Create(RescuePair(pair, new(-11, 1))).NaturalOrder(), jitter: 0, sprint: true);
        }
    }

    private static Vector2?[] RescuePair(PartyRole[] pair, Vector2 centre)
    {
        var spots = new Vector2?[8];
        spots[(int)pair[0]] = centre + new Vector2(1.2f, 0);
        spots[(int)pair[1]] = centre - new Vector2(1.2f, 0);
        return spots;
    }

    private static void Every(AiManager ai, float from, float to, Func<Vector2?[]> spots)
    {
        for (var t = from; t <= to + 0.001f; t += Step)
            ai.Move(t, () => AiMove.Create(spots()).NaturalOrder(), jitter: 0f, sprint: true);
    }

    private static Vector2?[] SupportsToRedEyeDpsToBlueEye()
    {
        var spots = new Vector2?[8];
        foreach (var (role, spot) in EyeSpread) spots[(int)role] = spot;
        return spots;
    }

    private static bool WantsRed(PartyRole role) => Swappers.Contains(role);

    private bool HasWrongColour(PartyRole role) => state.HoldsRed(role) != WantsRed(role);

    private Vector2?[] TradeWrongColoursOnEstinienThenPreposition()
    {
        var spots = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>())
            spots[(int)role] = HasWrongColour(role) ? Estinien : WantsRed(role) ? GildedOrbSpot[role] : AzureWait(role);
        return spots;
    }

    private Vector2?[] SupportsHandRedToDpsAfterGildedPop()
    {
        var spots = new Vector2?[8];
        foreach (var (melee, ranged) in PassPartner)
        {
            if (!state.HoldsRed(melee)) spots[(int)melee] = BlueEyeHuddle[melee];
            else if (Popped(melee)) spots[(int)melee] = Estinien;
            spots[(int)ranged] = state.HoldsRed(ranged) ? AzurePop(ranged) : Estinien;
        }
        return spots;
    }

    private Vector2?[] DpsToBoxCornersAfterAzurePop()
    {
        var spots = new Vector2?[8];
        foreach (var (melee, ranged) in PassPartner)
        {
            spots[(int)melee] = state.HoldsRed(melee) ? Estinien : BlueEyeHuddle[melee];
            if (!state.HoldsRed(ranged)) spots[(int)ranged] = Estinien;
            else spots[(int)ranged] = Popped(ranged) ? CornerSpot(HomeCorner[ranged]) : AzurePop(ranged);
        }
        return spots;
    }

    private bool Popped(PartyRole role) => world.Party.Get(role) is { } member && member.HasStatus(StatusId.FireResistanceDownII);

    private void RememberFirstDiveCorners()
    {
        foreach (var role in state.DiveTargets[0])
            if (world.Party.Get(role) is { } member)
                firstDiveCorners[role] = NearestCorner(member.Position);
    }

    private void PlanSwaps(int dive)
    {
        var plan = swapPlans[dive];
        plan.Clear();
        var dived = state.DiveTargets[dive]
            .Select(r => world.Party.Get(r))
            .Where(m => m != null)
            .Select(m => NearestCorner(m!.Position))
            .ToList();
        if (dived.Count == 0) return;
        var (groupOne, groupTwo) = SwappersFor(dive);
        if (groupOne is { } one) plan[one] = GroupOneSnake.First(dived.Contains);
        if (groupTwo is { } two && dived.Count > 1)
            plan[two] = GroupTwoSnake.First(c => dived.Contains(c) && !plan.ContainsValue(c));
    }

    private (PartyRole?, PartyRole?) SwappersFor(int dive)
    {
        switch (dive)
        {
            case 0: return (PartyRole.RegenHealer, PartyRole.ShieldHealer);
            case 1: return (PartyRole.MainTank, PartyRole.OffTank);
            default:
            {
                var firsts = firstDiveCorners.OrderBy(kv => kv.Key).Select(kv => (PartyRole?)kv.Key).ToList();
                return (firsts.ElementAtOrDefault(0), firsts.ElementAtOrDefault(1));
            }
        }
    }

    private Vector2?[] SwapIntoDivedCorners(int dive)
    {
        var spots = new Vector2?[8];
        foreach (var (role, corner) in swapPlans[dive])
            spots[(int)role] = CornerSpot(corner);
        foreach (var role in state.DiveTargets[dive].Where(r => !state.HoldsRed(r)))
            spots[(int)role] = BlueEyeHuddle[role];
        return spots;
    }

    public static Vector2 CornerSpot(Corner corner) => corner switch
    {
        Corner.NorthWest => new Vector2(-15f, -5f),
        Corner.SouthWest => new Vector2(-15f, 5f),
        Corner.SouthEast => new Vector2(-5f, 5f),
        _ => new Vector2(-5f, -5f),
    };

    private static Corner NearestCorner(Vector3 at) =>
        Enum.GetValues<Corner>().OrderBy(c => Vector2.Distance(CornerSpot(c), new Vector2(at.X, at.Z))).First();

    private static Vector2 TowardsCentreLine(Vector2 orb, float distance) => orb - new Vector2(0f, MathF.Sign(orb.Y) * distance);

    private static Vector2 AzureWait(PartyRole role) => TowardsCentreLine(AzureOrb[role], AzureWaitDistance);

    private static Vector2 AzurePop(PartyRole role) => TowardsCentreLine(AzureOrb[role], AzurePopDistance);


    private static Vector2?[] Stacked(Vector2 centre) =>
        Enumerable.Range(0, 8).Select(i => (Vector2?)(centre + new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1)))).ToArray();
}
