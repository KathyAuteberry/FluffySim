using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Tea.TeaConstants;

namespace AnoMech.Scenarios.Tea.P2BjCc;

public sealed class TeaP2BjCcAi : IScenarioAi<TeaP2BjCcState>
{
    public string Name => "Healer-first Nisi";
    public string Group => "Mana";

    private const float EdgeRadius = 17.5f;
    private const float ClearOfDash = 7.5f;
    private const float EdgeGap = 3f;
    private const float Clearance = 1.9f;
    private const float PuddleMargin = 1f;
    private const float Touching = 1f;
    private const float RobotReach = 1f;
    private const float FlarethrowerAimNorthOfTornado = 10f;
    private const float ShieldFacing = 0f;
    private const float LineupStaging = 3.4f;
    private const float ArenaEdge = 18.8f;
    private const float TornadoClearance = 11f;

    private static readonly Vector2 D = new(-13.5f, 0f);
    private static readonly Vector2 OuterEast = new(13.7f, 0f);
    private static readonly Vector2 InnerEast = new(5.8f, 0f);
    private static readonly (float From, float Until, Vector2 Spot)[] SettlingTornadoes = [(53.2f, 58.0f, D), (82.9f, 87.7f, OuterEast), (112.65f, 117.45f, InnerEast)];
    private static readonly Vector2 LightningPartnerSpot = new(0f, -17f);
    private static readonly Vector2 LightningHolderSpot = new(3.8f, -15.2f);
    private static readonly Vector2 SpinCrusherBaitSpot = new(-2f, 17.5f);
    private static readonly Vector2 OffTankMissileSpot = new(0f, 18f);
    private static readonly Vector2 RangedMissileSpot = new(9f, 15.588f);
    private static readonly Vector2 OffTankMinefieldSpot = new(0f, 12.2f);
    private static readonly Vector2 RangedMinefieldSpot = new(6.1f, 10.566f);
    private static readonly Vector2 OffTankIceSpot = new(-12.6f, 9.1f);
    private static readonly Vector2 MainTankIceSpot = new(-11.5f, -11.7f);
    private static readonly Vector2 OutOfOffTankMinefield = new(-9f, 12.2f);
    private static readonly Vector2 OffTankAfterMines = new(1.8f, 11.1f);
    private static readonly Vector2 MainTankLineSpot = new(0f, -14.8f);

    private static readonly PartyRole[] Dps = [MeleeDpsA, PhysRangedDps, MeleeDpsB, CasterDps];
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
    private PartyRole? secondWaterHolder;
    private PartyRole? thirdWaterHolder;
    private bool thirdWaterVulnerableStaged;
    private PartyRole[] thirdWaterOthers = [];
    private readonly Dictionary<PartyRole, Vector2> edgeSpots = [];
    private readonly Dictionary<PartyRole, int> walks = [];
    private readonly Dictionary<PartyRole, int> orders = [];
    private readonly Dictionary<PartyRole, Plan> plans = [];
    private readonly List<Vector2> mineSpots = [];
    private readonly Dictionary<PartyRole, (PartyRole Partner, float Until)> passContacts = [];
    private readonly Dictionary<PartyRole, ObstacleField> personalObstacles = [];

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
        passContacts.Clear();
        personalObstacles.Clear();
        foreach (var role in PerRole.All) Member(role)?.SetMovementObstacles(null);
        world.Events.Add(90f, KeepNisiClearance);
        secondWaterHolder = null;
        thirdWaterHolder = null;
        thirdWaterVulnerableStaged = false;
        thirdWaterOthers = [];

        ai.Move(0.3f, TakeClockSpots);
        ai.Move(5.1f, LureRobotsToCentre, jitter: 0f);
        ai.Move(8.0f, () => ByRole((MainTank, ClockSpot(MainTank)), (OffTank, ClockSpot(OffTank))));

        world.Events.Add(25.3f, PickEdgeSpots);
        world.Events.Add(25.35f, RunToEdgeSpots);
        world.Events.Add(32.3f, ReturnToClockSpots);

        foreach (var dps in Dps)
            Pass(dps == MeleeDpsB ? 48.4f : 39.9f, dps, () => PartnerOf(dps));

