using AnoMech.Core.Game;
using AnoMech.Scenarios.Fru.LightRampant;

namespace AnoMech.Tests;

public class FruLightRampantTimelineTests
{
    private static IEnumerable<TestCaseData> Recordings()
    {
        yield return new TestCaseData(4, new float[] { 12.851f, 14.415f, 15.976f, 17.542f, 19.101f, 16.155f, 30.832f, 21.866f, 24.319f, 27.351f, 34.044f, 43.007f });
        yield return new TestCaseData(6, new float[] { 12.91f, 14.47f, 16.032f, 17.592f, 19.152f, 16.165f, 30.865f, 21.907f, 24.308f, 27.339f, 34.036f, 42.998f });
        yield return new TestCaseData(8, new float[] { 12.879f, 14.445f, 16.012f, 17.574f, 19.138f, 16.146f, 30.844f, 21.864f, 24.318f, 27.353f, 34.096f, 43.053f });
        yield return new TestCaseData(9, new float[] { 12.912f, 14.473f, 16.034f, 17.599f, 19.16f, 16.168f, 30.839f, 21.878f, 24.33f, 27.365f, 34.051f, 43.011f });
        yield return new TestCaseData(13, new float[] { 12.912f, 14.47f, 16.031f, 17.592f, 19.149f, 16.165f, 30.819f, 21.913f, 24.319f, 27.343f, 34.025f, 42.984f });
        yield return new TestCaseData(15, new float[] { 12.914f, 14.472f, 16.032f, 17.591f, 19.152f, 16.165f, 30.815f, 21.91f, 24.315f, 27.342f, 34.015f, 42.968f });
        yield return new TestCaseData(16, new float[] { 12.851f, 14.416f, 15.98f, 17.54f, 19.101f, 16.159f, 30.839f, 21.866f, 24.321f, 27.312f, 34.05f, 42.98f });
    }

    [TestCaseSource(nameof(Recordings))]
    public void NormalizedSequenceMatchesRecording(int pull, float[] received)
    {
        var events = new List<(uint Action, float Time)>();
        var npcQueries=0;
        void Observe(AoeQuery query)
        {
            if (query.CandidateType == typeof(AnoMech.Core.SimObjects.SimEnemy)) { npcQueries++; return; }
            events.Add((query.ActionId, Plugin.Game.World.Events.Elapsed - 3f));
        }
        AoeQuery.Evaluated += Observe;
        ScenarioRun run;
        try { run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, pull); }
        finally { AoeQuery.Evaluated -= Observe; }
        Assert.That(run.Passed, run.ToString);
        Assert.That(npcQueries, Is.GreaterThan(0));
        float[] Times(uint action) => events.Where(e => e.Action == action).Select(e => e.Time).Distinct().Order().ToArray();
        var hammer = Times(40218);
        var towers = Times(40213);
        var stack = Times(40217);
        var burst = Times(40219);
        Assert.Multiple(() =>
        {
            Assert.That(events.Count(e => e.Action == 40218), Is.EqualTo(10));
            Assert.That(events.Count(e => e.Action == 40213), Is.EqualTo(7));
            Assert.That(events.Count(e => e.Action == 40217), Is.EqualTo(2));
            Assert.That(events.Count(e => e.Action == 40219), Is.EqualTo(6));
            Assert.That(events.Count(e => e.Action == 40188), Is.EqualTo(8));
        });
        var observed = hammer.Concat(towers).Concat(stack).Concat(burst)
            .Concat(events.Where(e => e.Action is 40222 or 40223).Select(e => e.Time).Distinct())
            .Concat(Times(40188)).ToArray();
        Assert.That(observed.Length, Is.EqualTo(received.Length));
        for (var i = 0; i < received.Length; i++)
        {
            // Cast-backed attacks snapshot before the received effect; this tests the sequence,
            // not equality between server damage snapshots and receive timestamps.
            var tolerance = i is 8 or 9 ? 0.36f : 0.13f;
            Assert.That(observed[i], Is.EqualTo(received[i]).Within(tolerance), $"pull {pull}, event {i}");
        }
    }
}
