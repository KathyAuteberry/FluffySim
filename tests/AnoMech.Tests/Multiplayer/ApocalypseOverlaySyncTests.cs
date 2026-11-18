using System.Globalization;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.Apocalypse;

namespace AnoMech.Tests;

// A peer rolls its own seed and runs no scenario, so its overlay can only come from the host's rolls.
public class ApocalypseOverlaySyncTests
{
    private sealed class Seen
    {
        public FruApocalypseLights? Lights;
        public bool LightsMoved;
        public readonly List<string> Pulses = [];
        public readonly List<string> Explosions = [];
    }

    [Test]
    public void PeersDrawTheHostsLightsPulsesAndExplosions()
    {
        var seen = new Dictionary<string, Seen>();
        var run = MultiplayerRun.Play(typeof(FruApocalypseScenario), 0, 1, new PlayerSeat(PartyRole.MeleeDpsA, BotControlled: false),
            [new PlayerSeat(PartyRole.MainTank, BotControlled: false), new PlayerSeat(PartyRole.CasterDps, BotControlled: false)],
            immortal: true, configure: client =>
            {
                var views = seen[client.Name] = new Seen();
                client.FrameObserved = observed =>
                {
                    if (observed.Game.ActiveScenario is not FruApocalypseScenario scenario) return;
                    views.Lights = scenario.Lights ?? views.Lights;
                    views.LightsMoved |= scenario.LightPositions.Count > 0;
                    Note(views.Pulses, scenario.CurrentPulse);
                    Note(views.Explosions, scenario.CurrentExplosion);
                };
            });

        Assert.That(run.Failure, Is.Null);
        var host = seen[run.Host.Name];
        Assert.That(host.Lights, Is.Not.Null);
        Assert.That(host.Pulses, Is.Not.Empty);
        Assert.That(host.Explosions, Is.Not.Empty);
        foreach (var peer in run.Peers)
        {
            var shown = seen[peer.Name];
            Assert.That(shown.Lights, Is.EqualTo(host.Lights), peer.Name);
            Assert.That(shown.LightsMoved, Is.True, peer.Name);
            Assert.That(shown.Pulses, Is.EqualTo(host.Pulses), peer.Name);
            Assert.That(shown.Explosions, Is.EqualTo(host.Explosions), peer.Name);
        }
    }

    private static void Note(List<string> views, IReadOnlyList<Vector3> shown)
    {
        if (shown.Count == 0) return;
        var view = string.Join(";", shown.Select(p => string.Create(CultureInfo.InvariantCulture, $"{p.X:F2},{p.Z:F2}")));
        if (views.Count == 0 || views[^1] != view) views.Add(view);
    }
}
