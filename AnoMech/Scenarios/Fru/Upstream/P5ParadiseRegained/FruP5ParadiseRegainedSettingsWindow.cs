using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained;

public sealed class FruP5ParadiseRegainedSettingsWindow
{
    public FruP5ParadiseRegainedStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup5paradise"))
        {
            SettingsGrid.FightOnlyNote();
            DrawRotation();
            DrawWings();
            DrawSecondTower();
            SettingsGrid.Row("Tank practice:");
            var invulnerability = Overrides.RequireInvulnerability;
            if (ImGui.Checkbox("Require invulnerability (tank practice)", ref invulnerability)) Overrides.RequireInvulnerability = invulnerability;
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.RequireInvulnerability = false;
        Overrides.Rotation = null;
        Overrides.EastSecond = null;
        Overrides.DarkFirst = null;
    }

    private void DrawRotation()
    {
        SettingsGrid.Row("First tower:");
        if (ImGui.RadioButton("Auto##prrotation", Overrides.Rotation == null)) Overrides.Rotation = null;
        for (var rotation = 0; rotation < 3; rotation++)
        {
            ImGui.SameLine();
            var label = rotation switch { 0 => "South", 1 => "Northwest", _ => "Northeast" };
            if (ImGui.RadioButton($"{label}##prrotation", Overrides.Rotation == rotation)) Overrides.Rotation = rotation;
        }
    }

    private void DrawWings()
    {
        var v = Overrides.DarkFirst;
        SettingsGrid.Row("First wing:");
        if (ImGui.RadioButton("Auto##prwing", v == null)) Overrides.DarkFirst = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Dark##prwing", v == true)) Overrides.DarkFirst = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Light##prwing", v == false)) Overrides.DarkFirst = false;
    }

    private void DrawSecondTower()
    {
        var v = Overrides.EastSecond;
        SettingsGrid.Row("Second tower:");
        if (ImGui.RadioButton("Auto##prtower", v == null)) Overrides.EastSecond = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("East##prtower", v == true)) Overrides.EastSecond = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("West##prtower", v == false)) Overrides.EastSecond = false;
    }
}
