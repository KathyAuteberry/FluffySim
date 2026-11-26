using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dmu;

// Which tank holds which P3 boss. Yan's split puts the off-tank on Chaos; some compositions swap it.
public static class DmuP3Tanks
{
    private static readonly string[] Labels = ["MT on Exdeath, ST on Chaos", "MT on Chaos, ST on Exdeath"];

    public static PartyRole ChaosTank(bool mainTankOnChaos) => mainTankOnChaos ? PartyRole.MainTank : PartyRole.OffTank;
    public static PartyRole ExdeathTank(bool mainTankOnChaos) => mainTankOnChaos ? PartyRole.OffTank : PartyRole.MainTank;

    public static bool Row(string id, bool mainTankOnChaos)
    {
        SettingsGrid.Row("Tanks:");
        var index = mainTankOnChaos ? 1 : 0;
        SettingsGrid.ItemWidth(220);
        ImGui.Combo(id, ref index, Labels, Labels.Length);
        return index == 1;
    }
}
