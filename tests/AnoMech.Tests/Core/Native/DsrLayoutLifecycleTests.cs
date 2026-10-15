#pragma warning disable CA1416
using AnoMech.Core.Game;
using AnoMech.Core.Native.Implementations;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios.Dsr;

namespace AnoMech.Tests;

public class DsrLayoutLifecycleTests
{
    [TestCase(false)] [TestCase(true)]
    public void RecordedP2HoldsSurviveColdStreamingAndLaterP1Reactivation(bool initiallyP1)
    {
        var fake = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Config), new Configuration());
        using var game = new Game();
        game.ZoneIn(DsrZone.Instance, SessionMode.Local);
        fake.Frame(0, game.Tick);
        var layout = new LayoutFunctions();
        Natives.Layout = layout;
        DsrZone.P2.RunClientSetup(game.World);
        var objects = new Dictionary<nint, (ushort Layer, uint Instance, bool Active)>
        {
            [1] = (0xEE3C, 1, initiallyP1),
            [2] = (0xEE79, 2, !initiallyP1),
            [3] = (0xEE3E, 0x833316, initiallyP1),
            [4] = (0xEE3E, 0x833308, true),
            [5] = (0xF2E0, 0x8A4AF3, initiallyP1),
            [6] = (0xF2E0, 0x83DF7A, true),
        };
        var writes = 0;
        void Reassert(bool ready, uint territory = 968) => layout.ReassertHeldLayers(territory, ready,
            layer => objects.Where(p => p.Value.Layer == layer).Select(p => p.Key).ToArray(),
            p => objects[p].Instance, p => objects[p].Active,
            (p, active) => { var o = objects[p]; objects[p] = (o.Layer, o.Instance, active); writes++; });
        Reassert(false);
        Assert.That(writes, Is.Zero);
        Reassert(true, 1238);
        Assert.That(writes, Is.Zero);
        Reassert(true);
        Assert.That(objects[1].Active, Is.False);
        Assert.That(objects[2].Active, Is.True);
        Assert.That(objects[3].Active, Is.False);
        Assert.That(objects[4].Active, Is.True);
        Assert.That(objects[5].Active, Is.False);
        Assert.That(objects[6].Active, Is.True);
        var before = writes;
        Reassert(true);
        Assert.That(writes, Is.EqualTo(before), "Already-correct inn scenery is untouched");
        objects[7] = (0xEE7A, 7, false);
        objects[1] = (0xEE3C, 1, true);
        Reassert(true);
        Assert.That(objects[7].Active, Is.True, "Late-streamed P2 scenery is picked up");
        Assert.That(objects[1].Active, Is.False, "Later initialization cannot restore P1");
        layout.ClearSuppressedLayers();
        objects[1] = (0xEE3C, 1, true);
        before = writes;
        Reassert(true);
        Assert.That(writes, Is.EqualTo(before));
    }
}
