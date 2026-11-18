using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.TopNative.OversampledWaveCannon;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

public class TopOversampledWaveCannonTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void EveryMonitorHitsTwoPlayersAndEveryPlayerIsHitOnce(bool omegaCleavesEast)
    {
        var stacks = new Dictionary<PartyRole, int>();
        var run = ScenarioRun.Execute(typeof(TopOversampledWaveCannonScenario), 0, Random.Shared.Next(), new()
        {
            Overrides = o => ((TopOversampledWaveCannonStateOverrides)o).OmegaCleavesEast = omegaCleavesEast,
            Probe = p =>
            {
                if (!p.Crossed(TopOversampledWaveCannonScenario.WaveCannonCast + TopOversampledWaveCannonScenario.FiresAfterCast + 0.8f)) return;
                foreach (var role in PerRole.All) stacks[role] = p.Member(role)!.FindStatus(StatusId.TwiceComeRuin)?.Stacks ?? 0;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(stacks.Values, Is.All.EqualTo(1), string.Join(", ", stacks.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    [Test]
    public void StandingOnTheMiddlePlayerDoublesTheirHit()
        => Negative<TopOversampledWaveCannonScenario>(RegenHealer)
            .Overrides<TopOversampledWaveCannonStateOverrides>(o => { o.Monitor.Mine = false; o.OmegaCleavesEast = false; })
            .TeleportAt(TopOversampledWaveCannonScenario.WaveCannonCast + 6f, to: new Vector2(-0.5f, 0f))
            .ShouldKillSomeone(ActionId.OversampledWaveCannonAoe);
}
