using AnoMech.SafetyTests.Harness;
using Xunit;
using Assert = Xunit.Assert;

namespace AnoMech.SafetyTests.Firewall;

// Dalamud's hook backends fail differently when they cannot patch the function: SafetyHook throws
// from Enable/Disable, a Reloaded hook (modelled at its worst) silently stays as it was. Neither
// may leave a filter up with nothing to release it, or lower one unverified.
public class HookFailureTests : FirewallTestBase
{
    public static TheoryData<HookBackend, HookRole> BackendsAndRoles => new()
    {
        { HookBackend.Reloaded, HookRole.Send },
        { HookBackend.Reloaded, HookRole.Receive },
        { HookBackend.SafetyHook, HookRole.Send },
        { HookBackend.SafetyHook, HookRole.Receive },
    };

    public static TheoryData<HookBackend> Backends => new() { HookBackend.Reloaded, HookBackend.SafetyHook };

    [Xunit.Theory]
    [MemberData(nameof(BackendsAndRoles))]
    public void AFilterThatWillNotArm_RefusesTheStart_WithBothFiltersDown(HookBackend backend, HookRole role)
    {
        Game.Backend = backend;
        Game.RefuseToEnableHook(role);
        Assert.NotNull(Plugin.ClickStart(SimZone.Top) ?? RunAndReadRefusal());
        Assert.DoesNotContain(Game.NativeLoads, l => !VirtualGame.IsInn(l.Territory));
        Assert.False(Game.SendFilterUp, Game.Report());
        Assert.False(Game.ReceiveFilterUp, Game.Report());
        Assert.False(Plugin.ScenarioActive);
        AssertClean();
    }

    // The send side came up, the receive side would not, and the send side then would not come
    // back down: the client's packets would be held with no session to ever release them.
    [Xunit.Theory]
    [MemberData(nameof(Backends))]
    public void AHalfArmedFilterThatWillNotComeBackDown_StopsTheGame(HookBackend backend)
    {
        Game.Backend = backend;
        Game.RefuseToEnableHook(HookRole.Receive);
        Game.RefuseToDisableHook(HookRole.Send);
        Plugin.ClickStart(SimZone.Top);
        Game.RunFor(0.5);
        AssertStoppedWithTheFirewallUp("would not come back down");
        Assert.DoesNotContain(Game.NativeLoads, l => !VirtualGame.IsInn(l.Territory));
    }

    [Xunit.Theory]
    [MemberData(nameof(BackendsAndRoles))]
    public void AFilterThatWillNotComeDownAtTheLift_StopsTheGameWithTheSendFilterUp(HookBackend backend, HookRole role)
    {
        Game.Backend = backend;
        Enter();
        Game.RefuseToDisableHook(role);
        Leave(4);
        AssertStoppedWithTheFirewallUp("would not come down");
    }

    [Xunit.Theory]
    [MemberData(nameof(BackendsAndRoles))]
    public void AFilterThatWillNotComeDownOnUnload_StopsTheGameWithTheSendFilterUp(HookBackend backend, HookRole role)
    {
        Game.Backend = backend;
        Enter();
        Game.RunFor(2);
        Game.RefuseToDisableHook(role);
        Plugin.Unload();
        AssertStoppedWithTheFirewallUp("unloaded");
    }

    [Xunit.Theory]
    [MemberData(nameof(Backends))]
    public void AHoldThatWillNotArm_HoldsNothing_AndBlocksNothing(HookBackend backend)
    {
        Game.Backend = backend;
        Game.RefuseToEnableHook(HookRole.Send);
        Plugin.Zone.HoldSendFirewall(true);
        Assert.False(Game.SendFilterUp);
        Assert.False(ZoneControlProbe.SendHoldActive(Plugin.Zone));
        Game.AllowHookChanges();
        Enter();
        Leave();
        AssertBackInTheInnWithTheFirewallDown();
    }

    // A hold that would not release is still a hold: starts wait on it, and a later release
    // finishes the job.
    [Xunit.Theory]
    [MemberData(nameof(Backends))]
    public void AHoldThatWillNotRelease_StaysAHold_UntilItCan(HookBackend backend)
    {
        Game.Backend = backend;
        Plugin.Zone.HoldSendFirewall(true);
        Game.RunFor(0.5);
        Game.RefuseToDisableHook(HookRole.Send);
        Plugin.Zone.HoldSendFirewall(false);
        Assert.True(Game.SendFilterUp);
        Assert.True(ZoneControlProbe.SendHoldActive(Plugin.Zone));
        Assert.Null(Plugin.ClickStart(SimZone.Top));
        Game.RunFor(2);
        Assert.False(Plugin.ScenarioActive, "A stay started under a hold that never released.");
        Game.AllowHookChanges();
        Plugin.Zone.HoldSendFirewall(false);
        Assert.False(Game.SendFilterUp);
        Game.RunFor(1);
        Assert.True(Plugin.ScenarioActive, Game.Report());
        Leave();
        AssertBackInTheInnWithTheFirewallDown();
    }

    private string? RunAndReadRefusal()
    {
        Game.RunFor(0.5);
        return Plugin.LastRefusal;
    }
}
