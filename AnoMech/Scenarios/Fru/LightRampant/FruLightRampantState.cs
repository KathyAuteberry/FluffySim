using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.LightRampant;

public sealed class FruLightRampantState
{
    public static IReadOnlyList<(PartyRole A, PartyRole B)> PuddlePairs { get; } =
        (from a in Enumerable.Range(0, 8) from b in Enumerable.Range(a + 1, 7 - a)
         select ((PartyRole)a, (PartyRole)b)).ToArray();

    public IReadOnlyList<PartyRole> PuddleTargets { get; }
    public IReadOnlyList<int> InitialLightsteeped { get; }
    public int WeightEdge { get; }
    public bool NorthOrbFirst { get; }
    public bool Spread { get; }
    public bool PairTargetsDps { get; }
    public IReadOnlyList<PartyRole> ChainRing { get; private set; } = [];
    public IReadOnlyList<PartyRole> WeightTargets { get; private set; } = [];

    public FruLightRampantState(Rng rng, FruLightRampantStateOverrides settings)
    {
        settings.Validate();
        var pair = PuddlePairs[settings.PuddlePair ?? rng.Next(28)];
        PuddleTargets = [pair.A, pair.B];
        var extra = settings.ExtraLightsteepedMask ?? rng.Shuffle(Enumerable.Range(0, 8)).Take(4).Sum(i => 1 << i);
        InitialLightsteeped = Enumerable.Range(0, 8)
            .Select(i => (PuddleTargets.Contains((PartyRole)i) ? 1 : 0) + ((extra >> i) & 1)).ToArray();
        WeightEdge = settings.WeightEdge ?? rng.Next(6);
        NorthOrbFirst = settings.NorthOrbFirst ?? rng.NextBool();
        Spread = settings.Spread ?? rng.NextBool();
        PairTargetsDps = settings.PairTargetsDps ?? rng.NextBool();
    }

    // The supported starting formation is a ring. Logs establish its cycle, not the server's
    // general matching algorithm for arbitrary/tangled prepositions.
    public void BindChains(IEnumerable<(PartyRole Role, Vector3 Position)> members)
    {
        ChainRing = members.Where(m => !PuddleTargets.Contains(m.Role))
            .OrderBy(m => MathF.Atan2(m.Position.X, -m.Position.Z)).Select(m => m.Role).ToArray();
        if (ChainRing.Count != 6 || ChainRing.Distinct().Count() != 6)
            throw new InvalidOperationException("Light Rampant requires six distinct chained players.");
        WeightTargets = [ChainRing[WeightEdge], ChainRing[(WeightEdge + 1) % 6]];
    }
}
