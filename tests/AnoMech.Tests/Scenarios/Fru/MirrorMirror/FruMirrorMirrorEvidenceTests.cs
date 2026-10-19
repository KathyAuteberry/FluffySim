using System.Text.Json;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.MirrorMirror;

namespace AnoMech.Tests;

public class FruMirrorMirrorEvidenceTests
{
    public sealed record MapEvent(float Time, uint Flags, byte Index);
    public sealed record Recording(string Source, int Ordinal, int Blue, int Red, bool Pairs,
        Dictionary<string, float[]> Effects, MapEvent[] Maps);
    private static IEnumerable<TestCaseData> Recordings()
        => JsonSerializer.Deserialize<Recording[]>(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Scenarios/Fru/MirrorMirror/RecordedTimelines.json")))!.Select(r => new TestCaseData(r).SetName($"MirrorTimeline{r.Source}_{r.Ordinal}"));
    [TestCaseSource(nameof(Recordings))]
    public void NativeEffectsMatchRecordedSequences(Recording recording)
    {
        FakeGame native = null!;
        var observed = new Dictionary<uint, List<float>>();
        var maps = new List<MapEvent>();
        var mapIndex = 0;
        var seen = new HashSet<(SimEnemy, int)>();
        var run = ScenarioRun.Execute(typeof(FruMirrorMirrorScenario), 0, recording.Ordinal, new()
        {
            ConfigureNative = f => native = f,
            Overrides = o => { var s = (FruMirrorMirrorStateOverrides)o; s.Blue = recording.Blue; s.Red = recording.Red; s.Pairs = recording.Pairs; },
            Probe = p =>
            {
                foreach (var effect in native.MapEffects.Applied.Skip(mapIndex)) maps.Add(new(p.Time - 1f, effect.Flags, effect.Index));
                mapIndex = native.MapEffects.Applied.Count;
                foreach (var actor in p.World.Children.OfType<SimEnemy>())
                {
                    if (actor.Casting.EffectSeq == 0 || !seen.Add((actor, actor.Casting.EffectSeq))) continue;
                    var id = actor.Casting.EffectActionId;
                    if (!observed.TryGetValue(id, out var times)) observed[id] = times = [];
                    if (times.Count == 0 || p.Time - 1f - times[^1] > .2f) times.Add(p.Time - 1f);
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        foreach (var pair in recording.Effects)
        {
            var id = uint.Parse(pair.Key);
            Assert.That(observed.ContainsKey(id), Is.True, pair.Key);
            for (var i = 0; i < pair.Value.Length; i++)
                Assert.That(observed[id][i], Is.EqualTo(pair.Value[i]).Within(.2f), pair.Key);
        }
        foreach (var expected in recording.Maps)
            Assert.That(maps.Any(m => m.Index == expected.Index && m.Flags == expected.Flags && MathF.Abs(m.Time - expected.Time) < .2f), Is.True, expected.ToString());
    }
}
