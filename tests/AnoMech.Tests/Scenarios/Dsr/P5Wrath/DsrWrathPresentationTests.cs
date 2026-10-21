#pragma warning disable CA1416
using System.Globalization;
using AnoMech.Core.SimObjects;
using AnoMech.Core.Game;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.P5Wrath;

namespace AnoMech.Tests;

public class DsrWrathPresentationTests
{
    [TestCase(0, 89)] [TestCase(5, 89)] [TestCase(19, 89)] [TestCase(5, 40)]
    public void UntargetableDragonIsListedAndCastsBeforeDiveWithDelayedSpawn(int delay, int stop)
    {
        var scenario = new DsrP5WrathScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame native = null!;
            SimEnemy? dragon = null;
            var checkedCast = false;
            SimWorld? world = null;
            var snapshots = new List<float>();
            void Snapshot(AoeQuery query)
            {
                if (query.ActionId == DsrP5Constants.ActionId.TwistingDive && query.CandidateType == typeof(SimCharacter))
                    snapshots.Add(world!.Events.Elapsed);
            }
            AoeQuery.Evaluated += Snapshot;
            ScenarioRun run;
            try
            {
                run = ScenarioRun.Execute(typeof(DsrP5WrathScenario), 0, repeat, new()
                {
                    ScenarioFactory = () => scenario, StopAt = stop,
                    ConfigureNative = f => { native = f; f.BattleCharas.PacketSpawnDelayFrames = delay; },
                    Probe = p =>
                    {
                        world = p.World;
                        if (p.Crossed(40f))
                        {
                            dragon = p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == DsrP5Constants.BNpcBaseId.Vedrfolnir);
                            Assert.That(dragon.Targetable, Is.False);
                            Assert.That(dragon.InEnemyList, Is.True);
                            Assert.That(dragon.Casting.ActionId, Is.EqualTo(DsrP5Constants.ActionId.TwistingDive));
                            Assert.That(dragon.Casting.IsCasting, Is.True);
                            Assert.That(dragon.Casting.Total, Is.EqualTo(5.7f).Within(.001));
                            checkedCast = true;
                        }
                        if (p.Crossed(45.1f)) Assert.That(dragon!.Casting.IsCasting, Is.False);
                    },
                });
            }
            finally { AoeQuery.Evaluated -= Snapshot; }
            Assert.That(run.Passed, run.ToString);
            Assert.That(checkedCast, Is.True);
            var rows = native.Timeline.Rows;
            var cast = rows.Single(r => r["kind"] == "cast" && r["id"] == DsrP5Constants.ActionId.TwistingDive.ToString());
            Assert.That(rows.Any(r => r["kind"] == "enemy_list" && r["actor"] == cast["actor"] && r["id"] == "1"), Is.True);
            Assert.That(rows.Any(r => r["kind"] == "enemy_list" && r["actor"] == cast["actor"] && r["id"] == "0"), Is.True);
            if (stop > 45)
            {
                Assert.That(snapshots, Has.Count.EqualTo(1));
                Assert.That(snapshots.Single(), Is.EqualTo(44.68f).Within(.04));
                var effect = rows.Single(r => r["kind"] == "ability" && r["id"] == cast["id"]);
                Assert.That(float.Parse(effect["t"], CultureInfo.InvariantCulture), Is.EqualTo(44.98f).Within(.04));
                Assert.That(effect["actor"], Is.EqualTo(cast["actor"]));
            }
            else Assert.That(rows.Any(r => r["kind"] == "ability" && r["id"] == cast["id"]), Is.False);
            Assert.That(native.BattleCharas.OccupiedSlots, Is.Zero);
            Assert.That(dragon!.IsActive, Is.False);
        }
    }
}
