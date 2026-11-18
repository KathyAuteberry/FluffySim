using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.TopNative.HelloWorld;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;

namespace AnoMech.Tests;

public class TopHelloWorldTests
{
    [Test]
    public void HelloWorldLeavesEveryoneWithEveryDebuggerAndCriticalErrorAddsNoBug()
    {
        var missing = new List<string>();
        var run = ScenarioRun.Execute(typeof(TopHelloWorldScenario), 0, Random.Shared.Next(), new()
        {
            Probe = p =>
            {
                if (p.Crossed(TopHelloWorldScenario.CriticalErrorCast - 0.5f))
                    foreach (var role in PerRole.All)
                    foreach (var debugger in new[] { StatusId.OverflowDebugger, StatusId.SynchronizationDebugger, StatusId.UnderflowDebugger, StatusId.PerformanceDebugger })
                        if (!p.Member(role)!.HasStatus(debugger)) missing.Add($"{role} lacks {debugger}");
                if (p.Crossed(TopHelloWorldScenario.CriticalErrorCast + 9f))
                    foreach (var role in PerRole.All)
                        if (p.Member(role)!.HasStatus(StatusId.CriticalOverflowBug)) missing.Add($"{role} got Critical Overflow Bug from Critical Error");
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(missing, Is.Empty);
    }

    [Test]
    public void NobodyCutsThroughTheCentreBeforeTheLastLoopMeetsThere()
    {
        var closest = float.MaxValue;
        var who = "";
        var until = TopHelloWorldScenario.ResolveTime(TopHelloWorldState.Loops - 1) + 1f;
        var run = ScenarioRun.Execute(typeof(TopHelloWorldScenario), 0, Random.Shared.Next(), new()
        {
            StopAt = until,
            Probe = p =>
            {
                if (p.Time < 2f) return;
                foreach (var role in PerRole.All)
                {
                    var pos = p.Member(role)!.Position;
                    var d = MathF.Sqrt(pos.X * pos.X + pos.Z * pos.Z);
                    if (d < closest) { closest = d; who = $"{role} at t={p.Time:F1}"; }
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(closest, Is.GreaterThan(5f), who);
    }
}
