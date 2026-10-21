using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

namespace AnoMech.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416")]
public class FruCrystallizeCommunityTests
{
    private static IEnumerable<TestCaseData> NonTankWings() =>
        from role in Enum.GetValues<PartyRole>().Where(r => !r.IsTank())
        from corner in Enum.GetValues<CrystallizeCorner>()
        from strict in new[] { false, true }
        select new TestCaseData(role, corner, strict);

    [TestCaseSource(nameof(NonTankWings))]
    public void HumanNonTankNeedsAntiKnockbackButNoPersonalReduction(PartyRole role, CrystallizeCorner corner, bool strict)
    {
        System.Numerics.Vector3 recorded = default;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = role, UserActionsEnabled = true, StopAt = 61,
            Configure = c => c.EnableTankMitigation = true,
            Takeovers = [new(50, null)],
            Overrides = o =>
            {
                var x = (FruP4CrystallizeTimeStateOverrides)o;
                x.Corner = corner;
                x.RequireTankMitigation = strict;
            },
            Probe = p =>
            {
                if (p.Crossed(44.8f)) recorded = p.Member(role)!.Position;
                if (p.Crossed(50.6f))
                    p.World.Party.Player!.AddStatus(role is PartyRole.CasterDps or PartyRole.RegenHealer or PartyRole.ShieldHealer ? (ushort)160 : (ushort)1209, 6);
                if (p.Crossed(53.6f) || p.Crossed(54f) || p.Crossed(58.2f) || p.Crossed(58.5f) || p.Crossed(60.5f))
                    TestContext.WriteLine($"t={p.Time:F2} " + string.Join("; ", p.World.Party.ActiveMembers().Select(m =>
                        $"{((ISimPartyMember)m).Role}@{m.Position} statuses=[{string.Join(",", m.ActiveStatusSnapshot.Select(s => s.StatusId))}]")));
                if (p.Crossed(54.3f) || p.Crossed(58.9f))
                {
                    var effect = p.World.Children.OfType<SimEnemy>()
                        .SelectMany(e => ((FakeBattleChara)e.Proxy!).ReceivedEffects)
                        .Last(e => e.ActionId == 40332);
                    var hits = effect.Targets!.Where(t => t.Effects!.Any(e => e.Type == 3)).ToArray();
                    Assert.That(hits, Has.Length.EqualTo(8));
                    Assert.That(hits.Count(t => t.Effects!.Any(e => e.Type == 0x0E)), Is.EqualTo(4));
                    TestContext.WriteLine("Wing effect recipients in delivery order: " + string.Join("; ", hits.Select(t =>
                    {
                        var member = p.World.Party.ActiveMembers().Single(m => m.GameObjectId == t.Id);
                        return $"{((ISimPartyMember)member).Role}: " + string.Join(",", t.Effects!.Select(e => $"type={e.Type:X2}/value={e.Value}"));
                    })));
                }
                if (p.Crossed(54f))
                    Assert.That(System.Numerics.Vector3.Distance(p.Member(role)!.Position,
                        recorded), Is.LessThan(.1f));
            },
            WriteArtifactsOnFailure = false,
        });
        TestContext.WriteLine(run.ToString());
        Assert.That(run.Passed, run.ToString);
    }
    [TestCase(false, false)] [TestCase(false, true)]
    [TestCase(true, false)] [TestCase(true, true)]
    public void WrongFrontOrderOrRepeatedVulnerableHitRemainsLethal(bool strict, bool vulnerable)
    {
        var checkedFailure = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.MeleeDpsA, UserActionsEnabled = true, StopAt = 1,
            Takeovers = [new(0, null)],
            Probe = p =>
            {
                if (!p.Crossed(.5f)) return;
                var player = p.World.Party.Player!;
                var tank = p.Member(PartyRole.MainTank)!;
                if (vulnerable) player.AddStatus(2941, 6);
                var action = FruP4CrystallizeTimeActions.Wings(strict);
                var context = new AnoMech.Core.EnemyActions.EnemyActionContext(action, tank, player,
                    tank.Placement(), p.World.Party, 0);
                action.Effects[0].Apply(context, vulnerable ? [tank, player] : [player, tank]);
                Assert.That(context.IsKilled(player), Is.True);
                checkedFailure = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedFailure, Is.True);
    }

    [TestCase(PartyRole.MeleeDpsA, false)] [TestCase(PartyRole.MeleeDpsB, true)]
    [TestCase(PartyRole.PhysRangedDps, false)] [TestCase(PartyRole.CasterDps, true)]
    [TestCase(PartyRole.RegenHealer, false)] [TestCase(PartyRole.ShieldHealer, true)]
    public void MissingOrExpiredAntiKnockbackStillFails(PartyRole role, bool expired)
    {
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = role, UserActionsEnabled = true, StopAt = 61,
            Takeovers = [new(50, null)],
            Overrides = o => ((FruP4CrystallizeTimeStateOverrides)o).Corner = CrystallizeCorner.NE,
            Probe = p =>
            {
                if (expired && p.Crossed(46f)) p.Member(role)!.AddStatus(1209, 6);
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == role && d.Cause.Contains("arena")), Is.True, run.ToString);
    }
}
