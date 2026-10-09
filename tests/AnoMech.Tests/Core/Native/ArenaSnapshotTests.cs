using System.Text.Json;
using AnoMech.Core.Native.Implementations;

namespace AnoMech.Tests;

public class ArenaSnapshotTests
{
    [TestCase("../replay-p2")]
    [TestCase("replay-p2/other")]
    [TestCase("a character name")]
    [TestCase("")]
    public void InvalidLabelRejectedBeforeNativeAccess(string label)
    {
        Assert.Throws<ArgumentException>(() => ArenaSnapshot.Save(label));
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
