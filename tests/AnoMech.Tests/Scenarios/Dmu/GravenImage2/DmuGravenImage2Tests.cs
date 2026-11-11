using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dmu.GravenImage2;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Tests;

public class DmuGravenImage2Tests
{
    // Cast at 19.17, first hit 24.25, second 27.38; the ST holds Kefka going in.
    // The human's Provoke takes the real input route (LocalProvokeHandler); the last case the bot one.
    [TestCase(20.00f, MarkedTankbusterSwap.Grade.OnTime, true, false)]
    [TestCase(24.10f, MarkedTankbusterSwap.Grade.OnTime, true, false)]
    [TestCase(24.45f, MarkedTankbusterSwap.Grade.Late, true, false)]
    [TestCase(15.00f, MarkedTankbusterSwap.Grade.Early, false, false)]
    [TestCase(null, MarkedTankbusterSwap.Grade.Missing, false, false)]
    [TestCase(21.00f, MarkedTankbusterSwap.Grade.OnTime, true, true)]
    public void MainTankProvokeIsGradedAgainstTheCastBar(float? provokeAt, MarkedTankbusterSwap.Grade grade, bool passes, bool botRoute)
    {
        var scenario = new DmuGravenImage2Scenario();
        var run = ScenarioRun.Execute(typeof(DmuGravenImage2Scenario), 0, 1, new()
        {
            PlayerRole = PartyRole.MainTank, ScenarioFactory = () => scenario, StopAt = 28.0f,
            Takeovers = [new(19.0f, new Vector2(-3f, 2.5f))],
            Probe = p =>
            {
                if (provokeAt is not { } at || !p.Crossed(at) || scenario.LastState?.Objects.Kefka is not { } kefka) return;
                if (botRoute) JobActions.ApplyEffects(p.Member(PartyRole.MainTank)!, Enmity.ProvokeActionId, (ulong)kefka.GameObjectId, Random.Shared);
                else new LocalProvokeHandler().OnAction(ActionType.Action, Enmity.ProvokeActionId, kefka.GameObjectId.ObjectId);
            },
            WriteArtifactsOnFailure = false,
        });
        var outcome = scenario.Swap!.Outcomes.Single();
        Assert.That(outcome.Grade, Is.EqualTo(grade), run.ToString);
        Assert.That(run.Passed, Is.EqualTo(passes), run.ToString);
    }

    private static IEnumerable<TestCaseData> ForcedVariants()
    {
        for (var bits = 0; bits < 32; bits++)
            yield return new TestCaseData(bits).SetName($"DmuGravenImage2 variant {bits:B5}");
    }

    [TestCaseSource(nameof(ForcedVariants))]
    public void EveryForcedVariantClearsWithTheYanStrategy(int bits)
    {
        var run = ScenarioRun.Execute(typeof(DmuGravenImage2Scenario), 0, 2000 + bits, new()
        {
            Overrides = o =>
            {
                var overrides = (DmuGravenImage2StateOverrides)o;
                overrides.IceLie = (bits & 1) != 0;
                overrides.DpsSpreadFirst = (bits & 2) != 0;
                overrides.DpsSpreadSecond = (bits & 4) != 0;
                overrides.FirstCleaveEast = (bits & 8) != 0;
                overrides.SecondCleaveEast = (bits & 16) != 0;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void EachSetDropsFourPuddlesWithTheirOwnSpawnArgument()
    {
        uint[]? args = null;
        var run = ScenarioRun.Execute(typeof(DmuGravenImage2Scenario), 0, 11, new()
        {
            StopAt = 40f,
            Probe = p =>
            {
                if (p.Crossed(39.5f))
                    args = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEventObject>()
                        .Where(o => o.EObjRowId == AnoMech.Scenarios.Dmu.DmuConstants.EObjId.GravitasPuddle)
                        .Select(o => o.SpawnConfig.Arg2).ToArray();
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(args, Is.EquivalentTo(Enumerable.Range(0, 8).Select(i => 0x500003u + ((uint)i << 16))));
    }

    [Test]
    public void AVitrophyreNextToAPuddleWipesTheParty()
    {
        var run = ScenarioRun.Execute(typeof(DmuGravenImage2Scenario), 0, 7, new()
        {
            PlayerRole = PartyRole.CasterDps, StopAt = 20f,
            Overrides = o => ((DmuGravenImage2StateOverrides)o).DpsSpreadFirst = true,
            Takeovers = [new(17.5f, new Vector2(6f, 12f))],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("reached a Gravitas puddle")), run.ToString);
    }

    [Test]
    public void StandingInTheHalfRoomCleaveKills()
    {
        var run = ScenarioRun.Execute(typeof(DmuGravenImage2Scenario), 0, 8, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, StopAt = 29f,
            Overrides = o => ((DmuGravenImage2StateOverrides)o).FirstCleaveEast = true,
            Takeovers = [new(27.5f, new Vector2(8f, 2f))],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.PhysRangedDps && d.Cause.Contains("east half-room cleave")), run.ToString);
    }

    [Test]
    public void AnUnsoakedPuddleWipesTheParty()
    {
        var run = ScenarioRun.Execute(typeof(DmuGravenImage2Scenario), 0, 9, new()
        {
            StopAt = 49f,
            Takeovers = [new(41f, new Vector2(-14f, 0f), Bot: PartyRole.MainTank)],
            Probe = p =>
            {
                if (!p.Crossed(47.0f)) return;
                foreach (var role in PerRole.All.Where(r => !r.IsDps()))
                    p.Member(role)?.SetPosition(new Vector3(-15f, 0f, 0f));
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("unsoaked")), run.ToString);
    }
}
