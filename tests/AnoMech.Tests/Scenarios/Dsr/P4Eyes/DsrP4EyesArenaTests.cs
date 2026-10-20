#pragma warning disable CA1416
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.Native.Policies;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr;

namespace AnoMech.Tests;

public class DsrP4EyesArenaTests
{
    [TestCase(false)] [TestCase(true)]
    public void EyesLayoutWaitsForReadinessAndUsesWeatherThree(bool delayed)
    {
        var native = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Log), TraceLog.Create(() => 0f));
        using var world = new SimWorld(new EventScheduler());
        native.MapEffects.TerritoryReady = !delayed;
        world.Map.Enter(968);
        DsrZone.P4.RunClientSetup(world);
        DsrZone.P4.Run(world);
        if (delayed) Assert.That(native.MapEffects.Applied, Is.Empty);
        native.MapEffects.TerritoryReady = true;
        world.Map.Tick();
        Assert.That(native.MapEffects.Applied.Take(9).Select(e => e.Flags), Is.All.EqualTo(0x00040004u));
        Assert.That(native.MapEffects.Applied.Last(), Is.EqualTo((0x00020001u, (byte)8)));
        var count = native.MapEffects.Applied.Count;
        for (var i = 0; i < 60; i++) world.Map.Tick();
        Assert.That(native.MapEffects.Applied.Count, Is.EqualTo(count));
        Assert.That(DsrZone.P4.Weather, Is.EqualTo(3));
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, DsrZone.P4.Weather!.Value, false), Is.True);
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, DsrZone.P4.Weather!.Value, true), Is.True);
        world.Despawn();
        Assert.That(native.MapEffects.Applied.Last(), Is.EqualTo((0x00040004u, (byte)8)));
        world.Map.Unload();
        Assert.That(native.MapEffects.Loaded, Is.False);
    }

    [Test]
    public void LeaveWhileEyesLayoutIsColdCancelsQueuedEffects()
    {
        var native = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Log), TraceLog.Create(() => 0f));
        using var world = new SimWorld(new EventScheduler());
        world.Map.Enter(968);
        native.MapEffects.TerritoryReady = false;
        DsrZone.P4.Run(world);
        world.Despawn();
        world.Map.Unload();
        native.MapEffects.TerritoryReady = true;
        world.Map.Tick();
        Assert.That(native.MapEffects.Applied, Is.Empty);
    }
}
