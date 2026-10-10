using System;
using System.Linq;
using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.UltimateRelativity;

public sealed class FruUltimateRelativitySettingsWindow
{
    public FruUltimateRelativityStateOverrides Overrides { get; } = new();
    private static readonly string[] Roles = ["Random", .. Enum.GetNames<PartyRole>()];
    private static readonly string[] Assignments = Enum.GetNames<RelativityAssignment>();
    public void Draw()
    {
        ImGui.TextWrapped("Full Ultimate Relativity through Shell Crusher. Core sequence playtested; Oracle appearance and the extended eight-marker display await FFXIV confirmation. Attack 1/2 = NW/NE short DPS; Bind 1/2 = SW/SE long supports; Attack 3 = S short support/Ice, Ignore 1/2 = W support/E DPS mid Fire, Bind 3 = single-yellow long DPS/Ice, relative to the single yellow tether as north. Face outward before Return.");
        var north = (Overrides.RelativeNorth ?? -1) + 1;
        if (ImGui.Combo("Single yellow tether", ref north, "Random\0N\0NE\0E\0SE\0S\0SW\0W\0NW\0")) Overrides.RelativeNorth = north == 0 ? null : north - 1;
        var ice = Overrides.DpsIce is { } dps ? dps ? 1 : 2 : 0;
        if (ImGui.Combo("Ice role", ref ice, "Random\0Long DPS\0Short support\0")) Overrides.DpsIce = ice == 0 ? null : ice == 1;
        var rotation = Overrides.ClockwiseMask is null ? 0 : Overrides.ClockwiseMask == 255 ? 1 : Overrides.ClockwiseMask == 0 ? 2 : 3;
        if (ImGui.Combo("Hourglass spins", ref rotation, "Random independently\0All clockwise\0All counterclockwise\0Custom mask\0"))
            Overrides.ClockwiseMask = rotation == 0 ? null : rotation == 1 ? 255 : 0;
        if (Overrides.ClockwiseMask is { } mask)
        {
            if (ImGui.InputInt("Clockwise mask (0-255)", ref mask)) Overrides.ClockwiseMask = Math.Clamp(mask, 0, 255);
        }
        var role = (int)(Overrides.PracticeRole ?? (PartyRole)(-1)) + 1;
        if (ImGui.Combo("Force role assignment", ref role, Roles, Roles.Length))
        {
            Overrides.PracticeRole = role == 0 ? null : (PartyRole)(role - 1);
            Overrides.PracticeAssignment = role == 0 ? null : Overrides.PracticeRole!.Value.IsDps() ? RelativityAssignment.ShortDps1 : RelativityAssignment.LongSupport1;
        }
        if (Overrides.PracticeRole is { } practice)
        {
            var indices = Enumerable.Range(practice.IsDps() ? 0 : 4, 4).ToArray();
            var names = indices.Select(i => Assignments[i]).ToArray();
            var assignment = Array.IndexOf(indices, (int)Overrides.PracticeAssignment!.Value);
            if (ImGui.Combo("Assignment", ref assignment, names, names.Length)) Overrides.PracticeAssignment = (RelativityAssignment)indices[assignment];
        }
    }
}
