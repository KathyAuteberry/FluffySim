using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.Sanctity2;
using AnoMech.Scenarios.Fru.UltimateRelativity;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

namespace AnoMech.Tests;

public class ScenarioHumanSeatTests
{
    private static PlayerSeat[] HumanPeers => Enum.GetValues<PartyRole>()
        .Where(r => r != PartyRole.MainTank).Reverse().Select(r => new PlayerSeat(r, false)).ToArray();

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(7)]
    public void MeteorsDoesNotGiveJoiningHumansAutomaticKnockbackImmunity(int peerCount)
    {
        var peers = HumanPeers.Take(peerCount).ToArray();
        var observed = false;
        var granted = new List<PartyRole>();
        var run = MultiplayerRun.Play(typeof(DsrSanctity2Scenario), 0, 1,
            new PlayerSeat(PartyRole.MainTank, false), peers, immortal: true,
            configure: c => c.FrameObserved = client =>
            {
                if (!client.IsHost || observed || client.Game.ActiveScenario == null || client.Game.Events.Elapsed < 21.55f) return;
                observed = true;
                foreach (var role in peers.Select(p => p.Role).Append(PartyRole.MainTank))
                    if (client.Game.World.Party.Get(role) is { } member && (member.HasStatus(160) || member.HasStatus(1209))) granted.Add(role);
            });
        Assert.That(run.Failure, Is.Null);
        Assert.That(observed, Is.True);
        Assert.That(granted, Is.Empty, "human seats must press their own anti-knockback");
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(7)]
    public void RelativityDoesNotTurnJoiningHumansForTheGaze(int peerCount)
    {
        var peers = HumanPeers.Take(peerCount).ToArray();
        var observed = false;
        var turned = new List<PartyRole>();
        var run = MultiplayerRun.Play(typeof(FruUltimateRelativityScenario), 0, 1,
            new PlayerSeat(PartyRole.MainTank, false), peers, immortal: true,
            configure: c => c.FrameObserved = client =>
            {
                if (!client.IsHost || observed || client.Game.ActiveScenario == null || client.Game.Events.Elapsed < 53.001f) return;
                observed = true;
                foreach (var role in peers.Select(p => p.Role))
                    if (client.Game.World.Party.Get(role) is { } member && MathF.Abs(member.Rotation) > .01f) turned.Add(role);
            });
        Assert.That(run.Failure, Is.Null);
        Assert.That(observed, Is.True);
        Assert.That(turned, Is.Empty, "the host must use each human's reported facing");
    }

    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(2, true)]
    [TestCase(3, true)]
    public void CrystallizeBotsGiveJoiningHumansSpaceForSpiritTaker(int seed, bool hard)
    {
        Vector3? occupiedSpot = null;
        var measured = false;
        var conflicts = new List<PartyRole>();
        var run = MultiplayerRun.Play(typeof(FruP4CrystallizeTimeScenario), 0, seed,
            new PlayerSeat(PartyRole.MainTank, false), [new PlayerSeat(PartyRole.CasterDps, false)], immortal: true,
            configure: c =>
            {
                c.Config.BotDifficulty = hard ? AnoMech.Core.Game.Ai.BotDifficulty.Hard : AnoMech.Core.Game.Ai.BotDifficulty.Easy;
                c.FrameObserved = client =>
                {
                    if (client.Game.ActiveScenario == null) return;
                    var world = client.Game.World;
                    var t = client.Game.Events.Elapsed;
                    if (!client.IsHost)
                    {
                        if (occupiedSpot is { } spot && t < 48.5f)
                            client.Fake.BattleCharas.Player.Teleport(world.Coordinates.ToGlobal(spot), 0);
                        return;
                    }
                    if (occupiedSpot == null && t >= 46.5f)
                        occupiedSpot = world.Party.AllMembers().OfType<SimPartyNpc>().Select(b => b.MoveDestination).FirstOrDefault(p => p != null);
                    if (occupiedSpot == null || measured || t < 48.1f) return;
                    measured = true;
                    var human = world.Party.Get(PartyRole.CasterDps)!;
                    foreach (var bot in world.Party.AllMembers().OfType<SimPartyNpc>())
                        if (Vector3.Distance(bot.MoveDestination ?? bot.Position, human.Position) < 5.9f) conflicts.Add(bot.Role);
                };
            });
        Assert.That(run.Failure, Is.Null);
        Assert.That(occupiedSpot, Is.Not.Null);
        Assert.That(measured, Is.True);
        Assert.That(conflicts, Is.Empty, "bots must reserve spread clearance around every human seat");
    }
}
