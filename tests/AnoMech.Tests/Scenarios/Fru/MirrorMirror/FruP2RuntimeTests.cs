using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.DiamondDust;
using AnoMech.Scenarios.Fru.MirrorMirror;

namespace AnoMech.Tests;

public class FruP2RuntimeTests
{
    [TestCase(typeof(FruDiamondDustScenario))]
    [TestCase(typeof(FruMirrorMirrorScenario))]
    public void EveryHumanSeatClearsWithInvisibleHardBots(Type type)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        for (var seed = 0; seed < 8; seed++)
        {
            var run = ScenarioRun.Execute(type, 0, seed, new()
            {
                PlayerRole = role, ShowSimulatedParty = false, BotDifficulty = BotDifficulty.Hard,
            });
            Assert.That(run.Passed, run.ToString);
        }
    }
    [Test]
    public void FailedMirrorActorsCannotProduceAnAutomaticClear()
    {
        var run = ScenarioRun.Execute(typeof(FruMirrorMirrorScenario), 0, 0, new()
        {
            ConfigureNative = f => f.BattleCharas.PacketSpawnDelayFrames = 600,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Failure, Is.EqualTo("mechanic mistake"));
        Assert.That(run.Deaths, Is.Empty);
    }
}
