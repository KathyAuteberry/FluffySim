#pragma warning disable CA1416
using System.Text.Json;
using AnoMech.Core.Native.Policies;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.P7DragonKing;

namespace AnoMech.Tests;

public class DsrP7EnvironmentTests
{
    private static Dictionary<ushort, IReadOnlyList<(uint Id, bool Active, bool ColliderLoaded, bool ColliderActive)>> Read(bool dragonKing)
    {
        using var stream = typeof(DsrP7EnvironmentTests).Assembly.GetManifestResourceStream("AnoMech.Tests.Core.Native.Fixtures.DsrP7Layers.json")!;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.EnumerateObject().ToDictionary(p => ushort.Parse(p.Name), p =>
        {
            var active = p.Value.GetProperty(dragonKing ? "p7Active" : "p2Active").GetBoolean();
            var collision = p.Value.GetProperty("collision").GetBoolean();
            return (IReadOnlyList<(uint, bool, bool, bool)>)[(p.Value.GetProperty("id").GetUInt32(), active, collision, collision && active)];
        });
    }

    [TestCase(false)] [TestCase(true)]
    public void DragonKingUsesItsOwnEnvironmentInInnAndOffline(bool offline)
    {
        Assert.That(DsrZone.P7.Weather, Is.EqualTo(47));
        Assert.That(DsrZone.P7.Weather, Is.Not.EqualTo(DsrZone.P2.Weather));
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, 47, offline), Is.True);
        Assert.That(DsrEnvironmentState.NeedsFinalization(1238, 47, offline), Is.False);
    }

    [Test]
    public void P2FloorAndCollisionCannotCertifyTheDragonKingArena()
    {
        var p2 = Read(false);
        var p7 = Read(true);
        Assert.That(DsrEnvironmentState.MatchesDragonKing(k => p2[k]), Is.False);
        Assert.That(DsrEnvironmentState.MatchesDragonKing(k => p7[k]), Is.True);
        Assert.That(DsrEnvironmentState.MatchesDragonKing(_ => []), Is.False);
        p7[61049] = p2[61049];
        Assert.That(DsrEnvironmentState.MatchesDragonKing(k => p7[k]), Is.False);
        p7 = Read(true);
        p7[60878] = p2[60878];
        Assert.That(DsrEnvironmentState.MatchesDragonKing(k => p7[k]), Is.False);
    }

    [TestCase((ushort)65362)] [TestCase((ushort)5210)]
    [TestCase((ushort)5212)] [TestCase((ushort)5644)]
    public void CollisionMustBeLoadedAndFollowTheSelectedP7Geometry(ushort layer)
    {
        var state = Read(true);
        var collider = state[layer].Single();
        state[layer] = [(collider.Id, collider.Active, false, collider.ColliderActive)];
        Assert.That(DsrEnvironmentState.MatchesDragonKing(k => state[k]), Is.False);
        state[layer] = [(collider.Id, collider.Active, true, !collider.ColliderActive)];
        Assert.That(DsrEnvironmentState.MatchesDragonKing(k => state[k]), Is.False);
    }

    [TestCase(false)] [TestCase(true)]
    public void ColdEntryWaitsForP7GeometryAndDoesNotRepeatedlyForceIt(bool offline)
    {
        var transition = new OfflineWeatherTransition();
        var state = Read(false);
        var writes = new List<byte>();
        byte environment = 45;
        var updates = 0;
        void Tick(nint generation) => transition.Tick(generation,
            () => environment == 47 && DsrEnvironmentState.MatchesDragonKing(k => state[k]) ? environment : (byte)0,
            writes.Add, () => { updates++; environment = writes[^1]; });
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, 47, offline), Is.True);
        transition.Request(47);
        for (var frame = 0; frame < 120; frame++) Tick(0);
        Assert.That(updates, Is.Zero);
        Tick(1);
        Assert.That(environment, Is.EqualTo(47));
        Assert.That(transition.Complete, Is.False);
        state = Read(true);
        Tick(1);
        Assert.That(transition.Complete, Is.True);
        for (var frame = 0; frame < 1000; frame++) Tick(1);
        Assert.That(updates, Is.EqualTo(1));
        transition.Request(47);
        Tick(1);
        Assert.That(updates, Is.EqualTo(2));
        Tick(0);
        Tick(2);
        Assert.That(updates, Is.EqualTo(3));
        transition.Clear();
        Tick(3);
        Assert.That(updates, Is.EqualTo(3));
        Assert.That(writes, Is.EqualTo(new byte[] { 47, 47, 47 }));
    }

    [Test]
    public void ARealP7RunNeverRequestsP2Weather()
    {
        FakeGame? native = null;
        var run = ScenarioRun.Execute(typeof(DsrP7DragonKingScenario), 0, 0, new()
        {
            ConfigureNative = f => native = f, StopAt = 13,
        });
        Assert.That(run.Passed, run.ToString);
        var weather = native!.Timeline.Rows.Where(r => r["kind"] == "weather").Select(r => r["id"]).ToArray();
        Assert.That(weather, Is.Not.Empty);
        Assert.That(weather, Is.All.EqualTo("47"));
    }
}
