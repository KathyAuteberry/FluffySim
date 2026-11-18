using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.TopNative.ProgramLoop;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;

namespace AnoMech.Tests;

public class TopProgramLoopTests
{
    [TestCase(45f)]
    [TestCase(-45f)]
    public void BotsChaseTheTetherOfAPlayerWhoWalksAwayAfterTheBlast(float walkDegrees)
    {
        var numbers = new (PartyRole Role, int Number)[]
        {
            (RegenHealer, 1), (MainTank, 1), (OffTank, 2), (MeleeDpsA, 2),
            (MeleeDpsB, 3), (CasterDps, 3), (PhysRangedDps, 4), (ShieldHealer, 4),
        };
        var blast = TopProgramLoopScenario.TowerTimes[0] + TopProgramLoopScenario.BlasterAfterTower;
        var playerHeld = false;
        var playerKept = true;
        PartyRole? taker = null;
        var run = ScenarioRun.Execute(typeof(TopProgramLoopScenario), 0, Random.Shared.Next(), new()
        {
            PlayerRole = CasterDps,
            StopAt = blast + 4.5f,
            Overrides = o => { foreach (var (role, number) in numbers) ((TopProgramLoopStateOverrides)o).Number.Set(role, number); },
            Takeovers = [new Takeover(blast - 1f, null)],
            Probe = p =>
            {
                var me = p.Member(CasterDps)!;
                if (p.Crossed(blast - 0.5f)) playerHeld = me.HasTetherInSlot0(TetherId.Blaster);
                if (p.Crossed(blast + 0.3f))
                {
                    var bearing = BearingDegrees(me.Position) + walkDegrees;
                    me.MoveTo(Polar(bearing, 12f));
                }
                if (!p.Crossed(blast + 4f)) return;
                playerKept = me.HasTetherInSlot0(TetherId.Blaster);
                foreach (var (role, number) in numbers)
                    if (number == 4 && p.Member(role)!.HasTetherInSlot0(TetherId.Blaster)) taker = role;
            },
        });
        Assert.That(playerHeld, Is.True, run.ToString);
        Assert.That(playerKept, Is.False, run.ToString);
        Assert.That(taker, Is.Not.Null, run.ToString);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void TheChosenNumberIsDealtAndCleared(int mine)
    {
        var scenario = new TopProgramLoopScenario();
        var run = ScenarioRun.Execute(typeof(TopProgramLoopScenario), 0, 7, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = PartyRole.MeleeDpsA,
            Overrides = o => ((TopProgramLoopStateOverrides)o).Number[PartyRole.MeleeDpsA] = mine,
        });
        Assert.That(run.Deaths, Is.Empty, run.ToString);
        Assert.That(scenario.LastState!.Numbers[PartyRole.MeleeDpsA], Is.EqualTo(mine));
    }
}
