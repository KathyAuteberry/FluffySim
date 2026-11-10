using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Tea.P2BjCc;
using AnoMech.Scenarios;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Tea.TeaConstants;

namespace AnoMech.Tests;

internal sealed class TeaP2BjCcLegacyAi(bool lightningThreeToOffTank) : IScenarioAi<TeaP2BjCcState>
{
    public string Name => lightningThreeToOffTank ? "NAUR (third lightning on OT)" : "NAUR";

    private const float EdgeRadius = 17.5f;
    private const float ClearOfDash = 7.5f;
    private const float EdgeGap = 3f;
    private const float Clearance = 1.9f;
    private const float PuddleMargin = 1f;
    private const float Touching = 1f;
    private const float PassSide = 2.6f;
    private const float RobotReach = 1f;
    private const float FlarethrowerAimNorthOfTornado = 10f;
    private const float ShieldAhead = 4.26f;
    private const float ShieldFacing = 40f;
    private const float LineupStaging = 3.4f;
    private const float ArenaEdge = 18.8f;
    private const float TornadoClearance = 11f;

    private static readonly Vector2 D = new(-13.75f, 0f);
    private static readonly Vector2 B = new(13.75f, -8.25f);
    private static readonly (float From, float Until, Vector2 Spot)[] SettlingTornadoes = [(53.2f, 58.0f, D), (82.9f, 87.7f, B), (112.65f, 117.45f, B)];
    private static readonly Vector2 LightningPartnerSpot = new(0f, -17f);
    private static readonly Vector2 LightningHolderSpot = new(3.8f, -15.2f);
    private static readonly Vector2 SpinCrusherBaitSpot = new(-2f, 17.5f);
    private static readonly Vector2 OffTankMissileSpot = new(-2.6f, 18f);
    private static readonly Vector2 RangedMissileSpot = new(1.6f, 18.3f);
    private static readonly Vector2 OffTankMinefieldSpot = new(-2.8f, 12.2f);
    private static readonly Vector2 RangedMinefieldSpot = new(1.4f, 12.4f);
    private static readonly Vector2 OffTankIceSpot = new(-12.6f, 9.1f);
    private static readonly Vector2 MainTankIceSpot = new(-11.5f, -11.7f);
    private static readonly Vector2 OutOfOffTankMinefield = new(-9f, 12.2f);
    private static readonly Vector2 OffTankAfterMines = new(1.8f, 11.1f);
    private static readonly Vector2 WestMeleeSpot = new(-0.9f, -4.5f);
    private static readonly Vector2 WestSecondMeleeSpot = new(-3.3f, -5.4f);
    private static readonly Vector2 WestHealerSpot = new(-3f, -1.9f);
    private static readonly Vector2 EastCasterSpot = new(6.2f, -3.6f);
    private static readonly Vector2 EastRangedSpot = new(7.9f, -0.4f);
    private static readonly Vector2 EastHealerSpot = new(5.6f, -1.2f);
    private static readonly Vector2 EastFlexSpot = new(8.4f, -4.6f);
    private static readonly Vector2 LightningHealerLineSpot = new(1.8f, -2f);
    private static readonly Vector2 OtherHealerLineSpot = new(0.5f, 1.5f);
    private static readonly Vector2 MainTankLineSpot = new(0f, -14.8f);
    private static readonly Vector2[] OutOfTheThirdTornado = [new(1.9f, -2f), new(4.8f, -0.2f), new(4.5f, 3.5f)];

