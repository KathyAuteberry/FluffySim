using System;
using System.Numerics;
using AnoMech.Scenarios.Fru.Apocalypse;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows;

internal static class ApocalypseRenderer
{
    internal static void Draw()
    {
        var game=Plugin.Game;
        if(game.ActiveScenario is not FruApocalypseScenario scenario || !scenario.UsePracticeLightOverlay || scenario.LastState is not { } state)return;
        var draw=ImGui.GetForegroundDrawList();
        var origin=game.World.ScenarioOrigin+new Vector3(0,.1f,0);
        void Line(Vector3 a,Vector3 b,uint color,float thickness)
        {
            if(Plugin.GameGui.WorldToScreen(a+origin,out var sa)&&Plugin.GameGui.WorldToScreen(b+origin,out var sb))draw.AddLine(sa,sb,color,thickness);
        }
        void Circle(Vector3 center,float radius,uint color)
        {
            for(var i=0;i<64;i++)Line(center+FruApocalypseState.Direction(i*MathF.Tau/64)*radius,center+FruApocalypseState.Direction((i+1)*MathF.Tau/64)*radius,color,2);
        }
        if(scenario.HasLightTelegraphs)
        {
            foreach(var p in scenario.CurrentPulse)Circle(p, .6f,0xFFFFFFFF);
            foreach(var p in scenario.LightPositions)
            {
                Circle(p,.35f,0xFFFFFFFF);
                if(Plugin.GameGui.WorldToScreen(p+origin,out var screen))draw.AddCircleFilled(screen,5,0xFFFFFFFF);
            }
            var time=scenario.LightElapsed;
            for(var pair=0;pair<3;pair++)
            {
                var travelled=time-(pair==2?4:pair==1?2:0);
                if(travelled<0)continue;
                var angle=(state.Axis+(state.Clockwise?1:-1)*travelled/2)*MathF.PI/4;
                for(var half=0;half<2;half++)
                    Line(FruApocalypseState.Direction(angle+half*MathF.PI)*14,FruApocalypseState.Direction(angle+half*MathF.PI-(state.Clockwise?1:-1)*.12f)*14,0xFFFFFFFF,4);
            }
        }
        foreach(var p in scenario.CurrentExplosion)Circle(p,9,0xFFCC66FF);
    }
}
