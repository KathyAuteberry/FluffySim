using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.TopNative.Transition;

public enum SniperDebuff { None, Sniper, HighPowered }

public sealed class TopTransitionStateOverrides
{
    public PerRoleSetting<SniperDebuff> Debuff { get; set; } = new();
    public bool? FirstArmsNorth { get; set; }

    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;
        conflicts.AtMost(4, PerRole.All.Where(r => Debuff[r] == SniperDebuff.Sniper).ToList(), "Sniper Cannon Fodder");
        conflicts.AtMost(2, PerRole.All.Where(r => Debuff[r] == SniperDebuff.HighPowered).ToList(), "High-powered Sniper Cannon Fodder");
        conflicts.AtMost(2, PerRole.All.Where(r => Debuff[r] == SniperDebuff.None).ToList(), "no debuff");
        return conflicts;
    }
}

public sealed class TopTransitionState
{
    public IReadOnlyDictionary<PartyRole, SniperDebuff> Debuffs { get; }
    // The first arms stand at N, 120° and 240°; otherwise at 60°, S and 300°.
    public bool FirstArmsNorth { get; }

    public TopTransitionState(Rng rng, SimParty party, TopTransitionStateOverrides overrides)
    {
        var slots = overrides.Debuff.Resolve(party.PlayerRole).ToDictionary(r => r.Role, r => r.Value switch
        {
            SniperDebuff.Sniper => new[] { 0, 1, 2, 3 },
            SniperDebuff.HighPowered => new[] { 4, 5 },
            _ => new[] { 6, 7 },
        });
        var order = new RoleListBuilder { Slots = slots }.Build(rng, party);
        Debuffs = Enumerable.Range(0, 8).ToDictionary(i => order[i], i => i < 4 ? SniperDebuff.Sniper : i < 6 ? SniperDebuff.HighPowered : SniperDebuff.None);
        FirstArmsNorth = overrides.FirstArmsNorth ?? rng.NextBool();
        DiagnosticLog.Info($"[TopTransition] {string.Join(",", Debuffs.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"))}; first arms north {FirstArmsNorth}");
    }

    private TopTransitionState(Dictionary<PartyRole, SniperDebuff> debuffs, bool firstArmsNorth)
    {
        Debuffs = debuffs;
        FirstArmsNorth = firstArmsNorth;
    }

    public static TopTransitionState FromNetworkReplay(Dictionary<PartyRole, SniperDebuff> debuffs, bool firstArmsNorth) => new(debuffs, firstArmsNorth);

    public float FirstArmsBearing(int i) => (FirstArmsNorth ? 0f : 60f) + 120f * i;
    public float SecondArmsBearing(int i) => (FirstArmsNorth ? 60f : 0f) + 120f * i;
}
