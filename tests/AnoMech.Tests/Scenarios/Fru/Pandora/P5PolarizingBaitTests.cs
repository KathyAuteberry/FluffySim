using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Pandora;

namespace AnoMech.Tests;

public class P5PolarizingBaitTests
{
    [Test]
    public void TwoNearestPlayersOnOneSideDoNotStealTheOtherSidesBait()
    {
        var placed = false;
        var observed = false;
        ScenarioRun.Execute(typeof(FruPolarizingStrikesScenario), 0, 81, new()
        {
            StopAt = 8.2f,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (!placed && p.Crossed(7.2f))
                {
                    placed = true;
                    p.World.BotMovement.Clear();
                    foreach (var member in p.World.Party.AllMembers())
                    {
                        member.StopMoving();
                        member.SetPosition(new Vector3(10f, 0f, 12f));
                    }
                    p.World.Party.Get(PartyRole.MainTank)!.SetPosition(new Vector3(-1f, 0f, 2f));
                    p.World.Party.Get(PartyRole.OffTank)!.SetPosition(new Vector3(-2f, 0f, 2f));
                    p.World.Party.Get(PartyRole.MeleeDpsA)!.SetPosition(new Vector3(5f, 0f, 1f));
                }
                if (!p.Crossed(8.1f)) return;
                var effects = p.World.Children.OfType<SimEnemy>()
                    .Where(e => e.Proxy is FakeBattleChara)
                    .SelectMany(e => ((FakeBattleChara)e.Proxy!).EffectSourceSnapshots)
                    .Where(e => e.Effect.ActionId is 40317 or 40318).ToArray();
                Assert.That(effects, Has.Length.EqualTo(2));
                Assert.That(effects.Single(e => e.Effect.ActionId == 40317).Effect.Rotation,
                    Is.EqualTo(MathF.Atan2(-1f, 2f)).Within(.02f));
                Assert.That(effects.Single(e => e.Effect.ActionId == 40318).Effect.Rotation,
                    Is.EqualTo(MathF.Atan2(5f, 1f)).Within(.02f));
                observed = true;
            },
        });
        Assert.That(placed && observed, Is.True);
    }
}
