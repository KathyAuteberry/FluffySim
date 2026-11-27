using static AnoMech.Tests.NegativeRun;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dmu.BlackHole;
using AnoMech.Scenarios.Umad;
using AnoMech.Scenarios.Umad.P3BlackHole;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Tests;

public class DmuBlackHoleTests
{
    private const float AfterSelfmarks = 10f;

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EachLinePressesItsOwnSelfmarks(int seed)
    {
        var markers = new Dictionary<PartyRole, Sign>();
        var lines = new Dictionary<PartyRole, ushort>();
        var run = ScenarioRun.Execute(typeof(DmuBlackHoleScenario), 0, seed, new()
        {
            Probe = p =>
            {
                if (!p.Crossed(AfterSelfmarks)) return;
                foreach (var (sign, who) in p.World.SoftMarkers.Assigned)
                    if (who is ISimPartyMember member) markers[member.Role] = sign;
                foreach (var role in PerRole.All)
                    lines[role] = new[] { UmadConstants.StatusId.FirstInLine, UmadConstants.StatusId.SecondInLine, UmadConstants.StatusId.ThirdInLine }
                        .First(s => p.Member(role)!.HasStatus(s));
            },
            WriteArtifactsOnFailure = false,
        });

        Assert.That(run.Passed, run.ToString);
        Assert.That(markers, Has.Count.EqualTo(8));
        foreach (var (role, sign) in markers)
            Assert.That(lines[role], Is.EqualTo(sign switch
            {
                Sign.Attack1 or Sign.Attack2 or Sign.Attack3 => UmadConstants.StatusId.FirstInLine,
                Sign.Bind1 or Sign.Bind2 or Sign.Bind3 => UmadConstants.StatusId.SecondInLine,
                _ => UmadConstants.StatusId.ThirdInLine,
            }), $"{role} has {sign}");
    }

    [Test]
    public void IssisIsTheDefaultThunderPlan()
    {
        var overrides = new DmuBlackHoleStateOverrides();
        Assert.That(overrides.ThunderSet1, Is.EqualTo(ThunderIIIAssignment.OtInvulnsBoth));
        Assert.That(overrides.ThunderSet2, Is.EqualTo(ThunderIIIAssignment.ShareMtFirst));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void TheMainTankCanHoldChaos(int seed)
    {
        var run = ScenarioRun.Execute(typeof(DmuBlackHoleScenario), 0, seed, new()
        {
            Overrides = o => ((DmuBlackHoleStateOverrides)o).MainTankOnChaos = true,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void ChaosTankWithoutAnInvulnDiesToTheFirstThunder()
        => Negative<DmuBlackHoleScenario>(OffTank)
            .WithMitigationChecks()
            .FreezeAt(42.4f)
            .ShouldKill(UmadConstants.ActionId.ThunderIII_Resolve, OffTank);

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void AttackOnePullsTheFirstTetherCounterClockwise(int seed)
    {
        PartyRole? attackOne = null;
        float? turn = null;
        var run = ScenarioRun.Execute(typeof(DmuBlackHoleScenario), 0, seed, new()
        {
            StopAt = 32f,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (!p.Crossed(31.5f)) return;
                attackOne = (p.World.SoftMarkers.Assigned.GetValueOrDefault(Sign.Attack1) as ISimPartyMember)?.Role;
                if (p.World.Children.OfType<SimTether>().FirstOrDefault(t => t.IsActive && t.TetherId == UmadConstants.TetherId.GrabbyTether)
                    is not { A: { } hole, B: ISimPartyMember holder } || holder.Role != attackOne) return;
                var member = p.Member(holder.Role)!;
                var delta = System.MathF.Atan2(member.Position.X, -member.Position.Z) - System.MathF.Atan2(hole.Position.X, -hole.Position.Z);
                turn = System.MathF.IEEERemainder(delta, System.MathF.Tau) * 180f / System.MathF.PI;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(turn, Is.Not.Null.And.InRange(-90f, -30f), $"Attack 1 ({attackOne}) should hold the first tether counter-clockwise of its hole");
    }

    [TestCase(1)]
    [TestCase(2)]
    public void StompAMoleStartsInTheMiddle(int seed)
    {
        var distances = new List<float>();
        var run = ScenarioRun.Execute(typeof(DmuBlackHoleScenario), 0, seed, new()
        {
            StopAt = 147f,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (!p.Crossed(146.6f)) return;
                distances.AddRange(PerRole.All.Select(r => new System.Numerics.Vector2(p.Member(r)!.Position.X, p.Member(r)!.Position.Z).Length()));
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(distances, Has.Count.EqualTo(8).And.All.LessThan(2.5f));
    }
}
