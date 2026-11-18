using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Dmu.Forsaken;
using AnoMech.Scenarios.Dsr.P6DoubleDragons;
using AnoMech.Scenarios.Fru.UltimateRelativity;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

namespace AnoMech.Tests;

// Peers run no scenario, so every marker they show is the host's.
public class SoftMarkerSyncTests
{
    private static readonly PartyRole[] PeerRoles = [PartyRole.MainTank, PartyRole.CasterDps];

    [TestCase(typeof(DmuForsakenScenario))]
    [TestCase(typeof(FruUltimateRelativityScenario))]
    [TestCase(typeof(DsrP6DoubleDragonsScenario))]
    [TestCase(typeof(FruP4CrystallizeTimeScenario))]
    public void PeersShowEveryMarkerTheHostShows(Type scenarioType)
    {
        var seen = new Dictionary<string, List<string>>();
        var run = Play(scenarioType, seen, runs: 1);

        Assert.That(seen[run.Host.Name].Where(view => view.Length > 0), Is.Not.Empty, "the host showed no marker");
        foreach (var peer in run.Peers)
        {
            Assert.That(seen[peer.Name], Is.EqualTo(seen[run.Host.Name]), peer.Name);
            Assert.That(MultiplayerRun.Taken(peer, run.Host).OfType<SoftMarkersMessage>(), Is.Not.Empty, $"{peer.Name} took no marker from the host");
        }
    }

    [Test]
    public void AMarkerOnAnEnemyAndOneHeldAcrossARestartReachPeers()
    {
        var seen = new Dictionary<string, List<string>>();
        var run = Play(typeof(DmuForsakenScenario), seen, runs: 2, host =>
        {
            var world = host.Game.World;
            if (host.Game.ActiveScenario == null || world.Events.Elapsed is not (>= 5f and < 6f or >= 100f and < 101f)) return;
            if (world.Children.OfType<SimEnemy>().FirstOrDefault(e => e.IsActive) is { } enemy) world.SoftMarkers.Set(Sign.Attack1, enemy);
        });

        Assert.That(seen[run.Host.Name], Has.Some.Contains("Attack1=actor"));
        foreach (var peer in run.Peers) Assert.That(seen[peer.Name], Is.EqualTo(seen[run.Host.Name]), peer.Name);
    }

    private static MultiplayerRun Play(Type scenarioType, Dictionary<string, List<string>> seen, int runs, Action<TestClient>? onHostFrame = null)
    {
        var bots = typeof(IMultiplayerReplayable).IsAssignableFrom(scenarioType);
        var peers = PeerRoles.Select(role => new PlayerSeat(role, bots)).ToArray();
        var run = MultiplayerRun.Play(scenarioType, 0, 1, new PlayerSeat(PartyRole.MeleeDpsA, bots), peers, immortal: true, runs: runs, configure: client =>
        {
            var views = seen[client.Name] = [];
            client.FrameObserved = observed =>
            {
                if (observed.IsHost) onHostFrame?.Invoke(observed);
                var view = View(observed);
                if (views.Count == 0 ? view.Length > 0 : views[^1] != view) views.Add(view);
            };
        });
        Assert.That(run.Failure, Is.Null);
        return run;
    }

    private static string View(TestClient client) => string.Join(",", client.Game.World.SoftMarkers.Assigned
        .OrderBy(marker => marker.Key)
        .Select(marker => $"{marker.Key}={client.NameAt(marker.Value.EntityId, double.MaxValue)}"));
}
