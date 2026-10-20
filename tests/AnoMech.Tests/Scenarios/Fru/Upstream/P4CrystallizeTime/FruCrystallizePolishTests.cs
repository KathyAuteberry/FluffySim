using System.Numerics;
using AnoMech.Scenarios;
using AnoMech.Windows;
using AnoMech.Core.Game;
using AnoMech.Core.UserActions;
using AnoMech.Core.SimObjects;
using FruIds = AnoMech.Scenarios.Fru.Upstream.FruConstants;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

namespace AnoMech.Tests;

public class FruCrystallizePolishTests
{
    [Test]
    public void BlueNumbersAreASeededPermutationAcrossAllHumanSeats()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        for (var seed = 0; seed < 4; seed++)
        {
            var a = new FruP4CrystallizeTimeState(new Rng(seed), role, new());
            var b = new FruP4CrystallizeTimeState(new Rng(seed), role, new());
            Assert.That(a.CleanseNumbers.Values.Order(), Is.EqualTo(new[] {1, 2, 3, 4}));
            Assert.That(a.CleanseNumbers.Keys.All(r => !FruP4CrystallizeTimeState.IsRed(a.Debuff(r))), Is.True);
            Assert.That(a.CleanseNumbers, Is.EqualTo(b.CleanseNumbers));
            Assert.That(a.RequireTankMitigation, Is.False);
        }
    }

    [TestCase(0, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(3, true)]
    [TestCase(4, true)] [TestCase(5, false)] [TestCase(6, true)] [TestCase(7, false)]
    public void NumberedPlayersCleanseTheirActualEastToWestPuddles(int seat, bool north)
    {
        var role = (PartyRole)seat;
        var settings = new FruP4CrystallizeTimeStateOverrides {PlayerDebuffs = [CrystallizeDebuff.BlueIce]};
        var number = seat % 4 + 1;
        var seed = Enumerable.Range(0, 128).First(s => new FruP4CrystallizeTimeState(new Rng(s), role, settings).CleanseNumbers[role] == number);
        var scenario = new FruP4CrystallizeTimeScenario();
        var markersChecked = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, seed, new()
        {
            PlayerRole = role, ScenarioFactory = () => scenario,
            Overrides = o =>
            {
                var x = (FruP4CrystallizeTimeStateOverrides)o;
                x.PlayerDebuffs = settings.PlayerDebuffs;
                x.Corner = north ? CrystallizeCorner.NE : CrystallizeCorner.SE;
            },
            Probe = p =>
            {
                if (!p.Crossed(12f)) return;
                Assert.That(p.World.SoftMarkers.Count, Is.EqualTo(4));
                Assert.That(p.World.SoftMarkers.Snapshot(Vector3.Zero).Select(m => m.Sign).Order(), Is.EqualTo(new[] {Sign.Attack1, Sign.Attack2, Sign.Attack3, Sign.Attack4}));
                Assert.That(p.World.SoftMarkers.Snapshot(Vector3.Zero, showBots: false).Single().Sign, Is.EqualTo((Sign)(number - 1)));
                markersChecked = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(markersChecked, Is.True);
        var state = scenario.LastState;
        var puddles = state.AllDragonPuddles.Values.OrderByDescending(p => p.X).ToArray();
        Assert.That(puddles, Has.Length.EqualTo(4));
        foreach (var (blue, assigned) in state.CleanseNumbers)
            Assert.That(state.CleansedAt[blue], Is.EqualTo(puddles[assigned - 1]), $"{blue} number {assigned}");
    }
    [TestCase(1, false)] [TestCase(2, false)] [TestCase(3, false)] [TestCase(4, false)]
    [TestCase(1, true)] [TestCase(2, true)] [TestCase(3, true)] [TestCase(4, true)]
    public void BotsReserveTheHumanPuddleUntilTheHumanCollectsIt(int number, bool collect)
    {
        var role = (PartyRole)(number - 1);
        var settings = new FruP4CrystallizeTimeStateOverrides {PlayerDebuffs = [CrystallizeDebuff.BlueIce]};
        var seed = Enumerable.Range(0, 128).First(s => new FruP4CrystallizeTimeState(new Rng(s), role, settings).CleanseNumbers[role] == number);
        var poses = new Dictionary<int, Vector3>();
        void Configure(object o)
        {
            var x = (FruP4CrystallizeTimeStateOverrides)o;
            x.PlayerDebuffs = settings.PlayerDebuffs;
            x.Corner = number % 2 == 0 ? CrystallizeCorner.NE : CrystallizeCorner.SE;
        }
        var reference = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, seed, new()
        {
            PlayerRole = role, StopAt = collect ? 41.05f : 52f, Overrides = Configure,
            Probe = p => poses[(int)MathF.Round(p.World.Events.Elapsed * 60)] = p.Member(role)!.Position,
        });
        Assert.That(reference.Passed, reference.ToString);
        var scenario = new FruP4CrystallizeTimeScenario();
        SimWorld? world = null;
        var checkedPuddle = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, seed, new()
        {
            PlayerRole = role, ScenarioFactory = () => scenario, StopAt = collect ? 41.05f : 52f,
            Takeovers = [new Takeover(29.65f, null)],
            Overrides = Configure,
            Probe = p =>
            {
                world = p.World;
                if (p.World.Events.Elapsed >= 29.65f && (p.World.Events.Elapsed < 40.6f || !collect))
                {
                    var position = poses[(int)MathF.Round(p.World.Events.Elapsed * 60)];
                    if (scenario.LastState.AllDragonPuddles.Count == 4)
                    {
                        var ownPosition = scenario.LastState.AllDragonPuddles.Values.OrderByDescending(v => v.X).ElementAt(number - 1);
                        var delta = position - ownPosition;
                        if (delta.Length() < 2.2f)
                            position = ownPosition + (delta.Length() > .01f ? Vector3.Normalize(delta) : -Vector3.UnitZ) * 2.2f;
                    }
                    p.Member(role)!.SetPosition(position);
                }
                if (p.Crossed(40.6f))
                {
                    var state = scenario.LastState;
                    var own = state.AllDragonPuddles.Values.OrderByDescending(v => v.X).ElementAt(number - 1);
                    Assert.That(state.CleansedAt.Keys, Is.EquivalentTo(state.CleanseNumbers.Keys.Where(r => r != role)));
                    Assert.That(state.DragonPuddles.Values, Does.Contain(own));
                    Assert.That(p.Member(role)!.HasStatus(FruIds.StatusId.Wyrmfang), Is.True);
                    if (collect) p.Member(role)!.SetPosition(own);
                    checkedPuddle = true;
                }
                if (p.Crossed(41f))
                    Assert.That(p.Member(role)!.HasStatus(FruIds.StatusId.Wyrmfang), Is.EqualTo(!collect));
                if (p.Crossed(FruP4CrystallizeTimeState.ReturnSavedAt))
                    foreach (var tank in new[] {PartyRole.MainTank, PartyRole.OffTank})
                        Assert.That(Vector3.Distance(p.Member(tank)!.Position,
                            FruP4CrystallizeTimeAi.ManaRewindSpot(tank, scenario.LastState.East, scenario.LastState.North)), Is.LessThan(.1f));
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.EqualTo(collect), run.ToString);
        if (!collect) Assert.That(run.Deaths.Any(d => d.Role == role && d.Cause.Contains("Wyrmfang not cleansed")), Is.True, run.ToString);
        Assert.That(checkedPuddle, Is.True);
        Assert.That(world!.SoftMarkers.Count, Is.Zero);
    }

    [TestCase(19)] [TestCase(21)] [TestCase(32)] [TestCase(37)]
    public void StrictStartRestoresOnlyTheCurrentJobsTankDefensives(int job)
    {
        FakeGame? native = null;
        var scenario = new FruP4CrystallizeTimeScenario();
        var restarted = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 0, new()
        {
            StopAt = 1f, ScenarioFactory = () => scenario,
            Probe = p =>
            {
                if (restarted || !p.Crossed(.5f)) return;
                restarted = true;
                p.Game.Reset();
                native!.Frame(0f, p.Game.Tick);
                p.Game.EnterAndStart(new RunScenarioParams(scenario,
                    new LocalPartyProvider {RoleOverride = PartyRole.MainTank}, 1, 0, 0), SessionMode.Local);
                native.Frame(ScenarioRun.FrameSeconds, p.Game.Tick);
            },
            ConfigureNative = f => native = f,
            BeforeStart = (_, f) => f.BattleCharas.Player.ClassJob = (byte)job,
            Overrides = o => ((FruP4CrystallizeTimeStateOverrides)o).RequireTankMitigation = true,
        });
        Assert.That(run.Passed, run.ToString);
        foreach (var action in TankMitigationPractice.ActionsForJob((byte)job))
            Assert.That(native!.Zone.ResetRecasts.Count(a => a == action), Is.EqualTo(2));
        var otherJobs = new byte[] {19, 21, 32, 37}.Where(j => j != job)
            .SelectMany(TankMitigationPractice.ActionsForJob)
            .Except(TankMitigationPractice.ActionsForJob((byte)job));
        Assert.That(native!.Zone.ResetRecasts.Intersect(otherJobs), Is.Empty);
    }

    [Test]
    public void WrongNumberedPuddleDetourStillFailsTheActiveHourglass()
    {
        var role = PartyRole.MainTank;
        var settings = new FruP4CrystallizeTimeStateOverrides {PlayerDebuffs = [CrystallizeDebuff.BlueIce]};
        var seed = Enumerable.Range(0, 128).First(s => new FruP4CrystallizeTimeState(new Rng(s), role, settings).CleanseNumbers[role] == 1);
        var scenario = new FruP4CrystallizeTimeScenario();
        var stole = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, seed, new()
        {
            PlayerRole = role, ScenarioFactory = () => scenario, StopAt = 52f,
            Overrides = o => ((FruP4CrystallizeTimeStateOverrides)o).PlayerDebuffs = settings.PlayerDebuffs,
            Probe = p =>
            {
                if (p.Crossed(33f))
                {
                    var wrong = scenario.LastState.AllDragonPuddles.Values.OrderByDescending(v => v.X).Last();
                    p.Member(role)!.SetPosition(wrong);
                    stole = true;
                }

            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(stole, Is.True);
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Deaths.Any(d => d.Role == role && d.Cause == "Maelstrom"), Is.True, run.ToString);
        var expected = scenario.LastState.AllDragonPuddles.Values.OrderByDescending(v => v.X).First();
        Assert.That(scenario.LastState.CleansedAt[role], Is.Not.EqualTo(expected));
    }

    [TestCase(false)] [TestCase(true)]
    public void OptionalMitigationChangesActualWingSurvivalWithoutChangingKnockbackPractice(bool strict)
    {
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.MainTank, UserActionsEnabled = true, StopAt = 61f,
            Configure = c => c.EnableTankMitigation = true,
            Takeovers = [new Takeover(50f, null)],
            Overrides = o =>
            {
                var x = (FruP4CrystallizeTimeStateOverrides)o;
                x.RequireTankMitigation = strict;
                x.Corner = CrystallizeCorner.NW;
            },
            Probe = p =>
            {
                if (p.Crossed(50.6f))
                    Assert.That(PracticeAntiKnockbackControls.Press(p.Game.ActiveScenario, true, p.World.PracticeAntiKnockback), Is.True);
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.EqualTo(!strict), run.ToString);
        if (strict) Assert.That(run.Deaths.Any(d => d.Cause.Contains("not enough mitigation")), Is.True, run.ToString);
    }

}
