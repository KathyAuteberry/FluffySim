using System.Linq;
using Xunit;
using Assert = Xunit.Assert;

namespace AnoMech.SafetyTests.Contract;

public class OfflinePluginLifecycleContractTests
{
    [Fact]
    public void SuspendedPluginHandleAndLifecycleMatchInstalledDalamud()
    {
        const string localName = "Dalamud.Plugin.Internal.Types.LocalPlugin";
        var exposed = RealDalamud.Type("Dalamud.dll", "Dalamud.Plugin.ExposedPlugin");
        Assert.Equal(1, exposed.Fields.Count(f => !f.IsStatic && !f.IsPublic && f.FieldType.FullName == localName));
        var local = RealDalamud.Type("Dalamud.dll", localName);
        Assert.Contains(local.Properties, p => p.Name == "State" && p.GetMethod.IsPublic);
        var unload = RealDalamud.Method(local, "UnloadAsync", "Dalamud.Plugin.Internal.Types.PluginLoaderDisposalMode");
        var load = RealDalamud.Method(local, "LoadAsync", "Dalamud.Plugin.PluginLoadReason", "System.Boolean", "System.Threading.CancellationToken");
        Assert.True(unload.IsPublic && load.IsPublic);
        Assert.Equal("System.Threading.Tasks.Task", unload.ReturnType.FullName);
        Assert.Equal("System.Threading.Tasks.Task", load.ReturnType.FullName);
        var mode = RealDalamud.Type("Dalamud.dll", "Dalamud.Plugin.Internal.Types.PluginLoaderDisposalMode");
        Assert.Contains(mode.Fields, f => f.Name == "WaitBeforeDispose");
    }

    [Fact]
    public void BridgeEventSubscriptionsAreRemovedByScopedServiceDisposal()
    {
        foreach (var name in new[] { "Dalamud.Game.ClientState.ClientStatePluginScoped", "Dalamud.Game.FrameworkPluginScoped" })
        {
            var type = RealDalamud.Type("Dalamud.dll", name);
            var dispose = RealDalamud.Method(type, "Dalamud.IInternalDisposableService.DisposeService");
            Assert.True(dispose.HasBody);
            var calls = RealDalamud.CalledNames(dispose).ToArray();
            var events = name.EndsWith("ClientStatePluginScoped")
                ? new[] { "TerritoryChanged", "Login", "Logout" }
                : new[] { "Update" };
            foreach (var eventName in events)
                Assert.Contains(calls, c => c.EndsWith($"::remove_{eventName}"));
        }
    }
}
