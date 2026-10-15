#pragma warning disable CA1416
using System.Reflection;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using Dalamud.Plugin.Services;

namespace AnoMech.Tests;

public class OfflineScenarioIntegrationTests
{
    public static IEnumerable<Type> FluffyScenarios() => ScenarioCatalog.Create().Scenarios
        .Where(s => s.Phase.Zone is AnoMech.Scenarios.Fru.FruZone or AnoMech.Scenarios.Dsr.DsrZone)
        .Select(s => s.GetType());

    [TestCaseSource(nameof(FluffyScenarios))]
    public void BootstrappedLocalInnDoesNotRequireALoggedInSessionToRunTheScenario(Type type)
    {
        var service=DispatchProxy.Create<IClientState,DalamudServices.NullService>();
        DalamudServices.Install(nameof(Plugin.ClientState),service);
        var inspected=false;
        var run=ScenarioRun.Execute(type,0,0,new()
        {
            Probe=p=>
            {
                if(!p.Crossed(2)) return;
                Assert.That(service.IsLoggedIn,Is.False);
                Assert.That(p.World.Party.AllMembers().Count(),Is.EqualTo(8));
                Assert.That(p.World.Party.AllMembers().OfType<SimPartyNpc>().Count(),Is.EqualTo(7));
                Assert.That(p.World.Children.OfType<SimEnemy>().Any(),Is.True);
                Assert.That(Natives.Zone.IsActive,Is.True);
                Assert.That(((FakeBgm)Natives.Bgm).Plays,Is.Not.Empty);
                inspected=true;
            },WriteArtifactsOnFailure=false,
        });
        Assert.That(inspected,Is.True);
        Assert.That(run.Passed,Is.True,run.ToString);
    }
}
