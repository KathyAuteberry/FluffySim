using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.TopNative.OversampledWaveCannon;

public sealed class TopOversampledWaveCannonStateOverrides
{
    public bool? OmegaCleavesEast { get; set; }
    public PerRoleSetting<bool> Monitor { get; set; } = new();

    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;
        conflicts.AtMost(3, PerRole.All.Where(r => Monitor[r] == true).ToList(), "a monitor");
        conflicts.AtMost(5, PerRole.All.Where(r => Monitor[r] == false).ToList(), "no monitor");
        return conflicts;
    }
}

public sealed class TopOversampledWaveCannonState
{
    public bool OmegaCleavesEast { get; }
    public IReadOnlyList<PartyRole> Monitors { get; }
    // Each player monitor cleaves to their right or left; true: right.
    public IReadOnlyDictionary<PartyRole, bool> MonitorRight { get; }

    public TopOversampledWaveCannonState(Rng rng, SimParty party, TopOversampledWaveCannonStateOverrides overrides)
    {
        OmegaCleavesEast = overrides.OmegaCleavesEast ?? rng.NextBool();
        var order = new RoleListBuilder
        {
            Size = 3,
            Membership = overrides.Monitor.Resolve(party.PlayerRole).ToDictionary(r => r.Role, r => r.Value),
        }.Build(rng, party);
        Monitors = order.List.OrderBy(r => r).ToArray();
        MonitorRight = Monitors.ToDictionary(r => r, _ => rng.NextBool());
        DiagnosticLog.Info($"[TopOversampled] Omega cleaves {(OmegaCleavesEast ? "east" : "west")}; monitors {string.Join(",", Monitors.Select(r => $"{r}:{(MonitorRight[r] ? "R" : "L")}"))}");
    }

    private TopOversampledWaveCannonState(bool omegaCleavesEast, Dictionary<PartyRole, bool> monitorRight)
    {
        OmegaCleavesEast = omegaCleavesEast;
        MonitorRight = monitorRight;
        Monitors = monitorRight.Keys.OrderBy(r => r).ToArray();
    }

    public static TopOversampledWaveCannonState FromNetworkReplay(bool omegaCleavesEast, Dictionary<PartyRole, bool> monitorRight)
        => new(omegaCleavesEast, monitorRight);
}
