using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows;

internal static class GroundTelegraphRenderer
{
    internal static void Circle(Vector3 center, float radius, uint color, float thickness = 3)
    {
        var draw = ImGui.GetForegroundDrawList();
        for (var i = 0; i < 64; i++)
        {
            var a = center + Offset(i * MathF.Tau / 64, radius);
            var b = center + Offset((i + 1) * MathF.Tau / 64, radius);
            if (Plugin.GameGui.WorldToScreen(a, out var sa) && Plugin.GameGui.WorldToScreen(b, out var sb)) draw.AddLine(sa, sb, color, thickness);
        }
    }
    internal static void Cone(Vector3 origin, float rotation, float halfAngle, float length, uint color)
    {
        var draw = ImGui.GetForegroundDrawList();
        for (var i = 0; i <= 24; i++)
        {
            var endpoint = origin + Offset(rotation - halfAngle + 2 * halfAngle * i / 24, length);
            if (Plugin.GameGui.WorldToScreen(origin, out var a) && Plugin.GameGui.WorldToScreen(endpoint, out var b)) draw.AddLine(a, b, color, i is 0 or 24 ? 3 : 1);
        }
    }
    internal static void Tower(Vector3 center, float radius, uint color)
    {
        Circle(center, radius, color);
        var draw = ImGui.GetForegroundDrawList();
        for (var i = 0; i < 4; i++)
        {
            var foot = center + Offset(i * MathF.PI / 2, radius);
            if (Plugin.GameGui.WorldToScreen(foot, out var a) && Plugin.GameGui.WorldToScreen(foot + new Vector3(0, 2, 0), out var b)) draw.AddLine(a, b, color, 2);
        }
    }
    private static Vector3 Offset(float angle, float radius) => new(MathF.Sin(angle) * radius, 0, MathF.Cos(angle) * radius);
}
