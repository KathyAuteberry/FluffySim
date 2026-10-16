using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows;

internal static class SoftMarkerRenderer
{
    internal static void Draw()
    {
        ApocalypseRenderer.Draw();
        DarklitRenderer.Draw();
        FallOfFaithRenderer.Draw();
        foreach (var marker in Plugin.Game.World.SoftMarkers.Snapshot(Plugin.Game.World.ScenarioOrigin, Plugin.Game.World.ShowSimulatedParty))
        {
            if (!Plugin.GameGui.WorldToScreen(marker.Position, out var screen)) continue;
            var texture = Plugin.TextureProvider.GetFromGameIcon(marker.IconId).GetWrapOrEmpty();
            if (texture.Handle == 0) continue;
            var size = new Vector2(48f, 48f);
            var top = screen - new Vector2(size.X / 2f, size.Y);
            ImGui.GetForegroundDrawList().AddImage(texture.Handle, top, top + size);
        }
    }
}
