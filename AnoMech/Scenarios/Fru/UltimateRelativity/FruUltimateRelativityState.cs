using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Scenarios.Fru.UltimateRelativity;

public enum RelativityAssignment { ShortDps1, ShortDps2, MidDps, LongDps, ShortSupport, MidSupport, LongSupport1, LongSupport2 }

public sealed class FruUltimateRelativityStateOverrides
{
    public int? RelativeNorth { get; set; }
    public bool? DpsIce { get; set; }
    public int? ClockwiseMask { get; set; }
    public PartyRole? PracticeRole { get; set; }
    public RelativityAssignment? PracticeAssignment { get; set; }
    public void Validate()
    {
        if (RelativeNorth is < 0 or > 7 || ClockwiseMask is < 0 or > 255) throw new ArgumentOutOfRangeException(nameof(RelativeNorth));
        if (PracticeRole.HasValue != PracticeAssignment.HasValue) throw new ArgumentException("Choose both practice role and assignment.");
        if (PracticeRole is { } role && PracticeAssignment is { } assignment &&
            (role.IsDps() != ((int)assignment < 4) || (int)role is < 0 or > 7 || (int)assignment is < 0 or > 7))
            throw new ArgumentException("Assignment must match the selected role's support/DPS group.");
    }
}

public sealed class FruUltimateRelativityState
{
    public IReadOnlyList<PartyRole> Roles { get; }
    public IReadOnlyList<PartyRole> StackTargets { get; }
    public PartyRole ShellTarget { get; }
    public int RelativeNorth { get; }
    public bool DpsIce { get; }
    public int ClockwiseMask { get; }
    public static IReadOnlyList<int> Clocks { get; } = Array.AsReadOnly(new[] { 7, 1, 2, 0, 4, 6, 5, 3 });

    public FruUltimateRelativityState(Rng rng, FruUltimateRelativityStateOverrides settings)
    {
        settings.Validate();
        RelativeNorth = settings.RelativeNorth ?? rng.Next(8);
        DpsIce = settings.DpsIce ?? rng.NextBool();
        ClockwiseMask = settings.ClockwiseMask ?? rng.Next(256);
        var roles = rng.Shuffle(Enumerable.Range(4, 4).Select(i => (PartyRole)i))
            .Concat(rng.Shuffle(Enumerable.Range(0, 4).Select(i => (PartyRole)i))).ToArray();
        if (settings.PracticeAssignment is { } assignment && settings.PracticeRole is { } role)
        {
            var old = Array.IndexOf(roles, role);
            (roles[old], roles[(int)assignment]) = (roles[(int)assignment], roles[old]);
        }
        Roles = Array.AsReadOnly(roles);
        var stacks = new List<PartyRole>();
        for (var order = 1; order <= 3; order++)
        {
            var candidates = roles.Where(r => FireOrder(r) != order && !stacks.Contains(r)).ToArray();
            stacks.Add(candidates[rng.Next(candidates.Length)]);
        }
        StackTargets = stacks.AsReadOnly();
        ShellTarget = roles[rng.Next(8)];
    }

    public RelativityAssignment Assignment(PartyRole role) => (RelativityAssignment)Roles.ToList().IndexOf(role);
    public int FireGroup(PartyRole role) => Assignment(role) switch
    {
        RelativityAssignment.ShortDps1 or RelativityAssignment.ShortDps2 or RelativityAssignment.ShortSupport => 1,
        RelativityAssignment.MidDps or RelativityAssignment.MidSupport => 2,
        _ => 3,
    };
    public bool IsIce(PartyRole role) => Assignment(role) == (DpsIce ? RelativityAssignment.LongDps : RelativityAssignment.ShortSupport);
    public int FireOrder(PartyRole role) => IsIce(role) ? 0 : FireGroup(role);
    public int LaserOrder(PartyRole role) => FireGroup(role) switch { 3 => 1, 1 => 2, _ => 3 };
    public int ReturnOrder(PartyRole role) => FireGroup(role) == 3 ? 2 : 1;
    public ushort FinalStatus(PartyRole role) => Assignment(role) switch
    {
        RelativityAssignment.MidDps => 2461,
        RelativityAssignment.LongDps or RelativityAssignment.LongSupport1 or RelativityAssignment.LongSupport2 => 2456,
        _ => 2460,
    };
    public Sign? Marker(PartyRole role) => Assignment(role) switch
    {
        RelativityAssignment.ShortDps1 => Sign.Attack1, RelativityAssignment.ShortDps2 => Sign.Attack2,
        RelativityAssignment.LongSupport1 => Sign.Bind1, RelativityAssignment.LongSupport2 => Sign.Bind2,
        RelativityAssignment.ShortSupport => Sign.Attack3, RelativityAssignment.MidSupport => Sign.Ignore1,
        RelativityAssignment.MidDps => Sign.Ignore2, RelativityAssignment.LongDps => Sign.Bind3,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
    public int Clock(PartyRole role) => Clocks[(int)Assignment(role)];
    public bool Clockwise(int clock) => (ClockwiseMask & (1 << clock)) != 0;
    public Vector3 Direction(int clock)
    {
        var angle = (clock + RelativeNorth) * MathF.PI / 4f;
        return new(MathF.Sin(angle), 0f, -MathF.Cos(angle));
    }
    public Vector3 Hourglass(int clock) => Direction(clock) * 9.5f;
}
