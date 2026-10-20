#pragma warning disable CA1416
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P5Wrath;
using AnoMech.Scenarios.Dsr.P5Death;

namespace AnoMech.Tests;

public class DsrP5PracticeTests
{
    [Test]
    public void PreferencesPersistIndependentlyAndDefaultToRandom()
    {
        var defaults = new Configuration();
        Assert.That(defaults.DsrWrathAssignment, Is.Zero);
        Assert.That(defaults.DsrDeathDoom, Is.Null);
        var config = new Configuration { DsrWrathAssignment = (int)WrathPracticeAssignment.LiquidHeavenBait, DsrDeathDoom = false };
        var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(Newtonsoft.Json.JsonConvert.SerializeObject(config))!;
        Assert.That(restored.DsrWrathAssignment, Is.EqualTo(config.DsrWrathAssignment));
        Assert.That(restored.DsrDeathDoom, Is.False);
    }

    [TestCase(WrathPracticeAssignment.Random)]
    [TestCase(WrathPracticeAssignment.BlueMarker)]
    [TestCase(WrathPracticeAssignment.DragoonTether)]
    [TestCase(WrathPracticeAssignment.GreenMarker)]
    [TestCase(WrathPracticeAssignment.Thunderstruck)]
    [TestCase(WrathPracticeAssignment.EruptionBait)]
    [TestCase(WrathPracticeAssignment.LiquidHeavenBait)]
    [TestCase(WrathPracticeAssignment.NoEarlyMarker)]
    public void WrathPreferencesPreserveValidAssignmentsAndBotsClear(WrathPracticeAssignment preference)
    {
        foreach (var role in new[] { PartyRole.MainTank, PartyRole.CasterDps })
        {
            var scenario = new DsrP5WrathScenario();
            var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 7 + (int)preference, new()
            {
                PlayerRole = role, ScenarioFactory = () => scenario,
                Overrides = o => ((DsrP5WrathStateOverrides)o).PracticeAssignment = preference,
            });
            Assert.That(run.Passed, run.ToString);
            var state = scenario.LastState!;
            Assert.That(state.Tethered.Distinct().Count(), Is.EqualTo(2));
            Assert.That(state.Marked.Append(state.Divebomb).Distinct().Count(), Is.EqualTo(4));
            Assert.That(state.Lightning.Distinct().Count(), Is.EqualTo(2));
            Assert.That(state.Plain.Contains(state.LiquidHeaven), Is.True);
            Assert.That(state.Marked.Append(state.Divebomb).Contains(state.Eruptions), Is.True);
            Assert.That(state.Lightning, Does.Not.Contain(state.LiquidHeaven).And.Not.Contain(state.Eruptions));
            Assert.That(preference switch
            {
                WrathPracticeAssignment.BlueMarker => state.Defamation == role,
                WrathPracticeAssignment.DragoonTether => state.Tethered.Contains(role),
                WrathPracticeAssignment.GreenMarker => state.Divebomb == role,
                WrathPracticeAssignment.Thunderstruck => state.Lightning.Contains(role),
                WrathPracticeAssignment.EruptionBait => state.Eruptions == role,
                WrathPracticeAssignment.LiquidHeavenBait => state.LiquidHeaven == role,
                WrathPracticeAssignment.NoEarlyMarker => !state.Marked.Contains(role),
                _ => true,
            }, Is.True);
        }
    }

    [TestCase(true)] [TestCase(false)]
    public void DoomPreferenceKeepsFourDoomsAndSurvivesRepeatedPractice(bool doom)
    {
        var scenario = new DsrP5DeathScenario();
        for (var seed = 0; seed < 2; seed++)
        {
            var run = ScenarioRun.Execute(typeof(DsrP5DeathScenario), 0, seed, new()
            {
                PlayerRole = PartyRole.RegenHealer, ScenarioFactory = () => scenario,
                Overrides = o => ((DsrP5DeathStateOverrides)o).PlayerDoom = doom,
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(scenario.LastState!.Dooms.Distinct().Count(), Is.EqualTo(4));
            Assert.That(scenario.LastState.IsDoomed(PartyRole.RegenHealer), Is.EqualTo(doom));
        }
    }
}
