using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using static AnoMech.Scenarios.Dsr.DsrP5Constants;

namespace AnoMech.Scenarios.Dsr.P5Death;

public enum ChainSymbol { Circle, Triangle, Square, Cross }

public sealed class DsrP5DeathState
{
    public const float CasterRadius = 9f;
    public const float OutsideRadius = 23f;
    public const float ThordanRadius = 25f;
    public const float EyeRadius = 40f;
    public const float SpearBearing = 60f;
    public const float TwisterDragonBearing = 120f;
    public const float CauterizeBearing = 0f;
    public const float WingsBearing = 270f;
    public const float HeavensflameBearing = 90f;
    public const float MeteorKnightBearing = 180f;

    public float WarriorBearing { get; }
    public IReadOnlyCollection<PartyRole> Dooms { get; }
    public float ThordanBearing { get; }
    public float EyeBearing { get; }
    public bool FirstDoomSquare { get; }
    public int? PlayerCongaSlot { get; }
    public IReadOnlyList<int> NonDoomOrder { get; }
    public Dictionary<PartyRole, ChainSymbol> Symbols { get; } = [];
    public List<Vector3> Puddles { get; } = [];

    public DsrP5DeathState(Rng rng, DsrP5DeathStateOverrides? overrides = null, PartyRole player = PartyRole.MainTank)
    {
        overrides ??= new DsrP5DeathStateOverrides();
        WarriorBearing = overrides.WarriorBearing ?? 90f * rng.Next(4);
        var dooms = rng.Shuffle(Enum.GetValues<PartyRole>()).Take(4).ToList();
        Dooms = overrides.Dooms ?? dooms;
        if (overrides.PlayerDoom is { } requested && Dooms.Contains(player) != requested)
        {
            var candidates = Enum.GetValues<PartyRole>().Where(r => Dooms.Contains(r) == requested).ToArray();
            var selected = candidates[rng.Next(candidates.Length)];
            Dooms = requested ? Dooms.Where(r => r != selected).Append(player).ToArray()
                : Dooms.Where(r => r != player).Append(selected).ToArray();
        }
        var thordan = 45f * rng.Next(8);
        ThordanBearing = overrides.ThordanBearing ?? thordan;
        var eye = (ThordanBearing + new[] { 135f, 180f, 225f }[rng.Next(3)]) % 360;
        EyeBearing = overrides.EyeBearing ?? eye;
        var firstSquare = rng.NextBool();
        FirstDoomSquare = overrides.FirstDoomSquare ?? firstSquare;
        NonDoomOrder = rng.Shuffle(new[] { 0, 1, 2, 3 }).ToList();
        PlayerCongaSlot = overrides.PlayerCongaSlot;
    }
    public bool IsDoomed(PartyRole role) => Dooms.Contains(role);

    public Vector3 Outside(float bearingFromWarrior, float radius) => AtBearing(WarriorBearing + bearingFromWarrior, radius);
    public Vector3 WarriorSpot => Outside(0f, CasterRadius);
    public Vector3 ThordanSpot => AtBearing(ThordanBearing, ThordanRadius);
    public Vector3 EyeSpot => AtBearing(EyeBearing, EyeRadius);
    public byte EyeSlot => (byte)(EyeBearing / 45f);

    public IEnumerable<(PartyRole A, PartyRole B, ChainSymbol Symbol)> ChainPairs() =>
        Symbols.GroupBy(kv => kv.Value)
            .Where(g => g.Count() == 2)
            .Select(g => (g.First().Key, g.Last().Key, g.Key));
    public void HandOutSymbols(IReadOnlyDictionary<PartyRole, Vector3> positions)
    {
        Symbols.Clear();
        var doomed = Dooms.Where(positions.ContainsKey).Order().ToArray();
        var circlePair = (from a in doomed from b in doomed where a < b
            orderby Vector3.DistanceSquared(positions[a], positions[b]) descending
            select (a, b)).First();
        Symbols[circlePair.a] = ChainSymbol.Circle;
        Symbols[circlePair.b] = ChainSymbol.Circle;
        var rest = positions.Keys.Where(r => r != circlePair.a && r != circlePair.b).Order().ToList();
        var pool = new List<ChainSymbol> { ChainSymbol.Square, ChainSymbol.Triangle, ChainSymbol.Square, ChainSymbol.Triangle, ChainSymbol.Cross, ChainSymbol.Cross };
        var doomOrder = FirstDoomSquare ? new[] { ChainSymbol.Square, ChainSymbol.Triangle } : [ChainSymbol.Triangle, ChainSymbol.Square];
        var doomIndex = 0;
        foreach (var role in rest.Where(IsDoomed))
        {
            var symbol = doomIndex < doomOrder.Length ? doomOrder[doomIndex++] : pool[0];
            pool.Remove(symbol);
            Symbols[role] = symbol;
        }
        var nonDooms = rest.Where(r => !IsDoomed(r)).ToList();
        var shuffled = NonDoomOrder.Where(i => i < nonDooms.Count).Select(i => nonDooms[i]).ToList();
        foreach (var role in shuffled)
        {
            var symbol = pool.Contains(ChainSymbol.Cross) ? ChainSymbol.Cross : pool[0];
            pool.Remove(symbol);
            Symbols[role] = symbol;
        }
    }
}
