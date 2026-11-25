using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dmu.Elements;

public enum Element { Fire, Water }

// The crystals stand on the intercardinals, 14 y out: NE, SE, SW, NW clockwise.
public static class DmuElementsCorners
{
    public const int Count = 4;
    public const float CrystalRadius = 14f;
    public static readonly string[] Names = ["NE", "SE", "SW", "NW"];

    public static Vector2 Direction(int corner) => corner switch
    {
        0 => Vector2.Normalize(new(1f, -1f)),
        1 => Vector2.Normalize(new(1f, 1f)),
        2 => Vector2.Normalize(new(-1f, 1f)),
        _ => Vector2.Normalize(new(-1f, -1f)),
    };

    public static Vector2 Crystal(int corner) => Direction(corner) * CrystalRadius;
}

public sealed class DmuElementsState
{
    private static readonly PartyRole[] TanksAndHealers = [MainTank, OffTank, RegenHealer, ShieldHealer];
    private static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    public Element First { get; }
    public int FireCorner { get; }
    public int WaterCorner => (FireCorner + 2) % DmuElementsCorners.Count;
    public int WindCorner { get; }
    // One tank or healer and one DPS per element, every pull so far.
    public IReadOnlyList<PartyRole> FireHolders { get; }
    public IReadOnlyList<PartyRole> WaterHolders { get; }
    public IReadOnlyDictionary<PartyRole, bool> Headwind { get; }
    // Latitudinal: the sides go first; Longitudinal: front and back.
    public bool Latitudinal { get; }
    public PartyRole ChaosTank { get; }
    public PartyRole ExdeathTank { get; }

    public Element Second => First == Element.Fire ? Element.Water : Element.Fire;
    public int Corner(Element element) => element == Element.Fire ? FireCorner : WaterCorner;
    public IReadOnlyList<PartyRole> Holders(Element element) => element == Element.Fire ? FireHolders : WaterHolders;

    // Any of the eight layouts: fire and water opposite, nothing opposite wind. UNVERIFIED: only Latitudinal seen.
    public DmuElementsState(Rng rng, DmuElementsStateOverrides overrides)
    {
        First = overrides.FireFirst is { } fireFirst ? fireFirst ? Element.Fire : Element.Water : rng.NextBool() ? Element.Fire : Element.Water;
        FireCorner = overrides.FireCorner ?? rng.Next(DmuElementsCorners.Count);
        var clockwise = overrides.WindClockwiseOfFire ?? rng.NextBool();
        WindCorner = (FireCorner + (clockwise ? 1 : 3)) % DmuElementsCorners.Count;
        var supports = rng.Shuffle(TanksAndHealers).ToList();
        var dps = rng.Shuffle(Dps).ToList();
        FireHolders = [supports[0], dps[0]];
        WaterHolders = [supports[1], dps[1]];
        Headwind = PerRole.All.ToDictionary(r => r, _ => rng.NextBool());
        Latitudinal = overrides.Latitudinal ?? rng.NextBool();
        ChaosTank = DmuP3Tanks.ChaosTank(overrides.MainTankOnChaos);
        ExdeathTank = DmuP3Tanks.ExdeathTank(overrides.MainTankOnChaos);
        DiagnosticLog.Info($"[DmuElements] Roll: first={First} fire={DmuElementsCorners.Names[FireCorner]} water={DmuElementsCorners.Names[WaterCorner]} "
                           + $"wind={DmuElementsCorners.Names[WindCorner]} fireHolders=[{string.Join(",", FireHolders)}] waterHolders=[{string.Join(",", WaterHolders)}] "
                           + $"headwind=[{string.Join(",", Headwind.Where(kv => kv.Value).Select(kv => kv.Key))}] {(Latitudinal ? "latitudinal" : "longitudinal")}");
    }
}

// Null leaves a roll random.
public sealed class DmuElementsStateOverrides
{
    public bool? FireFirst { get; set; }
    public int? FireCorner { get; set; }
    public bool? WindClockwiseOfFire { get; set; }
    public bool? Latitudinal { get; set; }
    public bool MainTankOnChaos { get; set; }
}
