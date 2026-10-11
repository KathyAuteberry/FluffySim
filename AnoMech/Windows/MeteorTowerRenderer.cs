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
        var draw = ImGui.GetForegroundDrawList();
        foreach (var tower in towers)
        {
            var center = tower.Position + game.World.ScenarioOrigin + new Vector3(0,.08f,0);
            const uint color = 0xFF70FFFF;
            for (var i = 0; i < 64; ++i)
            {
                var a = center + Offset(i * MathF.Tau / 64, tower.Radius);
                var b = center + Offset((i + 1) * MathF.Tau / 64, tower.Radius);
                if (Plugin.GameGui.WorldToScreen(a,out var sa) && Plugin.GameGui.WorldToScreen(b,out var sb))
                    draw.AddLine(sa,sb,color,3);
            }
            for (var i = 0; i < 4; ++i)
            {
                var foot = center + Offset(i * MathF.PI / 2,tower.Radius);
                if (Plugin.GameGui.WorldToScreen(foot,out var a) && Plugin.GameGui.WorldToScreen(foot + new Vector3(0,2,0),out var b))
                    draw.AddLine(a,b,color,2);
            }
        }
        if (!scenario.ShowTowerDiagnostics) return;
        ImGui.Begin("Meteor tower diagnostics");
        ImGui.TextUnformatted($"Rendering: local projected overlay; active towers: {towers.Count}");
        foreach (var tower in towers)
            ImGui.TextUnformatted($"Wave {(tower.SecondWave ? 2 : 1)} world {tower.Position + game.World.ScenarioOrigin} radius {tower.Radius} window {tower.From:F2}–{tower.Until:F2}s");
        ImGui.End();
    }
    private static Vector3 Offset(float angle,float radius) => new(MathF.Sin(angle)*radius,0,MathF.Cos(angle)*radius);
}
