using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;

namespace AnoMech.Offline;

internal interface IOfflinePlugin
{
    string Id { get; }
    string Name { get; }
    string InternalName { get; }
    bool IsLoaded { get; }
    Task Unload();
    Task Load();
}

internal sealed class OfflinePluginSuspension(Func<IEnumerable<IOfflinePlugin>> plugins, List<string> suspended)
{
    internal static bool IsIncompatible(string internalName) => internalName == "Dalamud.DiscordBridge";

    public async Task Suspend()
    {
        var targets = plugins().Where(p => IsIncompatible(p.InternalName) && p.IsLoaded).ToArray();
        foreach (var plugin in targets)
        {
            if (!suspended.Contains(plugin.Id)) suspended.Add(plugin.Id);
            await plugin.Unload();
            if (plugin.IsLoaded) throw new InvalidOperationException($"{plugin.Name} is still loaded.");
        }
    }

    public async Task RestoreAfter(Task? pending)
    {
        if (pending != null)
        {
            try { await pending; }
            catch (Exception) { }
        }
        OfflineProcessState.RestoringPlugins = true;
        try { await Restore(); }
        catch (Exception error) { Core.DiagnosticLog.Warn($"[Offline] Could not restore suspended plugins on unload: {error.GetBaseException().Message}"); }
    }

    public async Task Restore()
    {
        var errors = new List<string>();
        foreach (var id in suspended.ToArray())
        {
            try
            {
                var plugin = plugins().SingleOrDefault(p => p.Id == id);
                if (plugin == null) throw new InvalidOperationException("The suspended plugin is no longer installed.");
                if (!plugin.IsLoaded) await plugin.Load();
                if (!plugin.IsLoaded) throw new InvalidOperationException($"{plugin.Name} did not load.");
                suspended.Remove(id);
            }
            catch (Exception error) { errors.Add(error.GetBaseException().Message); }
        }
        if (errors.Count != 0) throw new InvalidOperationException(string.Join("; ", errors));
    }
}

// Use Dalamud's lifecycle without changing manifests or profile preferences. Resolve the entire
// contract before unloading anything so a changed internal API refuses offline entry cleanly.
internal sealed class DalamudOfflinePlugin : IOfflinePlugin
{
    private readonly IExposedPlugin exposed;
    private readonly object local;
    private readonly MethodInfo unload;
    private readonly MethodInfo load;
    private readonly object disposalMode;
    private readonly PropertyInfo state;

    internal DalamudOfflinePlugin(IExposedPlugin exposed)
    {
        this.exposed = exposed;
        var assembly = typeof(IDalamudPluginInterface).Assembly;
        var type = assembly.GetType("Dalamud.Plugin.Internal.Types.LocalPlugin", true)!;
        var field = exposed.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(f => f.FieldType == type);
        local = field.GetValue(exposed) ?? throw new InvalidOperationException("Dalamud's plugin handle is unavailable.");
        state = type.GetProperty("State") ?? throw new MissingMemberException(type.FullName, "State");
        var mode = assembly.GetType("Dalamud.Plugin.Internal.Types.PluginLoaderDisposalMode", true)!;
        disposalMode = Enum.Parse(mode, "WaitBeforeDispose");
        unload = type.GetMethod("UnloadAsync", [mode]) ?? throw new MissingMethodException(type.FullName, "UnloadAsync");
        load = type.GetMethod("LoadAsync", [typeof(PluginLoadReason), typeof(bool), typeof(CancellationToken)])
            ?? throw new MissingMethodException(type.FullName, "LoadAsync");
        if (unload.ReturnType != typeof(Task) || load.ReturnType != typeof(Task))
            throw new InvalidOperationException("Dalamud's plugin lifecycle contract has changed.");
    }

    public string Id => $"{InternalName}:{exposed.Manifest.WorkingPluginId}";
    public string Name => exposed.Name;
    public string InternalName => exposed.InternalName;
    public bool IsLoaded
    {
        get
        {
            var current = state.GetValue(local)?.ToString();
            if (current is "Loading" or "Unloading" or "UnloadError")
                throw new InvalidOperationException($"{Name} is {current}; wait for its operation to finish or restart the game after an unload error.");
            return exposed.IsLoaded;
        }
    }
    public Task Unload() => Task.Run(() => (Task)unload.Invoke(local, [disposalMode])!);
    public Task Load() => Task.Run(() => (Task)load.Invoke(local, [PluginLoadReason.Reload, false, CancellationToken.None])!);
}
