using AnoMech.Core.Game;
namespace AnoMech.Tests;
public class PeriodicExposureTests
{
    [Test]
    public void MovementOutBeforeGracePreventsBleedAndReentryGetsItsOwnTimer()
    {
        var exposure = new PeriodicExposure<int>(1, 1);
        Assert.That(exposure.Advance(0, true, 0), Is.Zero);
        Assert.That(exposure.Advance(0, false, .7f), Is.Zero);
        Assert.That(exposure.Advance(0, true, 2), Is.Zero);
        Assert.That(exposure.Advance(0, true, 2.9f), Is.Zero);
        Assert.That(exposure.Advance(0, true, 3), Is.EqualTo(1));
        Assert.That(exposure.Advance(0, true, 4), Is.EqualTo(2));
        Assert.That(exposure.Advance(0, true, 5), Is.EqualTo(3));
    }
    [Test]
    public void DifferentPlayersAndDuplicatePoolChecksCannotAccelerateTicks()
    {
        var exposure = new PeriodicExposure<int>(1, 1);
        exposure.Advance(0, true, 0); exposure.Advance(1, true, .5f);
        Assert.That(exposure.Advance(0, true, 1), Is.EqualTo(1));
        Assert.That(exposure.Advance(0, true, 1), Is.Zero);
        Assert.That(exposure.Advance(1, true, 1), Is.Zero);
        Assert.That(exposure.Advance(1, true, 1.5f), Is.EqualTo(1));
        exposure.Advance(0, false, 1.6f);
        Assert.That(exposure.Advance(1, true, 2.5f), Is.EqualTo(2));
        exposure.Clear();
        Assert.That(exposure.Advance(1, true, 10), Is.Zero);
    }
    [TestCase(.016f)] [TestCase(.1f)]
    public void TickCadenceUsesElapsedSimulationTime(float step)
    {
        var exposure = new PeriodicExposure<int>(1, 1); exposure.Advance(0, true, 0);
        var hits = new List<int>();
        for (var t = step; t < 3.2f; t += step)
            if (exposure.Advance(0, true, t) is > 0 and var tick) hits.Add(tick);
        Assert.That(hits, Is.EqualTo(new[] { 1, 2, 3 }));
    }
}
