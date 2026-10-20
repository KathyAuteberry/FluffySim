using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.DiamondDust;
using AnoMech.Scenarios.Fru.MirrorMirror;

namespace AnoMech.Tests;

public class FruP2RuntimeTests
{
    [TestCase(typeof(FruDiamondDustScenario))]
    [TestCase(typeof(FruMirrorMirrorScenario))]
    [Category("Extended")]
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
    [TestCase(typeof(FruDiamondDustScenario))]
    [TestCase(typeof(FruMirrorMirrorScenario))]
    public void RegisteredMeshlessHelpersDoNotRequireRenderedModels(Type type)
    {
        var run = ScenarioRun.Execute(type, 0, 0, new()
        {
            ConfigureNative = f => f.BattleCharas.MeshlessHelpersHaveDrawObjects = false,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(typeof(FruDiamondDustScenario))]
    [TestCase(typeof(FruMirrorMirrorScenario))]
    public void MissingVisibleModelsStillAbortStartup(Type type)
    {
        var run = ScenarioRun.Execute(type, 0, 0, new()
        {
            ConfigureNative = f => f.BattleCharas.VisibleActorsHaveDrawObjects = false,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Failure, Is.EqualTo("mechanic mistake"));
    }

    [Test]
    public void DelayedMirrorSharedGroupsReplayTheirQueuedNativeEffects()
    {
        FakeMapEffects? maps = null;
        var checkedEffects = false;
        var run = ScenarioRun.Execute(typeof(FruMirrorMirrorScenario), 0, 0, new()
        {
            ConfigureNative = f =>
            {
                maps = f.MapEffects;
                for (byte slot = 1; slot <= 8; slot++) maps.UnreadySlots.Add(slot);
                f.BattleCharas.MeshlessHelpersHaveDrawObjects = false;
                f.BattleCharas.PacketSpawnDelayFrames = 10;
            },
            Probe = p =>
            {
                if (p.Crossed(6.25f)) maps!.UnreadySlots.Clear();
                if (!p.Crossed(7f)) return;
                Assert.That(maps!.Applied.Count(e => e.Flags is 0x00020001u or 0x02000100u && e.Index is >= 1 and <= 8), Is.EqualTo(3));
                checkedEffects = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedEffects, Is.True);
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
