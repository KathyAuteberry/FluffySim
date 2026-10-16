#pragma warning disable CA1416
using AnoMech.Offline;

namespace AnoMech.Tests.Offline.Fakes;

internal sealed class FakeOfflinePlugin(string id, string internalName, bool loaded = true) : IOfflinePlugin
{
    public string Id => id;
    public string Name => internalName;
    public string InternalName => internalName;
    public bool IsLoaded { get; set; } = loaded;
    public int Loads;
    public int Unloads;
    public bool FailUnload;
    public bool FailLoad;
    public TaskCompletionSource? PendingUnload;
    public async Task Unload()
    {
        Unloads++;
        if (PendingUnload != null) await PendingUnload.Task;
        if (FailUnload) throw new InvalidOperationException("unload failed");
        IsLoaded = false;
    }
    public Task Load()
    {
        Loads++;
        if (FailLoad) throw new InvalidOperationException("load failed");
        IsLoaded = true;
        return Task.CompletedTask;
    }
}
