using System.Reflection;
using System.Runtime.CompilerServices;
using AnoMech.Core.Game.Ai;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;
using AnoMech.Windows;
using Newtonsoft.Json;

namespace AnoMech.Tests;

public class StrategySelectionTests
{
    private static MainWindow Window(Configuration config)
    {
        DalamudServices.Install(nameof(Plugin.Config), config);
        var window = (MainWindow)RuntimeHelpers.GetUninitializedObject(typeof(MainWindow));
        Set(window, "_waymarkMemory", new Dictionary<IZone, int>());
        Set(window, "_stratGroupMemory", new Dictionary<IScenario, string>());
        return window;
    }

    private static void Set(MainWindow window, string field, object value)
        => typeof(MainWindow).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, value);
    private static T Get<T>(MainWindow window, string field)
        => (T)typeof(MainWindow).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
    private static void Select(MainWindow window, IScenario scenario)
        => typeof(MainWindow).GetMethod("SelectScenario", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, [scenario, false]);

    [TestCase("{}")]
    [TestCase("{\"Version\":0,\"LastSelectedScenario\":\"old scenario\"}")]
    public void FreshAndLegacyConfigurationsSelectJpMana(string json)
    {
        var window = Window(JsonConvert.DeserializeObject<Configuration>(json)!);
        var scenario = new FruP4CrystallizeTimeScenario();
        Select(window, scenario);
        Assert.That(Get<string>(window, "_selectedStratGroup"), Is.EqualTo("JP"));
        Assert.That(scenario.AiStrats[Get<int>(window, "_selectedStrat")].Group, Is.EqualTo("Mana"));
        var groups = (IReadOnlyList<string>)typeof(MainWindow).GetMethod("StratGroups", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [scenario])!;
        Assert.That(groups, Is.EqualTo(new[] { "JP", "NA" }));
        Assert.That(scenario.AiStrats[1].SelectionLabel, Does.StartWith("Mana"));
    }

    [Test]
    public void ExplicitNaPreferenceSurvivesSerializationAndScenarioSwitches()
    {
        var scenario = new FruP4CrystallizeTimeScenario();
        var na = scenario.AiStrats[0];
        var config = new Configuration();
        config.ScenarioStrategies[scenario.FullName()] = $"{na.Group}\n{na.Name}";
        var window = Window(JsonConvert.DeserializeObject<Configuration>(JsonConvert.SerializeObject(config))!);
        Select(window, scenario);
        Select(window, ScenarioCatalog.Create().Scenarios[0]);
        Select(window, scenario);
        Assert.That(Get<string>(window, "_selectedStratGroup"), Is.EqualTo("NA"));
        Assert.That(Get<int>(window, "_selectedStrat"), Is.Zero);
    }
}
