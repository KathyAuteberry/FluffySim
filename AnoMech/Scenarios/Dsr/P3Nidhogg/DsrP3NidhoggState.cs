using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using static AnoMech.Scenarios.Dsr.P3Nidhogg.DsrP3NidhoggState;

namespace AnoMech.Scenarios.Dsr.P3Nidhogg;

public enum DiveArrow { Circle, Up, Down }

public sealed class DsrP3NidhoggState
{
    public static Vector3 AtBearing(float degrees, float radius)
    {
        var radians = degrees * MathF.PI / 180f;
        return new(radius * MathF.Sin(radians), 0, -radius * MathF.Cos(radians));
    }

    public const float ArrowDiveDistance = 14f;
    public const int Dives = 3;

    public static readonly float[] TowerBearings = [45f, 135f, 225f, 315f];
    private const float FourTowerRadius = 11.31f;

    public static Vector3 FourTowerSpot(int tower) => AtBearing(TowerBearings[tower], FourTowerRadius);

    public Vector3 SoulTetherSource(int tether) => tether == 0 ? Vector3.Zero : FourTowerSpot(QuietTower);

    private static readonly int[][] TowerSoakerSets = [[1, 1, 3, 3], [1, 2, 2, 3], [1, 1, 2, 4], [2, 2, 2, 2]];
    private static readonly int[] LineSizes = [3, 2, 3];
    private static readonly PartyRole[] NonTanks =
        [PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    public IReadOnlyDictionary<PartyRole, int> Lines { get; }
    public IReadOnlyDictionary<PartyRole, DiveArrow> Arrows { get; }
    public IReadOnlyList<bool> LashFirst { get; }
    public IReadOnlyList<int> TowerSoakers { get; }
    public int QuietTower { get; }
    public IReadOnlyList<PartyRole> SoulTetherTargets { get; }
    public IReadOnlyList<PartyRole> EyeTargets { get; }
    public PartyRole DrachenlanceTarget { get; }
    public int PreparationPattern { get; }
    public List<(PartyRole Role, System.Numerics.Vector3 Spot)>[] DiveTowers { get; } = [[], [], []];

    public DsrP3NidhoggState(Rng rng, DsrP3NidhoggStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new DsrP3NidhoggStateOverrides();
        var order = rng.Shuffle(Enum.GetValues<PartyRole>()).ToList();
        var lines = new Dictionary<PartyRole, int>();
        var next = 0;
        for (var line = 1; line <= Dives; line++)
        {
            foreach (var role in order.Skip(next).Take(LineSizes[line - 1]))
                lines[role] = line;
            next += LineSizes[line - 1];
        }
        if (player is { } me && overrides.PlayerLine is { } wantedLine and >= 1 and <= Dives && lines[me] != wantedLine)
        {
            var other = order.First(r => lines[r] == wantedLine);
            (lines[me], lines[other]) = (wantedLine, lines[me]);
        }
        Lines = overrides.Lines ?? lines;

        var arrowedLines = rng.Shuffle(new[] { 1, 2, 3 }).Take(1 + rng.Next(2)).ToList();
        var wantedArrow = player is null ? null : overrides.PlayerArrow;
        var playerLine = player is { } inLine ? Lines[inLine] : 0;
        if (wantedArrow is { } arrowWish) KeepArrowsPossible(arrowedLines, playerLine, arrowWish != DiveArrow.Circle);
        var arrows = new Dictionary<PartyRole, DiveArrow>();
        foreach (var line in Enumerable.Range(1, Dives))
        {
            var members = rng.Shuffle(Lines.Where(kv => kv.Value == line).Select(kv => kv.Key)).ToList();
            if (wantedArrow is { } wish && line == playerLine && player is { } who)
            {
                members.Remove(who);
                members.Insert(Math.Min(wish switch { DiveArrow.Up => 0, DiveArrow.Down => 1, _ => 2 }, members.Count), who);
            }
            for (var i = 0; i < members.Count; i++)
                arrows[members[i]] = !arrowedLines.Contains(line) ? DiveArrow.Circle : i switch { 0 => DiveArrow.Up, 1 => DiveArrow.Down, _ => DiveArrow.Circle };
        }
        Arrows = overrides.Arrows ?? arrows;

        var lashFirst = new[] { rng.NextBool(), rng.NextBool() };
        LashFirst = overrides.LashFirst ?? lashFirst;
        var soakers = rng.Shuffle(TowerSoakerSets[rng.Next(TowerSoakerSets.Length)]).ToList();
        TowerSoakers = overrides.TowerSoakers ?? soakers;
        QuietTower = overrides.QuietTower ?? rng.Next(TowerBearings.Length);
        var tethered = rng.Shuffle(NonTanks).Take(2).ToList();
        SoulTetherTargets = overrides.SoulTetherTargets ?? tethered;
        EyeTargets = [StackTarget(rng, 1), StackTarget(rng, 3)];
        DrachenlanceTarget = (PartyRole)rng.Next(8);
        PreparationPattern = rng.Next(3);
    }

    private static void KeepArrowsPossible(List<int> arrowedLines, int playerLine, bool wantsArrow)
    {
        if (wantsArrow && !arrowedLines.Contains(playerLine))
        {
            if (arrowedLines.Count == 2) arrowedLines.RemoveAt(1);
            arrowedLines.Add(playerLine);
        }
        else if (!wantsArrow && arrowedLines.Contains(playerLine) && LineSizes[playerLine - 1] < 3)
        {
            arrowedLines.Remove(playerLine);
            if (arrowedLines.Count == 0) arrowedLines.Add(playerLine % Dives + 1);
        }
    }

    private PartyRole StackTarget(Rng rng, int divingLine)
    {
        var candidates = Lines.Where(kv => kv.Value != divingLine).Select(kv => kv.Key).ToList();
        return candidates[rng.Next(candidates.Count)];
    }

    public IEnumerable<PartyRole> Line(int line) => Lines.Where(kv => kv.Value == line).Select(kv => kv.Key).OrderBy(r => r);

    public bool LineHasArrows(int line) => Line(line).Any(r => Arrows[r] != DiveArrow.Circle);

    public static float DiveOffset(DiveArrow arrow) => arrow switch
    {
        DiveArrow.Up => ArrowDiveDistance,
        DiveArrow.Down => -ArrowDiveDistance,
        _ => 0f,
    };
}
