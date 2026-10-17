using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.FallOfFaith;

public enum FaithPracticeAssignment { Random, Tether1, Tether2, Tether3, Tether4, Untethered }

public sealed class FruFallOfFaithStateOverrides
{
    public FaithPracticeAssignment PracticeAssignment { get; set; }
    public bool? PlayerFire { get; set; }
    public bool Advanced { get; set; }
    public int? TetherPermutation { get; set; }
    public int? FireMask { get; set; }
    public bool PracticeOutlines { get; set; }

    public void Validate()
    {
        if (!Enum.IsDefined(PracticeAssignment) || TetherPermutation is < 0 or >= 1680 || FireMask is < 0 or > 15)
            throw new ArgumentException("Choose a valid tether permutation (0–1679) and Fire mask (0–15).");
    }
}

public sealed class FruFallOfFaithState
{
    public static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();
    public static IReadOnlyList<PartyRole[]> Permutations { get; } =
        (from a in Roles from b in Roles where b != a
         from c in Roles where c != a && c != b
         from d in Roles where d != a && d != b && d != c select new[] { a, b, c, d }).ToArray();
    public IReadOnlyList<PartyRole> Targets { get; }
    public IReadOnlyList<PartyRole> Untethered { get; }
    public int FireMask { get; }
    public int Permutation { get; }

    public FruFallOfFaithState(Rng rng, FruFallOfFaithStateOverrides settings, PartyRole human)
    {
        settings.Validate();
        var candidates = Enumerable.Range(0, Permutations.Count).Where(i => settings.Advanced || settings.PracticeAssignment switch
        {
            FaithPracticeAssignment.Random => true,
            FaithPracticeAssignment.Untethered => !Permutations[i].Contains(human),
            _ => Permutations[i][(int)settings.PracticeAssignment - 1] == human,
        }).ToArray();
        Permutation = settings.Advanced && settings.TetherPermutation is { } exact ? exact : candidates[rng.Next(candidates.Length)];
        Targets = Array.AsReadOnly((PartyRole[])Permutations[Permutation].Clone());
        Untethered = Roles.Where(r => !Targets.Contains(r)).ToArray();
        var mask = settings.Advanced && settings.FireMask is { } fire ? fire : rng.Next(16);
        var order = Order(human);
        if (!settings.Advanced && order >= 0 && settings.PlayerFire is { } playerFire)
            mask = playerFire ? mask | (1 << order) : mask & ~(1 << order);
        FireMask = mask;
    }

    public int Order(PartyRole role) => Targets.ToList().IndexOf(role);
    public bool Fire(int order) => (FireMask & (1 << order)) != 0;
}
