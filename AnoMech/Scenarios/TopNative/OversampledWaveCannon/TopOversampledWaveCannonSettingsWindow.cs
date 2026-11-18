using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.TopNative.OversampledWaveCannon;

public sealed class TopOversampledWaveCannonSettingsWindow
{
    public TopOversampledWaveCannonStateOverrides Overrides { get; } = new();
    private PartyRole editingSeat = PartyRole.MainTank;

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.OmegaCleavesEast = null;
        if (SettingsGrid.Begin("##topoversampled"))
        {
            TopNativeSettings.BoolRow("Omega's monitors:", "##owcside", Overrides.OmegaCleavesEast, v => Overrides.OmegaCleavesEast = v, "East", "West");
            SettingsGrid.End();
        }
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto")) Overrides.Monitor.Clear();
        if (SettingsGrid.Begin("##topoversampledplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##topoversampledseat", editingSeat);
            var v = Overrides.Monitor.Effective(editingSeat);
            SettingsGrid.PlayerRow("monitor:");
            if (ImGui.RadioButton("Auto##owcmon", v == null)) Overrides.Monitor.Set(editingSeat, null);
            ImGui.SameLine();
            if (ImGui.RadioButton("Yes##owcmon", v == true)) Overrides.Monitor.Set(editingSeat, true);
            ImGui.SameLine();
            if (ImGui.RadioButton("No##owcmon", v == false)) Overrides.Monitor.Set(editingSeat, false);
            SettingsGrid.ForcedRecapRow("Monitors set:", Overrides.Monitor);
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }
}
