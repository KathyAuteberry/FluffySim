#pragma warning disable CA1416
using AnoMech.Core.Native.Implementations;
using AnoMech.Core.Native.Implementations.Interop;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;

namespace AnoMech.Tests;

public unsafe class OfflineHudPublicationTests
{
    [TestCase(false)] [TestCase(true)]
    public void EveryHudSlotReceivesItsOwnNameAndActorEvenWhenModelsAreHidden(bool hidden)
    {
        var names = new[] {"Practice Player", "Paladin", "White Mage", "Scholar", "Dragoon", "Monk", "Bard", "Black Mage"};
        var ids = new HashSet<uint>();
        var contents = new HashSet<ulong>();
        for (byte i = 0; i < 8; i++)
        {
            BattleChara actor = default;
            actor.EntityId = 0x10FFFE00u + i;
            actor.RenderFlags = (FFXIVClientStructs.FFXIV.Client.Game.Object.VisibilityFlags)(hidden ? 0x802 : 0);
            PartyMember group = default;
            group.EntityId = actor.EntityId;
            group.ContentId = 0xFF00000000000000UL | actor.EntityId;
            fixed (byte* name = group.Name) GameObjectHelper.WriteName(name, 64, names[i]);
            HudPartyMember hud = default;
            PartyHud.PublishHudMember(ref hud, &actor, ref group, i);
            Assert.That(hud.Name.ToString(), Is.EqualTo(names[i]));
            Assert.That(hud.EntityId, Is.EqualTo(actor.EntityId));
            Assert.That(hud.ContentId, Is.EqualTo(group.ContentId));
            Assert.That(hud.Object == &actor, Is.True);
            Assert.That(hud.Index, Is.EqualTo(i));
            Assert.That((ulong)actor.RenderFlags, Is.EqualTo(hidden ? 0x802UL : 0));
            Assert.That(ids.Add(hud.EntityId), Is.True);
            Assert.That(contents.Add(hud.ContentId), Is.True);
        }
    }

    [Test]
    public void NativeNamesUseAllEightDistinctTypedStringArraySlots()
    {
        PartyListStringArray strings = default;
        var offsets = new int[8];
        for (var i = 0; i < 8; i++) offsets[i] = PartyHud.NameIndex(&strings, i);
        Assert.That(offsets, Is.EqualTo(Enumerable.Range(0,8).Select(i=>7+23*i)));
        Assert.That(offsets.Distinct().Count(), Is.EqualTo(8));
        Assert.That(offsets.Max(), Is.LessThan(397));
    }

    [Test]
    public void ExitClearsOnlyVirtualHudPointersBeforeActorDestruction()
    {
        var members = new HudPartyMember[3];
        members[0].EntityId = 0x10FFA001;
        members[0].ContentId = 0x00FFA0FF00000001;
        members[1].EntityId = 0x10FFFE01;
        members[1].ContentId = 0xFF00000010FFFE01;
        members[2].EntityId = 1234;
        members[2].ContentId = 999;
        PartyHud.ClearVirtualHud(members);
        Assert.That(members[0].EntityId, Is.Zero);
        Assert.That(members[1].EntityId, Is.Zero);
        Assert.That(members[2].EntityId, Is.EqualTo(1234));
        Assert.That(members[2].ContentId, Is.EqualTo(999));
    }
}
