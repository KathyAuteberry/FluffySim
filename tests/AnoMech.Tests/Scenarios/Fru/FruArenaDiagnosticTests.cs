using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

public class FruArenaDiagnosticTests
{
    [TestCase(0)]
    [TestCase(1)]
    public void SoloInspectionDoesNotSpawnMechanicObjects(int seed)
    {
        var inspected = false;
        var run = ScenarioRun.Execute(typeof(FruArenaDiagnosticScenario), 0, seed, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruArenaDiagnosticScenario(),
            Solo = true,
            StopAt = 4f,
            Probe = p =>
            {
                if (!p.Crossed(3.5f)) return;
                inspected = true;
                Assert.That(p.World.Children.OfType<SimEnemy>(), Is.Empty);
                Assert.That(p.World.Children.OfType<SimTether>(), Is.Empty);
                Assert.That(Enumerable.Range(0, 8).Select(p.World.Party.Get).OfType<SimPartyNpc>(), Is.Empty);
                Assert.That(p.World.Party.Player, Is.Not.Null);
            },
        });
        Assert.That(inspected, Is.True);
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void ArenaInitializesAllRestingStatesBeforeP2Transition()
    {
        var inspected = false;
        AnoMech.Core.Map.MapController? map = null;
        var run = ScenarioRun.Execute(typeof(FruArenaDiagnosticScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruArenaDiagnosticScenario(),
            Solo = true,
            StopAt = 4f,
            Probe = p =>
            {
                if (!p.Crossed(0.1f)) return;
                var effects = ((FakeMapEffects)Natives.MapEffects).Applied;
                Assert.That(effects, Has.Count.EqualTo(55));
                for (byte i = 0; i < 54; i++) Assert.That(effects[i], Is.EqualTo((0x00040004u, i)));
                Assert.That(effects[54], Is.EqualTo((0x00020001u, (byte)0x17)));
                var held = ((FakeLayoutFunctions)Natives.Layout).HeldLayerStates;
                Assert.That(held, Has.Count.EqualTo(15));
                for (ushort key = 0x5F88; key <= 0x5F94; key++) Assert.That(held[(1238, key)], Is.False);
                Assert.That(held[(1238, 0x5F9F)], Is.False);
                Assert.That(held[(1238, 0x5FC9)], Is.True);
                Assert.That(FruZone.P2.Weather, Is.EqualTo(35));
                map = p.World.Map;
                inspected = true;
            }
        });
        Assert.That(inspected, Is.True);
        Assert.That(run.Passed, run.ToString);
        map!.Unload();
        Assert.That(((FakeLayoutFunctions)Natives.Layout).HeldLayerStates, Is.Empty);
        Assert.That(Natives.MapEffects.Loaded, Is.False);
    }
}
