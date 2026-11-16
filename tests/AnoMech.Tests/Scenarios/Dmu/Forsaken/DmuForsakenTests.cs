using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dmu.Forsaken;
using static AnoMech.Core.Game.Party.PartyRole;
using LockonId = AnoMech.Scenarios.Dmu.DmuConstants.LockonId;

namespace AnoMech.Tests;

// Stacks on MT and M1 make MT RH M1 R1 the tower group. With each tower's lockons dealt in role
// order, tower 3 leaves MT and RH with cones and M1 and R1 with AoE circles, which they then
// self-mark. Tower 8's towers sit SW and SE, the layout Kitten draws.
public class DmuForsakenTests
{
    private const float LastTower = 95.82f;

    private static void Pin(DmuForsakenStateOverrides o, bool coneSwapped, bool aoeSwapped)
    {
        o.NewNorth = Direction.All.Single(d => d.Rotate(7).Index() == Direction.S.Index());
        o.Rotation = 1;
        o.SupportLockon = LockonId.ForsakenChariot;
        o.SupportStackRole = MainTank;
        o.DpsStackRole = MeleeDpsA;
        o.ReassignLockonsInRoleOrder = true;
        o.FinalConeMarksSwapped = coneSwapped;
        o.FinalAoeMarksSwapped = aoeSwapped;
    }

    [TestCase(false, false, MainTank, MeleeDpsA)]
    [TestCase(true, false, RegenHealer, MeleeDpsA)]
    [TestCase(false, true, MainTank, PhysRangedDps)]
    [TestCase(true, true, RegenHealer, PhysRangedDps)]
    public void SelfMarkedOnesTakeTheWestTowerOfTheLastSet(bool coneSwapped, bool aoeSwapped, PartyRole bind1, PartyRole ignore1)
    {
        var marks = new Dictionary<Sign, PartyRole>();
        var spots = new Dictionary<PartyRole, Vector3>();
        var run = ScenarioRun.Execute(typeof(DmuForsakenScenario), 0, 7, new()
        {
            PlayerRole = OffTank,
            Overrides = o => Pin((DmuForsakenStateOverrides)o, coneSwapped, aoeSwapped),
            Probe = p =>
            {
                if (!p.Crossed(LastTower - 0.1f)) return;
                foreach (var marker in p.World.SoftMarkers.Snapshot(Vector3.Zero))
                    marks[marker.Sign] = PerRole.All.Single(r => Vector3.Distance(p.Member(r)!.Position + new Vector3(0f, 2.8f, 0f), marker.Position) < 0.01f);
                foreach (var role in PerRole.All) spots[role] = p.Member(role)!.Position;
            },
            StopAt = LastTower + 3f,
            WriteArtifactsOnFailure = false,
        });

        Assert.That(run.Passed, run.ToString);
        Assert.That(marks[Sign.Bind1], Is.EqualTo(bind1));
        Assert.That(marks[Sign.Ignore1], Is.EqualTo(ignore1));
        Assert.That(marks.Values, Is.EquivalentTo(new[] { MainTank, RegenHealer, MeleeDpsA, PhysRangedDps }));
        foreach (var one in new[] { Sign.Bind1, Sign.Ignore1 })
            Assert.That(spots[marks[one]].X, Is.LessThan(0f), $"{one} {marks[one]} at {spots[marks[one]]}");
        foreach (var two in new[] { Sign.Bind2, Sign.Ignore2 })
            Assert.That(spots[marks[two]].X, Is.GreaterThan(0f), $"{two} {marks[two]} at {spots[marks[two]]}");
    }

    // Hits come back in party order, so trading places across the two towers reorders the soakers.
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void ASeedDealsTheSameSpellsWhicheverTowerEachSoakerTakes(int tower)
    {
        var baseline = LockonsAfter(tower, swapAcrossTowers: false);
        var swapped = LockonsAfter(tower, swapAcrossTowers: true);
        Assert.That(swapped, Is.EqualTo(baseline));
    }

    private static Dictionary<PartyRole, uint> LockonsAfter(int tower, bool swapAcrossTowers)
    {
        float[] towerTimes = [22.66f, 32.67f, 43.71f, 53.72f];
        var scenario = new DmuForsakenScenario();
        var lockons = new Dictionary<PartyRole, uint>();
        ScenarioRun.Execute(typeof(DmuForsakenScenario), 0, 11, new()
        {
            ScenarioFactory = () => scenario,
            GodMode = true,
            Probe = p =>
            {
                var state = scenario.LastState!;
                if (swapAcrossTowers && p.Crossed(towerTimes[tower] - 0.05f))
                {
                    var (d1, d2) = state.GetTowers(tower);
                    var first = InTower(p, d1.Apply(new Vector3(0f, 0f, -8f)));
                    var second = InTower(p, d2.Apply(new Vector3(0f, 0f, -8f)));
                    var (a, b) = (p.Member(first)!, p.Member(second)!);
                    var (pa, pb) = (a.Position, b.Position);
                    a.SetPosition(pb);
                    b.SetPosition(pa);
                }
                if (p.Crossed(towerTimes[tower] + 1.5f))
                    foreach (var (role, lockon) in state.Lockons) lockons[role] = lockon;
            },
            StopAt = towerTimes[tower] + 2f,
            WriteArtifactsOnFailure = false,
        });
        return lockons;
    }

    private static PartyRole InTower(ScenarioProbe p, Vector3 centre)
        => PerRole.All.First(r => Vector2.Distance(new(p.Member(r)!.Position.X, p.Member(r)!.Position.Z), new(centre.X, centre.Z)) < 4f);

    [Test]
    public void PartyLinesUpInFourColumnsSouthOfKefka()
    {
        var spots = new Dictionary<PartyRole, Vector3>();
        ScenarioRun.Execute(typeof(DmuForsakenScenario), 0, 7, new()
        {
            Probe = p =>
            {
                if (p.Crossed(9.5f)) foreach (var role in PerRole.All) spots[role] = p.Member(role)!.Position;
            },
            StopAt = 10f,
            WriteArtifactsOnFailure = false,
        });

        Assert.That(PerRole.All.All(r => spots[r].Z > 3f), string.Join(", ", spots));
        PartyRole[][] columnsWestToEast = [[MainTank, RegenHealer], [OffTank, ShieldHealer], [MeleeDpsA, PhysRangedDps], [MeleeDpsB, CasterDps]];
        for (var c = 0; c < columnsWestToEast.Length; c++)
        {
            var (front, back) = (spots[columnsWestToEast[c][0]], spots[columnsWestToEast[c][1]]);
            Assert.That(back.Z, Is.GreaterThan(front.Z + 3f), $"column {c}");
            Assert.That(MathF.Abs(back.X - front.X), Is.LessThan(1.5f), $"column {c}");
            if (c > 0) Assert.That(front.X, Is.GreaterThan(spots[columnsWestToEast[c - 1][0]].X + 3f), $"column {c}");
        }
    }
}
