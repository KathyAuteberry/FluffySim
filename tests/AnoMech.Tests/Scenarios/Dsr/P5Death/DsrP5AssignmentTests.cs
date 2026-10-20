#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Scenarios;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P5Death;
using AnoMech.Scenarios.Dsr.P5Wrath;
namespace AnoMech.Tests;
public class DsrP5AssignmentTests
{
    [Test]
    public void BaitsExcludeLightningAndIncludeGreenDive()
    {
        var states = Enumerable.Range(0, 128).Select(seed => new DsrP5WrathState(new Rng(seed))).ToArray();
        foreach (var state in states)
        {
            Assert.That(state.Lightning, Does.Not.Contain(state.Eruptions));
            Assert.That(state.Lightning, Does.Not.Contain(state.LiquidHeaven));
            Assert.That(state.Marked.Append(state.Divebomb), Does.Contain(state.Eruptions));
            Assert.That(state.Plain, Does.Contain(state.LiquidHeaven));
        }
        Assert.That(states.Any(s => s.Eruptions == s.Divebomb), Is.True);
    }
    [Test]
    public void DoomDistributionAndGazeSeparationAreValid()
    {
        for (var seed = 0; seed < 128; seed++)
        {
            var state = new DsrP5DeathState(new Rng(seed));
            Assert.That(state.Dooms.Distinct().Count(), Is.EqualTo(4));
            var difference = MathF.Abs(state.ThordanBearing - state.EyeBearing);
            Assert.That(MathF.Min(difference, 360 - difference), Is.AnyOf(135f, 180f));
        }
        PartyRole[] lightParty = [PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.PhysRangedDps];
        Assert.That(new DsrP5DeathState(new Rng(0), new() { Dooms = lightParty }).Dooms, Is.EquivalentTo(lightParty));
    }
    [Test]
    public void CirclesAreTheFarthestDoomPairAndNeverNonDooms()
    {
        var roles = Enum.GetValues<PartyRole>();
        var state = new DsrP5DeathState(new Rng(0), new() { Dooms = roles.Take(4).ToArray() });
        var positions = roles.ToDictionary(r => r, r => new Vector3(20, 0, 0));
        positions[PartyRole.MainTank] = new(-4, 0, 0); positions[PartyRole.OffTank] = new(4, 0, 0);
        positions[PartyRole.RegenHealer] = new(0, 0, -2.5f); positions[PartyRole.ShieldHealer] = new(0, 0, 2.5f);
        state.HandOutSymbols(positions);
        Assert.That(state.Symbols.Where(kv => kv.Value == ChainSymbol.Circle).Select(kv => kv.Key), Is.EquivalentTo(new[] { PartyRole.MainTank, PartyRole.OffTank }));
        Assert.That(state.Symbols.Values.GroupBy(s => s).All(g => g.Count() == 2), Is.True);
    }
}
