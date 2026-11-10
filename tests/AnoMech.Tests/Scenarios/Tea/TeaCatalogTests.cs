using AnoMech.Scenarios;
using AnoMech.Scenarios.Tea;

namespace AnoMech.Tests;

public class TeaCatalogTests
{
    [Test]
    public void AllSixEntriesExposeOnlyImplementedManaStrategies()
    {
        var entries = ScenarioCatalog.Create().Scenarios.Where(s => s.Phase.Zone == TeaZone.Instance).ToArray();
        Assert.That(entries, Has.Length.EqualTo(6));
        Assert.That(entries.SelectMany(s => s.AiStrats).Select(a => a.Group), Is.All.EqualTo("Mana"));
        Assert.That(entries.All(s => s.SupportsSpectator), Is.True);
        Assert.That(entries.SelectMany(s => s.AiStrats).Any(a => a.Name.Contains("NAUR") || a.Name is "1256" || a.Name.Contains("APT")), Is.False);
    }

    [Test]
    public void EncounterIdentityIsDistinctFromRegionalStrategyPreference()
    {
        var entries = ScenarioCatalog.Create().Scenarios.Where(s => s.Phase.Zone == TeaZone.Instance).ToArray();
        Assert.That(entries.Select(s => s.FullName()), Is.Unique);
        Assert.That(entries.Select(s => s.FullName()), Is.All.StartsWith("The Epic of Alexander (Ultimate) — "));
        Assert.That(entries.Any(s => s.FullName().Contains("Mana") || s.FullName().Contains("Elemental")), Is.False);
        Assert.That(entries.Select(s => s.Phase.Name), Is.EquivalentTo(new[] { "P1", "I1", "P2", "I2", "P3", "P4" }));
    }

    [Test]
    public void ManaWaymarksPreserveTheGuidesInnerAndOuterCardinals()
    {
        var marks = TeaManaWaymarks.Layout;
        Assert.That(marks, Has.Count.EqualTo(8));
        for (var i = 0; i < 4; i++)
        {
            var inner = marks[i].Offset;
            var outer = marks[i + 4].Offset;
            Assert.That(System.Numerics.Vector3.Normalize(inner), Is.EqualTo(System.Numerics.Vector3.Normalize(outer)));
            Assert.That(outer.Length(), Is.GreaterThan(inner.Length()));
        }
    }
}
