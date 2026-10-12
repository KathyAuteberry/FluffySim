#pragma warning disable CA1416
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace AnoMech.Tests;

public class ConfigurationMigrationTests
{
    [Test]
    public void AssemblyMetadataMatchesAuthoritativeVersion()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "FluffySim.Version.props"))) root = root.Parent!;
        var version = XDocument.Load(Path.Combine(root.FullName, "FluffySim.Version.props")).Descendants("FluffySimVersion").Single().Value;
        var assembly = typeof(Plugin).Assembly;
        Assert.That(assembly.GetName().Name, Is.EqualTo("FluffySim"));
        Assert.That(assembly.GetName().Version, Is.EqualTo(new Version(version + ".0")));
        Assert.That(FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion, Is.EqualTo(version + ".0"));
        Assert.That(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion, Is.EqualTo(version));
    }

    [Test]
    public void LegacyTypedSettingsLoadWithoutChangingOriginalOrScenarioIds()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var legacy = Path.Combine(directory, "AnoMech.json");
            var text = JsonConvert.SerializeObject(new Configuration { LastSelectedScenario = "Futures Rewritten (Ultimate)/P2 Light Rampant (Mana)", PracticeGodMode = true, ShowSimulatedParty = false }, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects }).Replace(", FluffySim", ", AnoMech");
            File.WriteAllText(legacy, text);
            var current = new FileInfo(Path.Combine(directory, "FluffySim.json"));
            var config = ConfigurationMigration.Load(current, () => throw new Exception("New config should not be read"));
            Assert.That(config.LastSelectedScenario, Is.EqualTo("Futures Rewritten (Ultimate)/P2 Light Rampant (Mana)"));
            Assert.That(config.PracticeGodMode, Is.True);
            Assert.That(config.ShowSimulatedParty, Is.False);
            Assert.That(File.ReadAllText(legacy), Is.EqualTo(text));
            Assert.That(current.Exists, Is.False);
            File.WriteAllText(current.FullName, "{}"); current.Refresh();
            var preferred = new Configuration { LastSelectedScenario = "new" };
            Assert.That(ConfigurationMigration.Load(current, () => preferred), Is.SameAs(preferred));
        }
        finally { Directory.Delete(directory, true); }
    }
}
