using AnoMech.Scenarios.Fru;

namespace AnoMech.Tests;

public class FruArenaTests
{
    [Test]
    public void EveryPhaseStartsFromRetailsAllHiddenSnapshot()
    {
        Assert.That(FruZone.Instance.ArenaMapEffectSnapshot, Has.Count.EqualTo(54));
        Assert.That(FruZone.Instance.ArenaMapEffectSnapshot, Is.All.EqualTo(4));
    }

    [Test]
    public void PhasesShowOnlyTheirOwnScenery()
    {
        Assert.That(FruZone.P1.ArenaMapEffectOverrides, Is.Empty);
        Assert.That(FruZone.P2.ArenaMapEffectOverrides, Is.EquivalentTo(new Dictionary<byte, ushort> { [23] = 1 }));
        Assert.That(FruZone.P3.ArenaMapEffectOverrides, Is.EquivalentTo(new Dictionary<byte, ushort> { [40] = 1, [41] = 1 }));
        Assert.That(FruZone.P4.ArenaMapEffectOverrides, Is.EquivalentTo(new Dictionary<byte, ushort> { [40] = 1, [41] = 1 }));
        Assert.That(FruZone.P5.ArenaMapEffectOverrides, Is.EquivalentTo(new Dictionary<byte, ushort> { [47] = 1 }));
    }

    [Test]
    public void PhasesUseRetailWeatherAndMusic()
    {
        Assert.That(new[] { FruZone.P1, FruZone.P2, FruZone.P3, FruZone.P4, FruZone.P5 }.Select(p => (p.Weather, p.Bgm)), Is.EqualTo(new (byte?, ushort)[]
        {
            (2, 20134), (35, 20135), (105, 20105), (105, 20137), (108, 20099),
        }));
        Assert.That(FruZone.P5.Zone, Is.SameAs(FruZone.Instance));
        Assert.That(AnoMech.Scenarios.Fru.Upstream.FruZone.P5, Is.SameAs(FruZone.P5));
    }
}
