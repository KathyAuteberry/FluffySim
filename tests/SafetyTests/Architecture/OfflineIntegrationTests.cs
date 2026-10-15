using System.Linq;
using Mono.Cecil;
using Xunit;
using Assert = Xunit.Assert;

namespace AnoMech.SafetyTests.Architecture;

public class OfflineIntegrationTests
{
    [Fact]
    public void PartyHudRepairsNativeRosterLossAndUsesTheNativeTerritory()
    {
        foreach (var il in PluginIL.Builds)
        {
            var refresh = il.Method("AnoMech.Core.Native.Implementations.PartyHud", "Refresh");
            Assert.Contains(refresh.Body.Instructions, i => i.Operand is MethodReference m && m.Name == "MatchesNativeParty");
            var write = il.Method("AnoMech.Core.Native.Implementations.PartyHud", "WriteEntry");
            var send = il.Method("AnoMech.Core.Native.Implementations.PartyHud", "SendRoster");
            Assert.Contains(send.Body.Instructions, i => i.Operand is MethodReference m && PluginIL.Name(m) == "FFXIVClientStructs.FFXIV.Client.Game.GameMain::Instance");
            Assert.Contains(send.Body.Instructions, i => i.Operand is MethodReference m && m.Name == "WriteEntry");
            Assert.Contains(send.Body.Instructions, i => i.Operand is FieldReference f && f.Name == "CurrentTerritoryTypeId");
            Assert.Contains(write.Body.Instructions, i => i.Operand is FieldReference f && f.Name == "TerritoryType");
            Assert.DoesNotContain(send.Body.Instructions, i => i.Operand is MethodReference m && m.Name == "get_TerritoryType");
            Assert.DoesNotContain(write.Body.Instructions, i => i.Operand is MethodReference m && m.Name == "get_TerritoryType");
        }
    }

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
    [Fact]
    public void OfflineIdentityIsValidatedBeforeSnapshotOrGuardMutation()
    {
        foreach (var il in PluginIL.Builds)
        {
            var preflight = il.Method("AnoMech.Offline.OfflineBootstrap", "Preflight");
            var calls = preflight.Body.Instructions.Where(i => i.Operand is MethodReference)
                .Select(i => PluginIL.Name((MethodReference)i.Operand)).ToList();
            var identity = calls.IndexOf("AnoMech.Offline.OfflineIdentity::HasHomeWorld");
            Assert.True(identity >= 0);
            Assert.True(identity < calls.IndexOf("AnoMech.Offline.OfflineSnapshot::Take"));
            Assert.True(identity < calls.IndexOf("AnoMech.Offline.OfflineBootstrap::RearmGuards"));
        }
    }

    [Fact]
    public void OfflineNamesAreCachedLocallyWithoutAContentIdNameRequest()
    {
        foreach (var il in PluginIL.Builds)
        {
            Assert.True(!il.CallersOf("^FFXIVClientStructs.FFXIV.Client.Game.NameCache::GetNameByContentId$").Any());
            Assert.True(il.CallersOf("^FFXIVClientStructs.FFXIV.Client.Game.NameCache::SetNameForContentId$").Any());
            var configure = il.Method("AnoMech.Offline.OfflineIdentity", "ConfigureSpawn");
            Assert.DoesNotContain(configure.Body.Instructions, i => i.Operand is FieldReference f && f.DeclaringType.Name == "AgentLobby");
        }
    }

    [Fact]
    public void OfflineSpawnIdentifiersCannotBeInstalledInTheOnlineDoppelPath()
    {
        foreach (var il in PluginIL.Builds)
        {
            var spawn = il.Method("AnoMech.Core.Native.Implementations.BattleCharas", "SpawnDoppel");
            var prepare = spawn.Body.Instructions.Single(i => i.Operand is MethodReference m && m.Name == "PrepareDoppel");
            var check = spawn.Body.Instructions.Single(i => i.Operand is MethodReference m && PluginIL.Name(m) == "AnoMech.Offline.OfflineSession::get_InWorld");
            var outcome = ControlFlow.OutcomeOf(check);
            Assert.NotNull(outcome);
            Assert.False(new ControlFlow(spawn).FeasiblyReachableAvoiding([(outcome!.Value.Branch, outcome.Value.WhenTruthy)]).Contains(prepare));
        }
    }

    [Fact]
    public void ActualPartyAddonUpdateInvokesTheOfflinePresentationBridge()
    {
        foreach (var il in PluginIL.Builds)
        {
            var callback = il.Method("AnoMech.Core.Native.Implementations.PartyHud", "BeforePartyUpdate");
            Assert.Contains(callback.Body.Instructions, i => i.Operand is MethodReference m && m.Name == "PublishOfflinePresentation");
            var dispose = il.Method("AnoMech.Core.Native.Implementations.PartyHud", "Dispose");
            Assert.Contains(dispose.Body.Instructions, i => i.Operand is MethodReference m && m.Name == "UnregisterListener");
            Assert.Contains(dispose.Body.Instructions, i => i.Operand is MethodReference m && m.Name == "Clear");
            var publish = il.Method("AnoMech.Core.Native.Implementations.PartyHud", "PublishOfflinePresentation");
            var check = publish.Body.Instructions.Single(i => i.Operand is MethodReference m && PluginIL.Name(m) == "AnoMech.Offline.OfflineSession::get_InWorld");
            var outcome = ControlFlow.OutcomeOf(check);
            Assert.NotNull(outcome);
            var writes = publish.Body.Instructions.Where(i => i.Operand is MethodReference m && m.Name is "PublishHudMember" or "SetValue").ToArray();
            Assert.True(writes.Length >= 2);
            foreach (var write in writes)
                Assert.False(new ControlFlow(publish).FeasiblyReachableAvoiding([(outcome!.Value.Branch, outcome.Value.WhenTruthy)]).Contains(write));
        }
    }

}
