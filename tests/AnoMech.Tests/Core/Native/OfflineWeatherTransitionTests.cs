#pragma warning disable CA1416
using AnoMech.Core.Native.Policies;

namespace AnoMech.Tests;

public class OfflineWeatherTransitionTests
{
    [Test]
    public void ColdLoadCommitsOnceAndDoesNotFightLaterNativeState()
    {
        var transition = new OfflineWeatherTransition();
        byte current = 2;
        var queued = new List<byte>();
        var updates = 0;
        void Tick(nint layout) => transition.Tick(layout, () => current, queued.Add, () => { updates++; current = queued[^1]; });
        transition.Request(45);
        for (var i = 0; i < 100; i++) Tick(0);
        Assert.That(queued, Is.Empty);
        Tick(1);
        Assert.That(current, Is.EqualTo(45));
        for (var i = 0; i < 1000; i++) Tick(1);
        Assert.That(queued, Is.EqualTo(new byte[] {45}));
        Assert.That(updates, Is.EqualTo(1));
        current = 2;
        Tick(1);
        Assert.That(current, Is.EqualTo(2), "No perpetual visibility/weather enforcement");
        Tick(2);
        Assert.That(current, Is.EqualTo(45), "A genuinely rebuilt layout gets its own finalization");
        Assert.That(updates, Is.EqualTo(2));
    }

    [Test]
    public void RestartAndReadinessLossInvalidateEarlierInitialization()
    {
        var transition = new OfflineWeatherTransition();
        byte current = 2, next = 2;
        var writes = new List<byte>();
        void Tick(nint layout) => transition.Tick(layout, () => current, w => { writes.Add(w); next = w; }, () => current = next);
        transition.Request(45);
        var epoch = transition.Epoch;
        transition.Request(44);
        Assert.That(transition.Epoch, Is.GreaterThan(epoch));
        Tick(1);
        Assert.That(current, Is.EqualTo(44));
        transition.Clear();
        Tick(2);
        Assert.That(writes, Is.EqualTo(new byte[] {44}));
        transition.Request(45);
        Tick(1);
        Tick(0);
        current = 2;
        Tick(1);
        Assert.That(current, Is.EqualTo(45));
        Assert.That(writes, Is.EqualTo(new byte[] {44,45,45}));
    }

    [Test]
    public void NativeFadeCanFinishLaterWithoutRequeueingThePhase()
    {
        var transition = new OfflineWeatherTransition();
        byte current = 2;
        var queued = 0;
        var updates = 0;
        transition.Request(45);
        for (var frame = 0; frame < 10; frame++)
            transition.Tick(1, () => current, _ => queued++, () => { if (++updates == 3) current = 45; });
        Assert.That(queued, Is.EqualTo(1));
        Assert.That(updates, Is.EqualTo(3));
        Assert.That(current, Is.EqualTo(45));
    }
}
