using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.TopNative;

// The In Line numbers P1 deals twice: two players each of 1 to 4.
public sealed class TopNativeNumbers
{
    private readonly Dictionary<PartyRole, int> numbers;

    public TopNativeNumbers(Rng rng, SimParty party, PerRoleSetting<int> overrides)
    {
        var order = new RoleListBuilder
        {
            Slots = overrides.Resolve(party.PlayerRole)
                .Where(r => r.Value is >= 1 and <= 4)
                .ToDictionary(r => r.Role, r => new[] { 2 * (r.Value - 1), 2 * (r.Value - 1) + 1 }),
        }.Build(rng, party);
        numbers = Enumerable.Range(0, 8).ToDictionary(i => order[i], i => i / 2 + 1);
    }

    private TopNativeNumbers(Dictionary<PartyRole, int> numbers) => this.numbers = numbers;

    public static TopNativeNumbers FromRoles(IReadOnlyDictionary<PartyRole, int> numbers) => new(numbers.ToDictionary(kv => kv.Key, kv => kv.Value));

    public int this[PartyRole role] => numbers[role];

    public IReadOnlyDictionary<PartyRole, int> ByRole => numbers;

    public IReadOnlyList<PartyRole> WithNumber(int number, IReadOnlyList<PartyRole> priority)
        => priority.Where(r => numbers[r] == number).ToArray();

    public static SettingsConflicts Validate(PerRoleSetting<int> setting, string what)
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;
        for (var n = 1; n <= 4; n++)
        {
            var number = n;
            conflicts.AtMost(2, PerRole.All.Where(r => setting[r] == number).ToList(), $"{what} {number}");
        }
        return conflicts;
    }
}