        world.Events.Add(41.0f, TakeFirstStackSpots);
        world.Events.Add(45.0f, () => Go(MeleeDpsB, BesideCruiseChaser() + new Vector2(3f, 0f)));
        world.Events.Add(45.9f, DodgeSpinCrusher);
        world.Events.Add(48.7f, ReturnD2AfterFirstPass);
        world.Events.Add(49.6f, () => Go(OffTank, OffTankMissileSpot));

        world.Events.Add(53.9f, LeaveFirstStacks);
        world.Events.Add(61.95f, () =>
        {
            Go(OffTank, OffTankMinefieldSpot);
            Go(PhysRangedDps, RangedMinefieldSpot);
        });
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
        Pass(69.4f, PhysRangedDps, () => RegenHealer);
        Pass(69.4f, CasterDps, () => ShieldHealer);
        Pass(state.IceTank == MainTank ? 71.9f : 69.4f, MeleeDpsA, () => MainTank);
        Pass(state.IceTank == OffTank ? 72.0f : 69.6f, MeleeDpsB, () => OffTank);

        world.Events.Add(74.3f, TakeSecondStackSpots);
        for (var t = 65.75f; t < 83f; t += 0.5f) world.Events.Add(t, StepOutOfPuddles);
        world.Events.Add(83.7f, LeaveSecondStackSpots);
        world.Events.Add(84.0f, () => Go(RegenHealer, new Vector2(-6f, -2f)));
        world.Events.Add(84.0f, () => Go(ShieldHealer, new Vector2(-6f, 2f)));
        world.Events.Add(85.6f, BaitTheFlarethrower);
        world.Events.Add(92.6f, () => Go(MainTank, MainTankLineSpot));

        world.Events.Add(92.4f, StageThirdPass);
        world.Events.Add(93.8f, () => PassThirdSupport(RegenHealer));
        world.Events.Add(95.8f, () => PassThirdSupport(ShieldHealer));
        world.Events.Add(99.0f, () => PassThirdSupport(MainTank));
        world.Events.Add(101.0f, () => PassThirdSupport(OffTank));

        world.Events.Add(105.0f, StageThirdWaterVulnerable);
        world.Events.Add(106.0f, TakeThirdWaterSpots);
        world.Events.Add(105.6f, TakeThirdLightningSpots);
        world.Events.Add(116.0f, () => DragCruiseChaserBehindGaol(125.1f));

        world.Events.Add(113.1f, LeaveTheThirdTornado);
        world.Events.Add(113.4f, LineUpHolders);
        world.Events.Add(119.0f, StageJoiners);
        foreach (var (at, slot) in new[] { (120.8f, 0), (122.0f, 1), (123.2f, 2), (124.4f, 3) })
            world.Events.Add(at, () => JoinSlot(slot));
        world.Events.Add(125.3f, () => Go(OffTank, SlotApproach(OffTank), LineSlot(OffTank)));

