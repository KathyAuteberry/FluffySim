using System.Numerics;
using AnoMech.Core.Map;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

public class MapControllerTests
{
    private static (MapController Map, FakeMapEffects Effects) Enter(uint territory = 1238)
    {
        var game = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Log), TraceLog.Create(() => 0f));
        var map = new MapController();
        game.MapEffects.TerritoryReady = false;
        map.Enter(territory);
        return (map, game.MapEffects);
    }

    [Test]
    public void FruMapRequestsWaitForNativeReadinessAndApplyOnlyOnceInOrder()
    {
        var (map, effects) = Enter();
        map.AddEffect(0x00040004, 0x17);
        map.AddEffect(0x00020001, 0x17);
        map.Tick();
        Assert.That(effects.Applied, Is.Empty);
        effects.TerritoryReady = true;
        map.Tick();
        Assert.That(effects.Applied, Is.EqualTo(new[] { (0x00040004u, (byte)0x17), (0x00020001u, (byte)0x17) }));
        map.Tick();
        Assert.That(effects.Applied, Has.Count.EqualTo(2));
        map.Unload();
    }

    [Test]
    public void LeavingBeforeReadinessCancelsRequestsAndReentryDoesNotReplayThem()
    {
        var (map, effects) = Enter();
        map.AddEffect(0x00040004, 0x18);
        map.Unload();
        effects.TerritoryReady = true;
        map.Tick();
        Assert.That(effects.Applied, Is.Empty);
        map.Enter(1238);
        map.AddEffect(0x00020001, 0x17);
        map.Tick();
        Assert.That(effects.Applied, Is.EqualTo(new[] { (0x00020001u, (byte)0x17) }));
        map.Unload();
    }

    [Test]
    public void RestartDuringInitializationReplacesPreviousRequests()
    {
        var (map, effects) = Enter();
        map.AddEffect(0x00040004, 0x18);
        map.Enter(1238);
        map.AddEffect(0x00020001, 0x17);
        effects.TerritoryReady = true;
        map.Tick();
        Assert.That(effects.Applied, Is.EqualTo(new[] { (0x00020001u, (byte)0x17) }));
        map.Unload();
    }

    [Test]
    public void OtherTerritoryKeepsItsExistingPathAndFruEntryUsesReadiness()
    {
        var (map, effects) = Enter(1122);
        map.AddEffect(0x00020002, 0);
        Assert.That(effects.Applied, Has.Count.EqualTo(1));
        map.Unload();
        map.Enter(1238);
        map.AddEffect(0x00020001, 0x17);
        Assert.That(effects.Applied, Has.Count.EqualTo(1));
        effects.TerritoryReady = true;
        map.Tick();
        Assert.That(effects.Applied, Has.Count.EqualTo(2));
        map.Unload();
    }

    [Test]
    public void RestingStatesAreKnownOnlyAfterTheClientTookTheSnapshot()
    {
        var (map, effects) = Enter();
        ushort[] states = [1, 4];
        map.SetEffectSnapshot(states);
        Assert.That(map.IsRestingIn(states), Is.False);
        effects.TerritoryReady = true;
        map.Tick();
        Assert.That(map.IsRestingIn(states), Is.True);
        Assert.That(map.IsRestingIn(new ushort[] { 4, 4 }), Is.False);
        map.BeginPhase();
        Assert.That(map.IsRestingIn(states), Is.True);
        map.Unload();
    }

    [Test]
    public void ASnapshotDroppedByANewPhaseIsNotResting()
    {
        var (map, effects) = Enter();
        ushort[] states = [1, 4];
        map.SetEffectSnapshot(states);
        map.BeginPhase();
        effects.TerritoryReady = true;
        map.Tick();
        Assert.That(effects.Snapshots, Is.Empty);
        Assert.That(map.IsRestingIn(states), Is.False);
        map.Unload();
    }

    [Test]
    public void ASingleEffectOrANewStayForgetsTheRestingStates()
    {
        var (map, effects) = Enter();
        effects.TerritoryReady = true;
        ushort[] states = [1, 4];
        map.SetEffectSnapshot(states);
        map.AddEffect(0x00020001, 1);
        Assert.That(map.IsRestingIn(states), Is.False);
        map.SetEffectSnapshot(states);
        map.Enter(1238);
        Assert.That(map.IsRestingIn(states), Is.False);
        map.Unload();
    }

    [Test]
    public void HeldLayerStatesAreSessionScopedAndReentryDoesNotRetainOldRequests()
    {
        FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Log), TraceLog.Create(() => 0f));
        var map = new MapController();
        var layout = (FakeLayoutFunctions)Natives.Layout;
        map.HoldLayerActive(0x5FC9, true);
        Assert.That(layout.HeldLayerStates, Is.Empty);
        for (var i = 0; i < 2; i++)
        {
            map.Enter(1238);
            map.HoldLayerActive(0x5FC9, true);
            map.HoldLayerActive(0x5F88, false);
            Assert.That(layout.HeldLayerStates, Has.Count.EqualTo(2));
            Assert.That(layout.HeldLayerStates[(1238, 0x5FC9)], Is.True);
            map.Unload();
            Assert.That(layout.HeldLayerStates, Is.Empty);
            map.HoldLayerActive(0x5FC9, true);
            Assert.That(layout.HeldLayerStates, Is.Empty);
        }
    }
}
