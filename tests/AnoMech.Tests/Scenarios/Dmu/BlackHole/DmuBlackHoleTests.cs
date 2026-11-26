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
}
