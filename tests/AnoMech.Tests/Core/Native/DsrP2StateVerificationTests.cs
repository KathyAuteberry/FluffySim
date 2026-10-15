#pragma warning disable CA1416
using AnoMech.Core.Native.Policies;
using System.Text.Json;

namespace AnoMech.Tests;

public class DsrP2StateVerificationTests
{
    private static Dictionary<ushort, IReadOnlyList<(uint Id, bool Active)>> Read(string mode)
    {
        using var stream = typeof(DsrP2StateVerificationTests).Assembly.GetManifestResourceStream("AnoMech.Tests.Core.Native.Fixtures.DsrP2Layers.json")!;
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty(mode).EnumerateObject().ToDictionary(p => ushort.Parse(p.Name),
            p => (IReadOnlyList<(uint, bool)>)p.Value.EnumerateArray().Select(i => (i[0].GetUInt32(), i[1].GetBoolean())).ToArray());
    }

    [TestCase("online", true)] [TestCase("offline", false)]
    public void VerifiesActualRecordedLayerObjectStatesRatherThanWeatherAlone(string mode, bool correct)
    {
        var state = Read(mode);
        Assert.That(DsrP2LayoutState.Matches(key => state[key]), Is.EqualTo(correct));
    }

    [Test]
    public void MixedLayersRequireTheRightIndividualObjectsNotJustTheSameActiveCount()
    {
        var state = Read("online");
        var mixed = state[2995].ToArray();
        var active = Array.FindIndex(mixed, x => x.Active);
        var inactive = Array.FindIndex(mixed, x => !x.Active);
        mixed[active].Active = false;
        mixed[inactive].Active = true;
        state[2995] = mixed;
        Assert.That(DsrP2LayoutState.Matches(key => state[key]), Is.False);
    }

    [Test]
    public void DelayedOrRebuiltLayersAreUnverifiedUntilTheirRealObjectsMatch()
    {
        var correct = Read("online");
        Assert.That(DsrP2LayoutState.Matches(_ => Array.Empty<(uint, bool)>()), Is.False);
        Assert.That(DsrP2LayoutState.Matches(key => correct[key]), Is.True);
        var cold = Read("offline");
        Assert.That(DsrP2LayoutState.Matches(key => cold[key]), Is.False);
        Assert.That(DsrP2LayoutState.Matches(key => correct[key]), Is.True);
    }
}
