using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dmu.Trine;

public sealed class DmuTrineSettingsWindow
{
    public DmuTrineStateOverrides Overrides { get; } = new();

    private static readonly string[] CentreLabels = ["Random", "Points west", "Points east"];
    private static readonly string[] WingsLabels = ["Random", "Right half", "Left half"];

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.CentrePointsEast = Overrides.WingsCleaveLeft = null;
        if (!SettingsGrid.Begin("##dmutrine")) return;
        Overrides.CentrePointsEast = Choice("Centre triangle:", "##dmutrinecentre", CentreLabels, Overrides.CentrePointsEast);
        Overrides.WingsCleaveLeft = Choice("Wings cleave Kefka's:", "##dmutrinewings", WingsLabels, Overrides.WingsCleaveLeft);
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
