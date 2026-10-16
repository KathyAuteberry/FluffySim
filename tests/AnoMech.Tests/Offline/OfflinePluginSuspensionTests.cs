#pragma warning disable CA1416
using AnoMech.Offline;
using AnoMech.Tests.Offline.Fakes;

namespace AnoMech.Tests;

public class OfflinePluginSuspensionTests
{

    [Test]
    public async Task OnlyLoadedIncompatiblePluginsAreSuspendedAndRestored()
    {
        var bridge = new FakeOfflinePlugin("bridge", "Dalamud.DiscordBridge");
        var disabled = new FakeOfflinePlugin("disabled", "Dalamud.DiscordBridge", false);
        var other = new FakeOfflinePlugin("other", "Other");
        List<string> saved = [];
        var suspension = new OfflinePluginSuspension(() => [bridge, disabled, other], saved);
        await suspension.Suspend();
        Assert.That(saved, Is.EqualTo(new[] { "bridge" }));
        Assert.That(bridge.IsLoaded, Is.False);
        Assert.That(disabled.Unloads + other.Unloads, Is.Zero);
        await suspension.Restore();
        Assert.That(bridge.IsLoaded, Is.True);
        Assert.That(bridge.Loads, Is.EqualTo(1));
        Assert.That(disabled.Loads + other.Loads, Is.Zero);
        Assert.That(saved, Is.Empty);
        await suspension.Restore();
        Assert.That(bridge.Loads, Is.EqualTo(1));
    }

    [Test]
    public async Task EntryWaitsForUnloadToFinish()
    {
        var plugin = new FakeOfflinePlugin("bridge", "Dalamud.DiscordBridge") { PendingUnload = new() };
        var suspension = new OfflinePluginSuspension(() => [plugin], []);
        var pending = suspension.Suspend();
        Assert.That(pending.IsCompleted, Is.False);
        Assert.That(plugin.IsLoaded, Is.True);
        plugin.PendingUnload.SetResult();
        await pending;
        Assert.That(plugin.IsLoaded, Is.False);
    }

    [Test]
    public async Task PluginUnloadWaitsForPendingSuspensionBeforeRestoring()
    {
        var plugin = new FakeOfflinePlugin("bridge", "Dalamud.DiscordBridge") { PendingUnload = new() };
        List<string> saved = [];
        var suspension = new OfflinePluginSuspension(() => [plugin], saved);
        var pending = suspension.Suspend();
        var restore = suspension.RestoreAfter(pending);
        Assert.That(restore.IsCompleted, Is.False);
        Assert.That(plugin.Loads, Is.Zero);
        plugin.PendingUnload.SetResult();
        await restore;
        Assert.That(plugin.IsLoaded, Is.True);
        Assert.That(saved, Is.Empty);
    }

    [Test]
    public async Task FailedSuspensionCanRestoreAlreadyUnloadedPlugins()
    {
        var first = new FakeOfflinePlugin("first", "Dalamud.DiscordBridge");
        var second = new FakeOfflinePlugin("second", "Dalamud.DiscordBridge") { FailUnload = true };
        List<string> saved = [];
        var suspension = new OfflinePluginSuspension(() => [first, second], saved);
        Assert.ThrowsAsync<InvalidOperationException>(() => suspension.Suspend());
        await suspension.Restore();
        Assert.That(first.IsLoaded, Is.True);
        Assert.That(second.Loads, Is.Zero);
        Assert.That(saved, Is.Empty);
    }

    [Test]
    public async Task ReloadRecoversSavedInstallationsAndKeepsFailuresForRetry()
    {
        var first = new FakeOfflinePlugin("first", "Dalamud.DiscordBridge", false) { FailLoad = true };
        var second = new FakeOfflinePlugin("second", "Dalamud.DiscordBridge", false);
        List<string> saved = ["first", "second"];
        var suspension = new OfflinePluginSuspension(() => [first, second], saved);
        Assert.ThrowsAsync<InvalidOperationException>(() => suspension.Restore());
        Assert.That(second.IsLoaded, Is.True);
        Assert.That(saved, Is.EqualTo(new[] { "first" }));
        first.FailLoad = false;
        await suspension.Restore();
        Assert.That(saved, Is.Empty);
    }
}
