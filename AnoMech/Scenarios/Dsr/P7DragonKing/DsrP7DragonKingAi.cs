using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;
using State = AnoMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingState;
using Scenario = AnoMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingScenario;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

public sealed class DsrP7DragonKingAi : IScenarioAi<DsrP7DragonKingState>
{
    public string Name => "Static south exaflares, all 3-3-2";
    public string Group => "Elemental";

    private static readonly PartyRole[][] TrinityPairs = [[MeleeDpsA, MeleeDpsB], [PhysRangedDps, CasterDps], [RegenHealer, ShieldHealer]];
    private static readonly PartyRole[] GroupOne = [RegenHealer, MeleeDpsA, PhysRangedDps];
    private static readonly PartyRole[] GroupTwo = [ShieldHealer, MeleeDpsB, CasterDps];
    private static readonly PartyRole[] NonTanks = [RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();
    private static readonly Vector2 SouthMaxMelee = new(0f, 6f);
    private static readonly Vector2 UnderTheDragonKing = new(0f, 1f);
    private static readonly Vector2 AggroTankNorth = new(0f, -5f);
    private static readonly Vector2 OtherTankOnHisRight = new(5f, 0f);
    private static readonly Vector2 IceSouthExaflareEdge = new(0f, 0.8f);
    private static readonly Vector2 FlamesSouthExaflareEdge = new(0f, 15.2f);
    private static readonly Vector2 OutOfEveryTower = new(0f, 1f);
    private const float InsideAkhMornRadius = 7.2f;
    private const float FlamesAkhMornRadius = 9f;
    private const float IceGigaflareRadius = 6.5f;
    private const float GigaflareRadius = 10f;
    private const float StepAfterHit = 0.25f;
    private const float AfterTrinity = 0.3f;

    private DsrP7DragonKingState state = null!;
    private SimWorld world = null!;

    public void Run(DsrP7DragonKingState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(Scenario.StartAt + 0.5f, () => AiMove.Create(HoldAggroNorth(Cluster(SouthMaxMelee, Everyone))).NaturalOrder(), jitter: 0f);
        for (var cycle = 0; cycle < State.Cycles; cycle++)
        {
            var c = cycle;
            var exaflarePair = 3 * c;
            var akhMornPair = 3 * c + 1;
            var gigaflarePair = 3 * c + 2;
            var firstHit = Scenario.ExaflareFirstHits[c];

            var waitAtTheSouthExaflare = c == 0 ? Scenario.ExaflareCasts[0] - 1.3f : Scenario.TrinityHits[2 * (gigaflarePair - 3) + 1] + AfterTrinity;
            ai.Move(waitAtTheSouthExaflare, () => AiMove.Create(HoldAggroNorth(Cluster(SouthExaflareEdge(c), Everyone))).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.ExaflareCasts[c] + .1f, () => AiMove.Create(ExaflarePoints(c, Cluster(SouthExaflareEdge(c), Everyone))).NaturalOrder(), jitter: 0f);
            ai.Move(firstHit + StepAfterHit, () => AiMove.Create(ExaflarePoints(c, Cluster(State.Spot(EdgeSpot.Tanks), Everyone))).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(firstHit + State.ExaflareDelays[1] + 0.1f, () => AiMove.Create(ExaflarePoints(c, Cluster(State.SouthOfTheExaflares, Everyone))).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(firstHit + State.BaitStepInDelay, () => AiMove.Create(ExaflarePoints(c, FirstBaitStepsIntoTheSouthExaflare(exaflarePair))).NaturalOrder(), jitter: 0f);
            ai.Move(firstHit + State.ExaflareDelays[^1] + 0.2f, () => AiMove.Create(TrinitySpots(exaflarePair, 1)).NaturalOrder(), jitter: 0f);

            ai.Move(Scenario.TrinityHits[2 * exaflarePair + 1] + AfterTrinity, () => AiMove.Create(HoldAggroNorth(AkhMornSpots(c))).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.AkhMornCasts[c] + .1f, () => AiMove.Create(AkhMornSpots(c)).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.AkhMornFirstHits[c] + .4f, () => AiMove.Create(AkhMornSpots(c, inside: true)).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.AkhMornLastHit(c) + AfterTrinity, () => AiMove.Create(TrinitySpots(akhMornPair, 0)).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.TrinityHits[2 * akhMornPair] + AfterTrinity, () => AiMove.Create(TrinitySpots(akhMornPair, 1)).NaturalOrder(), jitter: 0f);

            if (c >= State.GigaflareCycles) continue;
            ai.Move(Scenario.TrinityHits[2 * akhMornPair + 1] + AfterTrinity, () => AiMove.Create(Cluster(OppositeGigaflare(c, 0), Everyone)).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.GigaflareHits[c][0] + StepAfterHit, () => AiMove.Create(Cluster(OppositeGigaflare(c, 1), Everyone)).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(Scenario.GigaflareHits[c][1] + StepAfterHit, () => AiMove.Create(Cluster(OppositeGigaflare(c, 2), Everyone)).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(Scenario.GigaflareHits[c][2] + AfterTrinity, () => AiMove.Create(TrinitySpots(gigaflarePair, 0)).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.TrinityHits[2 * gigaflarePair] + AfterTrinity, () => AiMove.Create(TrinitySpots(gigaflarePair, 1)).NaturalOrder(), jitter: 0f);
        }
        for (var pair = 0; pair < Scenario.TrinityHits.Length / 2 - 1; pair++)
        {
            var next = AggroTank(pair + 1);
            world.Events.Add(Scenario.TrinityHits[pair * 2 + 1] + .2f, () => Provoke(next));
        }
        ai.Move(Scenario.MornAfahsEdgeCast + 0.5f, () => AiMove.Create(HealersAndMainTankTakeTheFirstTowers()).NaturalOrder(), jitter: 0f);
    }

    private void Provoke(PartyRole role)
    {
        var tank = world.Party.Get(role);
        if (tank == null || !tank.IsAlive()) return;
        if (!world.Party.IsBotDriven(tank))
        {
            world.Announce($"{(role == MainTank ? "MT" : "ST")}: Provoke Thordan now.");
            return;
        }
        var boss = world.Children.OfType<SimEnemy>().SingleOrDefault(e => e.BNpcBaseId == DsrP7Constants.BNpcBaseId.DragonKingThordan);
        if (boss == null) return;
        world.Party.Act(tank, new EnemyAction(7533) { Cast = new() { CastSecondsOverride = 0, FaceTarget = false } }, boss);
        world.NotifyActionResolved(tank, 7533, boss.GameObjectId.ObjectId);
    }

    private Vector2 SouthExaflareEdge(int cycle)
        => state.ExaflareElement(cycle) == AscalonElement.Ice ? IceSouthExaflareEdge : FlamesSouthExaflareEdge;

    private Vector2 OppositeGigaflare(int cycle, int blast)
    {
        var radius = blast == 0 && state.GigaflareElement(cycle) == AscalonElement.Ice ? IceGigaflareRadius : GigaflareRadius;
        return -Vector2.Normalize(state.GigaflareSpot(cycle, blast)) * radius;
    }

    private static Vector2?[] FirstBaitStepsIntoTheSouthExaflare(int pair)
    {
        var bait = Bait(pair, 0);
        var spots = Cluster(State.SouthOfTheExaflares, NonTanks.Where(r => r != bait));
        spots[(int)bait] = State.Spot(EdgeSpot.Tanks);
        spots[(int)AggroTank(pair)] = State.ExaflareDarkTankSpot;
        spots[(int)OtherTank(pair)] = State.ExaflareLightTankSpot;
        return spots;
    }

    private static Vector2?[] TrinitySpots(int pair, int trinity)
    {
        var bait = Bait(pair, trinity);
        var spots = Cluster(SouthMaxMelee, NonTanks.Where(r => r != bait));
        spots[(int)bait] = UnderTheDragonKing;
        spots[(int)AggroTank(pair)] = AggroTankNorth;
        spots[(int)OtherTank(pair)] = OtherTankOnHisRight;
        return spots;
    }

    private static PartyRole Bait(int pair, int trinity) => TrinityPairs[pair % TrinityPairs.Length][trinity];

    private static PartyRole AggroTank(int pair) => pair % 2 == 0 ? MainTank : OffTank;

    private static PartyRole OtherTank(int pair) => pair % 2 == 0 ? OffTank : MainTank;

    private Vector2 AkhMornSpot(int cycle, EdgeSpot spot, bool inside)
        => state.AkhMornPoint(cycle, State.Spot(spot)) * ((inside || state.AkhMornElement(cycle) == AscalonElement.Ice ? InsideAkhMornRadius : FlamesAkhMornRadius) / State.SpotRadius);

    private Vector2?[] AkhMornSpots(int cycle, bool inside = false)
    {
        var spots = Cluster(AkhMornSpot(cycle, EdgeSpot.Party, inside), GroupOne);
        Place(spots, Cluster(AkhMornSpot(cycle, EdgeSpot.SecondParty, inside), GroupTwo));
        Place(spots, Cluster(AkhMornSpot(cycle, EdgeSpot.Tanks, inside), [MainTank, OffTank]));
        return spots;
    }

    private Vector2?[] HealersAndMainTankTakeTheFirstTowers()
    {
        var spots = Cluster(OutOfEveryTower, Everyone);
        spots[(int)RegenHealer] = state.MornAfahPoint(State.Spot(EdgeSpot.Party));
        spots[(int)ShieldHealer] = state.MornAfahPoint(State.Spot(EdgeSpot.SecondParty));
        spots[(int)MainTank] = state.MornAfahPoint(State.Spot(EdgeSpot.Tanks));
        return spots;
    }

    private Vector2?[] ExaflarePoints(int cycle, Vector2?[] points)
        => points.Select(point => point is { } at ? (Vector2?)state.ExaflarePoint(cycle, at) : null).ToArray();

    private Vector2?[] HoldAggroNorth(Vector2?[] points)
    {
        points[(int)state.AggroTank] = AggroTankNorth;
        return points;
    }

    private static void Place(Vector2?[] into, Vector2?[] from)
    {
        for (var i = 0; i < into.Length; i++)
            if (from[i] is { } spot) into[i] = spot;
    }

    private static Vector2?[] Cluster(Vector2 centre, IEnumerable<PartyRole> roles)
    {
        var spots = new Vector2?[8];
        var i = 0;
        foreach (var role in roles)
        {
            spots[(int)role] = centre + new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1));
            i++;
        }
        return spots;
    }
}
