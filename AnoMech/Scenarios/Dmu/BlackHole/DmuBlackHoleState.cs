using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Umad.UmadConstants;
using AnoMech.Scenarios.Umad;
using AnoMech.Scenarios.Umad.P3BlackHole;

namespace AnoMech.Scenarios.Dmu.BlackHole;

// Per-run randomized assignments the scenario and AI consume. Filled in the ctor
// (apply override if set, otherwise pick at random) so Run stays deterministic for
// the duration of one play. See UmadP4KefkaSaysState for the canonical shape.
public sealed class DmuBlackHoleState
{
    private readonly Rng rng = Rng.Detached;

    public DmuBlackHoleScenarioObjects ScenarioObjects { get; }

    public RoleList Roles { get; }
    public RoleList StackTargets { get; }
    public RoleList EdictTargets { get; }
    
    public uint ImplosionAttack { get; }
    public IReadOnlyList<uint> SlapAttacks { get; }
    public IReadOnlyList<RoleList> ConeTargets { get; }
    
    public IReadOnlyList<Direction> KefkaPosition { get; }
    
    public IReadOnlyList<Direction> BlackHoleDirections { get; }
    
    public int MiniBlackHoleInitialAngle { get; }
    public int MiniBlackHoleChirality { get; }

    // Selfmark press order inside each line: Attack 1-3 for first in line, Bind 1-3 for second,
    // Ignore 1-2 for third. Each list holds the seats in marker order.
    public IReadOnlyList<int> AttackSeats { get; }
    public IReadOnlyList<int> BindSeats { get; }
    public IReadOnlyList<int> IgnoreSeats { get; }
    public PartyRole ChaosTank { get; }
    public PartyRole ExdeathTank { get; }

    // Read by both the Ai (positioning) and the scenario (mitigation plan); both must agree.
    public ThunderIIIAssignment ThunderSet1 { get; }
    public ThunderIIIAssignment ThunderSet2 { get; }

    // The seat the debug first-slap option aims at, and whether anyone asked for it.
    private readonly PartyRole subject;
    private readonly bool slapAllOnSubject;

    public DmuBlackHoleState(SimWorld world, DmuBlackHoleStateOverrides overrides)
    {
        rng = world.Rng;
        var party = world.Party;
        // Only the debug "aim every cone at me" option needs a single subject; the first seat
        // asking for it wins.
        var slapSeats = overrides.FirstSlapAllOnMe.Resolve(party.PlayerRole);
        subject = slapSeats.FirstOrDefault(s => s.Value).Role;
        slapAllOnSubject = slapSeats.Any(s => s.Value);
        ScenarioObjects = new DmuBlackHoleScenarioObjects(world);
        Roles = BuildRoles(party, overrides);
        AttackSeats = rng.Shuffle(MarkerOrderAttack).ToList();
        BindSeats = rng.Shuffle(MarkerOrderBind).ToList();
        IgnoreSeats = rng.Shuffle(MarkerOrderIgnore).ToList();
        ChaosTank = DmuP3Tanks.ChaosTank(overrides.MainTankOnChaos);
        ExdeathTank = DmuP3Tanks.ExdeathTank(overrides.MainTankOnChaos);
        ThunderSet1 = overrides.ThunderSet1;
        ThunderSet2 = overrides.ThunderSet2;

        StackTargets = new RoleList(party, overrides.StackTargets ?? rng.Shuffle(rng.NextSupportRole(), rng.NextDpsRole()));
        EdictTargets = new RoleList(party, [overrides.EdictTarget ?? rng.NextRole(), rng.NextRole()]);
        ImplosionAttack = overrides.ImplosionAttack ?? rng.NextObj(ActionId.LatitudinalImplosion, ActionId.LongitudinalImplosion);
        SlapAttacks = Enumerable.Range(0, 3).Select(i => overrides.SlapAttacks[i] ?? NextSlap()).ToList();
        KefkaPosition = Enumerable.Range(0, 5).Select(i => overrides.KefkaPositions[i] ?? rng.NextDirection()).ToList();
        ConeTargets = Enumerable.Range(0, 3)
                                .Select(i => overrides.ConeTargets[i] is { } forced
                                                 ? new RoleList(party, forced)
                                                 : NextConeTargets(party, SlapAttacks[i], i == 0 && slapAllOnSubject))
                                .ToList();
        BlackHoleDirections = Enumerable.Range(0, 4).Select(i => overrides.BlackHoleDirections[i] ?? rng.NextCardinal()).ToList();
        MiniBlackHoleInitialAngle = overrides.MiniBlackHoleInitialAngle ?? rng.NextInt(2);
        MiniBlackHoleChirality = overrides.MiniBlackHoleChirality ?? rng.NextSign();
    }

    // The beam choreography is written for this marker order: seat 4 is Attack 1, seat 0 Attack 2,
    // seat 3 Attack 3, and so on. A run's presses map each of those onto the seat holding the marker.
    private static readonly int[] MarkerOrderAttack = [4, 0, 3];
    private static readonly int[] MarkerOrderBind = [5, 1, 7];
    private static readonly int[] MarkerOrderIgnore = [6, 2];

    public int MarkerSeat(int choreographySeat)
    {
        foreach (var (order, rolled) in new[] { (MarkerOrderAttack, AttackSeats), (MarkerOrderBind, BindSeats), (MarkerOrderIgnore, IgnoreSeats) })
        {
            var index = Array.IndexOf(order, choreographySeat);
            if (index >= 0) return rolled[index];
        }
        return choreographySeat;
    }

