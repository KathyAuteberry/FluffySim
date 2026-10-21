using System.Numerics;
using System.Reflection;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Pandora;
using AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained;

namespace AnoMech.Tests;

public class ParadiseOccupancyTests
{
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void ContinuousP5UsesManaRecipientsInBothParadiseRounds(int seat)
    {
        var inspected = 0;
        var scenario = new FruPandoraSequenceScenario();
        var stageField = typeof(FruPandoraSequenceScenario).GetField("stage", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var run = ScenarioRun.Execute(typeof(FruPandoraSequenceScenario), 0, 72 + seat, new()
        {
            PlayerRole = (PartyRole)seat, BotDifficulty = seat % 2 == 0 ? BotDifficulty.Easy : BotDifficulty.Hard,
            ScenarioFactory = () => scenario,
            Probe = p =>
            {
                foreach (var start in new[] { 37.35f, 154.7f })
                for (var tower = 0; tower < 3; tower++)
                {
                    if (!p.Crossed(start + FruP5ParadiseRegainedState.TowerHitAt(tower) + .04f)) continue;
                    var state = ((FruP5ParadiseRegainedScenario)stageField.GetValue(scenario)!).LastState!;
                    var occupants = p.World.Party.ActiveMembers()
                        .Where(m => Vector3.Distance(m.Position, state.TowerPosition(tower)) < 3)
                        .Select(m => ((ISimPartyMember)m).Role).ToArray();
                    Assert.That(occupants, Is.EquivalentTo(Enum.GetValues<PartyRole>().Where(r => FruP5ParadiseRegainedAi.AssignedTower(state, r) == tower)));
                    inspected++;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(inspected, Is.EqualTo(6));
    }
}
