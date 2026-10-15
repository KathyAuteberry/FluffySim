using System.Linq;
using Mono.Cecil;
using Xunit;
using Assert = Xunit.Assert;

namespace AnoMech.SafetyTests.Architecture;

public class OfflineIntegrationTests
{
    [Fact]
    public void OfflineImportHasSnapshotAndGuardsBeforeItCanMutateCharacterState()
    {
        foreach(var il in PluginIL.Builds)
        {
            var preflight=il.Method("AnoMech.Offline.OfflineBootstrap","Preflight");
            var calls=preflight.Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>PluginIL.Name((MethodReference)i.Operand)).ToList();
            Assert.True(calls.IndexOf("AnoMech.Offline.OfflineSnapshot::Take") < calls.IndexOf("AnoMech.Offline.OfflineBootstrap::RearmGuards"));
            var load=il.Method("AnoMech.Offline.OfflineBootstrap","LoadConfig");
            var mutations=load.Body.Instructions.Where(i=>i.Operand is MethodReference m && PluginIL.Name(m)=="AnoMech.Offline.OfflineGameFiles::LoadConfig").ToArray();
            Assert.Equal(1,mutations.Length);
            var check=load.Body.Instructions.Single(i=>i.Operand is MethodReference m && PluginIL.Name(m)=="AnoMech.Offline.OfflineBootstrap::get_GuardsIntact");
            var outcome=ControlFlow.OutcomeOf(check);
            Assert.NotNull(outcome);
            Assert.False(new ControlFlow(load).FeasiblyReachableAvoiding([(outcome!.Value.Branch,outcome.Value.WhenTruthy)]).Contains(mutations[0]), il.ToString());
        }
    }

    [Fact]
    public void OfflinePacketGuardsDropWithoutCallingTheirOriginalSenders()
    {
        foreach(var il in PluginIL.Builds)
            foreach(var name in new[]{"ZoneSendDetour","ChatSendDetour"})
                Assert.DoesNotContain(il.Method("AnoMech.Offline.OfflineNetworkGuard",name).Body.Instructions,
                    i=>i.Operand is MethodReference m && m.Name is "get_Original" or "Invoke");
    }

    [Fact]
    public void OfflineGuardsAreCheckedOnEveryFrameAndBeforeNormalSimulationWork()
    {
        foreach(var il in PluginIL.Builds)
        {
            var tick=il.Method("AnoMech.Offline.OfflineSession","Tick");
            Assert.Contains(tick.Body.Instructions,i=>i.Operand is MethodReference m && PluginIL.Name(m)=="AnoMech.Offline.OfflineBootstrap::get_GuardsIntact");
            Assert.Contains(tick.Body.Instructions,i=>i.Operand is MethodReference m && PluginIL.Name(m)=="AnoMech.Offline.OfflineSession::CloseGame");
            var update=il.Method("AnoMech.Plugin","OnFrameworkUpdate");
            var calls=update.Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>PluginIL.Name((MethodReference)i.Operand)).ToList();
            Assert.True(calls.IndexOf("AnoMech.Offline.OfflineSession::Tick") < calls.IndexOf("AnoMech.Core.Game.Game::Tick"));
        }
    }

    [Fact]
    public void NativeResourceResetRequiresTheVerifiedPracticeSession()
    {
        foreach(var il in PluginIL.Builds)
        {
            var reset=il.Method("AnoMech.Core.UserActions.ResetHandler","OnScenarioStart");
            var calls=reset.Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>PluginIL.Name((MethodReference)i.Operand)).ToList();
            Assert.True(calls.IndexOf("AnoMech.Core.Native.Implementations.ZoneControl::get_NativePracticeReady") < calls.IndexOf("AnoMech.Core.UserActions.ResetHandler::ClearGauge"));
            var broad=il.CallersOf("^AnoMech.Core.Native.Implementations.RecastTimes::ClearAll$");
            Assert.True(broad.All(c=>c=="AnoMech.Windows.DebugMenu::FireClearedRecastTimes"));
            Assert.Contains(il.Method("AnoMech.Core.Native.Implementations.RecastTimes","ClearAll").Body.Instructions,
                i=>i.Operand is MethodReference m && PluginIL.Name(m)=="AnoMech.Core.Native.Implementations.ZoneControl::get_HoldsSends");
        }
    }
}
