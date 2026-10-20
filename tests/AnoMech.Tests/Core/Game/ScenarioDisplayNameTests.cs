#pragma warning disable CA1416 // Scenario metadata tests run without native client calls.
using AnoMech.Core.Game;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.Strength;
using AnoMech.Scenarios.Dsr.Strength2;
using AnoMech.Scenarios.Dsr.Sanctity1;

namespace AnoMech.Tests;

public class ScenarioDisplayNameTests
{
    [TestCase(true, 0xA58C08u, 0xA58C07u, 15f)]
    [TestCase(false, 0x875BE2u, 0x875BE0u, 18f)]
    public void FluffyArenasBindTheirRecordedDutyEntrance(bool fru, uint placement, uint wall, float z)
    {
        IZone zone = fru ? AnoMech.Scenarios.Fru.FruZone.Instance : AnoMech.Scenarios.Dsr.DsrZone.Instance;
        Assert.That(zone.EntranceBarrier.Position, Is.EqualTo(new System.Numerics.Vector3(0, 0, z)));
        Assert.That(zone.PlayerSpawn, Is.EqualTo(new System.Numerics.Vector3(0, 0, 16)));
        Assert.That(zone.EntranceBarrier.EObjId, Is.EqualTo(2007457));
        Assert.That(zone.EntranceBarrier.EObjLayoutId, Is.EqualTo(placement));
        Assert.That(zone.EntranceBarrier.WallLayoutId, Is.EqualTo(wall));
    }

    [Test]
    public void DsrDisplayLabelsChangeWithoutChangingPersistentIdentitiesOrOrder()
    {
        var scenarios = ScenarioCatalog.Create().Scenarios.Where(s => s is DsrStrengthScenario or DsrStrength2Scenario or DsrSanctity1Scenario).ToArray();
        Assert.That(scenarios.Select(Game.DisplayName), Is.EqualTo(new[] {
            "P2 Strength of the Ward 1", "P2 Strength of the Ward 2", "P2 Sanctity of the Ward 1" }));
        Assert.That(scenarios.Select(s => s.DisplayName()), Is.EqualTo(scenarios.Select(Game.DisplayName)));
        Assert.That(scenarios.Select(s => s.FullName()), Is.EqualTo(scenarios.Select(Game.FullName)));
        Assert.That(scenarios.Select(Game.FullName), Is.EqualTo(new[] {
            "Dragonsong's Reprise (Ultimate) — P2 King Thordan Strength of the Ward — Part 1 (Elemental)",
            "Dragonsong's Reprise (Ultimate) — P2 King Thordan Strength of the Ward — Part 2 (Elemental)",
            "Dragonsong's Reprise (Ultimate) — P2 King Thordan Sanctity of the Ward — Part 1: Sacred Severs (Elemental)" }));
        Assert.That(scenarios.Select(s => s.GetType()), Is.EqualTo(new[] { typeof(DsrStrengthScenario), typeof(DsrStrength2Scenario), typeof(DsrSanctity1Scenario) }));
    }
}
