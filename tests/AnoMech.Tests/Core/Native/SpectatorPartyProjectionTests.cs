#pragma warning disable CA1416
using System.Runtime.InteropServices;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Implementations;
using AnoMech.Core.Native.Implementations.Interop;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;

namespace AnoMech.Tests;

public unsafe class SpectatorPartyProjectionTests
{
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void EightRoleProjectionReplacesSelfWithoutDisplacingAnyActor(int watched)
    {
        var actors = (BattleChara*)NativeMemory.AllocZeroed((nuint)(8 * sizeof(BattleChara)));
        try
        {
            var group = new GroupManager.Group { MemberCount = 8 };
            var hud = new HudPartyMember[10];
            PartyListNumberArray numbers = default;
            hud[0].EntityId = 0x12345678;
            group.PartyMembers[0].EntityId = 0x12345678;
            var roles = new[] { watched }.Concat(Enumerable.Range(0, 8).Where(r => r != watched)).ToArray();
            for (var role = 0; role < 8; role++)
            {
                var actor = &actors[role];
                actor->EntityId = 0x10FFFE00u + (uint)role;
                actor->ClassJob = PartyPresets.Standard[role].ClassJob;
                actor->Level = 100;
                actor->Health = (uint)(1000 + role);
                actor->MaxHealth = 100000;
                actor->Mana = (uint)(2000 + role);
                actor->MaxMana = 10000;
                actor->TargetableStatus = (ObjectTargetableFlags)1;
                actor->Name[0] = (byte)('A' + role);
                actor->StatusManager.Status[0].StatusId = (ushort)(100 + role);
                actor->StatusManager.Status[0].RemainingTime = 20 + role;
            }
            for (var row = 0; row < 8; row++)
            {
                var role = roles[row];
                SpectatorPartyProjection.PublishMember(&actors[role], ref group.PartyMembers[row],
                    ref hud[row], ref numbers.PartyMembers[row], (byte)row, id => new((uint)(10000 + id), true, 1));
                Assert.That(hud[row].Object == &actors[role], Is.True);
                Assert.That(hud[row].EntityId, Is.EqualTo(actors[role].EntityId));
                Assert.That(group.PartyMembers[row].EntityId, Is.EqualTo(hud[row].EntityId));
                Assert.That(group.PartyMembers[row].ClassJob, Is.EqualTo(actors[role].ClassJob));
                Assert.That(numbers.PartyMembers[row].CurrentHealth, Is.EqualTo(1000 + role));
                Assert.That(numbers.PartyMembers[row].CurrentMana, Is.EqualTo(2000 + role));
                Assert.That(numbers.PartyMembers[row].StatusIconIds[0], Is.EqualTo(10100 + role));
                Assert.That(group.PartyMembers[row].StatusManager.Status[0].StatusId, Is.EqualTo(100 + role));
                Assert.That(hud[row].Index, Is.EqualTo(row));
            }
            SpectatorPartyProjection.PublishCount(ref numbers, 8);
            Assert.That(numbers.PartyListCount, Is.EqualTo(8));
            Assert.That(hud.Take(8).Select(m => m.EntityId), Is.Unique);
            Assert.That(hud.Take(8).Select(m => m.EntityId), Does.Not.Contain(0x12345678u));
            Assert.That(hud[8].EntityId, Is.Zero);
            Assert.That(hud[9].EntityId, Is.Zero);
            Assert.That(hud[0].EntityId, Is.EqualTo(actors[watched].EntityId));
            Assert.That(hud.Take(8).Select(m => m.EntityId), Does.Contain(actors[7].EntityId));
            Assert.That(Enumerable.Range(0, 8).Select(i => group.PartyMembers[i].ClassJob),
                Is.EquivalentTo(PartyPresets.Standard.Select(p => p.ClassJob)));
        }
        finally { NativeMemory.Free(actors); }
    }

    [Test]
    public void RepeatedRefreshHandlesDeathStatusesAndCastingWithoutMutatingActor()
    {
        BattleChara actor = default;
        actor.EntityId = 0x10FFFE00;
        actor.Health = 100;
        actor.TargetableStatus = (ObjectTargetableFlags)1;
        for (var i = 0; i < 15; i++) actor.StatusManager.Status[i].StatusId = (ushort)(100 + i);
        PartyMember group = default;
        HudPartyMember hud = default;
        PartyListNumberArray.PartyListMemberNumberArray numbers = default;
        SpectatorPartyProjection.PublishMember(&actor, ref group, ref hud, ref numbers, 0, id => new(id, false, 1));
        Assert.That(numbers.StatusCount, Is.EqualTo(10));
        actor.Health = 0;
        actor.StatusManager.Status.Clear();
        SpectatorPartyProjection.PublishMember(&actor, ref group, ref hud, ref numbers, 0, id => default);
        Assert.That(numbers.CurrentHealth, Is.Zero);
        Assert.That(numbers.Targetable, Is.True);
        Assert.That(numbers.StatusCount, Is.Zero);
        Assert.That(numbers.StatusIconIds.ToArray(), Has.All.Zero);
        Assert.That(numbers.CastTime, Is.EqualTo(-1));
        Assert.That(actor.EntityId, Is.EqualTo(0x10FFFE00));
        Assert.That(actor.Health, Is.Zero);
    }

    [Test]
    public void ClearingOwnedHudEntriesPreservesForeignPlayerRows()
    {
        var hud = new HudPartyMember[10];
        hud[0].EntityId = 0x12345678;
        hud[0].ContentId = 0x12345678;
        for (var i = 1; i < 9; i++)
        {
            hud[i].EntityId = 0x10FFFE00u + (uint)i;
            hud[i].ContentId = 0xFF00000000000000UL | hud[i].EntityId;
        }
        PartyHud.ClearVirtualHud(hud);
        Assert.That(hud[0].EntityId, Is.EqualTo(0x12345678));
        Assert.That(hud.Skip(1).Select(m => m.EntityId), Has.All.Zero);
    }
    [Test]
    public void PartyCountCannotExpandBeyondTheNativeEightSlots()
    {
        PartyListNumberArray numbers = default;
        numbers.PartyListCount = 8;
        Assert.Throws<ArgumentOutOfRangeException>(() => SpectatorPartyProjection.PublishCount(ref numbers, 9));
        Assert.That(numbers.PartyListCount, Is.EqualTo(8));
    }

}
