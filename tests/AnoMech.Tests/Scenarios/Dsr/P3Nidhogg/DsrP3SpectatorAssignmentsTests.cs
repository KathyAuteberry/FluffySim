#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P3Nidhogg;

namespace AnoMech.Tests;

public class DsrP3SpectatorAssignmentsTests
{
    [Test]
    public void D1SpectatorGetsEveryUnarrowedSideAndClearsAllMechanics()
    {
        var sides = new HashSet<int>();
        var seeds = new Rng(7391);
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var seed = seeds.Next(int.MaxValue);
            var scenario = new DsrP3NidhoggScenario();
            var observed = false;
            var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, seed, new()
            {
                Spectator = true, PlayerRole = PartyRole.MeleeDpsA, ScenarioFactory = () => scenario,
                Overrides = o =>
                {
                    var settings = (DsrP3NidhoggStateOverrides)o;
                    settings.PlayerLine = 1;
                    settings.PlayerArrow = DiveArrow.Circle;
                },
                Probe = p =>
                {
                    if (!p.Crossed(36.9f)) return;
                    observed = true;
                    var state = scenario.LastState!;
                    Assert.That(state.Lines[PartyRole.MeleeDpsA], Is.EqualTo(1));
                    Assert.That(state.Arrows[PartyRole.MeleeDpsA], Is.EqualTo(DiveArrow.Circle));
                    foreach (var role in state.Line(1).Where(r => state.Arrows[r] != DiveArrow.Circle))
                        Assert.That(p.Member(role)!.Rotation, Is.EqualTo(MathF.PI / 2).Within(.01));
                    if (state.LineHasArrows(1)) return;
                    var position = p.Member(PartyRole.MeleeDpsA)!.Position;
                    var side = position.X < -1 ? -1 : position.X > 1 ? 1 : 0;
                    if (side == 0) Assert.That(position.Z, Is.GreaterThan(6));
                    sides.Add(side);
                },
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(observed, Is.True);
            Assert.That(scenario.Completed, Is.True);
        }
        Assert.That(sides, Is.EquivalentTo(new[] { -1, 0, 1 }));
    }
}
