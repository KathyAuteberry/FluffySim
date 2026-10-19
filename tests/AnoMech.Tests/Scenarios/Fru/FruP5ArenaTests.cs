using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru;

namespace AnoMech.Tests;

public class FruP5ArenaTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void PandoraPresetWaitsForReadinessAndDoesNotReapplyOnTicks(bool delayed)
    {
        var fake = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Log), TraceLog.Create(() => 0f));
        using var world = new SimWorld(new EventScheduler());
        fake.MapEffects.TerritoryReady = !delayed;
        world.Map.Enter(1238);
        FruZone.P5.RunClientSetup(world);
        FruZone.P5.Run(world);
        if (delayed) Assert.That(fake.MapEffects.Applied, Is.Empty);
        fake.MapEffects.TerritoryReady = true;
        world.Map.Tick();
        Assert.That(fake.MapEffects.Applied, Has.Count.EqualTo(54));
        for (byte i = 0; i < 54; i++)
            Assert.That(fake.MapEffects.Applied[i], Is.EqualTo((i == 47 ? 0x00010001u : 0x00040004u, i)));
        world.Map.Tick();
        Assert.That(fake.MapEffects.Applied, Has.Count.EqualTo(54));
        Assert.That(((FakeLayoutFunctions)Natives.Layout).HeldLayerStates.Values, Is.All.False);
        Assert.That(FruZone.P5.Weather, Is.EqualTo(108));
        Assert.That(FruZone.P5.Bgm, Is.EqualTo(20099));
        Assert.That(FruZone.P5.Zone, Is.SameAs(FruZone.Instance));
        Assert.That(AnoMech.Scenarios.Fru.Upstream.FruZone.P5, Is.SameAs(FruZone.P5));
        world.Map.Unload();
        Assert.That(((FakeLayoutFunctions)Natives.Layout).HeldLayerStates, Is.Empty);
    }

    [Test]
    public void LeavingDuringPandoraInitializationCancelsAllNativeRequests()
    {
        var fake = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Log), TraceLog.Create(() => 0f));
        using var world = new SimWorld(new EventScheduler());
        world.Map.Enter(1238);
        fake.MapEffects.TerritoryReady = false;
        FruZone.P5.Run(world);
        world.Map.Unload();
        fake.MapEffects.TerritoryReady = true;
        world.Map.Tick();
        Assert.That(fake.MapEffects.Applied, Is.Empty);
        Assert.That(fake.MapEffects.Loaded, Is.False);
    }
}
