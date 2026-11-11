using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dmu.GravenImage2;

public sealed class DmuGravenImage2SettingsWindow
{
    public DmuGravenImage2StateOverrides Overrides { get; } = new();

    private static readonly string[] TruthLabels = ["Random", "Truth", "Lie"];
    private static readonly string[] SpreadLabels = ["Random", "Supports", "DPS"];
    private static readonly string[] SideLabels = ["Random", "West", "East"];

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.IceLie = Overrides.DpsSpreadFirst = Overrides.DpsSpreadSecond = null;
            Overrides.FirstCleaveEast = Overrides.SecondCleaveEast = null;
        }
        if (!SettingsGrid.Begin("##dmugi2")) return;
        Overrides.IceLie = Choice("Blizzard III Blowout:", "##dmugi2ice", TruthLabels, Overrides.IceLie);
        Overrides.DpsSpreadFirst = Choice("First set spreads:", "##dmugi2spread1", SpreadLabels, Overrides.DpsSpreadFirst);
        Overrides.DpsSpreadSecond = Choice("Second set spreads:", "##dmugi2spread2", SpreadLabels, Overrides.DpsSpreadSecond);
        Overrides.FirstCleaveEast = Choice("First half-room cleave:", "##dmugi2cleave1", SideLabels, Overrides.FirstCleaveEast);
        Overrides.SecondCleaveEast = Choice("Second half-room cleave:", "##dmugi2cleave2", SideLabels, Overrides.SecondCleaveEast);
        SettingsGrid.End();
    }

    // Index 0 random, 1 false, 2 true.
    private static bool? Choice(string label, string id, string[] labels, bool? value)
    {
        SettingsGrid.Row(label);
        var index = value is { } v ? v ? 2 : 1 : 0;
        SettingsGrid.ItemWidth(160);
        ImGui.Combo(id, ref index, labels, labels.Length);
        return index == 0 ? null : index == 2;
    }
}
