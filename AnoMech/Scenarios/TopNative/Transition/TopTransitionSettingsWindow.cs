using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.TopNative.Transition;

public sealed class TopTransitionSettingsWindow
{
    public TopTransitionStateOverrides Overrides { get; } = new();
    private PartyRole editingSeat = PartyRole.MainTank;

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.FirstArmsNorth = null;
        if (SettingsGrid.Begin("##toptransition"))
        {
            TopNativeSettings.BoolRow("First arms:", "##arms", Overrides.FirstArmsNorth, v => Overrides.FirstArmsNorth = v, "N/120/240", "60/S/300");
            SettingsGrid.End();
        }
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto")) Overrides.Debuff.Clear();
        if (SettingsGrid.Begin("##toptransitionplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##toptransitionseat", editingSeat);
            var v = Overrides.Debuff.Effective(editingSeat);
            SettingsGrid.PlayerRow("debuff:");
            if (ImGui.RadioButton("Auto##trdebuff", v == null)) Overrides.Debuff.Set(editingSeat, null);
            foreach (var d in new[] { SniperDebuff.Sniper, SniperDebuff.HighPowered, SniperDebuff.None })
            {
                ImGui.SameLine();
                if (ImGui.RadioButton($"{d}##trdebuff", v == d)) Overrides.Debuff.Set(editingSeat, d);
            }
            SettingsGrid.ForcedRecapRow("Debuffs set:", Overrides.Debuff);
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }
}
