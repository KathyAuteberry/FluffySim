using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Tea.P1LivingLiquid;

namespace AnoMech.Tests;

public class HealerSupportActionsTests
{
    [TestCase(186u, 297)]
    [TestCase(24292u, 2609)]
    public void PartyBarrierUsesTheActionRangeAndRefreshesOneTimedStatus(uint action, int status)
    {
        var checkedRecipients = false;
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 19, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.ShieldHealer,
            StopAt = 2f,
            Probe = p =>
            {
                if (!p.Crossed(0.6f)) return;
                var healer = p.Member(PartyRole.ShieldHealer)!;
                var near = p.Member(PartyRole.MeleeDpsA)!;
                var far = p.Member(PartyRole.MeleeDpsB)!;
                healer.SetPosition(new Vector3(-10, 0, 0));
                near.SetPosition(new Vector3(0, 0, 0));
                far.SetPosition(new Vector3(15, 0, 0));
                JobActions.ApplyEffects(healer, action, healer.GameObjectId.ObjectId, new Random(1));
                JobActions.ApplyEffects(healer, action, healer.GameObjectId.ObjectId, new Random(1));
                Assert.Multiple(() =>
                {
                    Assert.That(healer.HasStatus((ushort)status), Is.True);
                    Assert.That(near.HasStatus((ushort)status), Is.True);
                    Assert.That(far.HasStatus((ushort)status), Is.False);
                    Assert.That(near.ActiveStatuses.Count(s => s.StatusId == status), Is.EqualTo(1));
                    Assert.That(near.FindStatus((ushort)status)!.RemainingTime, Is.EqualTo(30f).Within(0.02f));
                    Assert.That(Mitigation.Effective([(ushort)status], DamageKind.Magic), Is.GreaterThan(0f));
                });
                checkedRecipients = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedRecipients, Is.True);
    }

    [TestCase(185u, 297)]
    [TestCase(24291u, 2607)]
    public void SingleTargetBarrierProtectsTheSelectedPartyMember(uint action, int status)
    {
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 19, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.ShieldHealer,
            StopAt = 2f,
            Probe = p =>
            {
                if (!p.Crossed(0.6f)) return;
                var target = p.Member(PartyRole.MainTank)!;
                JobActions.ApplyEffects(p.Member(PartyRole.ShieldHealer)!, action, target.GameObjectId.ObjectId, new Random(1));
                Assert.That(target.HasStatus((ushort)status), Is.True);
                Assert.That(p.Member(PartyRole.ShieldHealer)!.HasStatus((ushort)status), Is.False);
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(7405u, 1934)]
    [TestCase(16889u, 1951)]
    [TestCase(16012u, 1826)]
    public void RangedPartyMitigationReachesOtherSeats(uint action, int status)
    {
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 19, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.PhysRangedDps,
            StopAt = 2f,
            Probe = p =>
            {
                if (!p.Crossed(0.6f)) return;
                JobActions.ApplyEffects(p.Member(PartyRole.PhysRangedDps)!, action, p.Member(PartyRole.PhysRangedDps)!.GameObjectId.ObjectId, new Random(1));
                foreach (var member in p.World.Party.ActiveMembers())
                    Assert.That(member.HasStatus((ushort)status), Is.True);
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void TemperanceGrantsItsPartyMitigationAlthoughTheButtonTargetsSelf()
    {
        var run = ScenarioRun.Execute(typeof(TeaP1LivingLiquidScenario), 0, 19, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.RegenHealer,
            StopAt = 2f,
            Probe = p =>
            {
                if (!p.Crossed(0.6f)) return;
                JobActions.ApplyEffects(p.Member(PartyRole.RegenHealer)!, 16536, p.Member(PartyRole.RegenHealer)!.GameObjectId.ObjectId, new Random(1));
                foreach (var member in p.World.Party.ActiveMembers())
                    Assert.That(member.HasStatus(3985), Is.True);
                Assert.That(p.Member(PartyRole.RegenHealer)!.HasStatus(3881), Is.True);
            },
        });
        Assert.That(run.Passed, run.ToString);
    }
}
