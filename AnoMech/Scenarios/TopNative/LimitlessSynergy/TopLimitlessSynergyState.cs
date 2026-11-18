using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.TopNative.LimitlessSynergy;

public sealed class TopLimitlessSynergyStateOverrides
{
    public bool? BladedanceNorthEast { get; set; }
}

public sealed class TopLimitlessSynergyState
{
    // Bladedance clones stand on opposite intercardinals; the others each on a 30° step, at 22y.
    public float BladedanceNorthBearing { get; }
    public bool BladedanceNorthIsF { get; }
    public float SagittariusBearing { get; }
    public float MeteorBearing { get; }
    public float BeyondDefenseBearing { get; }
    public IReadOnlyList<PartyRole> TetherSeeds { get; }
    public IReadOnlyList<PartyRole> Flares { get; }
    public bool BeyondDefenseOnSecondClosest { get; }
    public bool PilePitchOnSecondClosest { get; }

    public TopLimitlessSynergyState(Rng rng, SimParty party, TopLimitlessSynergyStateOverrides overrides)
    {
        BladedanceNorthBearing = overrides.BladedanceNorthEast is { } ne ? (ne ? 45f : 315f) : rng.NextObj(45f, 315f);
        BladedanceNorthIsF = rng.NextBool();
        var steps = rng.Shuffle(Enumerable.Range(1, 11).Where(i => i != 6).Select(i => i * 30f)).ToArray();
        SagittariusBearing = steps[0];
        MeteorBearing = steps[1];
        BeyondDefenseBearing = steps[2];
        TetherSeeds = rng.Shuffle(PerRole.All.Where(r => !r.IsTank())).Take(2).ToArray();
        Flares = rng.Shuffle(PerRole.All).Take(3).OrderBy(r => r).ToArray();
        BeyondDefenseOnSecondClosest = rng.NextBool();
        PilePitchOnSecondClosest = rng.NextBool();
        DiagnosticLog.Info($"[TopLimitless] bladedance {BladedanceNorthBearing}, sagittarius {SagittariusBearing}, meteor {MeteorBearing}, beyond defense {BeyondDefenseBearing}, tethers {string.Join(",", TetherSeeds)}");
    }

    private TopLimitlessSynergyState(float[] bearings, bool[] flags, PartyRole[] seeds, PartyRole[] flares)
    {
        (BladedanceNorthBearing, SagittariusBearing, MeteorBearing, BeyondDefenseBearing) = (bearings[0], bearings[1], bearings[2], bearings[3]);
        (BladedanceNorthIsF, BeyondDefenseOnSecondClosest, PilePitchOnSecondClosest) = (flags[0], flags[1], flags[2]);
        TetherSeeds = seeds;
        Flares = flares;
    }

    public float[] EncodeBearings() => [BladedanceNorthBearing, SagittariusBearing, MeteorBearing, BeyondDefenseBearing];
    public bool[] EncodeFlags() => [BladedanceNorthIsF, BeyondDefenseOnSecondClosest, PilePitchOnSecondClosest];

    public static TopLimitlessSynergyState? FromNetworkReplay(float[] bearings, bool[] flags, PartyRole[] seeds, PartyRole[] flares)
        => bearings.Length == 4 && flags.Length == 3 ? new(bearings, flags, seeds, flares) : null;

    public float BladedanceSouthBearing => BladedanceNorthBearing + 180f;
}