        PlanAfterGavel();
        foreach (var tank in new[] { MainTank, OffTank })
            ai.UseAction(150f, tank, job => (JobId)job switch
            {
                JobId.Paladin or JobId.Gladiator => 3542u,
                JobId.Warrior or JobId.Marauder => 3551u,
                JobId.DarkKnight => 7393u,
                JobId.Gunbreaker => 16161u,
                _ => 0u,
            });
        PlanSuperJump();
    }

    private static Vector2 ClockSpot(PartyRole role) => role switch
    {
        MainTank => new(0f, -3.2f),
        OffTank => new(0f, 3.4f),
        RegenHealer => new(-3f, 0f),
        ShieldHealer => new(3f, 0f),
        MeleeDpsA => new(2.3f, -2f),
        PhysRangedDps => new(-2.5f, -2.5f),
        MeleeDpsB => new(-2.1f, 2.4f),
        _ => new(2.3f, 2.4f),
    };

    private static PartyRole PartnerOf(PartyRole dps) => TeaManaP2Assignments.FirstPairSupport(dps);

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

    private void MoveWaterParty(IReadOnlyList<PartyRole> members, PartyRole holder, Vector2 centre)
    {
        Go(holder, centre);
        Vector2[] offsets = [new(-3f, -2.5f), new(3f, -2.5f), new(0f, 3.5f)];
        var others = members.Where(r => r != holder).ToArray();
        for (var i = 0; i < others.Length; i++) Go(others[i], centre + offsets[i]);
    }

    private void ReturnD2AfterFirstPass()
    {
        var letter = NisiOf(MeleeDpsB);
        if (letter is null || NisiOf(OffTank) != letter)
        {
            if (world.Events.Elapsed < 51f) world.Events.Add(.1f, ReturnD2AfterFirstPass);
            return;
        }
        if (state.FirstLightning == MeleeDpsB) Go(MeleeDpsB, new Vector2(5.5f, 0f), LightningHolderSpot);
        else
        {
            var others = FirstWaterDps().ToList();
            Vector2[] offsets = [new(-3f, -2.5f), new(3f, -2.5f), new(0f, 3.5f)];
            Go(MeleeDpsB, D + offsets[others.IndexOf(MeleeDpsB)]);
        }
    }

    private void StageThirdPass()
    {
        var line = TeaManaP2Assignments.ThirdPassLine;
        for (var i = 0; i < line.Count; i++)
        {
            var role = line[i];
            Go(role, new Vector2(0f, -14f + i * 5.2f));
        }
        Go(RegenHealer, new Vector2(-6f, -2f));
        Go(ShieldHealer, new Vector2(-6f, 2f));
    }

    private void PassThirdSupport(PartyRole support)
    {
        var dps = Dps.FirstOrDefault(d => PassThreePartner(d) == support);
        if (PassThreePartner(dps) != support) return;
        var side = support.IsTank() ? 3f : -3f;
        PassNow(dps, support, side, () => SideOf(support, side));
    }

    private void TakeFirstStackSpots()
    {
        Go(MainTank, LightningPartnerSpot);
        Go(OffTank, SpinCrusherBaitSpot);
        if (state.FirstWater == RegenHealer) Go(RegenHealer, D);
        else Go(ShieldHealer, new Vector2(3f, 4f), new Vector2(-9f, 4f), D);
        var water = FirstWaterDps();
        Go(water[0], new Vector2(-8f, -8f), new Vector2(-17f, -6f), D + new Vector2(-3f, -2.5f));
        Go(water[1], new Vector2(-6f, -5f), D + new Vector2(3f, -2.5f));
        Go(water[2], new Vector2(3f, 7f), new Vector2(-7f, 8f), D + new Vector2(0f, 3.5f));
        Go(state.FirstLightning, LightningRouteToMainTank(state.FirstLightning));
    }

    private PartyRole[] FirstWaterDps()
        => new[] { PhysRangedDps, MeleeDpsA, CasterDps, MeleeDpsB }.Where(r => r != state.FirstLightning).ToArray();

    private static Vector2[] LightningRouteToMainTank(PartyRole holder) => holder switch
    {
        CasterDps => [new(6f, -8f), LightningHolderSpot],
        PhysRangedDps => [new(-2f, -10f), LightningHolderSpot],
        MeleeDpsB => [new(-6f, 9f), new(-17.8f, 5f), new(-17.8f, -5.5f), new(-9f, -12.5f), new(3.8f, -12.5f), LightningHolderSpot],
        _ => [LightningHolderSpot],
    };

    private void DodgeSpinCrusher()
        => GoAvoiding(OffTank, [BesideCruiseChaser()], null, 0f, null);

    private Vector2 BesideCruiseChaser()
    {
        if (state.Objects.CruiseChaser is not { } cruiseChaser) return SpinCrusherBaitSpot;
        var facing = Forward(cruiseChaser.Rotation);
        return Flat(cruiseChaser.Position) + new Vector2(facing.Y, -facing.X) * 4.5f;
    }

    private void LeaveFirstStacks()
    {
        var water = FirstWaterDps();
        GoAvoiding(water[0], [new Vector2(-7f, -5.5f), new Vector2(-3.5f, -5.5f)], null, 0f, null);
        GoAvoiding(water[1], [new Vector2(3.5f, -2.5f)], null, 0f, null);
        GoAvoiding(water[2], [new Vector2(-5f, 6f), new Vector2(3.5f, 5.5f)], null, 0f, null);
        GoAvoiding(state.FirstLightning, [new Vector2(8f, -10f), new Vector2(8f, 3f)], null, 0f, null);
        world.Events.Add(.5f, () => GoAvoiding(RegenHealer, [new Vector2(-2.5f, -1.5f)], null, 0f, null));
        world.Events.Add(.8f, () => GoAvoiding(ShieldHealer, [new Vector2(-2.5f, 2f)], null, 0f, null));
        world.Events.Add(3f, () => Go(PhysRangedDps, RangedMissileSpot));
    }

    private void FlexForEnumerations()
    {
        var (west, east) = TeaManaP2Assignments.EnumerationParties(state.Enumerations);
        var westDps = west.Single(r => r.IsDps());
        var eastMarked = east.Single(state.Enumerations.Contains);
        Go(RegenHealer, new Vector2(-7f, -2f));
        Go(ShieldHealer, new Vector2(-7f, 2f));
        Go(westDps, new Vector2(-4f, 0f));
        Go(eastMarked, new Vector2(4f, 0f));
        var others = east.Where(r => r != eastMarked).ToArray();
        Go(others[0], new Vector2(7f, -2f));
        Go(others[1], new Vector2(7f, 2f));
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
        FlexForEnumerations();
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
        if (Holder(StatusId.CompressedWater) is not { } water) return;
        secondWaterHolder = water;
        var others = TeaManaP2Assignments.Dps.Where(r => r != water).ToArray();
        MoveToSecondWaterAfterPass(water, OuterEast);
        Vector2[] offsets = [new(-3f, -2.5f), new(3f, -2.5f), new(0f, 3.5f)];
        for (var i = 0; i < others.Length; i++) MoveToSecondWaterAfterPass(others[i], OuterEast + offsets[i]);
        MoveTankAfterSecondPass(MainTank, LightningPartnerSpot);
        Go(RegenHealer, LightningHolderSpot);
        Go(ShieldHealer, new Vector2(-5f, 0f));
        MoveTankAfterSecondPass(OffTank, FacingSpotForShield());
    }

    private bool PairHasPassed(PartyRole dps)
        => NisiOf(dps) is { } letter && NisiOf(PartnerOf(dps)) == letter;

    private void MoveToSecondWaterAfterPass(PartyRole role, Vector2 destination)
    {
        if (PairHasPassed(role))
        {
            Go(role, destination);
            return;
        }
        if (world.Events.Elapsed > 80f) return;
        PassNow(role, PartnerOf(role), 0f, null);
        world.Events.Add(.25f, () => MoveToSecondWaterAfterPass(role, destination));
    }

    private void MoveTankAfterSecondPass(PartyRole tank, Vector2 destination)
    {
        var dps = tank == MainTank ? MeleeDpsA : MeleeDpsB;
        if (PairHasPassed(dps)) Go(tank, destination);
        else if (world.Events.Elapsed < 80f) world.Events.Add(.25f, () => MoveTankAfterSecondPass(tank, destination));
    }

    private void LeaveSecondStackSpots()
    {
        if (secondWaterHolder is not { } holder) return;
        var others = TeaManaP2Assignments.Dps.Where(r => r != holder).ToArray();
        GoAvoiding(others[0], [new Vector2(-4f, 0f)], null, 0f, null);
        GoAvoiding(holder, [new Vector2(-1f, 3f)], null, 0f, null);
        GoAvoiding(others[1], [new Vector2(16f, -10f), new Vector2(8f, -11f), new Vector2(-2f, -5f)], null, 0f, null);
        GoAvoiding(others[2], [new Vector2(10f, 10f), new Vector2(0f, 8f)], null, 0f, null);
    }

    private Vector2 FacingSpotForShield()
    {
        if (state.Objects.CruiseChaser is not { } cruiseChaser || Member(OffTank) is not { } offTank) return new Vector2(1.1f, 7.2f);
        var stop = cruiseChaser.HitboxRadius + offTank.HitboxRadius + RobotReach;
        return Flat(cruiseChaser.Position) + Polar(ShieldFacing, 1f) * (stop - 0.9f);
    }

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

    private PartyRole? PassThreePartner(PartyRole dps)
    {
        if (state.NisiOnDps)
            return NisiOf(dps) is { } held ? Supports.Where(s => JudgmentOf(s) == held).Cast<PartyRole?>().FirstOrDefault() : null;
        return JudgmentOf(dps) is { } own ? Supports.Where(s => NisiOf(s) == own).Cast<PartyRole?>().FirstOrDefault() : null;
    }

    private Vector2 SideOf(PartyRole support, float side) => At(support) + new Vector2(side, 0f);

    private PartyRole? Vulnerable() => Dps
        .Where(r => Member(r) is { } m && m.ShowsStatus(StatusId.WaterResistanceDownIIP2)).Cast<PartyRole?>().FirstOrDefault();

    private void StageThirdWaterVulnerable()
    {
        if (thirdWaterVulnerableStaged || Vulnerable() is not { } vulnerable) return;
        var transferred = state.NisiOnDps
            ? NisiOf(vulnerable) is { } held && Supports.Any(s => JudgmentOf(s) == held && NisiOf(s) == held)
            : NisiOf(vulnerable) is { } received && received == JudgmentOf(vulnerable);
        if (!transferred)
        {
            if (world.Events.Elapsed < 109f) world.Events.Add(.1f, StageThirdWaterVulnerable);
            return;
        }
        thirdWaterVulnerableStaged = true;
        Go(vulnerable, new Vector2(-8f, At(vulnerable).Y), new Vector2(-8f, 8f), new Vector2(-5f, 8f));
    }

    private void TakeThirdWaterSpots()
    {
        if (Holder(StatusId.CompressedWater) is not { } water || Vulnerable() is not { } vulnerable) return;
        var members = TeaManaP2Assignments.WaterParty(3, state.FirstWater, state.FirstLightning, vulnerable);
        thirdWaterHolder = water;
        thirdWaterOthers = members.Where(r => r != water).ToArray();
        if (!thirdWaterVulnerableStaged) StageThirdWaterVulnerable();
        MoveWaterParty(members, water, InnerEast);
    }

    private void TakeThirdLightningSpots()
    {
        var healer = Holder(StatusId.CompressedLightning) ?? RegenHealer;
        Go(healer, new Vector2(-13.5f, -1.5f));
        Go(OtherHealer(healer), new Vector2(-13.5f, 1.5f));
        Go(MainTank, new Vector2(0f, -13f));
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
            var want = pillar + new Vector2(0f, 5.5f);
            var at = Flat(cruiseChaser.Position);
            if (!started)
            {
                started = true;
                Go(OffTank, pillar + new Vector2(8f, 11f), pillar + new Vector2(0f, 12f));
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

    private Vector2 GaolSpot()
        => state.Objects.GelidGaol is { IsActive: true } gaol ? Flat(gaol.Position)
            : state.Objects.Tornadoes.FirstOrDefault() is { IsActive: true } first ? Flat(first.Position)
            : D;

    private Vector2 LineSlot(PartyRole role)
    {
        var support = role.IsDps()
            ? TeaManaP2Assignments.FinalSupportLine.FirstOrDefault(s => JudgmentOf(s) == JudgmentOf(role))
            : role;
        var index = TeaManaP2Assignments.FinalSupportLine.ToList().IndexOf(support);
        return GaolSpot() + new Vector2(1.5f, -3.5f - 3.2f * (3 - index));
    }

    private Vector2 SlotApproach(PartyRole role) => LineSlot(role) + new Vector2(LineupStaging, 0f);

    private Vector2 SlotStaging(PartyRole role) => LineSlot(role) + new Vector2(LineupStaging * 2f, 0f);

    private void LeaveTheThirdTornado()
    {
        if (thirdWaterHolder is not { } holder || thirdWaterOthers.Length != 3) return;
        GoAvoiding(thirdWaterOthers[0], [new Vector2(0f, 12f)], null, 0f, null);
        GoAvoiding(thirdWaterOthers[1], [new Vector2(8f, -13f)], null, 0f, null);
        GoAvoiding(thirdWaterOthers[2], [new Vector2(9f, 12f)], null, 0f, null);
        world.Events.Add(.6f, () => GoAvoiding(holder, [new Vector2(-6f, 0f)], null, 0f, null));
    }

    private Vector2 TornadoSpot(int k) => state.Objects.Tornadoes.ElementAtOrDefault(k) is { } rage ? Flat(rage.Position) : SettlingTornadoes[k].Spot;

    private void LineUpHolders()
    {
        foreach (var role in TeaManaP2Assignments.FinalSupportLine.Where(r => r != OffTank))
            Go(role, SlotApproach(role), LineSlot(role));
    }

    private void StageJoiners()
    {
        foreach (var role in TeaManaP2Assignments.Dps)
            Go(role, SlotStaging(role));
    }

    private void JoinSlot(int order)
    {
        var support = TeaManaP2Assignments.FinalSupportLine[order];
        foreach (var role in TeaManaP2Assignments.Dps)
            if (JudgmentOf(role) == JudgmentOf(support)) Go(role, LineSlot(role));
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
            Go(PhysRangedDps, new Vector2(-6.3f, 0.2f));
            Go(MeleeDpsB, new Vector2(-3.5f, 1.5f));
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
        passContacts[role] = (other, world.Events.Elapsed + 8f);
        var at = Flat(target.Position);
        var stops = side != 0f ? new[] { at + new Vector2(side, 0f), at } : new[] { at };
        GoAvoiding(role, stops, other, MaxWait, seconds =>
        {
            if (backTo is null) return;
            var order = orders.GetValueOrDefault(role);
            world.Events.Add(seconds + 0.5f, () =>
            {
                if (orders.GetValueOrDefault(role) == order) Go(role, backTo());
            });
        });
    }

    private void KeepNisiClearance()
    {
        var now = world.Events.Elapsed;
        foreach (var (role, contact) in passContacts.ToArray())
            if (contact.Until <= now || NisiOf(role) is { } held && held == NisiOf(contact.Partner)) passContacts.Remove(role);
        foreach (var role in PerRole.All)
        {
            if (Member(role) is not { } member)
            {
                personalObstacles.Remove(role);
                continue;
            }
            if (!world.Party.IsBotDriven(member) || now >= 134f)
            {
                member.SetMovementObstacles(null);
                personalObstacles.Remove(role);
                continue;
            }
            if (!personalObstacles.TryGetValue(role, out var field)) personalObstacles[role] = field = new ObstacleField();
            field.Clear();
            field.Add(new ArenaBoundary());
            foreach (var obstacle in world.Obstacles.All) field.Add(obstacle);
            var mine = NisiOf(role);
            foreach (var other in PerRole.All)
            {
                if (other == role || Member(other) is not { } neighbour || !neighbour.IsAlive()) continue;
                var theirs = NisiOf(other);
                if (mine == theirs || CanTouch(role, other, now) || CanTouch(other, role, now)) continue;
                field.Add(new CircleObstacle(At(other), member.HitboxRadius + neighbour.HitboxRadius + .45f));
            }
            member.SetMovementObstacles(field);
        }
        if (now < 134f) world.Events.Add(.02f, KeepNisiClearance);
    }

    private sealed class ArenaBoundary : IObstacle
    {
        public float SignedDistance(Vector2 p) => ArenaEdge - p.Length();
        public Vector2 Normal(Vector2 p) => p.LengthSquared() > .001f ? -Vector2.Normalize(p) : Vector2.UnitY;
    }

    private bool CanTouch(PartyRole role, PartyRole other, float now)
    {
        if (NisiOf(role) is { } mine && NisiOf(other) is { } theirs && mine != theirs) return false;
        return passContacts.TryGetValue(role, out var contact) && contact.Partner == other && contact.Until > now
            || now >= 119f && JudgmentOf(role) is { } wanted && wanted == JudgmentOf(other);
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
        EObjId.VoidzoneIceSmall or EObjId.VoidzoneIceLarge => Geometry.LargeIceRadius,
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
