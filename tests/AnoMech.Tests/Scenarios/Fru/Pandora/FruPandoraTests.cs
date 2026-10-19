using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Pandora;
using AnoMech.Scenarios.Fru.Upstream.P5FulgentBlade;
using AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained;

namespace AnoMech.Tests;

public class FruPandoraTests
{
    private static IEnumerable<TestCaseData> TowerPatterns()
        => from rotation in Enumerable.Range(0, 3)
           from eastSecond in new[] { false, true }
           from darkFirst in new[] { false, true }
           from role in Enum.GetValues<PartyRole>()
           from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
           select new TestCaseData(rotation, eastSecond, darkFirst, role, difficulty);

    [TestCaseSource(nameof(TowerPatterns))]
    public void ManaTowersAndFourPolarizingRoundsClear(int rotation, bool eastSecond, bool darkFirst, PartyRole role, BotDifficulty difficulty)
    {
        var run = ScenarioRun.Execute(typeof(FruP5ParadiseRegainedScenario), 0, 72 + (int)role, new()
        {
            PlayerRole = role, BotDifficulty = difficulty,
            Overrides = o =>
            {
                var settings = (FruP5ParadiseRegainedStateOverrides)o;
                settings.Rotation = rotation;
                settings.EastSecond = eastSecond;
                settings.DarkFirst = darkFirst;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EveryExalineRotationAndMirrorClearsWithManaLightParties(bool mirrored)
    {
        for (var position = 0; position < 4; position++)
        for (var rotation = 0; rotation < 4; rotation++)
        {
            var p = position;
            var r = rotation;
            var run = ScenarioRun.Execute(typeof(FruP5FulgentBladeScenario), 0, position * 4 + rotation, new()
            {
                BotDifficulty = BotDifficulty.Hard,
                Overrides = o =>
                {
                    var settings = (FruP5FulgentBladeStateOverrides)o;
                    settings.PositionIndex = p;
                    settings.RotationIndex = r;
                    settings.Mirrored = mirrored;
                },
            });
            Assert.That(run.Passed, run.ToString);
        }
    }

    [Test]
    public void IsolatedPolarizingClearsEverySeat()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var run = ScenarioRun.Execute(typeof(FruPolarizingStrikesScenario), 0, 81, new() { PlayerRole = role });
            Assert.That(run.Passed, run.ToString);
        }
    }

    [Test]
    public void BoxRequiresActiveLbThreeAndConsumesGauge()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var sawProtection = false;
            var run = ScenarioRun.Execute(typeof(FruPandorasBoxScenario), 0, 0, new()
            {
                PlayerRole = role,
                Probe = p =>
                {
                    if (!p.Crossed(11f)) return;
                    sawProtection = true;
                    Assert.That(p.World.Party.LimitBreak.FilledBars, Is.Zero);
                    Assert.That(p.World.Party.ActiveMembers().All(FruPandorasBoxScenario.HasTankLimitBreakThree), Is.True);
                },
            });
            Assert.That(sawProtection, Is.True);
            Assert.That(run.Passed, run.ToString);
        }
    }

    [TestCase(1f)]
    [TestCase(14f)]
    public void EarlyOrLateLbFailsBox(float pressAt)
    {
        var run = ScenarioRun.Execute(typeof(FruPandorasBoxScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (p.Crossed(0.2f)) DebugBotControl.Enabled = false;
                if (!p.Crossed(pressAt)) return;
                DebugBotControl.Enabled = true;
                p.World.Party.UseLimitBreak(PartyRole.MainTank);
                DebugBotControl.Enabled = false;
            },
        });
        Assert.That(run.Deaths, Has.Count.EqualTo(8));
        Assert.That(run.Deaths.All(d => d.ActionId == FruPandorasBoxScenario.ActionId), Is.True);
    }

    [TestCase(2f)]
    [TestCase(0f)]
    public void LowerLevelOrMissingLimitBreakFailsBox(float bars)
    {
        var run = ScenarioRun.Execute(typeof(FruPandorasBoxScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (p.Crossed(0.2f)) p.World.Party.LimitBreak.Set(bars);
            },
        });
        Assert.That(run.Deaths, Has.Count.EqualTo(8), run.ToString);
        Assert.That(run.Deaths.All(d => d.ActionId == FruPandorasBoxScenario.ActionId), Is.True);
    }

    [Test]
    public void LeavingAParadiseTowerEmptyFailsTheSoak()
    {
        var run = ScenarioRun.Execute(typeof(FruP5ParadiseRegainedScenario), 0, 8, new()
        {
            PlayerRole = PartyRole.RegenHealer, StopAt = 17f, WriteArtifactsOnFailure = false,
            Overrides = o => ((FruP5ParadiseRegainedStateOverrides)o).Rotation = 0,
            Takeovers = [new(15f, new Vector2(0f, 15f))],
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == 40321), Is.True, run.ToString);
    }

    [Test]
    public void RejoiningTheSameColorAfterFrontBaitDiesToResistanceDown()
    {
        var sawResistance = false;
        var run = ScenarioRun.Execute(typeof(FruPolarizingStrikesScenario), 0, 81, new()
        {
            PlayerRole = PartyRole.MainTank, WriteArtifactsOnFailure = false,
            Takeovers = [new(11.3f, new Vector2(-4f, 4f))],
            Probe = p =>
            {
                if (p.Crossed(11.2f)) sawResistance = p.World.Party.Player!.HasStatus(4164);
            },
        });
        Assert.That(sawResistance, Is.True);
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == 40317
            && d.Cause.Contains("vuln up")), Is.True, run.ToString);
    }

    [Test]
    public void FailingToDodgePolarizingEchoKills()
    {
        var run = ScenarioRun.Execute(typeof(FruPolarizingStrikesScenario), 0, 81, new()
        {
            PlayerRole = PartyRole.MainTank, WriteArtifactsOnFailure = false,
            Takeovers = [new(7.6f, new Vector2(-4f, 4f))],
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank), Is.True);
    }
}
