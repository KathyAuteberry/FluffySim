using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using AnoMech.Core;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game.Party;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.Forsaken;

// Only CastBarAction goes over the wire; FromCastBarAction maps it back to the static instance.
public sealed record EndAttack(uint CastBarAction)
{
    // Future's End cleaves the half facing its aimed player as the cast starts, Past's End the other half.
    public static readonly EndAttack FuturesEnd = new(ActionId.FuturesEnd)
    {
        KefkaHit = EndHit(ActionId.FuturesEndResolve),
        CloneHit = EndHit(ActionId.FuturesEndCloneResolve),
        AllThingsEnding = AllThingsEndingHit(ActionId.AllThingsEndingFuture, 0f),
    };

    public static readonly EndAttack PastsEnd = new(ActionId.PastsEnd)
    {
        KefkaHit = EndHit(ActionId.PastsEndResolve),
        CloneHit = EndHit(ActionId.PastsEndCloneResolve),
        AllThingsEnding = AllThingsEndingHit(ActionId.AllThingsEndingPast, MathF.PI),
    };

    [JsonIgnore] public EnemyAction KefkaHit { get; private init; } = null!;
    [JsonIgnore] public EnemyAction CloneHit { get; private init; } = null!;
    [JsonIgnore] public EnemyAction AllThingsEnding { get; private init; } = null!;

    public static EndAttack FromCastBarAction(uint castBarAction)
        => castBarAction == PastsEnd.CastBarAction ? PastsEnd : FuturesEnd;

    private static EnemyAction EndHit(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 6f },
        Effects = [Damage(DmuForsakenActions.MagicallyVulnerable), DmuForsakenActions.LongMagicVulnerability],
        Timing = new() { DamageDelay = 0.75f },
    };

    // Snapshots as the ability lands, not at bar end: from bar end there is no time left to reach
    // the tower that follows. UNVERIFIED in game.
    private static EnemyAction AllThingsEndingHit(uint actionId, float rotation) => new(actionId)
    {
        Cast = new() { AnimationLock = 3f, Rotation = rotation },
        Area = new() { Size = DmuForsakenActions.AllThingsEndingHalfCone },
        Effects = [Damage(DmuForsakenActions.MagicallyVulnerable, Lethal)],
        Timing = new() { ResolveSnapshotOffset = CastSpec.ActionEffectOffset },
    };
}

public sealed class DmuForsakenState
{
    private static readonly PartyRole[] Supports = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer];
    private static readonly PartyRole[] Dps = [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    public const int Towers = 8;

    private readonly Rng rng = Rng.Detached;

    public EndAttack[] EndAttacks { get; }
    public Direction NewNorth { get; }
    public int Rotation { get; }
    // Per tower: soaker i, in role order, takes that tower's lockon LockonDeals[tower][i]. Rolled
    // here rather than as towers resolve, so a seed deals the same spells however the soakers
    // happened to stand.
    public int[][] LockonDeals { get; }
    public Dictionary<PartyRole, uint> Lockons = [];
    // Per end attack, per caster (Kefka, then the three clones): whose spot its All Things Ending aims at.
    public PartyRole[][] AllThingsEndingAims { get; }

    // The self-marking macro's coin: false gives the first holder in role order the 1.
    public bool FinalConeMarksSwapped { get; }
    public bool FinalAoeMarksSwapped { get; }

    public DmuForsakenState(Rng rng, DmuForsakenStateOverrides overrides)
    {
        this.rng = rng;
        EndAttacks = overrides.EndAttacks.Select(e => e ?? NextEnd()).ToArray();
        NewNorth = overrides.NewNorth ?? rng.NextDirection();
        Rotation = overrides.Rotation ?? rng.NextSign();

        var supportLockon = overrides.SupportLockon ?? rng.NextObj(LockonId.ForsakenChariot, LockonId.ForsakenCone);
        var dpsLockon = supportLockon == LockonId.ForsakenChariot ? LockonId.ForsakenCone : LockonId.ForsakenChariot;
        foreach (var role in Supports) Lockons[role] = supportLockon;
        foreach (var role in Dps) Lockons[role] = dpsLockon;
        Lockons[overrides.SupportStackRole ?? rng.NextObj(Supports)] = LockonId.ForsakenStack;
        Lockons[overrides.DpsStackRole ?? rng.NextObj(Dps)] = LockonId.ForsakenStack;

        FinalConeMarksSwapped = overrides.FinalConeMarksSwapped ?? rng.NextBool();
        FinalAoeMarksSwapped = overrides.FinalAoeMarksSwapped ?? rng.NextBool();
        LockonDeals = Enumerable.Range(0, Towers)
            .Select(_ => overrides.ReassignLockonsInRoleOrder ? new[] { 0, 1, 2, 3 } : rng.Shuffle(0, 1, 2, 3).ToArray())
            .ToArray();
        AllThingsEndingAims = Enumerable.Range(0, EndAttacks.Length)
            .Select(_ => Enumerable.Range(0, 4).Select(_ => rng.NextRole()).ToArray())
            .ToArray();
        DiagnosticLog.Info($"[DmuForsaken] Lockons {string.Join(",", Enum.GetValues<PartyRole>().Select(r => Lockons[r]))}");
    }

    private DmuForsakenState(EndAttack[] endAttacks, Direction newNorth, int rotation, Dictionary<PartyRole, uint> lockons,
        bool finalConeMarksSwapped, bool finalAoeMarksSwapped)
    {
        EndAttacks = endAttacks;
        LockonDeals = [];
        AllThingsEndingAims = [];
        NewNorth = newNorth;
        Rotation = rotation;
        Lockons = lockons;
        FinalConeMarksSwapped = finalConeMarksSwapped;
        FinalAoeMarksSwapped = finalAoeMarksSwapped;
    }

    public uint[] EncodeEndAttacks() => EndAttacks.Select(e => e.CastBarAction).ToArray();
    public bool[] EncodeFlags() => [FinalConeMarksSwapped, FinalAoeMarksSwapped];

    public static DmuForsakenState? FromNetworkReplay(uint[] endAttacks, float newNorthRadians, int rotation,
        Dictionary<PartyRole, uint> lockons, bool[] flags)
    {
        if (endAttacks.Length != 4 || flags.Length != 2 || rotation is not (1 or -1)) return null;
        return new(endAttacks.Select(EndAttack.FromCastBarAction).ToArray(), new Direction(newNorthRadians), rotation,
            new Dictionary<PartyRole, uint>(lockons), flags[0], flags[1]);
    }

    public (Direction, Direction) GetTowers(int index)
    {
        var north = NewNorthAt(index);
        return (north.Rotate(-1), north.Rotate(1));
    }

    public Direction NewNorthAt(int index) => NewNorth.Rotate(index * Rotation);

    private EndAttack NextEnd() => rng.NextObj(EndAttack.FuturesEnd, EndAttack.PastsEnd);
}
