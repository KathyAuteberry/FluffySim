#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P6DoubleDragons;
using AnoMech.Scenarios;

namespace AnoMech.Tests;

public class DsrWrothTests
{
    [Test]
    public void EveryDebuffConfigurationHasEightUniqueLocalSymbolsAndTwoMixedPairs()
    {
        var roles = Enum.GetValues<PartyRole>();
        var count = 0;
        for (var mask = 0; mask < 256; mask++)
        {
            var spreads = roles.Where(r => (mask & (1 << (int)r)) != 0).ToArray();
            if (spreads.Length != 4) continue;
            var rest = roles.Except(spreads).ToArray();
            for (var a = 0; a < 4; a++)
            for (var b = a + 1; b < 4; b++)
            {
                PartyRole[] stacks = [rest[a], rest[b]];
                var markers = new DsrWrothMarkers(spreads, stacks).Assignments;
                Assert.That(markers.Count, Is.EqualTo(8));
                Assert.That(markers.Values.Distinct().Count(), Is.EqualTo(8));
                Assert.That(markers, Is.EquivalentTo(new DsrWrothMarkers(spreads.Reverse().ToArray(), stacks.Reverse().ToArray()).Assignments));
                Assert.That(spreads.Select(r => markers[r]), Is.EquivalentTo(new[] { Sign.Attack1, Sign.Attack2, Sign.Attack3, Sign.Attack4 }));
                foreach (var pair in new[] { new[] { Sign.Bind1, Sign.Bind2 }, new[] { Sign.Ignore1, Sign.Ignore2 } })
                {
                    var members = markers.Where(m => pair.Contains(m.Value)).Select(m => m.Key).ToArray();
                    Assert.That(members.Count(stacks.Contains), Is.EqualTo(1));
                    Assert.That(members.Count(r => !spreads.Contains(r) && !stacks.Contains(r)), Is.EqualTo(1));
                }
                Assert.That(markers[spreads.OrderBy(r => (int)r).First()], Is.EqualTo(Sign.Attack4));
                count++;
            }
        }
        Assert.That(count, Is.EqualTo(420));
    }

    [Test]
    public void RandomDebuffsRemainIndependentOfLocalMarkerAllocation()
    {
        var seen = Enum.GetValues<PartyRole>().ToDictionary(r => r, _ => new HashSet<int>());
        for (var seed = 0; seed < 64; seed++)
        {
            var state = new DsrP6DoubleDragonsState(new Rng(seed));
            var spreads = state.Spreads.ToArray();
            var stacks = state.Stacks.ToArray();
            _ = new DsrWrothMarkers(state.Spreads, state.Stacks);
            Assert.That(state.Spreads, Is.EqualTo(spreads));
            Assert.That(state.Stacks, Is.EqualTo(stacks));
            foreach (var role in seen.Keys) seen[role].Add(spreads.Contains(role) ? 0 : stacks.Contains(role) ? 1 : 2);
        }
        foreach (var assignments in seen.Values) Assert.That(assignments, Is.EquivalentTo(new[] { 0, 1, 2 }));
    }

    private static IEnumerable<TestCaseData> Patterns() =>
        from lane in new[] { -1, 0, 1 }
        from north in new[] { -1, 1 }
        from eyeSide in new[] { -1, 1 }
        from wing in new[] { false, true }
        select new TestCaseData(lane, north, eyeSide, wing);

