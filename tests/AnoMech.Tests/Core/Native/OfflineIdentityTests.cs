#pragma warning disable CA1416
using AnoMech.Offline;
using AnoMech.Core.Native.Implementations;
using AnoMech.Core.Native.Implementations.Interop;
using AnoMech.Core.Native.Implementations.Pointers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using Newtonsoft.Json;
using System.Text;

namespace AnoMech.Tests;

public unsafe class OfflineIdentityTests
{
    [TestCase((ushort)0, true, false)]
    [TestCase((ushort)65535, false, false)]
    [TestCase((ushort)21, true, true)]
    public void MissingOrUnresolvableWorldIsRejectedBeforeBootstrap(ushort world, bool resolves, bool accepted)
        => Assert.That(OfflineIdentity.HasHomeWorld(world, _ => resolves), Is.EqualTo(accepted));

    [Test]
    public void OldConfigurationRemainsReadableButMustChooseAWorld()
    {
        var old = JsonConvert.DeserializeObject<OfflineChoice>("{\"Name\":\"Practice Player\"}")!;
        Assert.That(old.Name, Is.EqualTo("Practice Player"));
        Assert.That(old.HomeWorld, Is.Zero);
        old.HomeWorld = 21;
        var restored = JsonConvert.DeserializeObject<OfflineChoice>(JsonConvert.SerializeObject(old))!;
        Assert.That(restored.HomeWorld, Is.EqualTo(21));
        Assert.That(restored.Name, Is.EqualTo(old.Name));
    }

    [Test]
    public void PlayerSpawnHasIdentityBeforeItCanEnterTheObjectTable()
    {
        SpawnPlayerPacket packet = default;
        OfflineIdentity.ConfigureSpawn(ref packet, "Practice Player", 21);
        Assert.That(packet.HomeWorldId, Is.EqualTo(21));
        Assert.That(packet.CurrentWorldId, Is.EqualTo(21));
        Assert.That(Encoding.UTF8.GetString(packet.Common.Name[..packet.Common.Name.IndexOf((byte)0)]), Is.EqualTo("Practice Player"));
        Assert.That(packet.ContentId, Is.Zero, "Display metadata must not invent an authenticated content ID");
    }

    [Test]
    public void AllEightPartyEntriesCarryActorNamesWorldsAndAssociations()
    {
        for (var seat = 0; seat < 8; seat++)
        {
            BattleChara actor = default;
            var obj = (GameObject*)&actor;
            actor.EntityId = (uint)(0x10FFFE00 + seat);
            actor.HomeWorld = 21;
            actor.ClassJob = (byte)(19 + seat);
            actor.Level = 100;
            actor.Health = actor.MaxHealth = 10000;
            var name = seat == 0 ? "Practice Player" : $"Sim Party {seat}";
            fixed (byte* field = obj->Name) GameObjectHelper.WriteName(field, 64, name);
            UpdatePartyPacketMember entry = default;
            PartyHud.WriteEntry(&entry, &actor, 968);
            Assert.That(Encoding.UTF8.GetString(new ReadOnlySpan<byte>(entry.Name, 32)[..name.Length]), Is.EqualTo(name));
            Assert.That(entry.EntityId, Is.EqualTo(actor.EntityId));
            Assert.That(entry.HomeWorld, Is.EqualTo(21));
            Assert.That(entry.ClassJob, Is.EqualTo(actor.ClassJob));
            Assert.That(entry.Valid, Is.EqualTo(1));
            Assert.That(entry.ContentId, Is.EqualTo(0xFF00000000000000UL | actor.EntityId));
        }
    }
    [Test]
    public void OfflineDisplayCachesArePopulatedWithoutNameRequestsOrServerIds()
    {
        UpdatePartyPacket packet = default;
        var entries = (UpdatePartyPacketMember*)&packet;
        for (var i = 0; i < 8; i++)
        {
            entries[i].EntityId = (uint)(0x10FFFE00 + i);
            entries[i].ContentId = 0xFF00000000000000UL | entries[i].EntityId;
            entries[i].HomeWorld = 21;
            entries[i].Sex = (byte)(i % 2);
            GameObjectHelper.WriteName(entries[i].Name, 32, $"Sim Party {i}");
        }
        var cached = new List<(uint Entity, ulong Content, string Name, ushort World, byte Sex)>();
        PartyHud.CacheOfflineNames(ref packet, 8, (e, c, n, w, s) => cached.Add((e, c, n, w, s)));
        Assert.That(cached.Count, Is.EqualTo(8));
        for (var i = 0; i < 8; i++)
        {
            Assert.That(cached[i].Name, Is.EqualTo($"Sim Party {i}"));
            Assert.That(cached[i].Entity, Is.EqualTo(entries[i].EntityId));
            Assert.That(cached[i].Content, Is.EqualTo(entries[i].ContentId));
            Assert.That(cached[i].World, Is.EqualTo(21));
            Assert.That(cached[i].Sex, Is.EqualTo(i % 2));
        }
    }

    [Test]
    public void RepeatedVirtualRunsReplaceOnlyTheirOwnNameCacheEntries()
    {
        var entities = new NameCache.EntityCacheEntry[3];
        var contents = new NameCache.ContentIdCacheEntry[3];
        uint entity = 0x10FFFE01;
        ulong content = 0xFF00000000000000UL | entity;
        entities[0].EntityId = entity;
        entities[1].EntityId = 1234;
        contents[0].ContentId = content;
        contents[1].ContentId = 123456789;
        OfflinePartyIdentity.Invalidate(entities, contents, 1234, 123456789);
        Assert.That(entities[1].EntityId, Is.EqualTo(1234));
        Assert.That(contents[1].ContentId, Is.EqualTo(123456789));
        OfflinePartyIdentity.Invalidate(entities, contents, entity, content);
        Assert.That(entities[0].EntityId, Is.Zero);
        Assert.That(contents[0].ContentId, Is.Zero);
        Assert.That(entities[1].EntityId, Is.EqualTo(1234));
        Assert.That(contents[1].ContentId, Is.EqualTo(123456789));
        Assert.That(OfflinePartyIdentity.Owns(0x10FFA001, 0x00FFA0FF00000001), Is.True);
    }

}
