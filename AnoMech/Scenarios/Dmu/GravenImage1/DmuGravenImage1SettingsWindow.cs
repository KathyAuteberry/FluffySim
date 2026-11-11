using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dmu.GravenImage1;

public sealed class DmuGravenImage1SettingsWindow
{
    public DmuGravenImage1StateOverrides Overrides { get; } = new();

    private static readonly string[] TetherLabels = ["Random", "Supports", "DPS"];
    private static readonly string[] TruthLabels = ["Random", "Truth", "Lie"];
    private static readonly string[] MarkerLabels = ["Random", "Spread markers", "Stack markers"];

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.DpsTethered = null;
            Overrides.Ice1Lie = Overrides.Fire1Lie = Overrides.Fire1StackMarkers = null;
            Overrides.Ice2Lie = Overrides.Thunder2Lie = null;
        }
        if (!SettingsGrid.Begin("##dmugi1")) return;
        Overrides.DpsTethered = Choice("Knockback tether:", "##dmugi1tether", TetherLabels, Overrides.DpsTethered);
        Overrides.Ice1Lie = Choice("Mystery Magic 1 ice:", "##dmugi1ice1", TruthLabels, Overrides.Ice1Lie);
        Overrides.Fire1Lie = Choice("Mystery Magic 1 fire:", "##dmugi1fire1", TruthLabels, Overrides.Fire1Lie);
        Overrides.Fire1StackMarkers = Choice("Fire markers:", "##dmugi1markers", MarkerLabels, Overrides.Fire1StackMarkers);
        Overrides.Ice2Lie = Choice("Mystery Magic 2 ice:", "##dmugi1ice2", TruthLabels, Overrides.Ice2Lie);
        Overrides.Thunder2Lie = Choice("Mystery Magic 2 thunder:", "##dmugi1thunder2", TruthLabels, Overrides.Thunder2Lie);
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