    [TestCaseSource(nameof(Patterns))]
    public void CompleteWrothHasLocalMarkersHorizontalThreeDropsInwardTurnAndVow(int lane, int north, int eyeSide, bool wing)
    {
        var seed = (lane + 1) * 8 + (north + 1) * 2 + (eyeSide + 1) + (wing ? 1 : 0);
        var scenario = new DsrP6DoubleDragonsScenario();
        var drops = new List<Vector3>();
        var eyes = 0;
        var marked = false;
        var final = false;
        var vow = false;
        SoftMarkers? local = null;
        var run = ScenarioRun.Execute(typeof(DsrP6DoubleDragonsScenario), 0, seed, new()
        {
            PlayerRole = (PartyRole)(seed % 8), BotDifficulty = wing ? BotDifficulty.Hard : BotDifficulty.Easy,
            ShowSimulatedParty = eyeSide > 0, ScenarioFactory = () => scenario,
            AlwaysWriteArtifacts = lane == 0 && north == -1 && eyeSide == 1 && wing,
            Overrides = o =>
            {
                var settings = (DsrP6DoubleDragonsStateOverrides)o;
                settings.HraesvelgrLane = lane;
                settings.SecondSetCorner = (eyeSide, north);
                settings.HotWing = wing;
            },
            Probe = p =>
            {
                var state = scenario.LastState!;
                if (p.Crossed(70.2f))
                {
                    local = p.World.SoftMarkers;
                    var expected = new DsrWrothMarkers(state.Spreads, state.Stacks).Assignments;
                    Assert.That(local.Count, Is.EqualTo(8));
                    foreach (var (role, sign) in expected)
                        Assert.That(local.Snapshot(p.World.ScenarioOrigin).Single(m => m.Sign == sign).Position,
                            Is.EqualTo(p.Member(role)!.Position + p.World.ScenarioOrigin + new Vector3(0, 2.8f, 0)));
                    Assert.That(local.Snapshot(p.World.ScenarioOrigin, false), Has.Count.EqualTo(1));
                    marked = true;
                }
                foreach (var hit in DsrP6DoubleDragonsScenario.AkhMornHits)
                    if (p.Crossed(hit + .02f))
                    {
                        drops.Add(p.Member(state.AkhMornTarget)!.Position);
                        var target = drops[^1];
                        Assert.That(p.World.Party.ActiveMembers().All(m => Vector3.Distance(m.Position, target) < 3f), Is.True);
                    }
                foreach (var cast in DsrP6DoubleDragonsScenario.FlameBlastCasts)
                    if (p.Crossed(cast + DsrP6DoubleDragonsScenario.FlameBlastDelay + .02f)) eyes++;
                if (p.Crossed(85f))
                    Assert.That(p.World.Children.OfType<SimEventObject>().Count(e => e.EObjRowId == 0x1EB683), Is.EqualTo(4));
                if (p.Crossed(93.1f))
                {
                    var markers = new DsrWrothMarkers(state.Spreads, state.Stacks).Assignments;
                    foreach (var (role, sign) in markers)
                    {
                        var position = p.Member(role)!.Position;
                        var x = sign switch { Sign.Attack1 => -20f, Sign.Attack2 => -13.5f, Sign.Attack3 => -7f, Sign.Attack4 => -.5f,
                            Sign.Bind1 or Sign.Bind2 => 7.5f, _ => 17f };
                        Assert.That(position.X, Is.EqualTo(x).Within(.25f));
                        Assert.That(position.Z, Is.EqualTo(wing ? 0 : north * 13f).Within(.25f));
                    }
                    final = true;
                }
                if (p.Crossed(93.5f)) Assert.That(p.World.SoftMarkers.Count, Is.Zero);
                if (p.Crossed(97.2f))
                {
                    Assert.That(p.Member(PartyRole.OffTank)!.HasStatus(0xB50), Is.True);
                    vow = true;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(marked && final && vow, Is.True);
        Assert.That(eyes, Is.EqualTo(3));
        Assert.That(drops, Has.Count.EqualTo(4));
        Assert.That(drops[0].Z, Is.EqualTo(drops[1].Z).Within(.25f));
        Assert.That(drops[1].Z, Is.EqualTo(drops[2].Z).Within(.25f));
        Assert.That(MathF.Abs(drops[0].X), Is.GreaterThan(MathF.Abs(drops[1].X) + 4));
        Assert.That(MathF.Abs(drops[1].X), Is.GreaterThan(MathF.Abs(drops[2].X) + 2));
        Assert.That(MathF.Abs(drops[3].Z), Is.LessThan(MathF.Abs(drops[2].Z) - 4));
        Assert.That(local!.Count, Is.Zero);
    }
    [TestCase(PartyRole.MainTank, 0)] [TestCase(PartyRole.RegenHealer, 1)]
    [TestCase(PartyRole.MeleeDpsA, 2)] [TestCase(PartyRole.CasterDps, 0)]
    [TestCase(PartyRole.MainTank, 1)] [TestCase(PartyRole.RegenHealer, 2)]
    [TestCase(PartyRole.MeleeDpsA, 0)] [TestCase(PartyRole.CasterDps, 1)]
    [TestCase(PartyRole.MainTank, 2)] [TestCase(PartyRole.RegenHealer, 0)]
    [TestCase(PartyRole.MeleeDpsA, 1)] [TestCase(PartyRole.CasterDps, 2)]
    public void HumanCanFollowAtRunSpeedWithReactionDelayAndPositionSlack(PartyRole role, int debuff)
    {
        var scenario = new DsrP6DoubleDragonsScenario();
        var others = Enum.GetValues<PartyRole>().Where(r => r != role).ToArray();
        var spreads = debuff == 0 ? new[] { role }.Concat(others.Take(3)).ToArray() : others.Take(4).ToArray();
        var remaining = others.Except(spreads).ToArray();
        var stacks = debuff == 1 ? new[] { role, remaining[0] } : remaining.Take(2).ToArray();
        var east = (int)role % 2 == 0;
        var cx = east ? 1f : -1f;
        var cz = debuff == 1 ? -1f : 1f;
        var wing = debuff != 2;
        var run = ScenarioRun.Execute(typeof(DsrP6DoubleDragonsScenario), 0, 45 + (int)role + debuff, new()
        {
            PlayerRole = role, ScenarioFactory = () => scenario, StopAt = 98f,
            Takeovers = [new(78.5f, null)],
            Overrides = o =>
            {
                var settings = (DsrP6DoubleDragonsStateOverrides)o;
                settings.Spreads = spreads; settings.Stacks = stacks;
                settings.HraesvelgrLane = east ? -1 : 1;
                settings.SecondSetCorner = (1, -(int)cz);
                settings.HotWing = wing;
            },
            Probe = p =>
            {
                if (p.Time < 78.6f) return;
                var time = p.Time;
                var destination = new Vector2(cx * 20.5f, cz * 18.5f);
                if (time >= 79.51f) destination = new(cx * 14f, cz * 18.5f);
                if (time >= 81.16f) destination = new(cx * 10.5f, cz * 18.5f);
                if (time >= 82.72f) destination = new(cx * 3f, cz * 12.5f);
                if (time >= 84.28f) destination = Vector2.Zero;
                if (time >= 87.9f)
                {
                    var sign = new DsrWrothMarkers(spreads, stacks).Assignments[role];
                    destination = new(sign switch { Sign.Attack1 => -20, Sign.Attack2 => -13.5f, Sign.Attack3 => -7,
                        Sign.Attack4 => -.5f, Sign.Bind1 or Sign.Bind2 => 7.5f, _ => 17 }, wing ? 0 : -cz * 13);
                }
                if (time >= 93.5f)
                    destination = role == PartyRole.MainTank ? new(-.3f, 0) : role == PartyRole.OffTank ? new(.3f, 0) : new(0, -cz * 9);
                // Deliberately offset the human from bot destinations and use ordinary running speed.
                destination += new Vector2(.6f, .6f);
                var member = p.Member(role)!;
                var current = new Vector2(member.Position.X, member.Position.Z);
                var delta = destination - current;
                var step = delta.Length() <= 6.5f / 60 ? delta : Vector2.Normalize(delta) * (6.5f / 60);
                member.SetPosition(new Vector3(current.X + step.X, 0, current.Y + step.Y));
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(true)] [TestCase(false)]
    public void WrongSpreadOrOverlappingWhiteStacksStillFail(bool spread)
    {
        var scenario = new DsrP6DoubleDragonsScenario();
        var run = ScenarioRun.Execute(typeof(DsrP6DoubleDragonsScenario), 0, 18, new()
        {
            ScenarioFactory = () => scenario, WriteArtifactsOnFailure = false, StopAt = 94f,
            Probe = p =>
            {
                if (!p.Crossed(93.2f)) return;
                var state = scenario.LastState!;
                var roles = spread ? state.Spreads.ToArray() : state.Stacks.ToArray();
                var first = p.Member(roles[0])!;
                p.Member(roles[1])!.StopMoving();
                p.Member(roles[1])!.SetPosition(first.Position);
            },
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == (spread ? 0x742Bu : 0x742Cu)), Is.True, run.ToString);
    }

    [TestCase(72f)] [TestCase(83f)] [TestCase(94f)]
    public void LeavingOrRestartingClearsWrothMarkers(float stop)
    {
        var scenario = new DsrP6DoubleDragonsScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            SoftMarkers? markers = null;
            var run = ScenarioRun.Execute(typeof(DsrP6DoubleDragonsScenario), 0, repeat, new()
            {
                ScenarioFactory = () => scenario, StopAt = stop,
                Probe = p => { if (p.Crossed(71)) markers = p.World.SoftMarkers; },
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(markers!.Count, Is.Zero);
        }
    }

    [Test]
    public void RestartClearsOldMarkersBeforeTheNextDebuffRoll()
    {
        var scenario = new DsrP6DoubleDragonsScenario();
        FakeGame fake = null!;
        var restarted = false;
        var run = ScenarioRun.Execute(typeof(DsrP6DoubleDragonsScenario), 0, 8, new()
        {
            ScenarioFactory = () => scenario, ConfigureNative = f => fake = f,
            Probe = p =>
            {
                if (restarted || !p.Crossed(80)) return;
                Assert.That(p.World.SoftMarkers.Count, Is.EqualTo(8));
                var oldMarkers = p.World.SoftMarkers;
                p.Game.Reset();
                fake.Frame(0, p.Game.Tick);
                Assert.That(oldMarkers.Count, Is.Zero);
                p.Game.EnterAndStart(new RunScenarioParams(scenario, new LocalPartyProvider { RoleOverride = PartyRole.MeleeDpsA }, 0, 0, 8), SessionMode.Local);
                fake.Frame(ScenarioRun.FrameSeconds, p.Game.Tick);
                Assert.That(p.World.SoftMarkers.Count, Is.Zero);
                restarted = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(restarted, Is.True);
    }

    [Test]
    public void FailureClearsAllEightMarkers()
    {
        var cleared = false;
        var run = ScenarioRun.Execute(typeof(DsrP6DoubleDragonsScenario), 0, 8, new()
        {
            StopAt = 81f, WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (p.Crossed(80))
                {
                    Assert.That(p.World.SoftMarkers.Count, Is.EqualTo(8));
                    p.Member(PartyRole.MainTank)!.Die(0x6D46, "Deliberate failure for marker cleanup");
                }
                if (!p.Crossed(80.2f)) return;
                Assert.That(p.World.SoftMarkers.Count, Is.Zero);
                cleared = true;
            },
        });
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("Deliberate failure")), Is.True);
        Assert.That(cleared, Is.True);
    }

}
