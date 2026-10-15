using System.Linq;
using AnoMech.SafetyTests.Harness;
using FFXIVClientStructs.FFXIV.Client.Game;
using Xunit;
using Assert = Xunit.Assert;

namespace AnoMech.SafetyTests.Firewall;

public unsafe class NativePracticeIntegrationTests : FirewallTestBase
{
    [Fact]
    public void SimulationSequenceNumbersAreRestoredBeforeTheFilterDrops()
    {
        var am=ActionManager.Instance();
        am->LastUsedActionSequence=am->LastHandledActionSequence=27;
        Game.RunFor(3);
        Enter();
        am->LastUsedActionSequence=am->LastHandledActionSequence=48;
        Game.RunFor(2);
        Leave();
        AssertBackInTheInnWithTheFirewallDown();
        Assert.Equal((ushort)27,am->LastUsedActionSequence);
        Assert.Equal((ushort)27,am->LastHandledActionSequence);
        AssertNothingLeaked();
    }

    [Fact]
    public void PreexistingLongCooldownContinuesWhileThePracticeRecastIsCleared()
    {
        var recast=ActionManager.Instance()->GetRecastGroupDetail(17);
        *recast=new RecastDetail{IsActive=true,ActionId=43,Elapsed=5,Total=300};
        var capturedAt=Game.Now;
        Enter();
        recast->IsActive=false;
        recast->Elapsed=0;
        Game.RunFor(3);
        Leave();
        AssertBackInTheInnWithTheFirewallDown();
        Assert.True(recast->IsActive);
        Assert.Equal(43u,recast->ActionId);
        var liftedAt=double.Parse(Game.HookHistory.Last(h=>h.Contains("send off",System.StringComparison.OrdinalIgnoreCase)).Split(' ')[0],System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(System.Math.Abs(recast->Elapsed-(5+liftedAt-capturedAt))<.05, $"elapsed={recast->Elapsed}, capture={capturedAt}, lift={liftedAt}");
        Assert.Equal(300f,recast->Total);
    }

    [Fact]
    public void CooldownThatNaturallyExpiresIsNotRestartedByThePracticeRun()
    {
        var recast=ActionManager.Instance()->GetRecastGroupDetail(17);
        *recast=new RecastDetail{IsActive=true,ActionId=43,Elapsed=5,Total=7};
        Enter();
        recast->Elapsed=0;
        recast->Total=300;
        Game.RunFor(3);
        Leave();
        AssertBackInTheInnWithTheFirewallDown();
        Assert.False(recast->IsActive);
    }

    [Fact]
    public void ReadinessBeforeEntryIsRestoredAfterAClientLocalAbilityUse()
    {
        var recast=ActionManager.Instance()->GetRecastGroupDetail(17);
        Assert.False(recast->IsActive);
        Enter();
        *recast=new RecastDetail{IsActive=true,ActionId=43,Total=240};
        Game.ClientSends(VirtualGame.ActionRequestOpcode,"local defensive action");
        Leave();
        AssertBackInTheInnWithTheFirewallDown();
        Assert.False(recast->IsActive);
        AssertNothingLeaked();
    }

    [Fact]
    public void QueuedAndGroundTargetActionsAreDiscardedBeforeInnRestoration()
    {
        Enter();
        var am=ActionManager.Instance();
        am->ActionQueued=true;
        am->AreaTargetingActionId=43;
        am->AreaTargetingExecuteAtCursor=true;
        Leave();
        AssertBackInTheInnWithTheFirewallDown();
        Assert.False(am->ActionQueued);
        Assert.Equal(0u,am->AreaTargetingActionId);
        Assert.False(am->AreaTargetingExecuteAtCursor);
        AssertNothingLeaked();
    }
}
