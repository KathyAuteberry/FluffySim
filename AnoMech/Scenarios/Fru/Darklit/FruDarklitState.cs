using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.Darklit;

public enum DarklitTetherPattern { Bowtie, Box, Hourglass }
public enum DarklitPracticeAssignment { Random, Tethered, Untethered, TetheredWater, UntetheredWater }
public enum DarklitTowerRendering { Native, PracticeOverlay }
public sealed class FruDarklitStateOverrides
{
    public bool Advanced { get; set; }
    public DarklitPracticeAssignment PracticeAssignment { get; set; }
    public DarklitTetherPattern? TetherPattern { get; set; }
    public bool? WaterAdjustment { get; set; }
    public bool? EastWing { get; set; }
    public int? ExactPattern { get; set; }
    public PartyRole? SpiritTarget { get; set; }
    public PartyRole? BaitTank { get; set; }
    public DarklitTowerRendering TowerRendering { get; set; }
    public bool TowerDiagnostics { get; set; }
    public bool PathDiagnostics { get; set; }
}
public sealed record DarklitPattern(PartyRole Tank, PartyRole Healer, PartyRole WestDps, PartyRole EastDps,
    DarklitTetherPattern Shape, PartyRole TetherWater, PartyRole UntetherWater)
{
    public IReadOnlyList<PartyRole> Tethered => [Healer, Tank, EastDps, WestDps];
    public IReadOnlyList<PartyRole> ChainRing => Shape switch
    {
        DarklitTetherPattern.Bowtie => [Healer, WestDps, Tank, EastDps],
        DarklitTetherPattern.Box => [Healer, Tank, EastDps, WestDps],
        _ => [Healer, Tank, WestDps, EastDps],
    };
    public bool IsTethered(PartyRole role) => Tethered.Contains(role);
    public bool HasWater(PartyRole role) => role == TetherWater || role == UntetherWater;
    public bool RequiresDpsSideSwap => (int)WestDps / 2 == (int)EastDps / 2;
}
public sealed class FruDarklitState
{
    public static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();
    public static readonly DarklitPattern[] Patterns = GeneratePatterns().ToArray();
    public int PatternIndex { get; }
    public DarklitPattern Pattern => Patterns[PatternIndex];
    public bool EastWing { get; }
    public PartyRole SpiritTarget { get; }
    public PartyRole BaitTank { get; }
    public DarklitTowerRendering TowerRendering { get; }

    private static IEnumerable<DarklitPattern> GeneratePatterns()
    {
        foreach (var tank in Roles.Where(r => r.IsTank()))
        foreach (var healer in Roles.Where(r => r is PartyRole.RegenHealer or PartyRole.ShieldHealer))
        foreach (var west in Roles.Where(r => r.IsDps()))
        foreach (var east in Roles.Where(r => r > west && r.IsDps()))
        foreach (var shape in Enum.GetValues<DarklitTetherPattern>())
        {
            PartyRole[] tethered = [healer, tank, east, west];
            foreach (var tw in tethered)
            foreach (var uw in Roles.Except(tethered))
                yield return new(tank, healer, west, east, shape, tw, uw);
        }
    }
    public static int[] Candidates(FruDarklitStateOverrides o, PartyRole human)
        => Enumerable.Range(0, Patterns.Length).Where(i =>
        {
            if (o.Advanced) return o.ExactPattern is null || o.ExactPattern == i;
            var p = Patterns[i];
            if (o.TetherPattern is { } shape && p.Shape != shape) return false;
            if (o.WaterAdjustment is { } water && new DarklitManaAssignments(p).WaterAdjusted != water) return false;
            return o.PracticeAssignment switch
            {
                DarklitPracticeAssignment.Tethered => p.IsTethered(human),
                DarklitPracticeAssignment.Untethered => !p.IsTethered(human),
                DarklitPracticeAssignment.TetheredWater => human == p.TetherWater,
                DarklitPracticeAssignment.UntetheredWater => human == p.UntetherWater,
                _ => true,
            };
        }).ToArray();
    public FruDarklitState(Rng rng, FruDarklitStateOverrides o, PartyRole human, PartyRole defaultBait)
    {
        if (o.ExactPattern is < 0 || o.ExactPattern >= Patterns.Length || o.BaitTank is { } bait && !bait.IsTank()
            || o.SpiritTarget is { } spirit && !Roles.Contains(spirit)) throw new ArgumentException("Invalid Darklit override.");
        var candidates = Candidates(o, human);
        if (candidates.Length == 0) throw new ArgumentException("No encounter-valid Darklit assignment matches these settings.");
        PatternIndex = candidates[rng.Next(candidates.Length)];
        EastWing = o.EastWing ?? rng.NextBool();
        SpiritTarget = o.Advanced ? o.SpiritTarget ?? Roles[rng.Next(8)] : Roles[rng.Next(8)];
        BaitTank = o.BaitTank ?? defaultBait;
        TowerRendering = o.TowerRendering;
    }
}
