#pragma warning disable CA1416
using AnoMech.Core.Native.Implementations;
using FFXIVClientStructs.FFXIV.Client.Game.Group;

namespace AnoMech.Tests;

public class PartyHudLifecycleTests
{
    [Test]
    public void NativeRosterLossIsDetectedEvenWhenTheDesiredRosterHasNotChanged()
    {
        GroupManager.Group native = default;
        var expected = Enumerable.Range(0, 8).Select(i => (EntityId: 0x10FFFE00u + (uint)i, ClassJob: (byte)19, Level: (byte)100)).ToList();
        Assert.That(PartyHud.MatchesNativeParty(ref native, expected), Is.False);
        native.MemberCount = 8;
        for (var i = 0; i < 8; i++) native.PartyMembers[i].EntityId = expected[i].EntityId;
        Assert.That(PartyHud.MatchesNativeParty(ref native, expected), Is.True);
        native.MemberCount = 0;
        Assert.That(PartyHud.MatchesNativeParty(ref native, expected), Is.False);
        native.MemberCount = 8;
        native.PartyMembers[3].EntityId = 0xE0000000;
        Assert.That(PartyHud.MatchesNativeParty(ref native, expected), Is.False);
    }
}
