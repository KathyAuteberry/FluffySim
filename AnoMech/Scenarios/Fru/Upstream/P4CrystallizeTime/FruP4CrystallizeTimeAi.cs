using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.Upstream.FruConstants;
using static AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime.FruP4CrystallizeTimeRoutes;
using static AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime.FruP4CrystallizeTimeState;

namespace AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

internal enum CrystallizeTimeAssignment { AeroWest, IceWest, Eruption, Ice, Darkness, Water, AeroEast, IceEast }

public sealed class FruP4CrystallizeTimeAi(bool mana = false) : IScenarioAi<FruP4CrystallizeTimeState>
{
    public string Name => mana ? "Nukemaru / 7-1 Akh Morn (ST solo)" : "NA priority / 7-1 Akh Morn (MT solo)";
    public string Group => mana ? "Mana" : "NA";

    private const float RunSpeed = 6f;
    private const float ReferenceScale = 10f / 23.74f;
    private const float SpreadClearance = 6f;
    private const float AeroKnockbackEndsAt = 27.45f;
    private const float StopCleansingAt = 41.1f;
    private const float ArrivalMargin = 0.25f;

    private static readonly PartyRole[] NaPriority =
        [PartyRole.ShieldHealer, PartyRole.RegenHealer, PartyRole.OffTank, PartyRole.MainTank,
         PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    internal static readonly PartyRole[] ManaPriority =
        [PartyRole.RegenHealer, PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA,
         PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.ShieldHealer];

    private static readonly Vector3[] SpreadCandidates = CreateSpreadCandidates();

    private FruP4CrystallizeTimeState state = null!;
    private SimWorld world = null!;
    private readonly Dictionary<PartyRole, CrystallizeTimeAssignment> assignments = new();
    private readonly Dictionary<PartyRole, CrystallizeTimeAssignment> puddleTargets = new();
    private readonly Dictionary<PartyRole, Vector3> spreadSpots = new();
    private readonly HashSet<PartyRole> knockedByAero = [];
    private readonly HashSet<PartyRole> holdingClaw = [];
    private readonly HashSet<PartyRole> holdingFang = [];
    private readonly Dictionary<PartyRole, int> moveOrders = new();
    private readonly Dictionary<PartyRole, ObstacleField> puddleAvoidance = new();
    private readonly Dictionary<(PartyRole Walker, PartyRole Catcher), CircleObstacle> puddleObstacles = new();
    private float elapsed;

    public void Run(FruP4CrystallizeTimeState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        elapsed = 0f;
        puddleTargets.Clear();
        puddleAvoidance.Clear();
        puddleObstacles.Clear();
        spreadSpots.Clear();
        knockedByAero.Clear();
        holdingClaw.Clear();
        holdingFang.Clear();
        moveOrders.Clear();
        AssignSidesByPriority();

        world.Events.Add(48.6f, ApplyPartyWingMitigation);
        world.Events.Add(15.5f, () => FollowRoute(state.SlowNorthwest ? PRE_HG_1_NW : PRE_HG_1_NE, arriveBy: 22.7f));
        world.Events.Add(23.3f, () => FollowRoute(state.SlowNorthwest ? POST_HG_1_NW : POST_HG_1_NE, arriveBy: 25.9f));
        world.Events.Add(25.9f, RememberWhoAeroKnocksBack);
        world.Events.Add(26f, () => FollowRoute(PUDDLE_DODGE));
        world.Events.Add(26.7f, () => FollowRoute(state.SlowNorthwest ? POST_KB_NW : POST_KB_NE, arriveBy: 28.2f));
        world.Events.Add(28.5f, () => FollowRoute(state.SlowNorthwest ? POST_HG_2_NW : POST_HG_2_NE, arriveBy: 28.8f));
        world.Events.Add(29.7f, AssignNumberedCleansingRoutes);
        world.Events.Add(29.8f, () => FollowRoute(state.East ? POST_UD_E : POST_UD_W, arriveBy: 31.5f));
        world.Events.Add(31.5f, () => FollowRoute(state.East ? POST_EARLY_SOAK_E : POST_EARLY_SOAK_W, arriveBy: 32f));
        world.Events.Add(33.6f, () => FollowRoute(state.East ? POST_HG_3_E : POST_HG_3_W, arriveBy: 34.1f));
        world.Events.Add(34.6f, () => FollowRoute(state.East ? POST_EXA_2_E : POST_EXA_2_W, arriveBy: 36.1f));
        world.Events.Add(36.5f, () => FollowRoute(state.Corner switch
        {
            CrystallizeCorner.NW => POST_EXA_3_NW,
            CrystallizeCorner.NE => POST_EXA_3_NE,
            CrystallizeCorner.SE => POST_EXA_3_SE,
            _ => POST_EXA_3_SW,
        }, arriveBy: 38.1f));
        world.Events.Add(38.9f, () => FollowRoute(state.Corner switch
        {
            CrystallizeCorner.NW => POST_EXA_4_NW,
            CrystallizeCorner.NE => POST_EXA_4_NE,
            CrystallizeCorner.SE => POST_EXA_4_SE,
            _ => POST_EXA_4_SW,
        }, arriveBy: 40.5f));
        if (mana) world.Events.Add(40.55f, MoveTanksToRecordingArms);
        world.Events.Add(StopCleansingAt, () => MoveEveryBot(RewindSpot, arriveBy: 42.5f));
        world.Events.Add(45.9f, spreadSpots.Clear);
        if (mana)
            world.Events.Add(50.5f, () =>
            {
                foreach (var bot in Bots()) bot.AddStatus(1209, 6f);
            });
        world.Events.Add(61.7f, () => MoveEveryBot(AkhMornSpot, arriveBy: 68.9f));
        world.Events.Add(73f, () => MoveEveryBot(_ => Vector3.Zero, arriveBy: 79.5f));
    }

    public void Tick(FruP4CrystallizeTimeState stateParam, SimWorld worldParam, float delta)
    {
        elapsed += delta;
        foreach (var bot in Bots())
        {
            var role = ((ISimPartyMember)bot).Role;
            ReactToClawCaught(bot, role);
            ReactToFangCleansed(bot, role);
        }
        RefreshPuddleAvoidance();
        ChasePuddles();
        if (elapsed is >= 45.9f and < 48.5f)
        {
            if (mana) MoveEveryBot(ManaSpreadSpot, arriveBy: 48.3f);
            else SpreadAwayFromPlayerForSpiritTaker();
        }
    }

    private void AssignSidesByPriority()
    {
        assignments.Clear();
        foreach (var role in Enum.GetValues<PartyRole>())
            assignments[role] = state.Debuff(role) switch
            {
                CrystallizeDebuff.RedAero => IsWestOfPair(role) ? CrystallizeTimeAssignment.AeroWest : CrystallizeTimeAssignment.AeroEast,
                CrystallizeDebuff.RedIce => IsWestOfPair(role) ? CrystallizeTimeAssignment.IceWest : CrystallizeTimeAssignment.IceEast,
                CrystallizeDebuff.BlueEruption => CrystallizeTimeAssignment.Eruption,
                CrystallizeDebuff.BlueIce => CrystallizeTimeAssignment.Ice,
                CrystallizeDebuff.BlueDarkness => CrystallizeTimeAssignment.Darkness,
                _ => CrystallizeTimeAssignment.Water,
            };
    }

    private void AssignNumberedCleansingRoutes()
    {
        foreach (var (role, number) in state.CleanseNumbers)
            assignments[role] = number switch
            {
                1 => CrystallizeTimeAssignment.Darkness,
                2 => CrystallizeTimeAssignment.Water,
                3 => CrystallizeTimeAssignment.Ice,
                _ => CrystallizeTimeAssignment.Eruption,
            };
    }

    private void ApplyPartyWingMitigation()
    {
        foreach (var (role, status, duration) in new (PartyRole, ushort, float)[]
        {
            (PartyRole.RegenHealer, 3985, 20f),
            (PartyRole.ShieldHealer, 2711, 20f),
            (PartyRole.PhysRangedDps, 1934, 15f),
        })
        {
            if (world.Party.Get(role) is not { } caster || !caster.IsAlive() || !world.Party.IsBotDriven(caster)) continue;
            foreach (var member in world.Party.ActiveMembers())
                if (Vector3.Distance(caster.Position, member.Position) <= 30f + member.HitboxRadius)
                    member.AddStatus(status, duration);
        }
    }

    private bool IsWestOfPair(PartyRole role) => (mana ? ManaPriority : NaPriority).First(r => state.Debuff(r) == state.Debuff(role)) == role;

    private PartyRole RoleOf(CrystallizeTimeAssignment assignment) => assignments.First(a => a.Value == assignment).Key;

    private IEnumerable<SimCharacter> Bots() => world.Party.ActiveMembers().Where(world.Party.IsBotDriven).ToList();

    private void FollowRoute(IReadOnlyDictionary<CrystallizeTimeAssignment, CrystallizeTimeDestination> route, float? arriveBy = null)
    {
        foreach (var (assignment, destination) in route)
        {
            var role = RoleOf(assignment);
            puddleTargets.Remove(role);
            if (world.Party.Get(role) is not { } bot || !bot.IsAlive() || !world.Party.IsBotDriven(bot)) continue;
            if (destination.Puddle is { } source)
            {
                CancelPendingMove(role);
                puddleTargets[role] = source;
            }
            else
            {
                var leaveNoSoonerThan = knockedByAero.Contains(role) ? AeroKnockbackEndsAt : 0f;
                MoveLeavingAsLateAsPossible(bot, role, destination.Position, arriveBy, leaveNoSoonerThan);
            }
        }
    }

    private void MoveTanksToRecordingArms()
    {
        foreach (var bot in Bots())
        {
            var role = ((ISimPartyMember)bot).Role;
            if (!role.IsTank()) continue;
            puddleTargets.Remove(role);
            CancelPendingMove(role);
            bot.MoveTo(RewindSpot(role), RunSpeed);
        }
    }

    private void MoveEveryBot(Func<PartyRole, Vector3> spot, float arriveBy)
    {
        puddleTargets.Clear();
        foreach (var bot in Bots())
        {
            var role = ((ISimPartyMember)bot).Role;
            MoveLeavingAsLateAsPossible(bot, role, spot(role), arriveBy);
        }
    }

    private void MoveLeavingAsLateAsPossible(SimCharacter bot, PartyRole role, Vector3 destination, float? arriveBy, float leaveNoSoonerThan = 0f)
    {
        var walk = Vector2.Distance(new Vector2(bot.Position.X, bot.Position.Z), new Vector2(destination.X, destination.Z)) / RunSpeed;
        var margin = elapsed >= StopCleansingAt && world.BotDifficulty == BotDifficulty.Easy ? 0.75f : ArrivalMargin;
        var leaveAt = MathF.Max(leaveNoSoonerThan, arriveBy is { } deadline ? deadline - margin - walk : 0f);
        var order = CancelPendingMove(role);
        if (leaveAt <= elapsed)
        {
            bot.MoveTo(destination, RunSpeed);
            return;
        }
        world.Events.Add(leaveAt - elapsed, () =>
        {
            if (moveOrders[role] == order && bot.IsAlive()) bot.MoveTo(destination, RunSpeed);
        });
    }

    private void MoveNow(SimCharacter bot, PartyRole role, Vector3 destination)
    {
        CancelPendingMove(role);
        bot.MoveTo(destination, RunSpeed);
    }

    private int CancelPendingMove(PartyRole role) => moveOrders[role] = moveOrders.GetValueOrDefault(role) + 1;

    private void RememberWhoAeroKnocksBack()
    {
        var aeros = state.Holders(CrystallizeDebuff.RedAero).Select(world.Party.Get).OfType<SimCharacter>().ToList();
        foreach (var member in world.Party.ActiveMembers())
            if (member is ISimPartyMember slot && aeros.Any(a => a != member && Vector3.Distance(a.Position, member.Position) <= 15f))
                knockedByAero.Add(slot.Role);
    }

    private void ReactToClawCaught(SimCharacter bot, PartyRole role)
    {
        var holds = bot.HasStatus(StatusId.Wyrmclaw);
        if (holds) { holdingClaw.Add(role); return; }
        if (!holdingClaw.Remove(role) || state.Debuff(role) != CrystallizeDebuff.RedAero) return;
        var escape = state.East ? POST_EARLY_SOAK_E : POST_EARLY_SOAK_W;
        MoveNow(bot, role, escape[assignments[role]].Position);
    }

    private void ReactToFangCleansed(SimCharacter bot, PartyRole role)
    {
        var holds = bot.HasStatus(StatusId.Wyrmfang);
        if (holds) { holdingFang.Add(role); return; }
        if (!holdingFang.Remove(role) || elapsed >= StopCleansingAt) return;
        puddleTargets.Remove(role);
        if (AfterCleanse.TryGetValue(assignments[role], out var next)) MoveNow(bot, role, next.Position);
    }

    private IReadOnlyDictionary<CrystallizeTimeAssignment, CrystallizeTimeDestination> AfterCleanse => state.Corner switch
    {
        CrystallizeCorner.NW => POST_SOAK_TARGET_NW,
        CrystallizeCorner.NE => POST_SOAK_TARGET_NE,
        CrystallizeCorner.SE => POST_SOAK_TARGET_SE,
        _ => POST_SOAK_TARGET_SW,
    };

    private void RefreshPuddleAvoidance()
    {
        foreach (var bot in Bots())
        {
            var role = ((ISimPartyMember)bot).Role;
            if (state.DragonPuddles.Count == 0)
            {
                bot.SetMovementObstacles(null);
                continue;
            }
            if (!puddleAvoidance.TryGetValue(role, out var field))
                puddleAvoidance[role] = field = new ObstacleField();
            field.Clear();
            foreach (var obstacle in world.Obstacles.All) field.Add(obstacle);
            foreach (var (catcher, position) in state.DragonPuddles)
            {
                if (catcher == role) continue;
                if (bot.HasStatus(StatusId.Wyrmfang) && puddleTargets.TryGetValue(role, out var target) && RoleOf(target) == catcher) continue;
                var key = (role, catcher);
                if (!puddleObstacles.TryGetValue(key, out var obstacle))
                    puddleObstacles[key] = obstacle = new CircleObstacle(new Vector2(position.X, position.Z), 1.6f);
                obstacle.Center = new Vector2(position.X, position.Z);
                field.Add(obstacle);
            }
            bot.SetMovementObstacles(field);
        }
    }

    private void ChasePuddles()
    {
        foreach (var (role, source) in puddleTargets)
            if (world.Party.Get(role) is { } bot && bot.IsAlive()
                && state.DragonPuddles.TryGetValue(RoleOf(source), out var puddle))
                if (bot.MoveDestination is not { } destination || Vector3.DistanceSquared(destination, puddle) > .001f)
                    bot.MoveTo(puddle, RunSpeed);
    }

    private Vector3 RewindSpot(PartyRole role)
    {
        if (mana) return ManaRewindSpot(role, state.East, state.North);
        var groupOne = role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.PhysRangedDps;
        var tank = role.IsTank();
        var leadEast = state.Corner is CrystallizeCorner.NW or CrystallizeCorner.SE ? groupOne : !groupOne;
        var front = tank ? 22.7f : 20.5f;
        var back = tank ? 18.2f : 15.4f;
        var north = (state.North ? 1 : -1) * (leadEast ? back : front);
        var east = (state.East ? 1 : -1) * (leadEast ? front : back);
        return new Vector3(east * ReferenceScale, 0, -north * ReferenceScale);
    }

    private Vector3 AkhMornSpot(PartyRole role)
        => role == (mana ? PartyRole.OffTank : PartyRole.MainTank)
            ? new Vector3(mana ? 9.3f : -9.3f, 0, 0) : Vector3.Zero;

    internal static Vector3 ManaRewindSpot(PartyRole role, bool east, bool north)
    {
        var relativeNorth = north ? -1f : 1f;
        if (role.IsTank())
            return new Vector3((role == PartyRole.MainTank ? -12f : 12f) * (north ? 1f : -1f), 0, 14.5f * relativeNorth);
        var rank = (int)role - 2;
        var offset = (rank - 2.5f) * 0.12f;
        return new Vector3(offset * (east ? 1f : -1f), 0, (1.5f - offset) * relativeNorth);
    }

    private Vector3 ManaSpreadSpot(PartyRole role)
    {
        var spot = role switch
        {
            PartyRole.MainTank => new Vector3(-13, 0, -13),
            PartyRole.OffTank => new Vector3(13, 0, -13),
            PartyRole.RegenHealer => new Vector3(-10, 0, 0),
            PartyRole.ShieldHealer => new Vector3(10, 0, 0),
            PartyRole.MeleeDpsA => new Vector3(-9, 0, 9),
            PartyRole.MeleeDpsB => new Vector3(9, 0, 9),
            PartyRole.PhysRangedDps => new Vector3(-9, 0, -7),
            _ => new Vector3(9, 0, -7),
        };
        return state.North ? spot : -spot;
    }

    private static Vector3[] CreateSpreadCandidates()
    {
        var spots = new List<Vector3>();
        for (var x = -18; x <= 18; x += 6)
            for (var z = -18; z <= 18; z += 6)
            {
                var spot = new Vector3(x, 0, z);
                if (spot.Length() <= 18.5f && Vector3.Distance(spot, FragmentPosition) >= SpreadClearance) spots.Add(spot);
            }
        return spots.ToArray();
    }

    private void SpreadAwayFromPlayerForSpiritTaker()
    {
        var player = world.Party.Player;
        foreach (var bot in Bots().OrderBy(b => ((ISimPartyMember)b).Role))
        {
            var role = ((ISimPartyMember)bot).Role;
            bool Clear(Vector3 spot) =>
                (!player.IsAlive() || world.Party.IsBotDriven(player!) || Vector3.Distance(spot, player!.Position) >= SpreadClearance)
                && spreadSpots.All(s => s.Key == role || Vector3.Distance(spot, s.Value) >= SpreadClearance);
            if (spreadSpots.TryGetValue(role, out var current) && Clear(current)) continue;
            var spot = SpreadCandidates.Where(Clear).OrderBy(c => Vector3.DistanceSquared(bot.Position, c)).Cast<Vector3?>().FirstOrDefault();
            if (spot is not { } chosen) continue;
            spreadSpots[role] = chosen;
            bot.MoveTo(chosen, RunSpeed);
        }
    }
}
