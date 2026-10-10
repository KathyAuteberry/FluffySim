using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru;

namespace AnoMech.Tests;

public class FruP3ArenaTests
{
    [TestCase(0)]
    [TestCase(1)]
    public void P3PresetUsesRecordedTransitionsAndOwnsNoMechanicActors(int seed)
    {
        var inspected = false;
        AnoMech.Core.Map.MapController? map = null;
        var run = ScenarioRun.Execute(typeof(FruP3ArenaDiagnosticScenario), 0, seed, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruP3ArenaDiagnosticScenario(),
            Solo = true,
            StopAt = 4f,
            Probe = p =>
            {
                if (!p.Crossed(0.1f)) return;
                inspected = true;
                map = p.World.Map;
                var effects = ((FakeMapEffects)Natives.MapEffects).Applied;
                Assert.That(effects, Has.Count.EqualTo(58));
                for (byte i = 0; i < 54; i++) Assert.That(effects[i], Is.EqualTo((0x00040004u, i)));
                Assert.That(effects.Skip(54), Is.EqualTo(new[]
                {
                    (0x00080004u, (byte)0x17), (0x00020001u, (byte)0x28),
                    (0x00020001u, (byte)0x29), (0x00200010u, (byte)0x28),
                }));
                var held = ((FakeLayoutFunctions)Natives.Layout).HeldLayerStates;
                Assert.That(held, Has.Count.EqualTo(15));
                Assert.That(held.Values, Is.All.False);
                Assert.That(held.ContainsKey((1238, 0x606E)), Is.False);
                Assert.That(held.ContainsKey((1238, 0x5FD8)), Is.False);
                Assert.That(FruZone.P3.Weather, Is.EqualTo(106));
                Assert.That(p.World.Children.OfType<SimEnemy>(), Is.Empty);
                Assert.That(p.World.Children.OfType<SimTether>(), Is.Empty);
                Assert.That(Enumerable.Range(0, 8).Select(p.World.Party.Get).OfType<SimPartyNpc>(), Is.Empty);
            },
        });
        Assert.That(inspected, Is.True);
        Assert.That(run.Passed, run.ToString);
        map!.Unload();
        Assert.That(((FakeLayoutFunctions)Natives.Layout).HeldLayerStates, Is.Empty);
        Assert.That(Natives.MapEffects.Loaded, Is.False);
    }
}
