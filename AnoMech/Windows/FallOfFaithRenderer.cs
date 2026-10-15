using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.FallOfFaith;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows;

internal static class FallOfFaithRenderer
{
    internal static void Draw()
    {
        var game = Plugin.Game;
        if (game.ActiveScenario is not FruFallOfFaithScenario { IsRunning: true, LastState: { } state } scenario) return;
        if (scenario.PracticeOutlines)
            foreach (var cone in scenario.Cones.Where(c => game.World.Events.Elapsed - c.Time <= .7f))
                GroundTelegraphRenderer.Cone(cone.Origin + game.World.ScenarioOrigin + new Vector3(0, .08f, 0), cone.Rotation,
                    cone.Fire ? System.MathF.PI / 4 : System.MathF.PI / 3, 60, cone.Fire ? 0x884080FF : 0x88FFB040);
        if (!scenario.Diagnostics) return;
        ImGui.Begin("Fall of Faith live diagnostics");
        ImGui.TextUnformatted($"Revealed {scenario.RevealedCount}/4; resolved {scenario.ResolvedCount}/4; understack errors {scenario.Understacks}");
        ImGui.TextUnformatted($"Cones: native action effects{(scenario.PracticeOutlines ? " + local practice outlines" : "")}; {game.World.Events.Elapsed:F2}s");
        for (var i = 0; i < scenario.RevealedCount; i++)
            ImGui.TextUnformatted($"Tether {i + 1}: {SettingsGrid.RoleLabel(state.Targets[i])}, {(state.Fire(i) ? "Fire" : "Lightning")}");
        foreach (var source in scenario.SourceActors)
            ImGui.TextUnformatted($"NPC {source.BNpcBaseId:X}: sim {source.Position}; facing {source.Rotation:F3}; native {source.Proxy?.Position}; pending {source.PacketSpawnPending}");
        if (scenario.ResolvedCount < scenario.RevealedCount)
        {
            var holder = game.World.Party.Get(state.Targets[scenario.ResolvedCount]);
            if (holder != null)
                foreach (var member in game.World.Party.ActiveMembers().Where(m => m != holder).OrderBy(m => Vector3.DistanceSquared(m.Position, holder.Position)))
                    ImGui.TextUnformatted($"Closest: {SettingsGrid.RoleLabel(((ISimPartyMember)member).Role)} {member.Position}, {Vector3.Distance(member.Position, holder.Position):F2}y");
        }
        foreach (var cone in scenario.Cones.TakeLast(4))
            ImGui.TextUnformatted($"Tether {cone.Order + 1} @ {cone.Time:F3}: origin {cone.Origin}, angle {cone.Rotation:F3}, target {SettingsGrid.RoleLabel(cone.Target)}, hits {string.Join(", ", cone.Hits.Select(SettingsGrid.RoleLabel))}");
        ImGui.End();
    }
}
