using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.TopNative;

internal static class TopNativeSettings
{
    public static void NumberRow(PerRoleSetting<int> setting, PartyRole seat, string label, string id)
    {
        var v = setting.Effective(seat);
        SettingsGrid.PlayerRow(label);
        if (ImGui.RadioButton($"Auto{id}", v == null)) setting.Set(seat, null);
        for (var n = 1; n <= 4; n++)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton($"{n}{id}", v == n)) setting.Set(seat, n);
        }
    }

    // Solo has no mechanics dialog to open; a session assigns numbers per seat there instead.
    public static void YourNumberRow(PerRoleSetting<int> setting, string id)
    {
        if (PerRole.SeatsActive || Plugin.Multiplayer is { SessionCode: not null }) return;
        NumberRow(setting, PartyRole.MainTank, "your number:", id);
    }

    public static void BoolRow(string label, string id, bool? value, System.Action<bool?> set, string yes, string no)
    {
        SettingsGrid.Row(label);
        if (ImGui.RadioButton($"Auto{id}", value == null)) set(null);
        ImGui.SameLine();
        if (ImGui.RadioButton($"{yes}{id}", value == true)) set(true);
        ImGui.SameLine();
        if (ImGui.RadioButton($"{no}{id}", value == false)) set(false);
    }
}
