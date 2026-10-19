using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.DiamondDust;

public sealed class FruDiamondDustSettingsWindow
{
    public FruDiamondDustStateOverrides Overrides { get; } = new();
    public void Draw()
    {
        var stillness = Overrides.Stillness is { } still ? (still ? 1 : 2) : 0;
        if (ImGui.Combo("Twin order", ref stillness, "Random\0Stillness first\0Silence first\0")) Overrides.Stillness = stillness == 0 ? null : stillness == 1;
        var kick = Overrides.Axe is { } axe ? (axe ? 1 : 2) : 0;
        if (ImGui.Combo("Kick", ref kick, "Random\0Axe\0Scythe\0")) Overrides.Axe = kick == 0 ? null : kick == 1;
        var targets = Overrides.StonesOnSupports is { } supports ? (supports ? 1 : 2) : 0;
        if (ImGui.Combo("Frigid Stone targets", ref targets, "Random\0Supports\0DPS\0")) Overrides.StonesOnSupports = targets == 0 ? null : targets == 1;
        var axis = (Overrides.FirstIcicleAxis ?? -1) + 1;
        if (ImGui.Combo("First icicle axis", ref axis, "Random\0N/S\0NE/SW\0E/W\0SE/NW\0")) Overrides.FirstIcicleAxis = axis == 0 ? null : axis - 1;
        var gaze = (Overrides.GazeSector ?? -1) + 1;
        if (ImGui.Combo("Gaze origin", ref gaze, "Random\0N\0NE\0E\0SE\0S\0SW\0W\0NW\0")) Overrides.GazeSector = gaze == 0 ? null : gaze - 1;
        var sector = (Overrides.OracleSector ?? -1) + 1;
        if (ImGui.Combo("Oracle destination", ref sector, "Random\0N\0NE\0E\0SE\0S\0SW\0W\0NW\0")) Overrides.OracleSector = sector == 0 ? null : sector - 1;
    }
}
