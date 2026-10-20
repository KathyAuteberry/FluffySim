using System.Collections.Generic;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game;

internal static class NativeActorReadiness
{
    public static bool Check(IEnumerable<SimEnemy?> actors, string stage)
    {
        var ready = true;
        foreach (var actor in actors)
        {
            var reason = actor is null ? "spawn returned null"
                : actor.PacketSpawnFailed ? "packet registration failed"
                : actor.PacketSpawnPending ? "packet registration pending"
                : actor.Proxy is not { Exists: true } ? "actor not registered"
                : actor.SpawnConfig.Visibility != SpawnVisibility.InvisibleHelper && (!actor.Proxy.HasDrawObject || !actor.Proxy.HasLoadedModel) ? "visible model not loaded"
                : actor.SpawnConfig.Visibility == SpawnVisibility.Visible && !actor.ModelHidden && !actor.Proxy.IsDrawObjectVisible ? "visible model not drawing"
                : null;
            if (reason is null) continue;
            ready = false;
            DiagnosticLog.Warn($"[{stage}] {reason}: entity=0x{actor?.EntityId:X} base={actor?.SpawnConfig.BNpcBaseId} name={actor?.SpawnConfig.NameId} visibility={actor?.SpawnConfig.Visibility} layout=0x{actor?.SpawnConfig.LayoutId:X}.");
        }
        return ready;
    }
}
