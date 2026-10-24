using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Dsr.Sanctity2;

namespace AnoMech.Tests;

public class StaticVfxReplayTests
{
    private const string TowerPath = "vfx/omen/eff/d1025_sp012_bunsan01s.avfx";

    [TestCase(false)]
    [TestCase(true)]
    public void MeteorTowerGraphicsReachThePeerAndAreRemovedAcrossRuns(bool reset)
    {
        var run = MultiplayerRun.Play(typeof(DsrSanctity2Scenario), 0, 37,
            new PlayerSeat(PartyRole.MeleeDpsA, false), [new PlayerSeat(PartyRole.CasterDps, false)],
            peerLink: BadConnections.BadWifiPeer, immortal: true, runs: reset ? 2 : 1);
        Assert.That(run.Failure, Is.Null);
        var host = run.Host.Fake.Vfx.StaticSpawns;
        var peer = run.Peers[0].Fake.Vfx.StaticSpawns;
        Assert.That(host, Has.Count.EqualTo(reset ? 32 : 16));
        Assert.That(peer.Select(s => (s.Path, s.Placement, s.Scale)),
            Is.EqualTo(host.Select(s => (s.Path, s.Placement, s.Scale))));
        Assert.That(peer.All(s => s.Handle.RemoveCalls == 1), Is.True);
    }

    [Test]
    public void RemovingAndClearingOmensReleasesEachHandleOnce()
    {
        var fake = FakeGame.Install();
        using var world = new SimWorld(new EventScheduler());
        var natives = PeerNatives.Install();
        try
        {
            var replay = new PacketReplay(natives, world, PartyRole.CasterDps);
            replay.Apply(Tower(1));
            replay.Apply(new RemoveStaticVfxPacket(1));
            replay.Apply(new RemoveStaticVfxPacket(1));
            replay.Apply(Tower(2));
            replay.Clear();
            replay.Clear();
            Assert.That(fake.Vfx.StaticSpawns, Has.Count.EqualTo(2));
            Assert.That(fake.Vfx.StaticSpawns.All(s => s.Handle.RemoveCalls == 1), Is.True);
        }
        finally { natives.Uninstall(); }
    }

    [Test]
    public void UnknownAssetsAndInvalidTransformsNeverReachTheNativeLoader()
    {
        var fake = FakeGame.Install();
        using var world = new SimWorld(new EventScheduler());
        var natives = PeerNatives.Install();
        try
        {
            var replay = new PacketReplay(natives, world, PartyRole.CasterDps);
            replay.Apply(Tower(1) with { Path = "vfx/omen/eff/not-a-scenario-asset.avfx" });
            replay.Apply(Tower(2) with { Path = "../../anything.avfx" });
            replay.Apply(Tower(3) with { X = float.NaN });
            replay.Apply(Tower(4) with { ScaleX = float.PositiveInfinity });
            replay.Apply(Tower(5) with { ScaleY = -1 });
            replay.Apply(Tower(0));
            Assert.That(fake.Vfx.StaticSpawns, Is.Empty);
        }
        finally { natives.Uninstall(); }
    }

    private static SpawnStaticVfxPacket Tower(uint id) => new(id, TowerPath, 100, 0, 100, 0, 3, 6, 3);
}
