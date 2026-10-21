using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P4Eyes;

public sealed class DsrP4EyesState
{
    public const int Dives = 4;
    public PartyRole SoulMarked { get; }
    public IReadOnlyCollection<PartyRole> FirstRed { get; }
    public IReadOnlyList<(int, int)> DivePicks { get; }
    public Dictionary<Vector3, PartyRole[]> OrbSoakers { get; } = [];
    public HashSet<PartyRole> Red { get; } = [];
    public List<PartyRole>[] DiveTargets { get; } = [[], [], [], []];

    public DsrP4EyesState(Rng rng, DsrP4EyesStateOverrides? overrides = null)
    {
        overrides ??= new DsrP4EyesStateOverrides();
        var roles = Enum.GetValues<PartyRole>();
        SoulMarked = overrides.SoulMarked ?? roles[rng.Next(roles.Length)];
        var firstRed = rng.Shuffle(roles).Take(4).ToList();
        FirstRed = overrides.FirstRed ?? firstRed;
        var picks = new List<(int, int)>();
        for (var i = 0; i < Dives; i++)
        {
            var pair = rng.Shuffle(new[] { 0, 1, 2, 3 }).Take(2).ToList();
            picks.Add((pair[0], pair[1]));
        }
        DivePicks = overrides.DivePicks ?? picks;
    }

    public bool HoldsRed(PartyRole role) => Red.Contains(role);
}
