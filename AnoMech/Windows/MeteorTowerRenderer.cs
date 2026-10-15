using System;
using System.Numerics;
using AnoMech.Scenarios.Dsr.Sanctity2;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows;

internal static class MeteorTowerRenderer
{
    internal static void Draw()
    {
        var game = Plugin.Game;
        if (game.ActiveScenario is not DsrSanctity2Scenario scenario) return;
        var towers = scenario.ActiveTowerVisuals;
        foreach (var tower in towers)
        {
            var center = tower.Position + game.World.ScenarioOrigin + new Vector3(0,.08f,0);
            const uint color = 0xFF70FFFF;
            GroundTelegraphRenderer.Tower(center, tower.Radius, color);
        }
        if (!scenario.ShowTowerDiagnostics) return;
        ImGui.Begin("Meteor tower diagnostics");
        ImGui.TextUnformatted($"Simulation clock: {game.World.Events.Elapsed:F2}s (frozen while paused)");
        ImGui.TextUnformatted($"Rendering: local projected overlay; active towers: {towers.Count}");
        foreach (var tower in towers)
            ImGui.TextUnformatted($"Wave {(tower.SecondWave ? 2 : 1)} world {tower.Position + game.World.ScenarioOrigin} radius {tower.Radius} window {tower.From:F2}–{tower.Until:F2}s");
        ImGui.End();
    }
}
