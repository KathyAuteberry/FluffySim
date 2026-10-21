#pragma warning disable CA1416
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Darklit;

namespace AnoMech.Tests;

public class FruDarklitPresentationTests
{
    [Test]
    public void LeavingWithWaterActiveClearsBothStatusesBeforeReentry()
    {
        var scenario = new FruDarklitScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            SimCharacter[] recipients = [];
            var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
            {
                ScenarioFactory = () => scenario, StopAt = 10,
                Probe = p =>
                {
                    if (p.Crossed(9f))
                        recipients = p.World.Party.ActiveMembers().Where(m => m.HasStatus(2458)).ToArray();
                },
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(recipients, Has.Length.EqualTo(2));
            Assert.That(recipients.All(m => !m.HasStatus(2458) && !m.HasStatus(2461)), Is.True);
        }
    }

    [TestCase(PartyRole.MeleeDpsA)] [TestCase(PartyRole.RegenHealer)]
    public void WaterCompanionStatusFollowsRecipientsAndExpiresBeforeStack(PartyRole human)
    {
        var scenario = new FruDarklitScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame native = null!;
            SimCharacter[] recipients = [];
            var observed = false;
            var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, repeat, new()
            {
                PlayerRole = human, ScenarioFactory = () => scenario,
                ConfigureNative = f => { native = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
                Overrides = o => ((FruDarklitStateOverrides)o).PracticeAssignment = DarklitPracticeAssignment.TetheredWater,
                Probe = p =>
                {
                    if (p.Crossed(7.2f))
                    {
                        recipients = p.World.Party.ActiveMembers().Where(m => m.HasStatus(2461)).ToArray();
                        Assert.That(recipients, Has.Length.EqualTo(2));
                        Assert.That(recipients, Does.Contain(p.World.Party.Player));
                        Assert.That(p.World.Party.ActiveMembers().Where(m => m.HasStatus(2458)), Is.EquivalentTo(recipients));
                        foreach (var member in recipients)
                        {
                            Assert.That(member.FindStatus(2458)!.Snapshot.NativePresentation, Is.True);
                            Assert.That(member.LastLockonVfxId, Is.Null);
                        }
                        var remaining = recipients.Select(m => m.FindStatus(2458)!.RemainingTime).ToArray();
                        p.Game.Paused = true;
                        for (var frame = 0; frame < 120; frame++) native.Frame(1f / 60, p.Game.Tick);
                        Assert.That(recipients.Select(m => m.FindStatus(2458)!.RemainingTime), Is.EqualTo(remaining));
                        p.Game.Paused = false;
                        observed = true;
                    }
                    if (p.Crossed(21.2f))
                        foreach (var member in recipients)
                        {
                            Assert.That(member.HasStatus(2458), Is.False);
                            Assert.That(member.HasStatus(2461), Is.True);
                            Assert.That(member.LastLockonVfxId, Is.EqualTo(184));
                        }
                    if (p.Crossed(26.2f))
                        Assert.That(recipients.All(m => !m.HasStatus(2458) && !m.HasStatus(2461)), Is.True);
                },
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(observed, Is.True);
            Assert.That(recipients.All(m => !m.HasStatus(2458) && !m.HasStatus(2461)), Is.True);
            Assert.That(native.BattleCharas.OccupiedSlots, Is.Zero);
        }
    }
}
