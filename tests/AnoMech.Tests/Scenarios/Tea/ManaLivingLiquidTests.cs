using AnoMech.Core.UserActions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Tea;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Tea.P1LivingLiquid;
using System.Numerics;

namespace AnoMech.Tests;

public class ManaLivingLiquidTests
{
    [TestCase(0)] [TestCase(45)] [TestCase(90)] [TestCase(135)]
    [TestCase(180)] [TestCase(225)] [TestCase(270)] [TestCase(315)]
    public void PullFacesNorthAndSeparateBustersKeepThePartyBehindBothModels(int openSide)
    {
        var openingChecked = false;
        var handChecked = false;
        var scenario = new TeaP1LivingLiquidScenario();
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 1043 + openSide, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            Spectator = true,
            Overrides = o => ((TeaP1LivingLiquidStateOverrides)o).FirstTornadoesOpenSide = openSide,
            Probe = p =>
            {
                if (p.Crossed(10.2f))
                {
                    var boss = scenario.LastState!.Objects.LivingLiquid!;
                    var tank = p.Member(PartyRole.MainTank)!;
                    Assert.That(new Vector2(boss.Position.X, boss.Position.Z).Length(), Is.LessThan(3f));
                    Assert.That(tank.Position.Z, Is.LessThan(boss.Position.Z));
                    var native = ((FakeBattleChara)boss.Proxy!).EffectSourceSnapshots.Last(s => s.Effect.ActionId == TeaConstants.ActionId.FluidSwing);
                    var forward = new Vector3(MathF.Sin(native.Rotation), 0, MathF.Cos(native.Rotation));
                    Assert.That(Vector3.Dot(forward, Vector3.Normalize(tank.Position - boss.Position)), Is.GreaterThan(.95f));
                    foreach (var member in p.World.Party.AllMembers().Where(m => m != tank))
                        Assert.That(Vector3.Dot(forward, member.Position - boss.Position), Is.LessThan(0f));
                    openingChecked = true;
                }
                if (p.Crossed(55.95f))
                {
                    var hand = scenario.LastState!.Objects.LiquidHand!;
                    var tank = p.Member(PartyRole.OffTank)!;
                    var native = ((FakeBattleChara)hand.Proxy!).EffectSourceSnapshots.Last(s => s.Effect.ActionId == TeaConstants.ActionId.FluidStrike);
                    var forward = new Vector3(MathF.Sin(native.Rotation), 0, MathF.Cos(native.Rotation));
                    Assert.That(Vector3.Dot(forward, Vector3.Normalize(tank.Position - hand.Position)), Is.GreaterThan(.95f));
                    foreach (var role in PerRole.All.Where(r => r is not (PartyRole.MainTank or PartyRole.OffTank)))
                        Assert.That(Vector3.Dot(forward, p.Member(role)!.Position - hand.Position), Is.LessThan(0f));
                    var damaged = native.Effect.Targets!.Where(t => t.Effects!.Any(e => e.Type == 3)).Select(t => t.Id);
                    Assert.That(damaged, Is.EqualTo(new[] { tank.GameObjectId }));
                    handChecked = true;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(openingChecked && handChecked, Is.True);
    }

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
