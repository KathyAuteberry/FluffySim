using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.TopNative.ProgramLoop;

public sealed class TopProgramLoopSettingsWindow
{
    public TopProgramLoopStateOverrides Overrides { get; } = new();
    private PartyRole editingSeat = PartyRole.MainTank;

    public void Draw()
    {
        if (SettingsGrid.Begin("##topprogramloop"))
        {
            TopNativeSettings.YourNumberRow(Overrides.Number, "##plmynumber");
            SettingsGrid.End();
        }
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto")) Overrides.Number.Clear();
        if (SettingsGrid.Begin("##topprogramloopplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##topprogramloopseat", editingSeat);
            TopNativeSettings.NumberRow(Overrides.Number, editingSeat, "number:", "##plnumber");
            SettingsGrid.ForcedRecapRow("Numbers set:", Overrides.Number);
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }
}
