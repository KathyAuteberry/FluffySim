using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Darklit;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows;

internal static class DarklitRenderer
{
    internal static void Draw()
    {
        var game = Plugin.Game;
        if (game.ActiveScenario is not FruDarklitScenario scenario) return;
        if (scenario.UseTowerFallback)
            foreach (var position in scenario.ActiveTowerPositions)
                GroundTelegraphRenderer.Tower(position + game.World.ScenarioOrigin + new Vector3(0, .08f, 0), FruDarklitRules.TowerRadius, 0xFF70FFFF);
        if (!scenario.ShowTowerDiagnostics && !scenario.ShowPathDiagnostics) return;
        if (scenario.ShowPathDiagnostics && scenario.PathSnapshotTime >= 0 && game.World.Events.Elapsed - scenario.PathSnapshotTime < 1)
            foreach (var cone in scenario.PathCones)
                GroundTelegraphRenderer.Cone(cone.Source.Position + game.World.ScenarioOrigin + new Vector3(0, .08f, 0), cone.Source.Rotation,
                    System.MathF.PI / 6, AnoMech.Core.Native.Interfaces.Natives.Data.Action(40190)!.EffectRange, 0x8870FFFF);
        ImGui.Begin("Darklit presentation diagnostics");
        ImGui.TextUnformatted($"Tower path: {(scenario.UseTowerFallback ? "local overlay" : "native MapEffects 0x2A/0x2B")}; active {scenario.ActiveTowerPositions.Count}");
        foreach (var position in scenario.ActiveTowerPositions)
            ImGui.TextUnformatted($"World {position + game.World.ScenarioOrigin}; radius 4; window 10.144–18.249s; requires 2");
        ImGui.TextUnformatted($"Fragment HP {scenario.FragmentHealth}/{FruDarklitScenario.FragmentMaximumHealth}; compromised {scenario.FragmentCompromised}");
        if (scenario.ShowPathDiagnostics)
        {
            ImGui.TextUnformatted($"Shiva local {scenario.ShivaPosition}; facing {scenario.ShivaRotation:F3}; native world {scenario.NativeShivaPosition}; native facing {scenario.NativeShivaRotation:F3}");
            ImGui.TextUnformatted($"Build {typeof(DarklitRenderer).Assembly.ManifestModule.ModuleVersionId.ToString("N")[..8]} — Darklit presentation 2");
            ImGui.TextUnformatted($"Loaded binary: {typeof(DarklitRenderer).Assembly.Location}");
            var controls = PracticeInvulnerabilityControls.Evaluate(game.ActiveScenario, game.World.Party.PlayerRole,
                game.World.Party.Player?.IsAlive() == true, game.World.PracticeInvulnerability);
            ImGui.TextUnformatted($"Practice control: seat {game.World.Party.PlayerRole}; visible {controls.Visible}; enabled {controls.CanActivate}; {controls.State}");
            ImGui.TextUnformatted("Live distances:");
            foreach (var player in scenario.PathCandidates)
                ImGui.TextUnformatted($"{player.Role}: sim world {player.Position + game.World.ScenarioOrigin}; native world {player.NativeWorldPosition}; distance {player.Distance:F3}; alive {player.Alive}; selected {System.Linq.Enumerable.Contains(scenario.PathTargets, player.Role)}");
            ImGui.TextUnformatted("Snapshot distances:");
            foreach (var player in scenario.PathSnapshotCandidates)
                ImGui.TextUnformatted($"{player.Role}: sim world {player.Position + game.World.ScenarioOrigin}; native world {player.NativeWorldPosition}; distance {player.Distance:F3}; selected {System.Linq.Enumerable.Contains(scenario.PathTargets, player.Role)}");
            ImGui.TextUnformatted($"Snapshot {scenario.PathSnapshotTime:F3}s; native action 40190; outlines are diagnostic-only");
            foreach (var source in scenario.PathNativeSources)
                ImGui.TextUnformatted($"{source.Target}: live native cone source world {source.WorldPosition}; facing {source.Rotation:F3}");
            foreach (var cone in scenario.PathCones)
                ImGui.TextUnformatted($"{cone.Target}: cone angle {cone.Source.Rotation:F3}; source world {cone.Source.Position + game.World.ScenarioOrigin}; native rendering");
        }
        ImGui.End();
    }
}