    private static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] Supports = [MainTank, OffTank, RegenHealer, ShieldHealer];

    private static readonly float[][] EdgeBearings =
    [
        [46.5f, 219.7f, 270f, 90f, 58.6f, 35.4f, 230.4f, 133.3f],
        [353.9f, 175.1f, 316.1f, 106.2f, 6.1f, 341.1f, 289.4f, 145.8f],
        [0.3f, 180.6f, 321.3f, 122.8f, 17.5f, 346.8f, 222f, 148.7f],
        [12.2f, 197.4f, 256f, 74.1f, 25.9f, 353.7f, 238f, 165f],
    ];

    private TeaP2BjCcState state = null!;
    private SimWorld world = null!;
    private AiManager ai = null!;
    private readonly Dictionary<PartyRole, Vector2> edgeSpots = [];
    private readonly Dictionary<PartyRole, int> walks = [];
    private readonly Dictionary<PartyRole, int> orders = [];
    private readonly Dictionary<PartyRole, Plan> plans = [];
    private readonly List<Vector2> mineSpots = [];

    public void Run(TeaP2BjCcState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        ai = new AiManager(world);
        edgeSpots.Clear();
        walks.Clear();
        orders.Clear();
        plans.Clear();
        mineSpots.Clear();

        ai.Move(0.3f, TakeClockSpots);
        ai.Move(5.1f, LureRobotsToCentre, jitter: 0f);
        ai.Move(8.0f, () => ByRole((MainTank, ClockSpot(MainTank)), (OffTank, ClockSpot(OffTank))));

        world.Events.Add(25.3f, PickEdgeSpots);
        world.Events.Add(25.35f, RunToEdgeSpots);
        world.Events.Add(32.3f, ReturnToClockSpots);

        foreach (var dps in Dps)
            Pass(39.9f, dps, () => PartnerOf(dps));

        world.Events.Add(41.0f, TakeFirstStackSpots);
        world.Events.Add(41.6f, () =>
        {
            if (state.FirstLightning != CasterDps) Go(CasterDps, new Vector2(-10.5f, 4f));
        });
        world.Events.Add(45.9f, () => Go(OffTank, BesideCruiseChaser()));
        world.Events.Add(48.8f, () => Go(OffTank, OffTankMissileSpot));

        world.Events.Add(53.9f, LeaveFirstStacks);
        world.Events.Add(54.6f, () => Go(RegenHealer, ClearOfTornado(0, WestHealerSpot)));
        world.Events.Add(55.0f, () =>
        {
            if (state.FirstLightning != CasterDps) Go(CasterDps, EastCasterSpot);
        });
        world.Events.Add(55.1f, () => KeepClearOfTornado(0));
        world.Events.Add(61.95f, () =>
        {
            Go(OffTank, OffTankMinefieldSpot);
            Go(PhysRangedDps, RangedMinefieldSpot);
        });
        world.Events.Add(63.8f, FlexForEnumerations);
        world.Events.Add(64.0f, () =>
        {
            if (state.IceTank == MainTank) Go(MainTank, MainTankIceSpot);
        });
        world.Events.Add(64.3f, NoteMineSpots);
        world.Events.Add(64.4f, LeaveMinefields);
        ai.UseAction(65.8f, OffTank, 7531);
        ai.UseAction(65.9f, OffTank, job => (JobId)job switch
        {
            JobId.Paladin or JobId.Gladiator => 17u,
            JobId.Warrior or JobId.Marauder => 44u,
            JobId.DarkKnight => 3636u,
            JobId.Gunbreaker => 16148u,
            _ => 0u,
        });
        world.Events.Add(66.9f, () =>
        {
            if (state.IceTank != OffTank) PopMines();
        });
        world.Events.Add(69.3f, () =>
        {
            if (state.IceTank == OffTank) PopMines();
            if (state.IceTank == MainTank) Go(MainTank, LightningPartnerSpot);
        });
        Pass(69.4f, MeleeDpsB, () => RegenHealer);
        Pass(69.4f, CasterDps, () => ShieldHealer);
        Pass(state.IceTank == MainTank ? 71.9f : 69.4f, MeleeDpsA, () => MainTank);
        Pass(state.IceTank == OffTank ? 72.0f : 69.6f, PhysRangedDps, () => OffTank);

        world.Events.Add(74.3f, TakeSecondStackSpots);
        for (var t = 65.75f; t < 83f; t += 0.5f) world.Events.Add(t, StepOutOfPuddles);
        world.Events.Add(76.0f, () => Go(state.FirstWater, new Vector2(4.8f, -16.1f)));
        world.Events.Add(84.0f, () => Go(CasterDps, CasterShieldSpot()));
        world.Events.Add(84.0f, () => Go(OtherHealer(state.FirstWater), OtherHealerLine()));
        world.Events.Add(84.0f, () => Go(state.FirstWater, LightningHealerLine()));
        world.Events.Add(84.8f, () => Go(PhysRangedDps, RangedShieldSpot()));
        world.Events.Add(84.9f, () => KeepClearOfTornado(1));
        world.Events.Add(85.6f, BaitTheFlarethrower);
        world.Events.Add(92.6f, () => Go(MainTank, MainTankLineSpot));

        PassThree(96.8f, PhysRangedDps, PassSide);
        PassThree(97.0f, MeleeDpsA, -PassSide);
        PassThree(98.8f, CasterDps, PassSide + 1.2f);
        PassThree(99.0f, MeleeDpsB, -PassSide - 1.2f);

        world.Events.Add(103.2f, () =>
        {
            Go(MeleeDpsA, new Vector2(7.5f, -11.5f));
            Go(MeleeDpsB, new Vector2(7f, -7.7f));
        });
        world.Events.Add(104.2f, TakeThirdWaterSpots);
        world.Events.Add(105.6f, TakeThirdLightningSpots);
        if (!lightningThreeToOffTank) world.Events.Add(104.6f, () => DragCruiseChaserBehindGaol(125.1f));
        else world.Events.Add(113.4f, () => DragCruiseChaserBehindGaol(125.1f));

        world.Events.Add(113.1f, LeaveTheThirdTornado);
        world.Events.Add(113.4f, LineUpHolders);
        world.Events.Add(113.7f, () => KeepClearOfTornado(2));
        world.Events.Add(119.0f, StageJoiners);
        foreach (var (at, slot) in new[] { (120.8f, 0), (122.0f, 1), (123.2f, 2), (124.4f, 3) })
            world.Events.Add(at, () => JoinSlot(slot));
        world.Events.Add(125.3f, () => Go(OffTank, SlotApproach(OffTank), LineSlot(OffTank)));

        PlanAfterGavel();
        PlanSuperJump();
    }

    private static Vector2 ClockSpot(PartyRole role) => role switch
    {
        MainTank => new(0f, -3.2f),
        OffTank => new(0f, 3.4f),
        RegenHealer => new(-3f, 0f),
        ShieldHealer => new(3f, 0f),
        MeleeDpsA => new(2.3f, -2f),
        MeleeDpsB => new(-2.5f, -2.5f),
        PhysRangedDps => new(-2.1f, 2.4f),
        _ => new(2.3f, 2.4f),
    };

    private static PartyRole PartnerOf(PartyRole dps) => dps switch
    {
        MeleeDpsA => MainTank,
        MeleeDpsB => RegenHealer,
        PhysRangedDps => OffTank,
        _ => ShieldHealer,
    };

    private static PartyRole OtherHealer(PartyRole healer) => healer == RegenHealer ? ShieldHealer : RegenHealer;

    private IAiMove TakeClockSpots() => ByRole(PerRole.All.Select(r => (r, ClockSpot(r))).ToArray());

    private IAiMove LureRobotsToCentre() => ByRole(
        (MainTank, LureSpot(state.Objects.BruteJustice, MainTank, new Vector2(-0.5f, -0.3f))),
        (OffTank, LureSpot(state.Objects.CruiseChaser, OffTank, new Vector2(0.8f, 0.4f))));

    private Vector2 LureSpot(SimEnemy? boss, PartyRole tank, Vector2 bossTo)
    {
        if (boss is null || Member(tank) is not { } member) return ClockSpot(tank);
        var away = bossTo - Flat(boss.Position);
        return away.LengthSquared() < 0.01f
            ? ClockSpot(tank)
            : bossTo + Vector2.Normalize(away) * (boss.HitboxRadius + member.HitboxRadius + RobotReach);
    }

    private void PickEdgeSpots()
    {
        edgeSpots.Clear();
        var axis = ChakramAxisSeen();
        var dashes = state.Objects.Chakrams.Where(c => c.IsActive).Select(c => (From: Flat(c.Position), Along: Forward(c.Rotation))).ToList();
        var taken = new List<Vector2>();
        foreach (var role in PerRole.All)
        {
            var spot = EdgeSpotClearOfDashes(EdgeBearings[axis][(int)role], dashes, taken);
            edgeSpots[role] = spot;
            taken.Add(spot);
        }
    }

    private int ChakramAxisSeen()
    {
        if (state.Objects.Chakrams.FirstOrDefault(c => c.IsActive) is not { } chakram) return state.ChakramAxis;
        var at = Flat(chakram.Position);
        var bearing = (float.RadiansToDegrees(MathF.Atan2(at.X, -at.Y)) % 180f + 180f) % 180f;
        return (int)MathF.Round(bearing / 45f) % 4;
    }

    private static Vector2 EdgeSpotClearOfDashes(float bearing, List<(Vector2 From, Vector2 Along)> dashes, List<Vector2> taken)
    {
        for (var step = 0; step <= 40; step++)
        {
            var offset = (step + 1) / 2 * 2f * (step % 2 == 0 ? 1f : -1f);
            var spot = Polar(bearing + offset, EdgeRadius);
            if (dashes.All(d => MathF.Abs(Cross(d.Along, spot - d.From)) >= ClearOfDash) && taken.All(t => Vector2.Distance(t, spot) >= EdgeGap))
                return spot;
        }
        return Polar(bearing, EdgeRadius);
    }

    private void RunToEdgeSpots()
    {
        foreach (var role in PerRole.All)
            Go(role, edgeSpots.TryGetValue(role, out var spot) ? spot : ClockSpot(role));
    }

    private void ReturnToClockSpots()
    {
        foreach (var role in PerRole.All.OrderBy(r => Vector2.Distance(At(r), ClockSpot(r))))
            Go(role, ClockSpot(role));
    }

    private void TakeFirstStackSpots()
    {
        var water = state.FirstWater;
        var lightning = state.FirstLightning;
        Go(MainTank, LightningPartnerSpot);
        Go(OffTank, SpinCrusherBaitSpot);
        Go(water, D);
        Go(OtherHealer(water), water == ShieldHealer ? new Vector2(-12.5f, -4.5f) : new Vector2(-9f, -1f));
        if (lightning != MeleeDpsA) Go(MeleeDpsA, new Vector2(1.8f, -4.7f));
        if (lightning != MeleeDpsB) Go(MeleeDpsB, new Vector2(-2.1f, -5.3f));
        if (lightning != PhysRangedDps) Go(PhysRangedDps, new Vector2(-13.6f, 3.4f));
        Go(lightning, LightningRouteToMainTank(lightning));
    }

    private static Vector2[] LightningRouteToMainTank(PartyRole holder) => holder switch
    {
        CasterDps => [new(5.5f, -6f), LightningHolderSpot],
        PhysRangedDps => [new(-6f, 9f), new(-17.8f, 5f), new(-17.8f, -5.5f), new(-9f, -12.5f), new(3.8f, -12.5f), LightningHolderSpot],
        _ => [LightningHolderSpot],
    };

    private Vector2 BesideCruiseChaser()
    {
        if (state.Objects.CruiseChaser is not { } cruiseChaser) return SpinCrusherBaitSpot;
        var facing = Forward(cruiseChaser.Rotation);
        return Flat(cruiseChaser.Position) + new Vector2(facing.Y, -facing.X) * 4.5f;
    }

    private void LeaveFirstStacks()
    {
        var lightning = state.FirstLightning;
        Go(ShieldHealer, EastHealerSpot);
        Go(MeleeDpsA, WestMeleeSpot);
        Go(MeleeDpsB, WestSecondMeleeSpot);
        if (lightning == CasterDps) Go(CasterDps, EastCasterSpot);
        Go(PhysRangedDps, lightning == PhysRangedDps
            ? [new Vector2(9.5f, -11f), new Vector2(9.5f, 9f), RangedMissileSpot]
            : [RangedMissileSpot]);
    }

    private void FlexForEnumerations()
    {
        if (!EnumerationsNeedFlex()) return;
        Go(MeleeDpsA, new Vector2(3.5f, -7.5f), EastFlexSpot);
    }

    private bool EnumerationsNeedFlex()
    {
        var marked = Dps.Where(r => state.Enumerations.Contains(r)).ToList();
        return marked.Count == 2 && (marked.All(r => r is MeleeDpsA or MeleeDpsB) || marked.All(r => r is PhysRangedDps or CasterDps));
    }

    private void NoteMineSpots()
    {
        mineSpots.Clear();
        mineSpots.AddRange(world.Children.OfType<SimEnemy>()
            .Where(e => e.IsActive && e.IsCasting && e.CastActionId == ActionId.HiddenMinefield && e.CastTargetLocation is not null)
            .Select(e => Flat(e.CastTargetLocation!.Value)));
        if (mineSpots.Count == 0) mineSpots.AddRange([OffTankMinefieldSpot, RangedMinefieldSpot]);
    }

    private void LeaveMinefields()
    {
        Go(PhysRangedDps, EnumerationsNeedFlex() ? WestMeleeSpot : EastRangedSpot);
        Go(OffTank, state.IceTank == OffTank ? OffTankIceSpot : OutOfOffTankMinefield);
    }

    private void PopMines()
    {
        if (Member(OffTank) is not { } offTank) return;
        var from = Flat(offTank.Position);
        var order = mineSpots.OrderBy(m => Vector2.Distance(m, from)).ToList();
        GoAvoiding(OffTank, [.. order, OffTankAfterMines], null, 0f, null);
    }

    private void TakeSecondStackSpots()
    {
        var iceNorthWest = state.IceTank == MainTank;
        Go(MeleeDpsA, iceNorthWest ? new Vector2(-2f, -4.2f) : new Vector2(-5f, -7f));
        Go(MeleeDpsB, iceNorthWest ? new Vector2(0.3f, -5.6f) : new Vector2(-2.4f, -5.1f));
        var water = Holder(StatusId.CompressedWater);
        var other = OtherHealer(state.FirstWater);
        foreach (var role in new[] { other, CasterDps, PhysRangedDps })
        {
            var spot = role == water ? B : role == other ? new Vector2(10f, -5.6f) : role == CasterDps ? new Vector2(12.4f, -3.4f) : new Vector2(14.8f, -4.8f);
            world.Events.Add(role == PhysRangedDps ? 1.6f : 0f, () => Go(role, spot));
        }
        Go(OffTank, FacingSpotForShield());
    }

    private Vector2 FacingSpotForShield()
    {
        if (state.Objects.CruiseChaser is not { } cruiseChaser || Member(OffTank) is not { } offTank) return new Vector2(1.1f, 7.2f);
        var stop = cruiseChaser.HitboxRadius + offTank.HitboxRadius + RobotReach;
        return Flat(cruiseChaser.Position) + Polar(ShieldFacing, 1f) * (stop - 0.9f);
    }

    private (Vector2 At, Vector2 Facing) ExpectedShield()
    {
        if (state.Objects.CruiseChaser is not { } cruiseChaser) return (new Vector2(0.3f, 8.1f), Polar(ShieldFacing, 1f));
        var facing = Forward(cruiseChaser.Rotation);
        return (Flat(cruiseChaser.Position) + facing * ShieldAhead, facing);
    }

    private Vector2 RangedShieldSpot() => ShieldSpot(-20f, 5.2f, CasterShieldSpot());

    private Vector2 CasterShieldSpot() => ShieldSpot(20f, 6.8f, null);

    private Vector2 ShieldSpot(float degrees, float distance, Vector2? besides)
    {
        var (at, facing) = ExpectedShield();
        var keepClear = new List<Vector2> { LightningHealerLine(), OtherHealerLine(), FacingSpotForShield() };
        if (besides is { } other) keepClear.Add(other);
        foreach (var (turn, reach) in from turn in new[] { 0f, -8f, 8f, -16f, 16f } from reach in new[] { 0f, 1.5f, -1f, 3f } select (turn, reach))
        {
            var spot = at + Rotate(facing, degrees + turn) * (distance + reach);
            if (spot.Length() <= ArenaEdge && keepClear.All(k => Vector2.Distance(k, spot) >= 2.8f)
                && Vector2.Distance(spot, TornadoSpot(1)) >= TornadoClearance) return spot;
        }
        return at + Rotate(facing, degrees) * distance;
    }

    private Vector2 LightningHealerLine() => ClearOfTornado(1, LightningHealerLineSpot);

    private Vector2 OtherHealerLine() => ClearOfTornado(1, OtherHealerLineSpot);

    private void BaitTheFlarethrower()
    {
        var settled = SettlingTornadoes[1].Until - world.Events.Elapsed;
        if (settled > 0f && Vector2.Distance(FlarethrowerBaitSpot(), TornadoSpot(1)) < TornadoClearance)
            world.Events.Add(settled + 0.1f, BaitTheFlarethrower);
        else Go(MainTank, FlarethrowerBaitSpot());
    }

    private Vector2 FlarethrowerBaitSpot()
    {
        if (state.Objects.BruteJustice is not { } bruteJustice) return new Vector2(1.1f, -10.4f);
        var from = Flat(bruteJustice.Position);
        var tornado = TornadoSpot(1);
        var aim = tornado - from;
        var toward = aim.LengthSquared() > 0.01f ? Rotate(Vector2.Normalize(aim), -FlarethrowerAimNorthOfTornado) : new Vector2(1f, 0f);
        return from + toward * 1.1f;
    }

    private void PassThree(float time, PartyRole dps, float side)
        => world.Events.Add(time, () =>
        {
            if (PassThreePartner(dps) is not { } support) return;
            PassNow(dps, support, side, () => SideOf(support, side));
        });

    private PartyRole? PassThreePartner(PartyRole dps)
    {
        if (state.NisiOnDps)
            return NisiOf(dps) is { } held ? Supports.Where(s => JudgmentOf(s) == held).Cast<PartyRole?>().FirstOrDefault() : null;
        return JudgmentOf(dps) is { } own ? Supports.Where(s => NisiOf(s) == own).Cast<PartyRole?>().FirstOrDefault() : null;
    }

    private Vector2 SideOf(PartyRole support, float side) => At(support) + new Vector2(side, 0f);

    private PartyRole? Vulnerable() => new[] { OtherHealer(state.FirstWater), CasterDps, PhysRangedDps }
        .Where(r => Member(r) is { } m && m.ShowsStatus(StatusId.WaterResistanceDownIIP2)).Cast<PartyRole?>().FirstOrDefault();

    private void TakeThirdWaterSpots()
    {
        var water = Holder(StatusId.CompressedWater);
        var vulnerable = Vulnerable();
        var stack = new[] { OtherHealer(state.FirstWater), CasterDps, PhysRangedDps }.Where(r => r != vulnerable).OrderBy(r => r == water ? 0 : 1).ToList();
        for (var k = 0; k < stack.Count; k++)
        {
            var role = stack[k];
            world.Events.Add(0.7f * k, () => Go(role, role == water ? B : new Vector2(11.8f, -4.6f)));
        }
    }

    private void TakeThirdLightningSpots()
    {
        var healer = Holder(StatusId.CompressedLightning) ?? state.FirstWater;
        if (Vulnerable() is { } vulnerable) Go(vulnerable, new Vector2(9f, 6.2f));
        if (lightningThreeToOffTank)
        {
            Go(OffTank, new Vector2(-13f, 8.5f));
            world.Events.Add(2.4f, () => Go(healer, new Vector2(-10.2f, 9.6f)));
        }
        else Go(healer, new Vector2(6f, 8.6f));
    }

    private void DragCruiseChaserBehindGaol(float until)
    {
        var started = false;
        void Look()
        {
            var now = world.Events.Elapsed;
            if (now >= until || state.Objects.CruiseChaser is not { } cruiseChaser || Member(OffTank) is not { } offTank) return;
            var pillar = GaolSpot();
            var stop = cruiseChaser.HitboxRadius + offTank.HitboxRadius + RobotReach;
            var want = pillar + new Vector2(-1.35f, -0.4f);
            var at = Flat(cruiseChaser.Position);
            if (!started)
            {
                started = true;
                Go(OffTank, pillar + new Vector2(-5.25f, 1.2f), pillar + new Vector2(-3.25f, -6.6f));
                world.Events.Add(4.5f, Look);
                return;
            }
            if (Vector2.Distance(at, want) > 0.6f)
            {
                var away = want - at;
                var spot = want + Vector2.Normalize(away) * stop;
                if (spot.Length() > ArenaEdge) spot = Vector2.Normalize(spot) * ArenaEdge;
                Go(OffTank, spot);
            }
            world.Events.Add(0.8f, Look);
        }
        Look();
    }

    private static float SlotOffset(NisiLetter letter) => letter switch
    {
        NisiLetter.Alpha => 3.2f,
        NisiLetter.Gamma => 6.6f,
        NisiLetter.Beta => 10f,
        _ => 13.4f,
    };

    private static int SlotOrder(NisiLetter letter) => letter switch
    {
        NisiLetter.Alpha => 0,
        NisiLetter.Gamma => 1,
        NisiLetter.Beta => 2,
        _ => 3,
    };

    private Vector2 GaolSpot()
        => state.Objects.GelidGaol is { IsActive: true } gaol ? Flat(gaol.Position)
            : state.Objects.Tornadoes.FirstOrDefault() is { IsActive: true } first ? Flat(first.Position)
            : D;

    private Vector2 LineSlot(PartyRole role) => JudgmentOf(role) is { } letter ? GaolSpot() + new Vector2(SlotOffset(letter), 0f) : GaolSpot() + new Vector2(8f, 3f);

    private Vector2 SlotApproach(PartyRole role) => LineSlot(role) - new Vector2(0f, LineupStaging);

    private Vector2 SlotStaging(PartyRole role) => LineSlot(role) + new Vector2(0f, LineupStaging);

    private bool HoldsOwnJudgment(PartyRole role) => JudgmentOf(role) is { } letter && NisiOf(role) == letter;

    private void LeaveTheThirdTornado()
    {
        var tornado = TornadoSpot(2);
        var k = 0;
        foreach (var role in PerRole.All.Where(r => Member(r) is not null && Vector2.Distance(At(r), tornado) < TornadoClearance).OrderBy(r => Vector2.Distance(At(r), tornado)))
        {
            var spot = role switch
            {
                MeleeDpsA => new Vector2(2.5f, -16.4f),
                MeleeDpsB => new Vector2(-1.5f, -6.5f),
                _ => OutOfTheThirdTornado[Math.Min(k++, OutOfTheThirdTornado.Length - 1)],
            };
            GoAvoiding(role, [ClearOfTornado(2, spot)], null, 0f, null);
        }
    }

    private Vector2 TornadoSpot(int k) => state.Objects.Tornadoes.ElementAtOrDefault(k) is { } rage ? Flat(rage.Position) : SettlingTornadoes[k].Spot;

    private Vector2 ClearOfTornado(int k, Vector2 spot)
    {
        var tornado = TornadoSpot(k);
        var away = spot - tornado;
        if (away.Length() >= TornadoClearance) return spot;
        var outward = away.LengthSquared() > 1e-4f ? Vector2.Normalize(away) : tornado.LengthSquared() > 1e-4f ? -Vector2.Normalize(tornado) : Vector2.UnitX;
        var clear = tornado + outward * TornadoClearance;
        return clear.Length() > ArenaEdge ? Vector2.Normalize(clear) * ArenaEdge : clear;
    }

    private void KeepClearOfTornado(int k)
    {
        var now = world.Events.Elapsed;
        if (state.Objects.Tornadoes.Count <= k)
        {
            if (now < SettlingTornadoes[k].Until - 1f) world.Events.Add(0.1f, () => KeepClearOfTornado(k));
            return;
        }
        foreach (var role in PerRole.All)
        {
            if (Member(role) is not { } member) continue;
            var to = plans.TryGetValue(role, out var plan) && plan.End > now ? plan.Destination : Flat(member.Position);
            var clear = ClearOfTornado(k, to);
            if (Vector2.Distance(clear, to) > 0.1f) GoAvoiding(role, [clear], null, 0f, null);
        }
    }

    private void LineUpHolders()
    {
        var holders = PerRole.All.Where(r => r != OffTank && HoldsOwnJudgment(r)).OrderBy(r => JudgmentOf(r) is { } l ? SlotOrder(l) : 9).ToList();
        for (var k = 0; k < holders.Count; k++)
        {
            var role = holders[k];
            world.Events.Add(1.4f * k, () => Go(role, SlotApproach(role), LineSlot(role)));
        }
    }

    private void StageJoiners()
    {
        var joiners = PerRole.All.Where(r => r != OffTank && !HoldsOwnJudgment(r)).OrderBy(r => JudgmentOf(r) is { } l ? SlotOrder(l) : 9).ToList();
        for (var k = 0; k < joiners.Count; k++)
        {
            var role = joiners[k];
            world.Events.Add(0.8f * k, () => Go(role, SlotStaging(role)));
        }
    }

    private void JoinSlot(int order)
    {
        foreach (var role in PerRole.All)
            if (role != OffTank && !HoldsOwnJudgment(role) && JudgmentOf(role) is { } letter && SlotOrder(letter) == order)
                Go(role, LineSlot(role));
    }

    private void PlanAfterGavel()
    {
        world.Events.Add(135.5f, () =>
        {
            Go(MainTank, new Vector2(-15.1f, -4.9f));
            Go(OffTank, new Vector2(-15.4f, -3.9f));
            Go(RegenHealer, new Vector2(-9.1f, 7.2f));
            Go(ShieldHealer, new Vector2(-7.2f, -1.7f));
            Go(MeleeDpsA, new Vector2(-7.2f, 3.1f));
            Go(MeleeDpsB, new Vector2(-6.3f, 0.2f));
            Go(PhysRangedDps, new Vector2(-3.5f, 1.5f));
            Go(CasterDps, new Vector2(-8.4f, 5.4f));
        });
    }

    private void PlanSuperJump()
    {
        world.Events.Add(151.5f, () => Go(PhysRangedDps, new Vector2(17.5f, 0f)));
        world.Events.Add(156.0f, () =>
        {
            Go(RegenHealer, new Vector2(-15.5f, 6.5f));
            Go(ShieldHealer, new Vector2(-17.4f, 2.6f));
            Go(MeleeDpsA, new Vector2(-16.4f, 4.9f));
            Go(MeleeDpsB, new Vector2(-14.6f, 8.6f));
            Go(CasterDps, new Vector2(-17.2f, -5.6f));
        });
        world.Events.Add(159.3f, () => Go(PhysRangedDps, new Vector2(17.4f, -4.6f)));
        world.Events.Add(166.5f, () =>
        {
            Go(PhysRangedDps, new Vector2(-3.5f, 1.5f));
            Go(RegenHealer, new Vector2(-9.1f, 7.2f));
            Go(ShieldHealer, new Vector2(-7.2f, -1.7f));
            Go(MeleeDpsA, new Vector2(-7.2f, 3.1f));
            Go(MeleeDpsB, new Vector2(-6.3f, 0.2f));
            Go(CasterDps, new Vector2(-8.4f, 5.4f));
        });
    }

    private sealed record Plan(Vector2 From, float Start, List<(Vector2 At, float Eta)> Points)
    {
        public float End => Points.Count > 0 ? Points[^1].Eta : Start;
        public Vector2 Destination => Points.Count > 0 ? Points[^1].At : From;

        public Vector2 PositionAt(float time)
        {
            if (time <= Start) return From;
            var (previous, previousTime) = (From, Start);
            foreach (var (at, eta) in Points)
            {
                if (time <= eta) return eta - previousTime < 1e-4f ? at : Vector2.Lerp(previous, at, (time - previousTime) / (eta - previousTime));
                (previous, previousTime) = (at, eta);
            }
            return previous;
        }
    }

    private void Pass(float time, PartyRole role, Func<PartyRole?> partner, float side = 0f)
        => world.Events.Add(time, () =>
        {
            if (partner() is { } other) PassNow(role, other, side, null);
        });

    private void PassNow(PartyRole role, PartyRole other, float side, Func<Vector2>? backTo)
    {
        if (Member(other) is not { } target) return;
        var at = Flat(target.Position);
        var stops = side != 0f ? new[] { at + new Vector2(side, 0f), at } : new[] { at };
        GoAvoiding(role, stops, other, MaxWait, seconds =>
        {
            if (backTo is not null) world.Events.Add(seconds + 0.5f, () => Go(role, backTo()));
        });
    }

    private void Go(PartyRole role, params Vector2[] stops) => GoAvoiding(role, stops, null, NearASettlingTornado(role) ? 0f : MaxWait, null);

    private bool NearASettlingTornado(PartyRole role)
        => Member(role) is { } m && Enumerable.Range(0, SettlingTornadoes.Length).Any(k => world.Events.Elapsed >= SettlingTornadoes[k].From - 0.5f
            && world.Events.Elapsed <= SettlingTornadoes[k].Until && Vector2.Distance(Flat(m.Position), TornadoSpot(k)) < TornadoClearance);

    private void GoAvoiding(PartyRole role, Vector2[] stops, PartyRole? partner, float maxWait, Action<float>? walking)
    {
        if (Member(role) is not { } member || stops.Length == 0) return;
        var order = orders[role] = orders.GetValueOrDefault(role) + 1;
        var now = world.Events.Elapsed;
        var from = Flat(member.Position);
        var puddles = Puddles();
        stops = stops.Select(s => OutOf(puddles, s)).ToArray();
        var path = Route(from, stops, StandingSpots(role, partner, now), puddles);
        for (var wait = 0f; wait <= maxWait; wait += 0.25f)
        {
            if (CrossesWalkers(role, partner, from, path, now + wait)) continue;
            if (wait <= 0f)
            {
                var seconds = WalkPath(role, path);
                walking?.Invoke(seconds);
            }
            else
            {
                plans[role] = new Plan(from, now + wait, Timed(from, path, now + wait));
                world.Events.Add(wait, () =>
                {
                    if (orders.GetValueOrDefault(role) != order || Member(role) is not { } later) return;
                    var seconds = WalkPath(role, Route(Flat(later.Position), stops, StandingSpots(role, partner, world.Events.Elapsed), Puddles()));
                    walking?.Invoke(seconds);
                });
            }
            return;
        }
        var anyway = WalkPath(role, path);
        walking?.Invoke(anyway);
    }

    private const float MaxWait = 2.5f;

    private List<Vector2> StandingSpots(PartyRole role, PartyRole? except, float now)
    {
        var spots = new List<Vector2>();
        foreach (var other in PerRole.All)
        {
            if (other == role || other == except || Member(other) is not { } member) continue;
            if (plans.TryGetValue(other, out var plan) && plan.End > now)
            {
                if (plan.Start > now) spots.Add(Flat(member.Position));
                spots.Add(plan.Destination);
            }
            else spots.Add(Flat(member.Position));
        }
        return spots;
    }

    private bool CrossesWalkers(PartyRole role, PartyRole? except, Vector2 from, IReadOnlyList<Vector2> path, float start)
    {
        var now = world.Events.Elapsed;
        var mine = new Plan(from, start, Timed(from, path, start));
        foreach (var other in PerRole.All)
        {
            if (other == role || other == except || !plans.TryGetValue(other, out var plan) || plan.End <= now || Member(other) is null) continue;
            var until = MathF.Max(mine.End, plan.End);
            for (var t = MathF.Max(now, start); t <= until; t += 0.1f)
                if (Vector2.Distance(mine.PositionAt(t), plan.PositionAt(t)) < Clearance) return true;
        }
        return false;
    }

    private static List<(Vector2 At, float Eta)> Timed(Vector2 from, IReadOnlyList<Vector2> path, float start)
    {
        var timed = new List<(Vector2 At, float Eta)>();
        var eta = start;
        foreach (var point in path)
        {
            eta += Vector2.Distance(from, point) / AiManager.RunSpeed;
            timed.Add((point, eta));
            from = point;
        }
        return timed;
    }

    private float WalkPath(PartyRole role, IReadOnlyList<Vector2> points)
    {
        if (Member(role) is not { } member) return 0f;
        var walk = walks[role] = walks.GetValueOrDefault(role) + 1;
        var now = world.Events.Elapsed;
        var from = Flat(member.Position);
        var timed = Timed(from, points, now);
        plans[role] = new Plan(from, now, timed);
        for (var k = 0; k < timed.Count; k++)
        {
            var to = new Vector3(timed[k].At.X, 0f, timed[k].At.Y);
            var legStart = k == 0 ? 0f : timed[k - 1].Eta - now;
            if (legStart <= 0f) member.MoveTo(to, speed: AiManager.RunSpeed);
            else
                world.Events.Add(legStart, () =>
                {
                    if (member.IsAlive() && walks.GetValueOrDefault(role) == walk) member.MoveTo(to, speed: AiManager.RunSpeed);
                });
        }
        return timed.Count > 0 ? timed[^1].Eta - now : 0f;
    }

    private static List<Vector2> Route(Vector2 from, IReadOnlyList<Vector2> stops, IReadOnlyList<Vector2> obstacles, IReadOnlyList<Obstacle> puddles)
    {
        var path = new List<Vector2>();
        var at = from;
        if (OutOf(puddles, from) is var exit && exit != from)
        {
            path.Add(exit);
            at = exit;
        }
        foreach (var stop in stops)
        {
            path.AddRange(PlanLeg(at, stop, obstacles, puddles));
            at = stop;
        }
        return path;
    }

    private List<Obstacle> Puddles()
        => world.Children.OfType<SimEventObject>().Where(o => o.IsActive)
            .Select(o => new Obstacle(Flat(o.Position), PuddleRadius(o.EObjRowId) + PuddleMargin))
            .Where(p => p.Radius > PuddleMargin).ToList();

    private static float PuddleRadius(uint eobjId) => eobjId switch
    {
        EObjId.VoidzoneEarthMissile => Geometry.MissilePuddleRadius,
        EObjId.VoidzoneIceSmall => Geometry.SmallIceRadius,
        EObjId.VoidzoneIceLarge => Geometry.LargeIceRadius,
        _ => 0f,
    };

    private static Vector2 OutOf(IReadOnlyList<Obstacle> puddles, Vector2 spot)
    {
        foreach (var puddle in puddles)
        {
            var away = spot - puddle.At;
            var distance = away.Length();
            if (distance >= puddle.Radius) continue;
            spot = puddle.At + (distance > 0.01f ? away / distance : Vector2.UnitX) * puddle.Radius;
        }
        return spot;
    }

    private void StepOutOfPuddles()
    {
        var puddles = Puddles();
        if (puddles.Count == 0) return;
        var now = world.Events.Elapsed;
        foreach (var role in PerRole.All)
        {
            if (Member(role) is not { } member || !world.Party.IsBotDriven(member)) continue;
            var at = Flat(member.Position);
            if (OutOf(puddles, at) == at) continue;
            var stops = plans.TryGetValue(role, out var plan) ? plan.Points.Where(p => p.Eta > now).Select(p => p.At).ToList() : [];
            if (stops.Count > 0 && ClearOf(puddles, OutOf(puddles, at), stops)) continue;
            Go(role, stops.Count > 0 ? [.. stops] : [at]);
        }
    }

    private static bool ClearOf(IReadOnlyList<Obstacle> puddles, Vector2 from, IReadOnlyList<Vector2> stops)
    {
        foreach (var stop in stops)
        {
            if (OutOf(puddles, stop) != stop || !SegmentClear(from, stop, puddles)) return false;
            from = stop;
        }
        return true;
    }

    private const float Cell = 0.5f;
    private static readonly int Cells = (int)(2f * ArenaEdge / Cell) + 1;

    private static List<Vector2> PlanLeg(Vector2 from, Vector2 to, IReadOnlyList<Vector2> obstacles, IReadOnlyList<Obstacle> puddles)
    {
        var blocking = obstacles.Where(o => Vector2.Distance(o, to) >= Clearance && Vector2.Distance(o, from) > Touching)
            .Select(o => new Obstacle(o, Math.Clamp(Vector2.Distance(o, from) - Cell, Touching, Clearance)))
            .Concat(puddles)
            .ToList();
        if (SegmentClear(from, to, blocking)) return [to];
        var blocked = new bool[Cells, Cells];
        for (var i = 0; i < Cells; i++)
        for (var j = 0; j < Cells; j++)
            blocked[i, j] = CellPoint(i, j).Length() > ArenaEdge;
        foreach (var o in blocking)
        {
            var reach = (int)MathF.Ceiling(o.Radius / Cell);
            var (ci, cj) = CellOf(o.At);
            for (var i = Math.Max(0, ci - reach); i <= Math.Min(Cells - 1, ci + reach); i++)
            for (var j = Math.Max(0, cj - reach); j <= Math.Min(Cells - 1, cj + reach); j++)
                if (Vector2.Distance(CellPoint(i, j), o.At) < o.Radius) blocked[i, j] = true;
        }
        var start = CellOf(from);
        var goal = CellOf(to);
        blocked[start.I, start.J] = false;
        blocked[goal.I, goal.J] = false;
        var cost = new Dictionary<(int I, int J), float> { [start] = 0f };
        var came = new Dictionary<(int I, int J), (int I, int J)>();
        var open = new PriorityQueue<(int I, int J), float>();
        open.Enqueue(start, 0f);
        var found = false;
        while (open.TryDequeue(out var cell, out _))
        {
            if (cell == goal)
            {
                found = true;
                break;
            }
            for (var di = -1; di <= 1; di++)
            for (var dj = -1; dj <= 1; dj++)
            {
                if (di == 0 && dj == 0) continue;
                var next = (I: cell.I + di, J: cell.J + dj);
                if (next.I < 0 || next.J < 0 || next.I >= Cells || next.J >= Cells || blocked[next.I, next.J]) continue;
                var step = cost[cell] + (di != 0 && dj != 0 ? 1.4142f : 1f) * Cell;
                if (cost.TryGetValue(next, out var known) && known <= step) continue;
                cost[next] = step;
                came[next] = cell;
                open.Enqueue(next, step + Vector2.Distance(CellPoint(next.I, next.J), to));
            }
        }
        if (!found || start == goal) return [to];
        var cells = new List<Vector2>();
        for (var c = goal; c != start; c = came[c]) cells.Add(CellPoint(c.I, c.J));
        cells.Reverse();
        cells[^1] = to;
        var path = new List<Vector2>();
        var anchor = from;
        var k = 0;
        while (k < cells.Count)
        {
            var farthest = k;
            for (var m = cells.Count - 1; m > k; m--)
                if (SegmentClear(anchor, cells[m], blocking))
                {
                    farthest = m;
                    break;
                }
            path.Add(cells[farthest]);
            anchor = cells[farthest];
            k = farthest + 1;
        }
        return path;
    }

    private static (int I, int J) CellOf(Vector2 p)
        => (Math.Clamp((int)MathF.Round((p.X + ArenaEdge) / Cell), 0, Cells - 1), Math.Clamp((int)MathF.Round((p.Y + ArenaEdge) / Cell), 0, Cells - 1));

    private static Vector2 CellPoint(int i, int j) => new(i * Cell - ArenaEdge, j * Cell - ArenaEdge);

    private sealed record Obstacle(Vector2 At, float Radius);

    private static bool SegmentClear(Vector2 from, Vector2 to, IReadOnlyList<Obstacle> obstacles)
    {
        var leg = to - from;
        var lengthSquared = leg.LengthSquared();
        foreach (var o in obstacles)
        {
            var t = lengthSquared < 1e-6f ? 0f : Math.Clamp(Vector2.Dot(o.At - from, leg) / lengthSquared, 0f, 1f);
            if (Vector2.Distance(from + leg * t, o.At) < o.Radius) return false;
        }
        return true;
    }

    private SimCharacter? Member(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private Vector2 At(PartyRole role) => Member(role) is { } member ? Flat(member.Position) : Vector2.Zero;

    private PartyRole? Holder(ushort statusId)
        => PerRole.All.Where(r => Member(r) is { } m && m.ShowsStatus(statusId)).Cast<PartyRole?>().FirstOrDefault();

    private NisiLetter? NisiOf(PartyRole role)
        => Member(role) is { } member ? Enum.GetValues<NisiLetter>().Where(l => member.ShowsStatus(StatusId.FinalDecreeNisi(l))).Cast<NisiLetter?>().FirstOrDefault() : null;

    private NisiLetter? JudgmentOf(PartyRole role)
        => Member(role) is { } member ? Enum.GetValues<NisiLetter>().Where(l => member.ShowsStatus(StatusId.FinalJudgment(l))).Cast<NisiLetter?>().FirstOrDefault() : null;

    private static IAiMove ByRole(params (PartyRole Role, Vector2 Spot)[] spots)
    {
        var coords = new Vector2?[8];
        foreach (var (role, spot) in spots) coords[(int)role] = spot;
        return AiMove.Create(coords).NaturalOrder();
    }

    private static Vector2 Polar(float degrees, float radius) => Flat(Geometry.OnCompass(degrees, radius));

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        var r = float.DegreesToRadians(degrees);
        var (sin, cos) = MathF.SinCos(r);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private static Vector2 Forward(float rotation) => new(MathF.Sin(rotation), MathF.Cos(rotation));

    private static Vector2 Flat(Vector3 v) => new(v.X, v.Z);
}
