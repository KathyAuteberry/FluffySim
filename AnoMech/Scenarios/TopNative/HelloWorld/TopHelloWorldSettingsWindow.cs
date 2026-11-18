using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.TopNative.HelloWorld;

public sealed class TopHelloWorldSettingsWindow
{
    public TopHelloWorldStateOverrides Overrides { get; } = new();
    private PartyRole editingSeat = PartyRole.MainTank;

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.OverflowWithBlue = null;
        if (SettingsGrid.Begin("##tophelloworld"))
        {
            TopNativeSettings.BoolRow("Defamations:", "##hwcolor", Overrides.OverflowWithBlue, v => Overrides.OverflowWithBlue = v, "Blue", "Red");
            SettingsGrid.End();
        }
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto")) Overrides.FirstRole.Clear();
        if (SettingsGrid.Begin("##tophelloworldplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##tophelloworldseat", editingSeat);
            var v = Overrides.FirstRole.Effective(editingSeat);
            SettingsGrid.PlayerRow("first loop:");
            if (ImGui.RadioButton("Auto##hwrole", v == null)) Overrides.FirstRole.Set(editingSeat, null);
            foreach (var r in new[] { HelloRole.Overflow, HelloRole.Synchronization, HelloRole.Local, HelloRole.Remote })
            {
                ImGui.SameLine();
                if (ImGui.RadioButton($"{r}##hwrole", v == r)) Overrides.FirstRole.Set(editingSeat, r);
            }
            SettingsGrid.ForcedRecapRow("Roles set:", Overrides.FirstRole);
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }
}
