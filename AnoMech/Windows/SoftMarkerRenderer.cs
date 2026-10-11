using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows;

internal static class SoftMarkerRenderer
{
    internal static void Draw()
    {
        MeteorTowerRenderer.Draw();
        ApocalypseRenderer.Draw();
        foreach (var marker in Plugin.GameInstance.World.SoftMarkers.Snapshot(Plugin.GameInstance.World.ScenarioOrigin))
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
