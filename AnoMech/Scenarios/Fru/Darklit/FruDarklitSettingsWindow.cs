using System.Collections.Generic;
using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.Darklit;

public sealed class FruDarklitSettingsWindow
{
    public FruDarklitStateOverrides Overrides { get; } = new();
    private PartyRole Human => Plugin.MainWindow?.SelectedRoleOverride ?? Plugin.Game.World.Party.PlayerRole;
    public IReadOnlyList<string> Conflicts => FruDarklitState.Candidates(Overrides, Human).Length == 0
        ? ["No encounter-valid combination matches your practice filters."] : [];
    public void Draw()
    {
        ImGui.TextWrapped("Mana Nukemaru bowtie: H1 H2 MT ST north; D1 D2 D3 D4 south. Resolve D2/D3 and tether sorting, then adjust Water before the cones. Keep tethers between 8 and 26 yalms. The bait tank uses Practice invulnerability before Somber Dance.");
        var advanced = Overrides.Advanced;
        if (ImGui.Checkbox("Advanced / Exact Pattern Overrides", ref advanced)) Overrides.Advanced = advanced;
        if (!Overrides.Advanced)
        {
            var assignment = (int)Overrides.PracticeAssignment;
            if (ImGui.Combo("Practice Assignment", ref assignment, "Random\0Tethered\0Untethered\0Tethered + Water\0Untethered + Water\0")) Overrides.PracticeAssignment = (DarklitPracticeAssignment)assignment;
            var shape = Overrides.TetherPattern is { } s ? (int)s + 1 : 0;
            if (ImGui.Combo("Tether Pattern", ref shape, "Random\0Bowtie\0Box\0Hourglass\0")) Overrides.TetherPattern = shape == 0 ? null : (DarklitTetherPattern)(shape - 1);
            var water = Overrides.WaterAdjustment is null ? 0 : Overrides.WaterAdjustment.Value ? 1 : 2;
            if (ImGui.Combo("Water Adjustment", ref water, "Random\0Required\0Not Required\0")) Overrides.WaterAdjustment = water == 0 ? null : water == 1;
        }
        else
        {
            var pattern = Overrides.ExactPattern ?? -1;
            if (ImGui.SliderInt("Assignment pattern (-1 random)", ref pattern, -1, FruDarklitState.Patterns.Length - 1)) Overrides.ExactPattern = pattern < 0 ? null : pattern;
            if (Overrides.ExactPattern is { } index && index < FruDarklitState.Patterns.Length)
            {
                var p = FruDarklitState.Patterns[index];
                ImGui.TextWrapped($"{p.Shape}: {p.Tank}, {p.Healer}, {p.WestDps}, {p.EastDps}; Waters {p.TetherWater} + {p.UntetherWater}. D2/D3 side adjustment: {p.RequiresDpsSideSwap}.");
            }
            var spirit = Overrides.SpiritTarget is { } role ? (int)role + 1 : 0;
            if (ImGui.Combo("Spirit Taker target", ref spirit, "Random\0MT\0ST\0H1\0H2\0D1\0D2\0D3\0D4\0")) Overrides.SpiritTarget = spirit == 0 ? null : (PartyRole)(spirit - 1);
        }
        var wing = Overrides.EastWing is null ? 0 : Overrides.EastWing.Value ? 1 : 2;
        if (ImGui.Combo("Hallowed Wings cleave", ref wing, "Random\0East\0West\0")) Overrides.EastWing = wing == 0 ? null : wing == 1;
        var bait = Overrides.BaitTank is null ? 0 : Overrides.BaitTank == PartyRole.MainTank ? 1 : 2;
        if (ImGui.Combo("Somber Dance bait", ref bait, "Auto (MT; ST if MT is PLD)\0MT\0ST\0")) Overrides.BaitTank = bait == 0 ? null : bait == 1 ? PartyRole.MainTank : PartyRole.OffTank;
        foreach (var conflict in Conflicts) ImGui.TextWrapped(conflict);
    }
}
