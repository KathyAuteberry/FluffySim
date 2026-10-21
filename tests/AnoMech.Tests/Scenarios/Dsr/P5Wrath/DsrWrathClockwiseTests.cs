using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.P5Wrath;

namespace AnoMech.Tests;

public class DsrWrathClockwiseTests
{
    [TestCase(0f)] [TestCase(90f)] [TestCase(180f)] [TestCase(270f)]
    public void MarkedBotsDodgeClockwiseAfterHitsAndFinishWrath(float bearing)
    {
        var scenario = new DsrP5WrathScenario();
        var initial = new Dictionary<PartyRole, Vector3>();
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 9, new()
        {
            ScenarioFactory = () => scenario,
            Overrides = o => ((DsrP5WrathStateOverrides)o).DragonBearing = bearing,
            Probe = p =>
            {
                var state = scenario.LastState;
                if (p.Crossed(44.95f))
                    foreach (var role in state.Marked) initial[role] = p.Member(role)!.Position;
                if (p.Crossed(45.12f))
                {
                    foreach (var role in state.Marked)
                        Assert.That(Vector3.Distance(p.Member(role)!.Position, initial[role]), Is.LessThan(.05f), "moved before attack snapshots");
                    foreach (var role in state.Marked) Assert.That(p.Member(role)!.HasStatus(DsrP5Constants.StatusId.MagicVulnerabilityUp), Is.True);
                    checks++;
                }
                if (p.Crossed(46.65f))
                {
                    var actions = p.World.Children.OfType<SimEnemy>().SelectMany(e => ((FakeBattleChara)e.Proxy!).ReceivedEffects).ToArray();
                    Assert.That(actions.Count(a => a.ActionId == DsrP5Constants.ActionId.SpiralPierce), Is.EqualTo(2));
                    Assert.That(actions.Count(a => a.ActionId == DsrP5Constants.ActionId.WrathSkywardLeap), Is.EqualTo(1));
                    foreach (var role in state.Marked)
                    {
                        var at = p.Member(role)!.Position;
                        var angle = ClockwiseAngle(initial[role], at);
                        Assert.That(angle, Is.InRange(8f, 75f), $"{role}: {initial[role]} -> {at}");
                        Assert.That(Vector3.Distance(at, initial[role]), Is.GreaterThan(2f));
                        Assert.That(at.Length(), Is.GreaterThan(16f), "route cut through the arena");
                    }
                    checks++;
                }
                if (p.Crossed(50.9f))
                {
                    foreach (var (role, angle) in new[] { (state.Defamation, 330f), (state.Tethered[0], 270f), (state.Tethered[1], 220f) })
                        Assert.That(Vector3.Distance(p.Member(role)!.Position, DsrP5Constants.AtBearing(bearing + angle, 15)), Is.LessThan(.1f));
                    var members = p.World.Party.ActiveMembers().ToArray();
                    for (var i = 0; i < members.Length; i++)
                    for (var j = i + 1; j < members.Length; j++)
                        Assert.That(Vector3.Distance(members[i].Position, members[j].Position), Is.GreaterThan(5f));
                    checks++;
                }
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(checks, Is.EqualTo(3));
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Completed, Is.True);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void HumanMarkedSeatKeepsMovementResponsibility(int assignment)
    {
        var scenario = new DsrP5WrathScenario();
        var position = Vector3.Zero;
        var human = assignment == 0 ? PartyRole.ShieldHealer : assignment == 1 ? PartyRole.MeleeDpsA : PartyRole.PhysRangedDps;
        var checkedPosition = false;
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 9, new()
        {
            PlayerRole = human, ScenarioFactory = () => scenario, StopAt = 47,
            Takeovers = [new(44.8f, null)],
            Overrides = o =>
            {
                var x = (DsrP5WrathStateOverrides)o;
                x.Defamation = PartyRole.ShieldHealer;
                x.Tethered = [PartyRole.MeleeDpsA, PartyRole.PhysRangedDps];
            },
            Probe = p =>
            {
                if (p.Crossed(44.95f)) position = p.Member(human)!.Position;
                if (!p.Crossed(46.65f)) return;
                Assert.That(Vector3.Distance(p.Member(human)!.Position, position), Is.LessThan(.05f));
                checkedPosition = true;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(checkedPosition, Is.True);
        Assert.That(run.Deaths.Any(d => d.Role == human && d.ActionId == DsrP5Constants.ActionId.Twister), Is.True, run.ToString);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void HumanCanPerformEachMarkedRouteWhileOtherBotsFinish(int assignment)
    {
        var human = assignment == 0 ? PartyRole.ShieldHealer : assignment == 1 ? PartyRole.MeleeDpsA : PartyRole.PhysRangedDps;
        var poses = new List<Vector3>();
        void Assign(object o)
        {
            var x = (DsrP5WrathStateOverrides)o;
            x.DragonBearing = 90;
            x.Defamation = PartyRole.ShieldHealer;
            x.Tethered = [PartyRole.MeleeDpsA, PartyRole.PhysRangedDps];
        }
        var reference = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 9, new()
        {
            PlayerRole = human, Overrides = Assign, Probe = p => poses.Add(p.Member(human)!.Position), WriteArtifactsOnFailure = false,
        });
        Assert.That(reference.Passed, reference.ToString);
        var frame = 0;
        var run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, 9, new()
        {
            PlayerRole = human, Overrides = Assign, Takeovers = [new(44.8f, null)],
            Probe = p =>
            {
                // Input replay supplies the human pose; the encounter AI still cannot steer that seat.
                if (p.Time >= 44.8f) p.Member(human)!.SetPosition(poses[Math.Min(frame, poses.Count - 1)]);
                frame++;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(run.Elapsed, Is.GreaterThan(88));
    }

    private static float ClockwiseAngle(Vector3 from, Vector3 to)
    {
        var angle = (MathF.Atan2(to.X, -to.Z) - MathF.Atan2(from.X, -from.Z)) * 180f / MathF.PI;
        return (angle + 360f) % 360f;
    }
}
