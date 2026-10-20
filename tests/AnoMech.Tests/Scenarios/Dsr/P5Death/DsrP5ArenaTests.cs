#pragma warning disable CA1416
using System.Text.Json;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Policies;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr;
namespace AnoMech.Tests;
public class DsrP5ArenaTests
{
    [TestCase(false)] [TestCase(true)]
    public void DarkThordanWaitsForNativeLayoutAndUsesWeather46(bool delayed)
    {
        var native = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Log), TraceLog.Create(() => 0));
        using var world = new SimWorld(new EventScheduler());
        world.Map.Enter(968); native.MapEffects.TerritoryReady = !delayed;
        DsrZone.P5.RunClientSetup(world); DsrZone.P5.Run(world);
        if (delayed) Assert.That(native.MapEffects.Applied, Is.Empty);
        native.MapEffects.TerritoryReady = true; world.Map.Tick();
        Assert.That(native.MapEffects.Applied.Count, Is.EqualTo(9));
        Assert.That(native.MapEffects.Applied.Select(e => e.Flags), Is.All.EqualTo(0x00040004));
        for (var i = 0; i < 60; i++) world.Map.Tick();
        Assert.That(native.MapEffects.Applied.Count, Is.EqualTo(9));
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, 46, false), Is.True);
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, 46, true), Is.True);
        world.Map.Unload(); Assert.That(native.MapEffects.Loaded, Is.False);
    }
    [Test]
    public void Weather46RequiresDarkThordanGeometryAndLoadedColliders()
    {
        using var stream = typeof(DsrP5ArenaTests).Assembly.GetManifestResourceStream("AnoMech.Tests.Core.Native.Fixtures.DsrP5Layers.json")!;
        using var json = JsonDocument.Parse(stream);
        var layers = json.RootElement.EnumerateObject().ToDictionary(p => ushort.Parse(p.Name), p =>
            (IReadOnlyList<(uint Id, bool Active, bool ColliderLoaded, bool ColliderActive)>)p.Value.GetProperty("ids").EnumerateArray().Select(id =>
            {
                var active = p.Value.GetProperty("active").GetBoolean();
                var collision = p.Value.GetProperty("collision").GetBoolean();
                return (id.GetUInt32(), active, collision, collision && active);
            }).ToArray());
        Assert.That(DsrEnvironmentState.MatchesDarkThordan(k => layers[k]), Is.True);
        var expected = layers[65362].Single();
        layers[65362] = [(expected.Id, expected.Active, false, false)];
        Assert.That(DsrEnvironmentState.MatchesDarkThordan(k => layers[k]), Is.False);
        Assert.That(DsrEnvironmentState.MatchesDarkThordan(_ => []), Is.False);
    }
}
