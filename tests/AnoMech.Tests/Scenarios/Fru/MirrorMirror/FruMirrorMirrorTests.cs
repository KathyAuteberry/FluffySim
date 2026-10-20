using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.MirrorMirror;

namespace AnoMech.Tests;

public class FruMirrorMirrorTests
{
    private static IEnumerable<TestCaseData> Patterns()
        => from blue in Enumerable.Range(0, 8)
           from red in Enumerable.Range(0, 8).Where(r => r != blue && (r + 2) % 8 != blue)
           from pairs in new[] { false, true }
           from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
           select new TestCaseData(blue, red, pairs, difficulty);
    [TestCaseSource(nameof(Patterns))]
    public void ManaMirrorsAndBanishSurvive(int blue, int red, bool pairs, BotDifficulty difficulty)
    {
        var run = ScenarioRun.Execute(typeof(FruMirrorMirrorScenario), 0, blue * 8 + red, new()
        {
            ScenarioFactory = () => new FruMirrorMirrorScenario(),
            PlayerRole = (PartyRole)((blue + red) % 8), BotDifficulty = difficulty, WriteArtifactsOnFailure = false,
            Overrides = o =>
            {
                var s = (FruMirrorMirrorStateOverrides)o;
                s.Blue = blue; s.Red = red; s.Pairs = pairs;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }
    [Test]
    public void ReportedSeedKeepsTankFlanksFixedAtTheRedReflection()
    {
        var scenario = new FruMirrorMirrorScenario();
        var checkedPositions = false;
        var run = ScenarioRun.Execute(typeof(FruMirrorMirrorScenario), 0, 592545838, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = PartyRole.MainTank,
            Probe = p =>
            {
                if (!p.Crossed(28.7f)) return;
                var state = scenario.LastState!;
                var source = FruMirrorMirrorAi.MeleeRed(state);
                var origin = FruMirrorMirrorState.Point(source, 20);
                var right = FruMirrorMirrorState.Point(source + 6, 1);
                Assert.That(Vector3.Dot(p.Member(PartyRole.MainTank)!.Position - origin, right), Is.GreaterThan(2));
                Assert.That(Vector3.Dot(p.Member(PartyRole.OffTank)!.Position - origin, right), Is.LessThan(-2));
                var rangedSource = FruMirrorMirrorAi.RangedRed(state);
                var rangedOrigin = FruMirrorMirrorState.Point(rangedSource, 20);
                var rangedRight = FruMirrorMirrorState.Point(rangedSource + 6, 1);
                Assert.That(Vector3.Dot(p.Member(PartyRole.RegenHealer)!.Position - rangedOrigin, rangedRight), Is.LessThan(-2));
                Assert.That(Vector3.Dot(p.Member(PartyRole.ShieldHealer)!.Position - rangedOrigin, rangedRight), Is.GreaterThan(2));
                foreach (var ranged in new[] {PartyRole.PhysRangedDps, PartyRole.CasterDps})
                    Assert.That(MathF.Abs(Vector3.Dot(p.Member(ranged)!.Position - rangedOrigin, rangedRight)), Is.LessThan(1.3f));
                var offTank = p.Member(PartyRole.OffTank)!.Position;
                Assert.That(Vector3.Distance(offTank, p.Member(PartyRole.MeleeDpsA)!.Position),
                    Is.LessThan(Vector3.Distance(offTank, p.Member(PartyRole.MeleeDpsB)!.Position)),
                    "The reported ST must be on the Dragoon flank, not the Monk flank");
                checkedPositions = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedPositions, Is.True);
    }

    [Test]
    public void RotatingTheOtherMirrorNeverExchangesSupportFlanks()
    {
        foreach (var blue in Enumerable.Range(0, 8))
        foreach (var red in Enumerable.Range(0, 8).Where(r => r != blue && (r + 2) % 8 != blue))
        {
            var state = new FruMirrorMirrorState(new AnoMech.Scenarios.Rng(0), new() { Blue = blue, Red = red });
            foreach (var role in new[] {PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer})
            {
                var source = FruMirrorMirrorAi.MeleeGroup(role) ? FruMirrorMirrorAi.MeleeRed(state) : FruMirrorMirrorAi.RangedRed(state);
                var offset = FruMirrorMirrorAi.RedPocket(state, role) - FruMirrorMirrorState.Point(source, 20);
                var right = FruMirrorMirrorState.Point(source + 6, 1);
                var expectedRight = role is PartyRole.MainTank or PartyRole.ShieldHealer;
                Assert.That(Vector3.Dot(offset, right) * (expectedRight ? 1 : -1), Is.GreaterThan(2), $"blue={blue} red={red} role={role}");
            }
        }
    }

}
