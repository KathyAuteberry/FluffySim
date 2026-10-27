#pragma warning disable CA1416
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru;

namespace AnoMech.Tests;

public class PracticeLoopTests
{
    [TestCase(null)]
    [TestCase(123)]
    public void AutoRestartPreservesSeedSelectionAcrossArenaPreparation(int? selectedSeed)
    {
        var fake = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Config), new Configuration());
        using var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        var practice = new PracticeLoop(game) { AutoRestart = true };
        var scenario = new SeedScenario();
        var selections = new List<int?>();
        game.RunStarted += (p, _) =>
        {
            selections.Add(p.Seed);
            if (selections.Count == 3) practice.AutoRestart = false;
        };
        game.EnterAndStart(new(scenario, new LocalPartyProvider { RoleOverride = PartyRole.MainTank }, null, 0, selectedSeed), SessionMode.Local);
        for (var frame = 0; frame < 1800 && selections.Count < 3; frame++)
            fake.Frame(1f / 60f, game.Tick);
        Assert.That(selections, Has.Count.EqualTo(3));
        Assert.That(selections, Is.All.EqualTo(selectedSeed));
        if (selectedSeed is { } seed)
            Assert.That(scenario.Seeds, Is.All.EqualTo(seed));
        else
            Assert.That(scenario.Seeds.Distinct().Count(), Is.EqualTo(3));
    }

    private sealed class SeedScenario : IScenario
    {
        public string Name => "Seed selection";
        public IPhase Phase => FruZone.P1;
        public bool SupportsSolo => true;
        public IReadOnlyList<IScenarioAi> AiStrats => [];
        public List<int> Seeds { get; } = [];
        public void Run(SimWorld world, int? selectedAi)
        {
            Seeds.Add(world.Rng.Seed);
            world.Events.Add(0.1f, () => { });
        }
    }
}
