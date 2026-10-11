using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.Sanctity1;

public enum SacredSeverPractice { Random, Sword1, Sword2, UnmarkedStay, UnmarkedSwap }

public sealed class DsrSanctity1StateOverrides
{
    public bool AdvancedMode { get; set; }
    public SacredSeverPractice PracticeAssignment { get; set; }
    public bool? Clockwise { get; set; }
    public int? ZephirinPosition { get; set; }
    public int? ThordanPosition { get; set; }
    public int? EyeSeparation { get; set; }
    public PartyRole? Sword1 { get; set; }
    public PartyRole? Sword2 { get; set; }
}

public sealed class DsrSanctity1State
{
    public static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();
    public static readonly (PartyRole One, PartyRole Two)[] SwordPairs =
        (from one in Roles from two in Roles where one != two select (one, two)).ToArray();
    public int ZephirinPosition { get; }
    public bool Clockwise { get; }
    public int ThordanPosition { get; }
    public int EyePosition { get; }
    public PartyRole Sword1 { get; }
    public PartyRole Sword2 { get; }
    private readonly int[] groups;
    public bool MoveEarly => (ZephirinPosition % 2 == 1) == Clockwise;
    public Vector3 ZephirinSpawn => Direction((ZephirinPosition * 2 + 1) * MathF.PI / 4) * 15f;
    public Vector3 ThordanSpawn => Direction(ThordanPosition * MathF.PI / 4) * 25f;
    public Vector3 EyeSpawn => Direction(EyePosition * MathF.PI / 4) * 40f;

    public DsrSanctity1State(Rng rng, DsrSanctity1StateOverrides o, PartyRole playerRole)
    {
        ZephirinPosition = Roll(rng, o.AdvancedMode ? o.ZephirinPosition : null, 4);
        Clockwise = o.Clockwise ?? rng.NextBool();
        ThordanPosition = Roll(rng, o.AdvancedMode ? o.ThordanPosition : null, 8);
        int[] separations = [2,3,4,5,6];
        EyePosition = (ThordanPosition + separations[Roll(rng, o.AdvancedMode ? o.EyeSeparation : null, 5)]) % 8;
        var pairs = SwordPairs.Where(pair => o.AdvancedMode
            ? (o.Sword1 is null || pair.One == o.Sword1) && (o.Sword2 is null || pair.Two == o.Sword2)
            : MatchesPractice(pair.One, pair.Two, playerRole, o.PracticeAssignment)).ToArray();
        if (pairs.Length == 0) throw new ArgumentException("Sword targets must be distinct and compatible with the practice assignment.");
        (Sword1, Sword2) = pairs[rng.Next(pairs.Length)];
        groups = AssignGroups(Sword1, Sword2);
    }

    public static int BaseGroup(PartyRole role) => (int)role % 2;
    public static PartyRole Partner(PartyRole role) => (PartyRole)((int)role ^ 1);
    public int Group(PartyRole role) => groups[(int)role];
    public bool Swaps(PartyRole role) => Group(role) != BaseGroup(role);
    public static int[] AssignGroups(PartyRole one, PartyRole two)
    {
        if (one == two) throw new ArgumentException("Sword targets must be distinct.");
        var result = Roles.Select(BaseGroup).ToArray();
        Adjust(one, 0); Adjust(two, 1);
        return result;
        void Adjust(PartyRole role, int group)
        {
            if (result[(int)role] == group) return;
            result[(int)role] = group;
            result[(int)Partner(role)] = 1 - group;
        }
    }
    public static bool MatchesPractice(PartyRole one, PartyRole two, PartyRole player, SacredSeverPractice practice)
    {
        var groups = AssignGroups(one, two);
        return practice switch
        {
            SacredSeverPractice.Random => true,
            SacredSeverPractice.Sword1 => player == one,
            SacredSeverPractice.Sword2 => player == two,
            SacredSeverPractice.UnmarkedStay => player != one && player != two && groups[(int)player] == BaseGroup(player),
            SacredSeverPractice.UnmarkedSwap => player != one && player != two && groups[(int)player] != BaseGroup(player),
            _ => throw new ArgumentOutOfRangeException(nameof(practice)),
        };
    }
    public Vector3 InitialPosition(PartyRole role) => SafePosition(role, MoveEarly ? 15f : 11.7f);
    public Vector3 FinalPosition(PartyRole role) => SafePosition(role, 33.3f);
    private Vector3 SafePosition(PartyRole role, float offset)
    {
        var angle = (ZephirinPosition * 2 + 1) * MathF.PI / 4 + (Clockwise ? 1 : -1) * offset * MathF.PI / 180;
        return Direction(angle + (Group(role) == 0 ? MathF.PI : 0)) * 20f;
    }
    public IReadOnlyList<Vector3> ChargePoints(bool east)
    {
        var first = (east == Clockwise) ? MathF.PI : 0;
        var step = (Clockwise ? 1 : -1) * MathF.PI * 5 / 8;
        return [new(east ? 5 : -5,0,0), Direction(first)*21, Direction(first+step)*21, Direction(first+step*2)*21];
    }
    public IReadOnlyList<Vector3> OrbPositions(bool east)
    {
        var p = ChargePoints(east);
        return [p[0], Vector3.Lerp(p[0],p[1],.5f), p[1], Vector3.Lerp(p[1],p[2],1f/3),
            Vector3.Lerp(p[1],p[2],2f/3), p[2], Vector3.Lerp(p[2],p[3],1f/3), Vector3.Lerp(p[2],p[3],2f/3),p[3]];
    }
    public static Vector3 Direction(float clockwiseFromNorth) => new(MathF.Sin(clockwiseFromNorth),0,-MathF.Cos(clockwiseFromNorth));
    private static int Roll(Rng rng, int? value, int count) => value is { } n
        ? n >= 0 && n < count ? n : throw new ArgumentOutOfRangeException(nameof(value)) : rng.Next(count);
}
