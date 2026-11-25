using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dmu.Elements;

public sealed class DmuElementsSettingsWindow
{
    public DmuElementsStateOverrides Overrides { get; } = new();

    private static readonly string[] FirstLabels = ["Random", "Water", "Fire"];
    private static readonly string[] CornerLabels = ["Random", "NE", "SE", "SW", "NW"];
    private static readonly string[] WindLabels = ["Random", "Counter-clockwise of fire", "Clockwise of fire"];
    private static readonly string[] ImplosionLabels = ["Random", "Longitudinal", "Latitudinal"];

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.FireFirst = Overrides.WindClockwiseOfFire = Overrides.Latitudinal = null;
            Overrides.FireCorner = null;
        }
        if (!SettingsGrid.Begin("##dmuelements")) return;
        Overrides.FireFirst = Choice("First element:", "##dmuelementsfirst", FirstLabels, Overrides.FireFirst);
        SettingsGrid.Row("Fire crystal:");
        var corner = Overrides.FireCorner is { } c ? c + 1 : 0;
        SettingsGrid.ItemWidth(160);
        ImGui.Combo("##dmuelementsfire", ref corner, CornerLabels, CornerLabels.Length);
        Overrides.FireCorner = corner == 0 ? null : corner - 1;
        Overrides.WindClockwiseOfFire = Choice("Wind crystal:", "##dmuelementswind", WindLabels, Overrides.WindClockwiseOfFire);
        Overrides.Latitudinal = Choice("Implosion:", "##dmuelementsimplosion", ImplosionLabels, Overrides.Latitudinal);
        Overrides.MainTankOnChaos = DmuP3Tanks.Row("##dmuelementstanks", Overrides.MainTankOnChaos);
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
