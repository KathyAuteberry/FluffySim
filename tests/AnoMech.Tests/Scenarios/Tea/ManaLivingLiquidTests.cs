using AnoMech.Core.UserActions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Tea;
using AnoMech.Scenarios.Tea.P1LivingLiquid;
using System.Numerics;

namespace AnoMech.Tests;

public class ManaLivingLiquidTests
{
    [TestCase(0)]
    [TestCase(45)]
    [TestCase(90)]
    [TestCase(135)]
    [TestCase(180)]
    [TestCase(225)]
    [TestCase(270)]
    [TestCase(315)]
    public void DollSkipCompletesWithoutFeedingDolls(int openSide)
    {
        var fed = 0;
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 1043 + openSide, new ScenarioRunOptions
        {
            Spectator = true,
            Overrides = o => ((TeaP1LivingLiquidStateOverrides)o).FirstTornadoesOpenSide = openSide,
            Probe = p =>
            {
                if (p.Crossed(63f))
                    fed = p.World.Children.OfType<SimEnemy>().Count(e => e.IsActive && e.BNpcBaseId == TeaConstants.BNpcBaseId.JagdDoll && e.HpFraction == 1f);
            },
        });
        Assert.Multiple(() =>
        {
            Assert.That(run.Passed, run.ToString);
            Assert.That(fed, Is.EqualTo(4), "Dolls remain unhit and unfed");
            Assert.That(run.Elapsed, Is.LessThan(70f));
        });
    }

    [Test]
    public void AllRolesCompleteOrdinaryAndSpectatorPractice([Values] PartyRole role, [Values(false, true)] bool spectator)
    {
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 913, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP1LivingLiquidScenario(),
            PlayerRole = role,
            Spectator = spectator,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void RemovingTheBarrierKillsABotAtUnequalHandOfPain()
    {
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 1043, new ScenarioRunOptions
        {
            Spectator = true,
            StopAt = 43f,
            WriteArtifactsOnFailure = false,
            Overrides = o => ((TeaP1LivingLiquidStateOverrides)o).FirstTornadoesOpenSide = 0,
            Probe = p =>
            {
                if (p.Crossed(39.5f)) p.Member(PartyRole.CasterDps)!.RemoveStatus(297);
            },
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.CasterDps && d.ActionId == TeaConstants.ActionId.HandOfPain), Is.True, run.ToString);
    }

    [Test]
    public void ASecondRunWithTheSameSeedReproducesTheFirst()
    {
        var scenario = new TeaP1LivingLiquidScenario();
        var results = new List<string>();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 901, new ScenarioRunOptions
            {
                ScenarioFactory = () => scenario,
                Spectator = true,
            });
            Assert.That(run.Passed, run.ToString);
            results.Add($"{scenario.LastState!.FirstTornadoes}/{scenario.LastState.FirstCalmTornado}/{scenario.LastState.FirstHandOrbFromEarlier}");
        }
        Assert.That(results[1], Is.EqualTo(results[0]));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void HumanShieldHealerMustResolveTheBarrierAfterProtean(bool castsBarrier)
    {
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 1043, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.ShieldHealer,
            StopAt = 43f,
            UserActionsEnabled = true,
            WriteArtifactsOnFailure = false,
            Overrides = o => ((TeaP1LivingLiquidStateOverrides)o).FirstTornadoesOpenSide = 0,
            Probe = p =>
            {
                if (p.Crossed(38.8f)) DebugBotControl.Enabled = false;
                if (castsBarrier && p.Crossed(39.5f))
                    JobActions.ApplyEffects(p.Member(PartyRole.ShieldHealer)!, 186, p.Member(PartyRole.ShieldHealer)!.GameObjectId.ObjectId, new Random(1));
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Any(d => d.ActionId == TeaConstants.ActionId.HandOfPain), Is.EqualTo(!castsBarrier), run.ToString);
    }

    [Test]
    public void AManualInvulnerabilityIsRequiredForTheSecondWaterBusterEvenOnABot()
    {
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 1043, new ScenarioRunOptions
        {
            Spectator = true,
            StopAt = 38f,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (!p.Crossed(36.5f)) return;
                foreach (var status in new ushort[] { 82, 409, 810, 1836 }) p.Member(PartyRole.MainTank)!.RemoveStatus(status);
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == TeaConstants.ActionId.FluidStrike), Is.True, run.ToString);
    }

    [Test]
    public void ManaNorthIsTheLoneSpoutOppositeTheOpenSide()
    {
        var tornadoes = new TornadoT(0);
        Assert.That(Vector2.Distance(TeaP1LivingLiquidAi.ManaPosition(tornadoes, new Vector2(0, -15)), tornadoes.Tornado(TornadoT.South)), Is.LessThan(0.001f));
    }
}
