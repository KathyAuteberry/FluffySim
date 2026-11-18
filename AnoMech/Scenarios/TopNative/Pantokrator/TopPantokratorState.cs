using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.TopNative.Pantokrator;

public sealed class TopPantokratorStateOverrides
{
    public PerRoleSetting<int> Number { get; set; } = new();
    // Bearing of the first Flame Thrower pair from Omega's facing, 0-150 in 30° steps.
    public int? FirstFlameBearing { get; set; }
    public bool? FlamesClockwise { get; set; }
    public PerRoleSetting<bool> FirstWaveCannon { get; set; } = new();
    // A pair adjusts when both its players start in the same half of the priority line.
    public bool? OnesAdjust { get; set; }
    public bool? TwosAdjust { get; set; }
    public bool? ThreesAdjust { get; set; }
    public bool? FoursAdjust { get; set; }

    public bool?[] AdjustingPairs => [OnesAdjust, TwosAdjust, ThreesAdjust, FoursAdjust];

    public SettingsConflicts Validate()
    {
        var conflicts = TopNativeNumbers.Validate(Number, "number");
        if (AdjustingPairs.All(a => a.HasValue) && AdjustingPairs.Count(a => a == true) % 2 == 1)
            conflicts.Add("an odd number of pairs adjusting sides");
        if (!PerRole.SeatsActive) return conflicts;
        var forced = PerRole.All.Where(r => Number[r] is >= 1 and <= 4).ToDictionary(r => r, r => Number[r]!.Value);
        if (forced.Count > 0 && AdjustingPairs.Any(a => a.HasValue) && !conflicts.Any && TopPantokratorPairs.Deals(forced, AdjustingPairs).Count == 0)
            conflicts.Add("these seat numbers with these adjusting pairs");
        conflicts.AtMost(3, PerRole.All.Where(r => FirstWaveCannon[r] == true).ToList(), "the first Wave Cannon");
        conflicts.AtMost(3, PerRole.All.Where(r => FirstWaveCannon[r] == false).ToList(), "the second Wave Cannon");
        return conflicts;
    }
}

// Every way to deal the numbers that keeps the forced seats and the wanted adjusting pairs.
public static class TopPantokratorPairs
{
    public static bool Adjusts(IReadOnlyDictionary<PartyRole, int> numbers, int number)
        => TopPantokratorAi.Priority.Take(4).Count(r => numbers[r] == number) != 1;

    public static List<Dictionary<PartyRole, int>> Deals(IReadOnlyDictionary<PartyRole, int> forced, bool?[] adjusting)
    {
        var deals = new List<Dictionary<PartyRole, int>>();
        var deal = new Dictionary<PartyRole, int>();
        var left = new[] { 2, 2, 2, 2 };
        Place(0);
        return deals;

        void Place(int seat)
        {
            if (seat == TopPantokratorAi.Priority.Length)
            {
                if (Enumerable.Range(1, 4).All(n => adjusting[n - 1] is not { } wanted || Adjusts(deal, n) == wanted))
                    deals.Add(new Dictionary<PartyRole, int>(deal));
                return;
            }
            var role = TopPantokratorAi.Priority[seat];
            for (var n = 1; n <= 4; n++)
            {
                if (left[n - 1] == 0 || forced.TryGetValue(role, out var f) && f != n) continue;
                left[n - 1]--;
                deal[role] = n;
                Place(seat + 1);
                left[n - 1]++;
            }
        }
    }
}

public sealed class TopPantokratorState
{
    public TopNativeNumbers Numbers { get; }
    public int FirstFlameBearing { get; }
    // +1: the flames step clockwise.
    public int Rotation { get; }
    public IReadOnlyList<PartyRole> FirstWaveCannon { get; }
    public IReadOnlyList<PartyRole> SecondWaveCannon { get; }

    public TopPantokratorState(Rng rng, SimParty party, TopPantokratorStateOverrides overrides)
    {
        Numbers = DealNumbers(rng, party, overrides);
        FirstFlameBearing = overrides.FirstFlameBearing ?? rng.Next(6) * 30;
        Rotation = overrides.FlamesClockwise is { } cw ? (cw ? 1 : -1) : rng.NextSign();
        var nonTanks = PerRole.All.Where(r => !r.IsTank()).ToArray();
        var forced = overrides.FirstWaveCannon.Resolve(party.PlayerRole).Where(r => !r.Role.IsTank()).ToDictionary(r => r.Role, r => r.Value);
        var first = forced.Where(kv => kv.Value).Select(kv => kv.Key).Take(3).ToList();
        foreach (var role in rng.Shuffle(nonTanks.Where(r => !forced.ContainsKey(r))))
            if (first.Count < 3) first.Add(role);
        foreach (var role in nonTanks.Where(r => forced.TryGetValue(r, out var v) && !v))
            if (first.Count < 3 && !first.Contains(role)) first.Add(role);
        FirstWaveCannon = first.OrderBy(r => r).ToArray();
        SecondWaveCannon = nonTanks.Where(r => !first.Contains(r)).ToArray();
        DiagnosticLog.Info($"[TopPantokrator] Numbers {string.Join(",", Numbers.ByRole.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"))}; first flame {FirstFlameBearing}° rotation {Rotation}; first wave cannon {string.Join(",", FirstWaveCannon)}");
    }

    private static TopNativeNumbers DealNumbers(Rng rng, SimParty party, TopPantokratorStateOverrides overrides)
    {
        if (overrides.AdjustingPairs.All(a => a == null)) return new TopNativeNumbers(rng, party, overrides.Number);
        var forced = overrides.Number.Resolve(party.PlayerRole).Where(r => r.Value is >= 1 and <= 4).ToDictionary(r => r.Role, r => r.Value);
        var deals = TopPantokratorPairs.Deals(forced, overrides.AdjustingPairs);
        if (deals.Count > 0) return TopNativeNumbers.FromRoles(deals[rng.Next(deals.Count)]);
        DiagnosticLog.Info("[TopPantokrator] No deal fits the forced numbers and adjusting pairs; ignoring the adjusting pairs.");
        return new TopNativeNumbers(rng, party, overrides.Number);
    }

    private TopPantokratorState(TopNativeNumbers numbers, int firstFlameBearing, int rotation, PartyRole[] firstWaveCannon)
    {
        Numbers = numbers;
        FirstFlameBearing = firstFlameBearing;
        Rotation = rotation;
        FirstWaveCannon = firstWaveCannon;
        SecondWaveCannon = PerRole.All.Where(r => !r.IsTank() && !firstWaveCannon.Contains(r)).ToArray();
    }

    public static TopPantokratorState FromNetworkReplay(Dictionary<PartyRole, int> numbers, int firstFlameBearing, int rotation, PartyRole[] firstWaveCannon)
        => new(TopNativeNumbers.FromRoles(numbers), firstFlameBearing, rotation, firstWaveCannon);

    // Bearing from north of flame pair k (Omega faces north).
    public float FlameBearing(int k) => FirstFlameBearing + Rotation * 30f * k;
}
