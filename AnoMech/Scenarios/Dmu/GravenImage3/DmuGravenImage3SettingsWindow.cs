using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dmu.GravenImage3;

public sealed class DmuGravenImage3SettingsWindow
{
    public DmuGravenImage3StateOverrides Overrides { get; } = new();

    private static readonly string[] GroupLabels = ["Random", "Supports", "DPS"];
    private static readonly string[] GazeLabels = ["Random", "Indolent Will (look away)", "Ave Maria (look at)"];
    private static readonly string[] TruthLabels = ["Random", "Truth", "Lie"];
    private static readonly string[] MarkerLabels = ["Random", "Spread markers", "Stack markers"];

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.DpsIdentical = Overrides.DpsConfused = Overrides.AveMaria = null;
            Overrides.FireLie = Overrides.FireStackMarkers = Overrides.ThunderLie = null;
        }
        if (!SettingsGrid.Begin("##dmugi3")) return;
        Overrides.DpsIdentical = Choice("Same-arrow group:", "##dmugi3same", GroupLabels, Overrides.DpsIdentical);
        Overrides.DpsConfused = Choice("Confused group:", "##dmugi3confused", GroupLabels, Overrides.DpsConfused);
        Overrides.AveMaria = Choice("Gaze:", "##dmugi3gaze", GazeLabels, Overrides.AveMaria);
        Overrides.FireLie = Choice("Final fire:", "##dmugi3fire", TruthLabels, Overrides.FireLie);
        Overrides.FireStackMarkers = Choice("Fire markers:", "##dmugi3markers", MarkerLabels, Overrides.FireStackMarkers);
        Overrides.ThunderLie = Choice("Final thunder:", "##dmugi3thunder", TruthLabels, Overrides.ThunderLie);
        SettingsGrid.End();
    }

    // Index 0 random, 1 false, 2 true.
    private static bool? Choice(string label, string id, string[] labels, bool? value)
    {
        SettingsGrid.Row(label);
        var index = value is { } v ? v ? 2 : 1 : 0;
        SettingsGrid.ItemWidth(200);
        ImGui.Combo(id, ref index, labels, labels.Length);
        return index == 0 ? null : index == 2;
    }
}
