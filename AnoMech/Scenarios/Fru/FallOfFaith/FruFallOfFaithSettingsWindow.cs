using System.Linq;
using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.FallOfFaith;

public sealed class FruFallOfFaithSettingsWindow
{
    public FruFallOfFaithStateOverrides Overrides { get; } = new();
    public void Draw()
    {
        ImGui.TextWrapped("Mana: West-to-East H1 H2 MT ST D1 D2 D3 D4. First/third tethers west; second/fourth east. Swap promptly after the first two fetters expire. Easy and Hard intentionally use the same prompt movement. Native presentation awaits in-game validation.");
        var advanced = Overrides.Advanced;
        if (ImGui.Checkbox("Advanced / Exact Pattern Overrides", ref advanced)) Overrides.Advanced = advanced;
        if (!advanced)
        {
            var practice = (int)Overrides.PracticeAssignment;
            if (ImGui.Combo("Practice Assignment", ref practice, "Random\0Tether 1\0Tether 2\0Tether 3\0Tether 4\0Untethered\0")) Overrides.PracticeAssignment = (FaithPracticeAssignment)practice;
            if (Overrides.PracticeAssignment != FaithPracticeAssignment.Untethered)
            {
                var element = Overrides.PlayerFire is { } fire ? fire ? 1 : 2 : 0;
                if (ImGui.Combo("My tether element", ref element, "Random\0Fire\0Lightning\0")) Overrides.PlayerFire = element == 0 ? null : element == 1;
            }
        }
        else
        {
            ImGui.TextWrapped("Exact overrides replace Simple practice filters. Bit 0 is tether 1; a set bit means Fire.");
            var permutation = (Overrides.TetherPermutation ?? -1) + 1;
            if (ImGui.SliderInt("Tether permutation (0 = Random)", ref permutation, 0, 1680)) Overrides.TetherPermutation = permutation == 0 ? null : permutation - 1;
            if (Overrides.TetherPermutation is { } p) ImGui.TextUnformatted(string.Join(" → ", FruFallOfFaithState.Permutations[p].Select(SettingsGrid.RoleLabel)));
            var mask = (Overrides.FireMask ?? -1) + 1;
            if (ImGui.SliderInt("Element pattern (0 = Random)", ref mask, 0, 16)) Overrides.FireMask = mask == 0 ? null : mask - 1;
            if (Overrides.FireMask is { } m) ImGui.TextUnformatted(string.Concat(Enumerable.Range(0, 4).Select(i => (m & (1 << i)) != 0 ? "F" : "L")));
        }
        var outlines = Overrides.PracticeOutlines;
        if (ImGui.Checkbox("Local practice cone outlines (resolution only)", ref outlines)) Overrides.PracticeOutlines = outlines;
        var diagnostics = Overrides.Diagnostics;
        if (ImGui.Checkbox("Fall of Faith live diagnostics", ref diagnostics)) Overrides.Diagnostics = diagnostics;
    }
}
