using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.TopNative.ProgramLoop;

public sealed class TopProgramLoopStateOverrides
{
    public PerRoleSetting<int> Number { get; set; } = new();

    public SettingsConflicts Validate() => TopNativeNumbers.Validate(Number, "number");
}

public sealed class TopProgramLoopState
{
    // Clockwise from NW, the order the priority reads them in.
    public static readonly float[] SpotBearings = [346f, 14f, 76f, 104f, 166f, 194f, 256f, 284f];
    public static readonly uint[] SpotLayoutIds = [0x91D93B, 0x91D93C, 0x91D93D, 0x91D93E, 0x91D940, 0x91D941, 0x91D942, 0x91D943];
    public const float SpotRadius = 12.37f;

    public TopNativeNumbers Numbers { get; }

    // Four sets of two spot indices. Each set takes two spots 14° to the same side of their
    // cardinals, so 90° or 180° apart; every spot is used once.
    public IReadOnlyList<int[]> TowerSets { get; }

    public TopProgramLoopState(Rng rng, SimParty party, TopProgramLoopStateOverrides overrides)
    {
        Numbers = new TopNativeNumbers(rng, party, overrides.Number);
        var pairs = new List<int[]>();
        foreach (var family in new[] { new[] { 0, 2, 4, 6 }, new[] { 1, 3, 5, 7 } })
        {
            var shuffled = rng.Shuffle(family).ToArray();
            pairs.Add([shuffled[0], shuffled[1]]);
            pairs.Add([shuffled[2], shuffled[3]]);
        }
        TowerSets = rng.Shuffle(pairs).Select(p => p.OrderBy(i => i).ToArray()).ToArray();
        DiagnosticLog.Info($"[TopProgramLoop] Numbers {string.Join(",", Numbers.ByRole.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"))}; towers {string.Join(" ", TowerSets.Select(s => $"{SpotBearings[s[0]]}/{SpotBearings[s[1]]}"))}");
    }

    private TopProgramLoopState(TopNativeNumbers numbers, IReadOnlyList<int[]> towerSets)
    {
        Numbers = numbers;
        TowerSets = towerSets;
    }

    public static TopProgramLoopState FromNetworkReplay(Dictionary<PartyRole, int> numbers, int[] towerSpots)
        => new(TopNativeNumbers.FromRoles(numbers), Enumerable.Range(0, 4).Select(i => new[] { towerSpots[2 * i], towerSpots[2 * i + 1] }).ToArray());

    public int[] EncodeTowers() => TowerSets.SelectMany(s => s).ToArray();

    public static int Cardinal(int spot) => spot / 2;

    public static float CardinalBearing(int cardinal) => cardinal * 90f;
}
