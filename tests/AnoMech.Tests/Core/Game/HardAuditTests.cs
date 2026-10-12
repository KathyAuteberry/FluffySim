#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Core.SimObjects;
using Newtonsoft.Json;

namespace AnoMech.Tests;

public class HardAuditTests
{
    [TestCaseSource(typeof(SharedPracticeTests), nameof(SharedPracticeTests.AllScenarios))]
    public void SameAssignmentsClearWithActualLaterMovement(Type type)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        for (var seed = 0; seed < 4; seed++)
        {
            BotDeparture? delayed = null;
            var observedHardWaiting = false;
            var observedEasyMoving = false;
            var assignments = new Dictionary<BotDifficulty, string>();
            foreach (var difficulty in new[] { BotDifficulty.Hard, BotDifficulty.Easy })
            {
                var scenario = (IScenario)Activator.CreateInstance(type)!;
                var run = ScenarioRun.Execute(type, 0, seed, new()
                {
                    ScenarioFactory = () => scenario, BotDifficulty = difficulty, PlayerRole = role,
                    Probe = p =>
                    {
                        if (difficulty == BotDifficulty.Hard)
                        {
                            delayed ??= p.World.BotMovement.Departures
                                .Where(d => d.DepartAt - d.RequestedAt > .3f && d.Distance > 1)
                                .Select(d => (BotDeparture?)d).FirstOrDefault();
                            if (delayed is { } d && p.Crossed(d.RequestedAt + MathF.Min(.5f, (d.DepartAt - d.RequestedAt) / 2)))
                            {
                                Assert.That(Vector3.Distance(d.Member.Position, d.From), Is.LessThan(.02f));
                                observedHardWaiting = true;
                            }
                        }
                        else if (delayed is { } d && p.Crossed(d.RequestedAt + MathF.Min(.5f, (d.DepartAt - d.RequestedAt) / 2)))
                        {
                            var matching = p.World.BotMovement.Departures.First(x =>
                                MathF.Abs(x.RequestedAt - d.RequestedAt) < .02f && x.Destination == d.Destination
                                && ((ISimPartyMember)x.Member).Role == ((ISimPartyMember)d.Member).Role);
                            Assert.That(Vector3.Distance(matching.Member.Position, matching.From), Is.GreaterThan(.2f));
                            observedEasyMoving = true;
                        }
                        if (p.Crossed(15)) assignments[difficulty] = JsonConvert.SerializeObject(type.GetProperty("LastState")!.GetValue(scenario));
                    }, WriteArtifactsOnFailure = false,
                });
                Assert.That(run.Passed, run.ToString);
            }
            Assert.That(observedHardWaiting && observedEasyMoving, Is.True, $"{type.Name}, seed {seed}");
            Assert.That(assignments[BotDifficulty.Hard], Is.EqualTo(assignments[BotDifficulty.Easy]));
        }
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void LightRampantFinalClocksWaitForHouseOfLightWindow(int seed)
    {
        var delayed = false;
        var waiting = false;
        var run = ScenarioRun.Execute(typeof(AnoMech.Scenarios.Fru.LightRampant.FruLightRampantScenario), 0, seed, new()
        {
            BotDifficulty = BotDifficulty.Hard,
            Overrides = o => ((AnoMech.Scenarios.Fru.LightRampant.FruLightRampantStateOverrides)o).Spread = false,
            Probe = p =>
            {
                var request = p.World.BotMovement.Departures.FirstOrDefault(d => d.Deadline == 45.4f && d.DepartAt - d.RequestedAt > 1);
                if (request.Member is null) return;
                delayed = true;
                if (p.Crossed(37.9f))
                {
                    Assert.That(Vector3.Distance(request.Member.Position, request.From), Is.LessThan(.02f));
                    waiting = true;
                }
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(delayed && waiting, Is.True);
    }
}