    public Sign? Marker(int seat)
    {
        var attack = AttackSeats.ToList().IndexOf(seat);
        if (attack >= 0) return Sign.Attack1 + attack;
        var bind = BindSeats.ToList().IndexOf(seat);
        if (bind >= 0) return Sign.Bind1 + bind;
        var ignore = IgnoreSeats.ToList().IndexOf(seat);
        return ignore >= 0 ? Sign.Ignore1 + ignore : null;
    }

    // Final-slot (post-swap) line number and Accretion, mirroring the per-index status
    // assignment in DmuBlackHoleScenario.Run_OtherDebuffs. Slots 0-3 hold the supports
    // (slot 3 never a tank), slots 4-7 the DPS; Swap(3,7) trades the two Accretion holders.
    private static readonly int[] SlotLine = [1, 2, 3, 1, 1, 2, 3, 2];          // 1/2/3 = First/Second/Third in line
    private static readonly bool[] SlotAccretion = [false, false, false, true, false, false, false, true];

    // Every seat's line/Accretion request solved together against the fight's own slot layout:
    // supports fill slots 0-3 and DPS 4-7 before the coin-flip Swap(3,7), and pre-slot 3 is
    // never a tank. Both Accretion slots (3 and 7) sit in different blocks, so which seats can
    // hold Accretion depends on that swap -- hence solving for both and taking the first that
    // satisfies everyone.
    private RoleList BuildRoles(SimParty party, DmuBlackHoleStateOverrides overrides)
    {
        if (overrides.SeatLinesInRoleOrder)
            return new RoleList(party, PerRole.All);

        var requests = overrides.Requests(party.PlayerRole);

        var swapFirst = rng.NextBool();
        foreach (var swap in new[] { swapFirst, !swapFirst })
            if (TrySeatRoles(swap, requests) is { } solved)
                return new RoleList(party, solved);

        // Nothing seats everyone as asked (two seats on the same line with only one slot for
        // it, a tank asking for Accretion). Drop requests from the last seat back until it
        // solves; an empty request set always does.
        var ordered = requests.Keys.OrderBy(r => (int)r).ToList();
        for (var drop = ordered.Count - 1; drop >= 0; drop--)
        {
            var dropped = ordered[drop];
            requests.Remove(dropped);
            DiagnosticLog.Warn($"[DmuBlackHole] Can't satisfy every line/Accretion request -- dropping {dropped}'s and leaving that seat to the roll.");
            foreach (var swap in new[] { swapFirst, !swapFirst })
                if (TrySeatRoles(swap, requests) is { } relaxed)
                    return new RoleList(party, relaxed);
        }
        return RoleList.RandomRoleStable(rng, party);
    }

    // Backtracking fill of the eight final slots. A seat with no request goes anywhere its role
    // is allowed; a seat with one only goes where the line and Accretion match.
    private PartyRole[]? TrySeatRoles(bool swap, Dictionary<PartyRole, (int? Line, bool? Accretion)> requests)
        // Shuffled so unforced seats still vary per run.
        => TrySeatRoles(swap, requests, rng.Shuffle(PerRole.All).ToArray());

    // Whether one swap can seat every request at once. The settings panel asks this (through
    // DmuBlackHoleStateOverrides.Validate) so an impossible combination is caught while the
    // host is still editing, not dropped mid-run.
    internal static bool CanSeat(Dictionary<PartyRole, (int? Line, bool? Accretion)> requests)
        => TrySeatRoles(false, requests, PerRole.All) != null
           || TrySeatRoles(true, requests, PerRole.All) != null;

    private static PartyRole[]? TrySeatRoles(bool swap, Dictionary<PartyRole, (int? Line, bool? Accretion)> requests, PartyRole[] order)
    {
        var slots = new PartyRole[8];
        var used = new bool[8];

        bool Fill(int slot)
        {
            if (slot == 8) return true;
            foreach (var role in order)
            {
                if (used[(int)role] || !SlotAllows(role, slot, swap)) continue;
                if (requests.TryGetValue(role, out var want)
                    && ((want.Line is { } line && SlotLine[slot] != line)
                        || (want.Accretion is { } accretion && SlotAccretion[slot] != accretion)))
                    continue;
                slots[slot] = role;
                used[(int)role] = true;
                if (Fill(slot + 1)) return true;
                used[(int)role] = false;
            }
            return false;
        }

        return Fill(0) ? slots : null;
    }

    // Which roles a final slot can hold. Swap(3,7) trades the one non-tank support slot with
    // the DPS block's Accretion slot, so it decides which of 3/7 is which.
    private static bool SlotAllows(PartyRole role, int slot, bool swap)
    {
        var dpsSlot = swap ? slot is 3 or >= 4 and <= 6 : slot >= 4;
        var nonTankSupportSlot = swap ? slot == 7 : slot == 3;
        if (dpsSlot) return role.IsDps();
        if (nonTankSupportSlot) return !role.IsDps() && !role.IsTank();
        return !role.IsDps();
    }

    private uint NextSlap()
    {
        return rng.NextObj(ActionId.SlapHappy_Left, ActionId.SlapHappy_Right);
    }

    // Left = one shocking-impact stack target; Right = three shockwave cones. When
    // allOnPlayer is set, every target slot collapses onto the player so the cones
    // all land on the practising player.
    private RoleList NextConeTargets(SimParty party, uint slap, bool allOnPlayer)
    {
        if (slap == ActionId.SlapHappy_Left)
            return new RoleList(party, [allOnPlayer ? subject : rng.NextRole()]);

        return allOnPlayer
                   ? new RoleList(party, [subject, subject, subject])
                   : new RoleList(party, [rng.NextDpsRole(), rng.NextTankRole(), rng.NextHealerRole()]);
    }
}
