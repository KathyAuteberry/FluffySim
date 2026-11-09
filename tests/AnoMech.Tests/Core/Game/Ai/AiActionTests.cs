using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr;

namespace AnoMech.Tests;

public class AiActionTests
{
    private sealed class ProcScenario(PartyRole role, bool scheduled) : IScenario
    {
        public string Name => "Action proc test";
        public IPhase Phase => DsrZone.P2;
        public IReadOnlyList<IScenarioAi> AiStrats => [];
        public List<bool> Procs { get; } = [];

        public void Run(SimWorld world, int? selectedAi)
        {
            var ai = new AiManager(world);
            for (var i = 0; i < 32; i++)
            {
                var time = 1f + i * .2f;
                world.Events.Add(time, () => world.Party.Get(role)!.RemoveStatus(1234));
                if (scheduled) ai.UseAction(time, role, 7505);
                else world.Events.Add(time, () => ((ISimPartyMember)world.Party.Get(role)!).UseAction(7505));
                world.Events.Add(time, () => Procs.Add(world.Party.Get(role)!.HasStatus(1234)));
            }
            world.Events.Add(8f, () => { });
        }
    }

    [TestCase(PartyRole.MainTank, true)]
    [TestCase(PartyRole.CasterDps, true)]
    [TestCase(PartyRole.MainTank, false)]
    [TestCase(PartyRole.CasterDps, false)]
    public void BotActionsReproduceTheirProcsFromTheRunSeed(PartyRole actor, bool scheduled)
    {
        const int seed = 57;
        var first = Run(seed, actor, scheduled);
        var repeated = Run(seed, actor, scheduled);
        var different = Run(seed + 1, actor, scheduled);

        Assert.That(first, Has.Count.EqualTo(32));
        Assert.That(first, Does.Contain(true));
        Assert.That(first, Does.Contain(false));
        Assert.That(repeated, Is.EqualTo(first));
        Assert.That(different, Is.Not.EqualTo(first));
    }

    private static IReadOnlyList<bool> Run(int seed, PartyRole actor, bool scheduled)
    {
        var scenario = new ProcScenario(actor, scheduled);
        var run = ScenarioRun.Execute(typeof(ProcScenario), 0, seed, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = PartyRole.MainTank,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        return scenario.Procs;
    }
}
