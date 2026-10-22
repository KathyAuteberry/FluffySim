using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.P5Wrath;

namespace AnoMech.Tests;

public class DsrWrathTwisterNavigationTests
{
    [TestCase(0f, BotDifficulty.Easy)] [TestCase(90f, BotDifficulty.Easy)]
    [TestCase(180f, BotDifficulty.Easy)] [TestCase(270f, BotDifficulty.Easy)]
    [TestCase(0f, BotDifficulty.Hard)] [TestCase(90f, BotDifficulty.Hard)]
    [TestCase(180f, BotDifficulty.Hard)] [TestCase(270f, BotDifficulty.Hard)]
    public void HumanBaitsOnTetherClockwiseWaypointAndEntireWrathSurvives(float bearing, BotDifficulty difficulty)
        => RunObstructedWrath(bearing, difficulty, 250f, 18.5f);

    [TestCase(BotDifficulty.Easy)] [TestCase(BotDifficulty.Hard)]
    public void HumanBaitsBetweenClockwiseWaypointsAndEntireWrathSurvives(BotDifficulty difficulty)
        => RunObstructedWrath(0f, difficulty, 260f, 17f);

    private static void RunObstructedWrath(float bearing, BotDifficulty difficulty, float baitBearing, float baitRadius)
    {
        void Assign(object o)
        {
            var x = (DsrP5WrathStateOverrides)o;
            x.DragonBearing = bearing;
            x.Defamation = PartyRole.ShieldHealer;
            x.Tethered = [PartyRole.MeleeDpsA, PartyRole.PhysRangedDps];
            x.Divebomb = PartyRole.CasterDps;
        }
        var poses = new List<Vector3>();
        var reference = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 9, new()
        {
            PlayerRole = PartyRole.MainTank, BotDifficulty = difficulty, Overrides = Assign,
            Probe = p => poses.Add(p.Member(PartyRole.MainTank)!.Position), WriteArtifactsOnFailure = false,
        });
        Assert.That(reference.Passed, reference.ToString);
        var scenario = new DsrP5WrathScenario();
        var bait = DsrP5Constants.AtBearing(bearing + baitBearing, baitRadius);
        var arrival = poses[(int)(50.8f / ScenarioRun.FrameSeconds)];
        var frame = 0;
        var observed = false;
        var previous = new Dictionary<PartyRole, Vector3>();
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 9, new()
        {
            ScenarioFactory = () => scenario, PlayerRole = PartyRole.MainTank, BotDifficulty = difficulty,
            Overrides = Assign, Takeovers = [new(44.8f, new(bait.X, bait.Z))],
            Probe = p =>
            {
                if (p.Time >= 44.8f)
                {
                    var fraction = Math.Clamp((p.Time - 45.15f) * AiManager.SprintSpeed / Vector3.Distance(bait, arrival), 0, 1);
                    var position = p.Time < 51.1f ? Vector3.Lerp(bait, arrival, fraction) : poses[Math.Min(frame, poses.Count - 1)];
                    p.Member(PartyRole.MainTank)!.SetPosition(position);
                }
                if (p.Crossed(45f))
                {
                    Assert.That(Vector3.Distance(scenario.LastState!.Twisters.Single(t => t.Owner == PartyRole.MainTank).Position, bait), Is.LessThan(.001f));
                    observed = true;
                }
                if (p.Crossed(46.5f))
                {
                    var snapshot = scenario.LastState!.Twisters.Single(t => t.Owner == PartyRole.MainTank);
                    Assert.That(Vector3.Distance(snapshot.Position, bait), Is.LessThan(.001f));
                    Assert.That(Vector3.Distance(p.Member(PartyRole.MainTank)!.Position, bait), Is.GreaterThan(5));
                    var oldFrom = DsrP5Constants.AtBearing(bearing + (baitBearing == 250f ? 230f : 250f), 18.5f);
                    var oldTo = baitBearing == 250f ? bait : DsrP5Constants.AtBearing(bearing + 270f, 15f);
                    Assert.That(TimedCircleRoute.SegmentClear(new(oldFrom.X, oldFrom.Z), new(oldTo.X, oldTo.Z), baitBearing == 250f ? 46.05f : 46.95f, AiManager.SprintSpeed, [snapshot.Hazard]), Is.False, "old route must intersect the human hazard");
                }
                foreach (var role in Enum.GetValues<PartyRole>().Where(r => r != PartyRole.MainTank))
                {
                    var at = p.Member(role)!.Position;
                    if (p.Time >= 46.72f && p.Time < 50.9f && previous.TryGetValue(role, out var from))
                    {
                        Assert.That(TimedCircleRoute.SegmentClear(new(from.X, from.Z), new(at.X, at.Z), p.Time - ScenarioRun.FrameSeconds, AiManager.SprintSpeed,
                            scenario.LastState!.Twisters.Select(t => t.Hazard with { Radius = WrathTwister.CollisionRadius - TimedCircleRoute.Clearance }).ToArray()), Is.True, $"{role}@{p.Time}");
                    }
                    if (p.Time > 45.2f && p.Time < 49f && scenario.LastState!.Marked.Contains(role) && previous.TryGetValue(role, out var old))
                    {
                        Assert.That(old.X * at.Z - old.Z * at.X, Is.GreaterThanOrEqualTo(-.01f), $"{role} reversed clockwise movement");
                        Assert.That(at.Length(), Is.InRange(12f, 20.01f));
                    }
                    previous[role] = at;
                }
                frame++;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(observed, Is.True);
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Completed, Is.True);
    }

    [Test]
    public void MovingBaiterDoesNotMoveItsTwisterAndStationaryBaiterIsPunished()
    {
        var scenario = new DsrP5WrathScenario();
        Vector3 bait = default;
        var checkedSnapshot = false;
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 9, new()
        {
            PlayerRole = PartyRole.ShieldHealer, ScenarioFactory = () => scenario,
            Overrides = o => ((DsrP5WrathStateOverrides)o).Defamation = PartyRole.ShieldHealer,
            Takeovers = [new(44.8f, null)], StopAt = 47,
            Probe = p =>
            {
                if (p.Crossed(45)) bait = scenario.LastState!.Twisters.Single(t => t.Owner == PartyRole.ShieldHealer).Position;
                if (!p.Crossed(46.5f)) return;
                Assert.That(scenario.LastState!.Twisters.Single(t => t.Owner == PartyRole.ShieldHealer).Position, Is.EqualTo(bait));
                checkedSnapshot = true;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(checkedSnapshot, Is.True);
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.ShieldHealer && d.ActionId == DsrP5Constants.ActionId.Twister), Is.True);
    }
}
