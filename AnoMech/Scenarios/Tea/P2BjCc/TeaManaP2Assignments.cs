using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Tea.P2BjCc;

public static class TeaManaP2Assignments
{
    public static IReadOnlyList<PartyRole> Dps { get; } = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    public static IReadOnlyList<PartyRole> ThirdPassLine { get; } = [MainTank, MeleeDpsA, PhysRangedDps, CasterDps, MeleeDpsB, OffTank];
    public static IReadOnlyList<PartyRole> FinalSupportLine { get; } = [MainTank, RegenHealer, ShieldHealer, OffTank];
    public static IReadOnlyList<PartyRole> EnumerationPriority { get; } = [PhysRangedDps, CasterDps, MeleeDpsB, MeleeDpsA];

    public static PartyRole FirstPairSupport(PartyRole dps) => dps switch
    {
        MeleeDpsA => MainTank,
        MeleeDpsB => OffTank,
        PhysRangedDps => RegenHealer,
        CasterDps => ShieldHealer,
        _ => throw new ArgumentOutOfRangeException(nameof(dps)),
    };

    public static IReadOnlyList<PartyRole> WaterParty(int round, PartyRole firstWater, PartyRole firstLightning, PartyRole? waterVulnerable = null)
        => round switch
        {
            1 => [firstWater, .. Dps.Where(r => r != firstLightning)],
            2 => Dps,
            3 when waterVulnerable is { } vulnerable && vulnerable.IsDps() => [OffTank, .. Dps.Where(r => r != vulnerable)],
            _ => throw new ArgumentOutOfRangeException(nameof(round)),
        };

    public static IReadOnlyList<PartyRole> LightningParty(int round, PartyRole firstLightning)
        => round switch
        {
            1 => [MainTank, firstLightning],
            2 => [MainTank, RegenHealer],
            3 => [RegenHealer, ShieldHealer],
            _ => throw new ArgumentOutOfRangeException(nameof(round)),
        };

    public static (IReadOnlyList<PartyRole> West, IReadOnlyList<PartyRole> East) EnumerationParties(IReadOnlyList<PartyRole> marked)
    {
        if (marked.Count != 2 || marked.Distinct().Count() != 2 || marked.Any(r => !r.IsDps()))
            throw new ArgumentException("Enumeration requires two distinct DPS.", nameof(marked));
        var west = EnumerationPriority.First(marked.Contains);
        var east = EnumerationPriority.Last(marked.Contains);
        var unmarked = EnumerationPriority.Where(r => !marked.Contains(r)).ToArray();
        return ([RegenHealer, ShieldHealer, west], [east, .. unmarked]);
    }
}
