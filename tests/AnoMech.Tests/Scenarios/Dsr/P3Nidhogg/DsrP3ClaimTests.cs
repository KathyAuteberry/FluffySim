using AnoMech.Scenarios;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P3Nidhogg;
using NUnit.Framework;

namespace AnoMech.Tests.Scenarios.Dsr.P3Nidhogg;

[TestFixture]
public class DsrP3ClaimTests
{
    [Test]
    public void EveryRoleCanClaimEveryEligibleSlotWithoutArrows()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        foreach (var line in new[] { 1, 2, 3 })
        {
            var counts = new int[line == 2 ? 2 : 3];
            for (var seed = 0; seed < 192; seed++)
            {
                var settings = new DsrP3NidhoggStateOverrides { PlayerLine = line, PlayerArrow = DiveArrow.Circle };
                var state = new DsrP3NidhoggState(new Rng(seed), settings, role);
                var repeat = new DsrP3NidhoggState(new Rng(seed), settings, role);
                Assert.That(state.Line(line), Is.EqualTo(repeat.Line(line)));
                Assert.That(state.Arrows, Is.EquivalentTo(repeat.Arrows));
                Assert.That(state.Lines.Values.GroupBy(n => n).OrderBy(g => g.Key).Select(g => g.Count()), Is.EqualTo(new[] { 3, 2, 3 }));
                foreach (var number in new[] { 1, 2, 3 })
                {
                    var arrows = state.Line(number).Select(r => state.Arrows[r]).ToArray();
                    Assert.That(arrows.Count(a => a == DiveArrow.Up), Is.EqualTo(arrows.Count(a => a == DiveArrow.Down)));
                    Assert.That(arrows.Count(a => a == DiveArrow.Up), Is.InRange(0, 1));
                }
                if (state.LineHasArrows(line)) continue;
                counts[Array.IndexOf(state.Line(line).ToArray(), role)]++;
            }
            Assert.That(counts, Has.All.GreaterThan(5), $"{role}, line {line}: {string.Join(",", counts)}");
            Assert.That(counts.Max(), Is.LessThan(counts.Sum() * .65), $"{role}, line {line}: {string.Join(",", counts)}");
        }
    }
}
