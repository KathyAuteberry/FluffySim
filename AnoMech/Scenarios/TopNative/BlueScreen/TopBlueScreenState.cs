using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.TopNative.BlueScreen;

public sealed class TopBlueScreenStateOverrides
{
    public PerRoleSetting<bool> FirstMarker { get; set; } = new();
    public PerRoleSetting<bool> SecondMarker { get; set; } = new();
    public PerRoleSetting<bool> ThirdMarker { get; set; } = new();

    public PerRoleSetting<bool> Marker(int wave) => wave switch { 0 => FirstMarker, 1 => SecondMarker, _ => ThirdMarker };

    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;
        for (var wave = 0; wave < TopBlueScreenState.Waves; wave++)
        {
            var setting = Marker(wave);
            conflicts.AtMost(2, PerRole.All.Where(r => setting[r] == true).ToList(), $"a stack marker in wave {wave + 1}");
            conflicts.AtMost(6, PerRole.All.Where(r => setting[r] == false).ToList(), $"no stack marker in wave {wave + 1}");
        }
        return conflicts;
    }
}

public sealed class TopBlueScreenState
{
    public const int Waves = 3;

    // Two line-stack targets per wave.
    public IReadOnlyList<IReadOnlyList<PartyRole>> Markers { get; }

    public TopBlueScreenState(Rng rng, SimParty party, TopBlueScreenStateOverrides overrides)
    {
        Markers = Enumerable.Range(0, Waves).Select(wave => (IReadOnlyList<PartyRole>)new RoleListBuilder
        {
            Size = 2,
            Membership = overrides.Marker(wave).Resolve(party.PlayerRole).ToDictionary(r => r.Role, r => r.Value),
        }.Build(rng, party).List.OrderBy(r => r).ToArray()).ToArray();
        DiagnosticLog.Info($"[TopBlueScreen] Stack markers {string.Join(" / ", Markers.Select(m => string.Join(",", m)))}");
    }

    private TopBlueScreenState(IReadOnlyList<IReadOnlyList<PartyRole>> markers) => Markers = markers;

    public static TopBlueScreenState FromNetworkReplay(PartyRole[] markers)
        => new(Enumerable.Range(0, Waves).Select(w => (IReadOnlyList<PartyRole>)markers.Skip(2 * w).Take(2).ToArray()).ToArray());

    public PartyRole[] EncodeMarkers() => Markers.SelectMany(m => m).ToArray();
}
