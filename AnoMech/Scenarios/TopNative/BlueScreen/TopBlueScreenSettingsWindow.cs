using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.TopNative.BlueScreen;

public sealed class TopBlueScreenSettingsWindow
{
    public TopBlueScreenStateOverrides Overrides { get; } = new();
    private PartyRole editingSeat = PartyRole.MainTank;

    // Solo has no mechanics dialog to open; a session assigns markers per seat there instead.
    public void Draw()
    {
        if (PerRole.SeatsActive || Plugin.Multiplayer is { SessionCode: not null }) return;
        if (SettingsGrid.Begin("##topbluescreen"))
        {
            for (var wave = 0; wave < TopBlueScreenState.Waves; wave++) MarkerRow(wave, PartyRole.MainTank);
            SettingsGrid.End();
        }
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto"))
            for (var wave = 0; wave < TopBlueScreenState.Waves; wave++) Overrides.Marker(wave).Clear();
        if (SettingsGrid.Begin("##topbluescreenplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##topbluescreenseat", editingSeat);
            for (var wave = 0; wave < TopBlueScreenState.Waves; wave++) MarkerRow(wave, editingSeat);
            for (var wave = 0; wave < TopBlueScreenState.Waves; wave++)
                SettingsGrid.ForcedRecapRow($"Wave {wave + 1} markers set:", Overrides.Marker(wave));
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }

    private void MarkerRow(int wave, PartyRole seat)
    {
        var setting = Overrides.Marker(wave);
        var v = setting.Effective(seat);
        var id = $"##bsmarker{wave}";
        SettingsGrid.PlayerRow($"wave {wave + 1} stack marker:");
        if (ImGui.RadioButton($"Auto{id}", v == null)) setting.Set(seat, null);
        ImGui.SameLine();
        if (ImGui.RadioButton($"Yes{id}", v == true)) setting.Set(seat, true);
        ImGui.SameLine();
        if (ImGui.RadioButton($"No{id}", v == false)) setting.Set(seat, false);
    }
}
