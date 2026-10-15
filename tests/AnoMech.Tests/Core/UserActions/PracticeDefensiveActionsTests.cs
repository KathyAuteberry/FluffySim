#pragma warning disable CA1416
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Fru.Darklit;
using AnoMech.Scenarios.Dsr.Sanctity2;
using AnoMech.Windows;

namespace AnoMech.Tests;

public class PracticeDefensiveActionsTests
{
    [TestCase(30u,82)] [TestCase(43u,409)] [TestCase(3638u,810)] [TestCase(16152u,1836)]
    [TestCase(7548u,1209)] [TestCase(7559u,160)]
    public void ExplicitHotbarResolutionAppliesTheCorrectPrivateTimedStatus(uint action, int status)
    {
        var native = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Config), new Configuration());
        using var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        game.RunScenario(new LegacyRunScenarioParams(new FruDarklitScenario(), PartyRole.MainTank, 0, 0, 0));
        native.Frame(.01f,game.Tick);
        Assert.That(game.Player!.HasStatus((ushort)status), Is.False);
        Assert.That(PracticeDefensiveActions.Apply(game.Player,action), Is.True);
        Assert.That(game.Player.HasStatus((ushort)status), Is.True);
        var duration = action is 7548 or 7559 ? 6f : 10f;
        game.Player.Tick(duration - .1f);
        Assert.That(game.Player.HasStatus((ushort)status), Is.True);
        game.Player.Tick(.2f);
        Assert.That(game.Player.HasStatus((ushort)status), Is.False);
        Assert.That(PracticeDefensiveActions.Apply(game.Player,999), Is.False);
    }

    [TestCase(30u,true)] [TestCase(43u,true)] [TestCase(3638u,true)] [TestCase(16152u,true)]
    [TestCase(30u,false)] [TestCase(43u,false)] [TestCase(3638u,false)] [TestCase(16152u,false)]
    public void HotbarStatusProtectsBothTankbusterHitsOnlyWhenTimedCorrectly(uint action,bool timely)
    {
        var scenario = new FruDarklitScenario();
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario),0,0,new()
        {
            PlayerRole = PartyRole.MainTank, ScenarioFactory = () => scenario,
            Overrides = o => ((FruDarklitStateOverrides)o).BaitTank = PartyRole.MainTank,
            Probe = p =>
            {
                if (p.Crossed(29.1f))
                {
                    var player = p.World.Party.Player!;
                    foreach (var status in new ushort[] {82,409,810,1836}) player.RemoveStatus(status);
                    Assert.That(PracticeDefensiveActions.Apply(player,action), Is.True);
                    if (!timely) player.Tick(10.1f);
                }
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.EqualTo(timely), run.ToString);
        if (timely) Assert.That(scenario.ResolvedSomberHits, Is.EqualTo(2));
        else Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId is 40284 or 40285), Is.True, run.ToString);
    }

    [TestCase(19u,30u)] [TestCase(21u,43u)] [TestCase(32u,3638u)] [TestCase(37u,16152u)] [TestCase(24u,0u)]
    public void TankRecastSelectionUsesTheActualJobRatherThanThePracticeSeat(uint job,uint action)
        => Assert.That(PracticeDefensiveActions.TankAction(job), Is.EqualTo(action));

    [Test]
    public void LockedAbilitySubstituteNeedsNoNativeJobOrLevelAndRespectsPauseAndOncePerRun()
    {
        var native = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Config), new Configuration());
        using var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        var scenario = new DsrSanctity2Scenario();
        game.RunScenario(new LegacyRunScenarioParams(scenario,PartyRole.RegenHealer,0,0,0));
        native.Frame(.01f,game.Tick);
        game.Paused = true;
        Assert.That(PracticeAntiKnockbackControls.Press(game.ActiveScenario,true,game.World.PracticeAntiKnockback), Is.False);
        game.Paused = false;
        Assert.That(PracticeAntiKnockbackControls.Press(game.ActiveScenario,true,game.World.PracticeAntiKnockback), Is.True);
        Assert.That(game.World.PracticeAntiKnockback.Active, Is.True);
        Assert.That(PracticeAntiKnockbackControls.Press(game.ActiveScenario,true,game.World.PracticeAntiKnockback), Is.False);
    }
}
