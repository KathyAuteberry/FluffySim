#pragma warning disable CA1416
using AnoMech.Core.Native.Implementations.Interop;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace AnoMech.Tests;

public unsafe class OfflinePartyPresentationTests
{
    [TestCase(true)] [TestCase(false)]
    public void OfflineDoppelWithLoadedButSuppressedDrawObjectFollowsVisibilityPreference(bool hidden)
    {
        BattleChara actor = default;
        DrawObject draw = new() { Flags = 8 };
        actor.EntityId = 0x10FFFE03;
        actor.ObjectKind = ObjectKind.Pc;
        actor.DrawObject = &draw;
        actor.RenderFlags = (VisibilityFlags)PartyModelVisibility.RepresentationSuppressed;
        PartyModelVisibility.Reconcile(&actor, 3, hidden, true);
        Assert.That(draw.IsVisible, Is.EqualTo(!hidden));
        Assert.That((ulong)actor.RenderFlags & PartyModelVisibility.RepresentationSuppressed, Is.Zero);
        PartyModelVisibility.Reconcile(&actor, 3, !hidden, true);
        Assert.That(draw.IsVisible, Is.EqualTo(hidden));
    }

    [Test]
    public void LateDrawObjectAndRepeatedNativeSuppressionRecoverWithoutRecreatingActor()
    {
        BattleChara actor = default;
        actor.EntityId = 0x10FFFE07;
        actor.ObjectKind = ObjectKind.Pc;
        PartyModelVisibility.Reconcile(&actor, 7, false, true);
        Assert.That(actor.DrawObject == null, Is.True);
        DrawObject draw = new() { Flags = 8 };
        actor.DrawObject = &draw;
        for (var i = 0; i < 5; ++i)
        {
            actor.RenderFlags = (VisibilityFlags)PartyModelVisibility.RepresentationSuppressed;
            draw.Flags = 8;
            PartyModelVisibility.Reconcile(&actor, 7, false, true);
            Assert.That(draw.IsVisible, Is.True);
            Assert.That(actor.EntityId, Is.EqualTo(0x10FFFE07));
        }
    }

    [Test]
    public void ClientCullingRemainsAuthoritativeAfterTheModelIsLoaded()
    {
        BattleChara actor = default;
        DrawObject draw = default;
        actor.EntityId = 0x10FFFE03;
        actor.ObjectKind = ObjectKind.Pc;
        actor.DrawObject = &draw;
        PartyModelVisibility.Reconcile(&actor, 3, false, true);
        Assert.That(draw.IsVisible, Is.False);
        Assert.That(draw.Flags, Is.EqualTo(1));
        draw.Flags |= 8;
        PartyModelVisibility.Reconcile(&actor, 3, false, true);
        Assert.That(draw.IsVisible, Is.True);
    }

    [TestCase(false, 3, 0x10FFFE03u, ObjectKind.Pc)]
    [TestCase(true, -1, 0x10FFFE03u, ObjectKind.Pc)]
    [TestCase(true, 3, 0x10FFFE04u, ObjectKind.Pc)]
    [TestCase(true, 3, 0x10FFFE03u, ObjectKind.BattleNpc)]
    public void InnPlayerForeignSlotAndEnemiesKeepNativeSuppression(bool offline, int slot, uint entity, ObjectKind kind)
    {
        BattleChara actor = default;
        DrawObject draw = new() { Flags = 8 };
        actor.EntityId = entity;
        actor.ObjectKind = kind;
        actor.DrawObject = &draw;
        actor.RenderFlags = (VisibilityFlags)PartyModelVisibility.RepresentationSuppressed;
        PartyModelVisibility.Reconcile(&actor, slot, false, offline);
        Assert.That(draw.IsVisible, Is.False);
        Assert.That((ulong)actor.RenderFlags, Is.EqualTo(PartyModelVisibility.RepresentationSuppressed));
    }

    [Test]
    public void OtherNativeHideReasonsAreNeverCleared()
    {
        var flags = PartyModelVisibility.Apply(PartyModelVisibility.RepresentationSuppressed | 0x10000, false, true, true);
        Assert.That(flags, Is.EqualTo(0x10000));
    }
}
