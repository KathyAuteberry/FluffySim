using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.TopNative.Pantokrator;

public sealed class TopPantokratorSettingsWindow
{
    public TopPantokratorStateOverrides Overrides { get; } = new();
    private PartyRole editingSeat = PartyRole.MainTank;

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.FirstFlameBearing = null;
            Overrides.FlamesClockwise = null;
            Overrides.OnesAdjust = Overrides.TwosAdjust = Overrides.ThreesAdjust = Overrides.FoursAdjust = null;
            if (!PerRole.SeatsActive) Overrides.Number.Mine = null;
        }
        if (SettingsGrid.Begin("##toppantokrator"))
        {
            TopNativeSettings.YourNumberRow(Overrides.Number, "##pkmynumber");
            SettingsGrid.Row("First flames:");
            if (ImGui.RadioButton("Auto##flame", Overrides.FirstFlameBearing == null)) Overrides.FirstFlameBearing = null;
            for (var b = 0; b < 180; b += 30)
            {
                ImGui.SameLine();
                if (ImGui.RadioButton($"{b}°##flame", Overrides.FirstFlameBearing == b)) Overrides.FirstFlameBearing = b;
            }
            TopNativeSettings.BoolRow("Flames turn:", "##flamedir", Overrides.FlamesClockwise, v => Overrides.FlamesClockwise = v, "Clockwise", "Counterclockwise");
            TopNativeSettings.BoolRow("1s adjust sides:", "##pkadjust1", Overrides.OnesAdjust, v => Overrides.OnesAdjust = v, "Yes", "No");
            TopNativeSettings.BoolRow("2s adjust sides:", "##pkadjust2", Overrides.TwosAdjust, v => Overrides.TwosAdjust = v, "Yes", "No");
            TopNativeSettings.BoolRow("3s adjust sides:", "##pkadjust3", Overrides.ThreesAdjust, v => Overrides.ThreesAdjust = v, "Yes", "No");
            TopNativeSettings.BoolRow("4s adjust sides:", "##pkadjust4", Overrides.FoursAdjust, v => Overrides.FoursAdjust = v, "Yes", "No");
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.Number.Clear();
            Overrides.FirstWaveCannon.Clear();
        }
        if (SettingsGrid.Begin("##toppantokratorplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##toppantokratorseat", editingSeat);
            TopNativeSettings.NumberRow(Overrides.Number, editingSeat, "number:", "##pknumber");
            var v = Overrides.FirstWaveCannon.Effective(editingSeat);
            SettingsGrid.PlayerRow("wave cannon:");
            if (ImGui.RadioButton("Auto##pkwc", v == null)) Overrides.FirstWaveCannon.Set(editingSeat, null);
            ImGui.SameLine();
            if (ImGui.RadioButton("First##pkwc", v == true)) Overrides.FirstWaveCannon.Set(editingSeat, true);
            ImGui.SameLine();
            if (ImGui.RadioButton("Second##pkwc", v == false)) Overrides.FirstWaveCannon.Set(editingSeat, false);
            SettingsGrid.ForcedRecapRow("Numbers set:", Overrides.Number);
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }
}
