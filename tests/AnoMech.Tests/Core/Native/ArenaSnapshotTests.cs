using System.Text.Json;
using AnoMech.Core.Native.Implementations;

namespace AnoMech.Tests;

public class ArenaSnapshotTests
{
    [TestCase("anomech-p3")]
    [TestCase("replay-p3")]
    [TestCase("replay-p3-entry")]
    [TestCase("dsr-p2-replay")]
    [TestCase("dsr-p2-sim")]
    [TestCase("generic-test")]
    public void DescriptiveLabelsAreAcceptedWithoutNativeAccess(string label)
    {
        Assert.That(ArenaSnapshot.IsAllowedLabel(label), Is.True);
    }

    [TestCase("../replay-p2")]
    [TestCase("replay-p2/other")]
    [TestCase("a character name")]
    [TestCase("")]
    public void InvalidLabelRejectedBeforeNativeAccess(string label)
    {
        Assert.Throws<ArgumentException>(() => ArenaSnapshot.Save(label));
    }

    [TestCase(1238u)] [TestCase(968u)] [TestCase(177u)]
    public void AnyEstablishedMatchingTerritoryIsAccepted(uint territory) =>
        Assert.That(ArenaSnapshot.IsReady(territory, territory, 7, 2), Is.True);

    [TestCase(0u, 0u, 7, 2)] [TestCase(968u, 1238u, 7, 2)]
    [TestCase(968u, 968u, 6, 2)] [TestCase(968u, 968u, 7, 1)]
    public void UnavailableOrTransitioningLayoutIsRejected(uint territory, uint layout, int init, int load) =>
        Assert.That(ArenaSnapshot.IsReady(territory, layout, init, (uint)load), Is.False);

    [Test]
    public void RepeatedSnapshotsHaveUniqueSafeNames()
    {
        var utc = DateTime.UtcNow;
        var names = Enumerable.Range(0, 100).Select(_ => ArenaSnapshot.FileName("dsr-p2-replay", utc)).ToArray();
        Assert.That(names.Distinct().Count(), Is.EqualTo(100));
        Assert.That(names.All(n => Path.GetFileName(n) == n && n.EndsWith(".json")), Is.True);
    }

    [Test]
    public void SnapshotCannotOverwriteAnExistingFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "snapshot.json");
        try
        {
            ArenaSnapshot.WriteDocument(path, new { State = 1 });
            var original = File.ReadAllBytes(path);
            Assert.Throws<IOException>(() => ArenaSnapshot.WriteDocument(path, new { State = 4 }));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            using var parsed = JsonDocument.Parse(original);
            Assert.That(parsed.RootElement.GetProperty("State").GetInt32(), Is.EqualTo(1));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
