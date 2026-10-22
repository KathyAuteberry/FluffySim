#pragma warning disable CA1416
using AnoMech.Offline;

namespace AnoMech.Tests;

public class OfflineRecastAttributesTests
{
    [TestCase(400)]
    [TestCase(420)]
    public void NeutralOfflineHastePreservesPaladinTwoPointFiveSecondRecast(int baseSpeed)
    {
        var attributes = new int[74];
        OfflineLoadouts.AddRecastBaselines(attributes, baseSpeed);
        Assert.Multiple(() =>
        {
            Assert.That(attributes[45], Is.EqualTo(baseSpeed));
            Assert.That(attributes[46], Is.EqualTo(baseSpeed));
            Assert.That(attributes[47], Is.EqualTo(100));
            Assert.That(NativeStatAdjustment(2500, 1000, attributes[47]), Is.EqualTo(2500));
            Assert.That(NativeStatAdjustment(2500, 1000, 0), Is.EqualTo(1500));
        });
    }

    [Test]
    public void BaselinesPreserveEquipmentSpeedAndDoNotInjectTemporaryHaste()
    {
        var attributes = new int[74];
        attributes[45] = 321;
        attributes[46] = 456;
        attributes[1] = 123;
        OfflineLoadouts.AddRecastBaselines(attributes, 420);
        Assert.That(attributes[45], Is.EqualTo(741));
        Assert.That(attributes[46], Is.EqualTo(876));
        Assert.That(attributes[47], Is.EqualTo(100));
        Assert.That(attributes[1], Is.EqualTo(123));
    }

    [TestCase(9u, "PLD")] [TestCase(31u, "WAR")]
    [TestCase(3617u, "DRK")] [TestCase(16137u, "GNB")]
    [TestCase(53u, "MNK")] [TestCase(2240u, "NIN")]
    [TestCase(75u, "DRG")] [TestCase(7477u, "SAM")]
    [TestCase(34606u, "VPR")] [TestCase(24373u, "RPR")]
    [TestCase(97u, "BRD")] [TestCase(2866u, "MCH")]
    [TestCase(15989u, "DNC")] [TestCase(119u, "WHM")]
    [TestCase(17869u, "SCH")] [TestCase(3596u, "AST")]
    [TestCase(24283u, "SGE")] [TestCase(141u, "BLM")]
    [TestCase(163u, "SMN")] [TestCase(7503u, "RDM")]
    [TestCase(34650u, "PCT")] [TestCase(11385u, "BLU")]
    public void EverySupportedJobRetainsItsSheetGcdGroup(uint action, string job)
    {
        var row = DataminingGameData.RecastData(action);
        Assert.That(row.Recast, Is.EqualTo(2.5f), job + " before native traits/status adjustments");
        Assert.That(row.Group, Is.EqualTo(58), job);
        Assert.That(row.Category, Is.AnyOf(2u, 3u));
    }

    [TestCase(7384u, 1.5f, 2.5f)]
    [TestCase(7527u, 0f, 1.5f)]
    [TestCase(34626u, 0f, 2.2f)]
    public void CastTimesAndAcceleratedRecastsRemainDistinct(uint action, float cast, float recast)
    {
        var row = DataminingGameData.RecastData(action);
        Assert.That(row.Cast, Is.EqualTo(cast));
        Assert.That(row.Recast, Is.EqualTo(recast));
        Assert.That(row.Group, Is.EqualTo(58));
    }

    [TestCase(0u, 100, 2500)]
    [TestCase(364u, 95, 2375)]
    [TestCase(365u, 90, 2250)]
    [TestCase(366u, 85, 2125)]
    [TestCase(367u, 80, 2000)]
    [TestCase(584u, 85, 2125)]
    public void PermanentTraitHasteUsesNativePercentageAndReplacesLowerTraits(uint highestTrait, int haste, int recast)
    {
        var actual = OfflineLoadouts.HasteFromTraits(id => highestTrait == 584 ? id == 584 : id >= 364 && id <= highestTrait);
        Assert.That(actual, Is.EqualTo(haste));
        Assert.That(NativeStatAdjustment(2500, 1000, actual), Is.EqualTo(recast));
    }

    private static int NativeStatAdjustment(int recastMs, int speedModifier, int hastePercent)
        => Math.Max(recastMs * speedModifier / 1000 * hastePercent / 100, 1500);
}
