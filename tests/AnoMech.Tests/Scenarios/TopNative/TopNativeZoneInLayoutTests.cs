using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios;
using AnoMech.Scenarios.TopNative;

namespace AnoMech.Tests;

public class TopNativeZoneInLayoutTests
{
    [Test]
    public void SettlingInstancesStayHiddenUntilTheClientHasSettled()
    {
        var fake = NativeEntryLifecycleTests.Install(false, false, true);
        using var game = NativeEntryLifecycleTests.CreateGame();
        var layout = (FakeLayoutFunctions)Natives.Layout;
        IZone zone = TopNativeZone.Instance;
        var expected = zone.ZoneInSettlingInstances.Select(i => (zone.TerritoryId, i.Layer, i.Instance)).ToArray();
        Assert.That(expected, Is.Unique.And.Not.Empty);
        game.ZoneIn(zone, SessionMode.Local);
        NativeEntryLifecycleTests.Frames(fake, game, 2);
        Assert.That(layout.HeldInstanceStates.Keys, Is.EquivalentTo(expected));
        Assert.That(layout.HeldInstanceStates.Values, Is.All.False);
        NativeEntryLifecycleTests.Frames(fake, game, 5 * 60);
        Assert.That(layout.HeldInstanceStates.Keys, Is.EquivalentTo(expected));
        NativeEntryLifecycleTests.Frames(fake, game, 2 * 60);
        Assert.That(layout.HeldInstanceStates, Is.Empty);
        game.Leave();
        NativeEntryLifecycleTests.Frames(fake, game, 2);
    }
}
