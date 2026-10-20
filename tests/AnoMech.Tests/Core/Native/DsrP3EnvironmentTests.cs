#pragma warning disable CA1416
using AnoMech.Core.Native.Policies;
using System.Text.Json;

namespace AnoMech.Tests;

public class DsrP3EnvironmentTests
{
    private static Dictionary<ushort, IReadOnlyList<(uint Id, bool Active, bool ColliderLoaded, bool ColliderActive)>> Read(bool nidhogg)
    {
        using var stream = typeof(DsrP3EnvironmentTests).Assembly.GetManifestResourceStream("AnoMech.Tests.Core.Native.Fixtures.DsrP3Layers.json")!;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.EnumerateObject().ToDictionary(p => ushort.Parse(p.Name), p =>
            (IReadOnlyList<(uint, bool, bool, bool)>)p.Value.GetProperty("objects").EnumerateArray().Select(o =>
            {
                var active = nidhogg ? p.Value.GetProperty("p3Active").GetBoolean() : o[1].GetBoolean();
                var collision = p.Value.GetProperty("collision").GetBoolean();
                return (o[0].GetUInt32(), active, collision, collision && active);
            }).ToArray());
    }

    [TestCase(false)] [TestCase(true)]
    public void DirectP3EntryUsesNativeFinalizationInInnAndOffline(bool offline)
    {
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, 3, offline), Is.True);
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, 45, offline), Is.EqualTo(offline));
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, 2, offline), Is.False);
        Assert.That(DsrEnvironmentState.NeedsFinalization(1238, 3, offline), Is.False);
    }

    [Test]
    public void WeatherThreeAloneCannotCertifyP1OrP2SceneryAsNidhogg()
    {
        var p2 = Read(false);
        var p3 = Read(true);
        Assert.That(DsrEnvironmentState.MatchesNidhogg(key => p2[key]), Is.False);
        Assert.That(DsrEnvironmentState.MatchesNidhogg(key => p3[key]), Is.True);
        Assert.That(DsrEnvironmentState.MatchesNidhogg(_ => []), Is.False);
        var stage = p3[62172].ToArray();
        stage[0].Active = true;
        p3[62172] = stage;
        Assert.That(DsrEnvironmentState.MatchesNidhogg(key => p3[key]), Is.False);
    }

    [TestCase((ushort)65362)] [TestCase((ushort)5210)] [TestCase((ushort)5212)] [TestCase((ushort)5644)]
    public void CollisionMustBeLoadedAndFollowTheNativeSelectedGeometry(ushort layer)
    {
        var state = Read(true);
        var collider = state[layer].Single();
        state[layer] = [(collider.Id, collider.Active, false, collider.ColliderActive)];
        Assert.That(DsrEnvironmentState.MatchesNidhogg(key => state[key]), Is.False);
        state[layer] = [(collider.Id, collider.Active, true, !collider.ColliderActive)];
        Assert.That(DsrEnvironmentState.MatchesNidhogg(key => state[key]), Is.False);
    }

    [TestCase(false)] [TestCase(true)]
    public void ColdLayoutRestartAndLeaveDoNotRepeatOrLeakTransitions(bool offline)
    {
        var transition = new OfflineWeatherTransition();
        var state = Read(false);
        byte environment = 2;
        var writes = new List<byte>();
        var updates = 0;
        void Tick(nint generation) => transition.Tick(generation,
            () => environment == 3 && DsrEnvironmentState.MatchesNidhogg(key => state[key]) ? environment : (byte)0,
            writes.Add, () => { updates++; environment = writes[^1]; });
        Assert.That(DsrEnvironmentState.NeedsFinalization(968, 3, offline), Is.True);
        transition.Request(3);
        for (var frame = 0; frame < 120; frame++) Tick(0);
        Assert.That(updates, Is.Zero);
        Tick(1);
        Assert.That(transition.Complete, Is.False);
        state = Read(true);
        Tick(1);
        Assert.That(transition.Complete, Is.True);
        for (var frame = 0; frame < 1000; frame++) Tick(1);
        Assert.That(updates, Is.EqualTo(1));
        transition.Request(3);
        Tick(1);
        Assert.That(updates, Is.EqualTo(2));
        Tick(0);
        Tick(2);
        Assert.That(updates, Is.EqualTo(3));
        transition.Clear();
        Tick(3);
        Assert.That(updates, Is.EqualTo(3));
        Assert.That(writes, Is.EqualTo(new byte[] { 3, 3, 3 }));
    }
    [TestCase(false)] [TestCase(true)]
    public void ChangingPhaseCancelsTheOldP3RequestBeforeAnotherLayoutArrives(bool offline)
    {
        var transition = new OfflineWeatherTransition();
        var writes = new List<byte>();
        void Select(byte weather)
        {
            transition.Clear();
            if (DsrEnvironmentState.NeedsFinalization(968, weather, offline)) transition.Request(weather);
        }
        void Tick() => transition.Tick(1, () => 0, writes.Add, () => { });
        Select(3);
        Select(45);
        Tick();
        Assert.That(writes, Is.EqualTo(offline ? new byte[] { 45 } : Array.Empty<byte>()));
        Select(3);
        Tick();
        Assert.That(writes.Last(), Is.EqualTo(3));
        Select(2);
        Tick();
        Assert.That(writes.Last(), Is.EqualTo(3));
    }
}
