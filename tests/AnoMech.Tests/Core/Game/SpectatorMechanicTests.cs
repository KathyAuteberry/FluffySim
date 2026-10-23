#pragma warning disable CA1416
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P5Wrath;
using AnoMech.Scenarios.Dsr.P5Death;
using AnoMech.Scenarios.Dsr.P4Eyes;
using AnoMech.Scenarios.Fru.Pandora;
using AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained;

namespace AnoMech.Tests;

public class SpectatorMechanicTests
{
    [TestCase(false)] [TestCase(true)]
    public void BotProvokeTransfersAggroAndPlaysItsAction(bool wrath)
    {
        var scenario = wrath ? (AnoMech.Scenarios.IScenario)new DsrP5WrathScenario() : new DsrP5DeathScenario();
        FakeGame? native = null;
        var run = ScenarioRun.Execute(scenario.GetType(), 0, 7391, new()
        {
            ScenarioFactory = () => scenario, Spectator = true, PlayerRole = PartyRole.OffTank,
            ConfigureNative = f => native = f,
        });
        Assert.That(run.Passed, run.ToString);
        var swap = wrath ? ((DsrP5WrathScenario)scenario).TankSwap! : ((DsrP5DeathScenario)scenario).TankSwap!;
        Assert.That(swap.Snapshots.First().Target, Is.EqualTo(PartyRole.MainTank));
        Assert.That(swap.Snapshots.Skip(1).Select(s => s.Target), Has.All.EqualTo(PartyRole.OffTank));
        Assert.That(native!.Timeline.Rows.Any(r => r["kind"] == "ability" && r["id"] == "7533"), Is.True);
    }

    [TestCase(false)] [TestCase(true)]
    public void StrictParadiseUsesRealBotInvulnerabilityAcrossTheWholeSequence(bool continuous)
    {
        var scenario = continuous ? (AnoMech.Scenarios.IScenario)new FruPandoraSequenceScenario() : new FruP5ParadiseRegainedScenario();
        var checks = 0;
        var run = ScenarioRun.Execute(scenario.GetType(), 0, 7391, new()
        {
            ScenarioFactory = () => scenario, Spectator = true, PlayerRole = PartyRole.MainTank,
            Overrides = o =>
            {
                if (o is FruPandoraSequenceOverrides sequence) sequence.RequireInvulnerability = true;
                else ((FruP5ParadiseRegainedStateOverrides)o).RequireInvulnerability = true;
            },
            Probe = p =>
            {
                if (!(continuous ? p.Crossed(51f) || p.Crossed(168.3f) : p.Crossed(14f))) return;
                checks++;
                foreach (var role in new[] { PartyRole.MainTank, PartyRole.OffTank })
                    Assert.That(new ushort[] { 82, 409, 810, 1836 }.Any(p.Member(role)!.HasStatus), Is.True, role.ToString());
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(continuous ? 2 : 1));
    }

    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.OffTank)]
    public void BoxSpendsLbThreeAndAppliesPartyProtection(PartyRole watched)
    {
        var checkedLb = false;
        var run = ScenarioRun.Execute(typeof(FruPandorasBoxScenario), 0, 7391, new()
        {
            Spectator = true, PlayerRole = watched,
            Probe = p =>
            {
                if (!p.Crossed(11)) return;
                checkedLb = true;
                Assert.That(p.World.Party.LimitBreak.FilledBars, Is.Zero);
                Assert.That(p.World.Party.AllMembers().All(FruPandorasBoxScenario.HasTankLimitBreakThree), Is.True);
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedLb, Is.True);
    }
}
