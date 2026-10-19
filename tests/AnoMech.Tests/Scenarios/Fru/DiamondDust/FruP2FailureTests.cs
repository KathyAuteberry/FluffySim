using System.Numerics;
using AnoMech.Scenarios;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.DiamondDust;
using AnoMech.Scenarios.Fru.MirrorMirror;

namespace AnoMech.Tests;

public class FruP2FailureTests
{
    [TestCase(false)] [TestCase(true)]
    public void DiamondDustGazeChecksFacing(bool lookAway)
    {
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new()
        {
            ScenarioFactory = () => new FruDiamondDustScenario(), PlayerRole = PartyRole.MainTank,
            StopAt = 42f, WriteArtifactsOnFailure = false,
            Overrides = o => ((FruDiamondDustStateOverrides)o).GazeSector = 0,
            Takeovers = [new(40.5f, Vector2.Zero, lookAway ? 0f : MathF.PI)],
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == 40185), Is.EqualTo(!lookAway), run.ToString);
    }
    [Test]
    public void MissingTheFirstTwinSlideDies()
    {
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new()
        {
            ScenarioFactory = () => new FruDiamondDustScenario(), PlayerRole = PartyRole.MainTank,
            StopAt = 49f, WriteArtifactsOnFailure = false,
            Overrides = o => { var s = (FruDiamondDustStateOverrides)o; s.OracleSector = 0; s.Stillness = true; },
            Takeovers = [new(46f, Vector2.Zero)],
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == 40193), Is.True, run.ToString);
    }
    [Test]
    public void LeavingAHealerStackFailsHoly()
    {
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new()
        {
            ScenarioFactory = () => new FruDiamondDustScenario(), PlayerRole = PartyRole.MainTank,
            StopAt = 34f, WriteArtifactsOnFailure = false, Takeovers = [new(32.4f, Vector2.Zero)],
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == 40209), Is.True, run.ToString);
    }
    [Test]
    public void MissingAMirrorPocketFailsScythe()
    {
        var run = ScenarioRun.Execute(typeof(FruMirrorMirrorScenario), 0, 0, new()
        {
            ScenarioFactory = () => new FruMirrorMirrorScenario(), PlayerRole = PartyRole.MainTank,
            StopAt = 20f, WriteArtifactsOnFailure = false,
            Overrides = o => ((FruMirrorMirrorStateOverrides)o).Blue = 0,
            Takeovers = [new(17.7f, Vector2.Zero)],
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == 40203), Is.True, run.ToString);
    }
    [TestCase(false)] [TestCase(true)]
    public void WrongBanishSoakActuallyFails(bool pairs)
    {
        var s = new FruMirrorMirrorState(new Rng(0), new() { Blue = 0, Red = 1, Pairs = pairs });
        var target = FruMirrorMirrorAi.BanishSpot(s, PartyRole.MainTank);
        var run = ScenarioRun.Execute(typeof(FruMirrorMirrorScenario), 0, 0, new()
        {
            ScenarioFactory = () => new FruMirrorMirrorScenario(), PlayerRole = PartyRole.MeleeDpsA,
            StopAt = 36f, WriteArtifactsOnFailure = false,
            Overrides = o => { var settings = (FruMirrorMirrorStateOverrides)o; settings.Blue = 0; settings.Red = 1; settings.Pairs = pairs; },
            Takeovers = [new(34.2f, pairs ? Vector2.Zero : new Vector2(target.X, target.Z))],
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == (pairs ? 40222u : 40223u)), Is.True, run.ToString);
    }
}
