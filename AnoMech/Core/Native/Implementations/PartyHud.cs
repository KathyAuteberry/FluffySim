using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Hooking;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using AnoMech.Core.SimObjects;
using AnoMech.Core.Game.Party;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GroupPartyMember = FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.Native.Implementations.Interop;
using AnoMech.Core.Native.Implementations.Pointers;

namespace AnoMech.Core.Native.Implementations;

// Mirrors the sim party into MainGroup the way the server does: an UpdateParty packet whenever
// the roster changes. The _PartyList addon only renders members when MemberCount > 0, and
// resolves each slot's BattleChara by EntityId (registered in CharacterManager; scenarios are
// inn-gated upstream) for status icons and timer text.
//
// The firewall keeps real UpdateParty packets out during a sim, so the pre-sim party captured
// at the first send is what Clear restores.
internal sealed unsafe class PartyHud : IPartyHud, IDisposable
{
    // Reserved display identities never overlap authenticated content IDs.
    private const ulong DoppelContentIdTag = 0xFF00000000000000UL;
    private const long SimPartyId = 0xFFFFFFFF;

    private readonly List<(uint EntityId, byte ClassJob, byte Level)> sentRoster = new();
    private readonly List<(uint EntityId, byte ClassJob, byte Level)> roster = new();
    private bool ownsRoster;
    private UpdatePartyPacket realParty;

    private List<nint> presentedActors = new();
    private bool spectatorPresentation;
    private Hook<AgentHUD.Delegates.Update>? hudUpdateHook;
    private readonly Dictionary<ushort, SpectatorPartyProjection.StatusIcon> statusIcons = new();
    private string? lastPresentationTrace;
    private int presentationTraceChanges;

    public PartyHud() => Plugin.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "_PartyList", BeforePartyUpdate);

    public void Dispose()
    {
        Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "_PartyList", BeforePartyUpdate);
        Clear();
        hudUpdateHook?.Dispose();
    }

    private void UpdateHudDetour(AgentHUD* hud, uint frameCount)
    {
        hudUpdateHook!.Original(hud, frameCount);
        if (!spectatorPresentation || hud != AgentHUD.Instance()) return;
        try { PublishSpectatorPresentation(); }
        catch (Exception e)
        {
            DiagnosticLog.Warn($"[PartyHud] Spectator presentation failed; restoring the authentic roster: {e}");
            try { Clear(); }
            catch (Exception cleanup) { DiagnosticLog.Warn($"[PartyHud] Presentation restoration failed: {cleanup}"); }
        }
    }

    private void BeforePartyUpdate(AddonEvent type, AddonArgs args)
    {
        if (spectatorPresentation) PublishSpectatorPresentation();
        else PublishOfflinePresentation();
    }

    internal static void PublishHudMember(ref HudPartyMember target, BattleChara* actor, ref GroupPartyMember member, byte index)
    {
        target.Object = actor;
        target.EntityId = member.EntityId;
        target.ContentId = member.ContentId;
        target.Index = index;
        fixed (byte* name = member.Name) target.Name = name;
    }

    private void PublishOfflinePresentation()
    {
        if (!AnoMech.Offline.OfflineSession.InWorld) return;
        if (!ownsRoster) return;
        var group = GroupManager.Instance();
        var agent = AgentHUD.Instance();
        var stage = AtkStage.Instance();
        if (group == null || agent == null || stage == null) return;
        var data = stage->GetStringArrayData(StringArrayType.PartyList);
        var strings = PartyListStringArray.Instance();
        var count = Math.Min(presentedActors.Count, (int)group->MainGroup.MemberCount);
        var trace = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            var actor = (BattleChara*)presentedActors[i];
            ref var member = ref group->MainGroup.PartyMembers[i];
            if (actor == null || actor->EntityId != member.EntityId) return;
            var before = agent->PartyMembers[i];
            var beforeName = AnoMech.Offline.OfflineProbe.TraceEnvironment ? before.Name.ToString() : "";
            var displayed = !AnoMech.Offline.OfflineProbe.TraceEnvironment || strings == null ? "unavailable" : strings->Members[i].Name.ToString();
            PublishHudMember(ref agent->PartyMembers[i], actor, ref member, (byte)i);
            if (data != null && strings != null)
            {
                var nameIndex = NameIndex(strings, i);
                if (nameIndex >= 0 && nameIndex < data->Size)
                    data->SetValue(nameIndex, agent->PartyMembers[i].Name, true, true, false);
            }
            if (AnoMech.Offline.OfflineProbe.TraceEnvironment)
            {
                var api = i < Plugin.PartyList.Length ? Plugin.PartyList[i] : null;
                trace.AppendLine($"slot={i} actor=0x{actor->EntityId:X} roster=0x{member.EntityId:X} actorContent=0x{actor->ContentId:X} rosterContent=0x{member.ContentId:X} contentMatch={actor->ContentId == member.ContentId} name={agent->PartyMembers[i].Name} world={member.HomeWorld}/{actor->CurrentWorld} job={member.ClassJob} HUD-before={beforeName}/0x{before.EntityId:X} display-before={displayed} Dalamud={api?.Name.TextValue}/0x{api?.EntityId:X} associated={api?.GameObject != null}");
            }
        }
        agent->PartyMemberCount = (short)count;
        if (AnoMech.Offline.OfflineProbe.TraceEnvironment && presentationTraceChanges < 24)
        {
            var table = trace.ToString();
            if (table != lastPresentationTrace)
            {
                DiagnosticLog.Info($"[OfflinePartyPresentation] monotonic={System.Diagnostics.Stopwatch.GetTimestamp()} count={count} source=PartyHud/PreRequestedUpdate\n{table}");
                lastPresentationTrace = table;
                presentationTraceChanges++;
            }
        }
    }

    public void Refresh(SimParty party)
    {
        if (party.AllMembers().Count() < 2) return;

        var gm = GroupManager.Instance();
        if (gm == null) return;

        spectatorPresentation = party.WatchedRole is not null;
        if (spectatorPresentation && hudUpdateHook is null && AgentHUD.Instance() != null)
        {
            hudUpdateHook = Plugin.GameInterop.HookFromAddress<AgentHUD.Delegates.Update>(
                (nint)AgentHUD.Instance()->VirtualTable->Update, UpdateHudDetour);
            hudUpdateHook.Enable();
        }
        var members = OrderedMembers(party);
        presentedActors = members;
        roster.Clear();
        foreach (var p in members)
        {
            var bc = (BattleChara*)p;
            roster.Add((((GameObject*)bc)->EntityId, bc->ClassJob, bc->Level));
        }

        if (!ownsRoster || !roster.SequenceEqual(sentRoster) || !MatchesNativeParty(ref gm->MainGroup, roster))
        {
            if (!ownsRoster)
            {
                realParty = SnapshotPacket(ref gm->MainGroup);
                ownsRoster = true;
            }
            SendRoster(members, realParty);
            var ui = FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkUnitManager.Instance();
            var frames = ui == null ? null : ui->GetAddonByName("_PartyList");
            AnoMech.Core.DiagnosticLog.Info($"[PartyHud] Local roster requested={members.Count}, native={gm->MainGroup.MemberCount}, party frames={(frames == null ? "absent" : frames->IsVisible ? "visible" : "hidden")}.");
            sentRoster.Clear();
            sentRoster.AddRange(roster);
        }
        if (spectatorPresentation) PublishSpectatorPresentation();
        else PublishOfflinePresentation();
    }

    private SpectatorPartyProjection.StatusIcon StatusIcon(ushort id)
    {
        if (statusIcons.TryGetValue(id, out var icon)) return icon;
        var sheet = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>();
        icon = sheet.TryGetRow(id, out var row) ? new(row.Icon, row.CanDispel, row.MaxStacks) : default;
        statusIcons[id] = icon;
        return icon;
    }

    private void PublishSpectatorPresentation()
    {
        if (!ownsRoster || !Natives.Zone.IsActive || presentedActors.Count != 8) return;
        var gm = GroupManager.Instance();
        var hud = AgentHUD.Instance();
        var stage = AtkStage.Instance();
        if (gm == null) return;
        for (var i = 0; i < 8; i++)
            SpectatorPartyProjection.PublishGroupMember((BattleChara*)presentedActors[i], ref gm->MainGroup.PartyMembers[i]);
        gm->MainGroup.MemberCount = 8;
        if (hud == null || stage == null) return;
        var numberData = stage->GetNumberArrayData(NumberArrayType.PartyList);
        var stringData = stage->GetStringArrayData(StringArrayType.PartyList);
        if (numberData == null || numberData->IntArray == null || numberData->Size < sizeof(PartyListNumberArray) / sizeof(int)
            || stringData == null || stringData->StringArray == null || stringData->Size < sizeof(PartyListStringArray) / sizeof(nint)) return;
        var numbers = (PartyListNumberArray*)numberData->IntArray;
        var strings = (PartyListStringArray*)stringData->StringArray;
        for (var i = 0; i < 8; i++)
        {
            var actor = (BattleChara*)presentedActors[i];
            if (actor == null) return;
            SpectatorPartyProjection.PublishMember(actor, ref gm->MainGroup.PartyMembers[i],
                ref hud->PartyMembers[i], ref numbers->PartyMembers[i], (byte)i, StatusIcon);
            for (var field = 0; field < strings->Members[i].Data.Length; field++)
                SetPartyString(stringData, strings, i, field, "");
            SetPartyString(stringData, strings, i, 0, (i + 1).ToString());
            SetPartyString(stringData, strings, i, 1, hud->PartyMembers[i].Name.ToString());
            SetPartyString(stringData, strings, i, 2, actor->CastInfo.IsCasting ? Natives.Data.ActionName(actor->CastInfo.ActionId) : "");
            var statusSlot = 0;
            foreach (var status in actor->StatusManager.Status)
            {
                if (status.StatusId == 0 || StatusIcon(status.StatusId).Icon == 0) continue;
                SetPartyString(stringData, strings, i, 3 + statusSlot++, Natives.Data.StatusName(status.StatusId));
                if (statusSlot == 10) break;
            }
        }
        gm->MainGroup.MemberCount = 8;
        hud->PartyMemberCount = 8;
        for (var i = 8; i < hud->PartyMembers.Length; i++) hud->PartyMembers[i] = default;
        SpectatorPartyProjection.PublishCount(ref *numbers, 8);
        numberData->UpdateState = 1;
    }

    private static void SetPartyString(StringArrayData* data, PartyListStringArray* strings, int row, int field, string text)
    {
        var index = (int)(((nint)Unsafe.AsPointer(ref strings->Members[row].Data[field]) - (nint)strings) / sizeof(nint));
        if (index >= 0 && index < data->Size) data->SetValue(index, text, true, true, false);
    }

    private static void ClearSpectatorArrays()
    {
        var stage = AtkStage.Instance();
        if (stage == null) return;
        var numberData = stage->GetNumberArrayData(NumberArrayType.PartyList);
        var data = stage->GetStringArrayData(StringArrayType.PartyList);
        var numbers = numberData != null && numberData->IntArray != null && numberData->Size >= sizeof(PartyListNumberArray) / sizeof(int)
            ? (PartyListNumberArray*)numberData->IntArray : null;
        var strings = data != null && data->StringArray != null && data->Size >= sizeof(PartyListStringArray) / sizeof(nint)
            ? (PartyListStringArray*)data->StringArray : null;
        if (numbers != null)
        {
            foreach (ref var member in numbers->PartyMembers) member = default;
            SpectatorPartyProjection.PublishCount(ref *numbers, 0);
            numberData->UpdateState = 1;
        }
        if (strings != null && data != null)
            for (var row = 0; row < 8; row++)
                for (var field = 0; field < strings->Members[row].Data.Length; field++)
                    SetPartyString(data, strings, row, field, "");
    }

    internal static bool MatchesNativeParty(ref GroupManager.Group group, List<(uint EntityId, byte ClassJob, byte Level)> expected)
    {
        if (group.MemberCount != expected.Count) return false;
        for (var i = 0; i < expected.Count; i++)
            if (group.PartyMembers[i].EntityId != expected[i].EntityId) return false;
        return true;
    }

    internal static void ClearVirtualHud(Span<HudPartyMember> members)
    {
        foreach (ref var member in members)
            if (OfflinePartyIdentity.Owns(member.EntityId, member.ContentId)) member = default;
    }

    internal static int NameIndex(PartyListStringArray* strings, int slot)
        => (int)(((nint)Unsafe.AsPointer(ref strings->Members[slot].Name) - (nint)strings) / sizeof(nint));

    public void Clear()
    {
        if (ownsRoster && (spectatorPresentation || AnoMech.Offline.OfflineSession.InWorld))
        {
            var hud = AgentHUD.Instance();
            if (hud != null)
            {
                ClearVirtualHud(hud->PartyMembers);
                hud->PartyMemberCount = 0;
                if (spectatorPresentation) ClearSpectatorArrays();
            }
        }
        spectatorPresentation = false;
        presentedActors.Clear();
        lastPresentationTrace = null;
        presentationTraceChanges = 0;
        if (!ownsRoster) return;
        ownsRoster = false;
        sentRoster.Clear();
        var packet = realParty;
        PacketDispatcherPointers.HandleUpdatePartyPacket(0, &packet, 0);
        realParty = default;
    }

    // The native self row must refer to the watched actor before the other seven rows are filled.
    private static List<nint> OrderedMembers(SimParty party)
    {
        var list = new List<nint>();
        foreach (var member in PartyPresentation.Members(party))
        {
            var bc = member.BattleCharaPtr();
            if (bc != null) list.Add((nint)bc);
        }
        return list;
    }

    private void SendRoster(List<nint> members, in UpdatePartyPacket real)
    {
        var packet = new UpdatePartyPacket();
        var entries = (UpdatePartyPacketMember*)&packet;
        var count = Math.Min(members.Count, UpdatePartyPacket.Capacity);
        var game = GameMain.Instance();
        var territory = game == null ? (ushort)0 : (ushort)game->CurrentTerritoryTypeId;
        for (var i = 0; i < count; i++)
            WriteEntry(&entries[i], (BattleChara*)members[i], territory);
        if (AnoMech.Offline.OfflineSession.InWorld)
            CacheOfflineNames(ref packet, count, OfflinePartyIdentity.Store);
        packet.MemberCount = (byte)count;
        packet.LeaderIndex = 0;
        packet.PartyId = real.PartyId != 0 ? real.PartyId : SimPartyId;
        packet.PartyId_2 = real.PartyId_2 != 0 ? real.PartyId_2 : SimPartyId;
        PacketDispatcherPointers.HandleUpdatePartyPacket(0, &packet, 0);
        if (AnoMech.Offline.OfflineSession.InWorld && AnoMech.Offline.OfflineProbe.TraceEnvironment)
        {
            var manager = GroupManager.Instance();
            for (var i = 0; manager != null && i < count; i++)
            {
                var actor = (BattleChara*)members[i];
                ref var member = ref manager->MainGroup.PartyMembers[i];
                var nativeName = member.Name;
                var terminator = nativeName.IndexOf((byte)0);
                var rosterName = System.Text.Encoding.UTF8.GetString(terminator < 0 ? nativeName : nativeName[..terminator]);
                AnoMech.Core.DiagnosticLog.Info($"[OfflineIdentity] slot={i} actor=0x{actor->EntityId:X} roster=0x{member.EntityId:X} actorName={((GameObject*)actor)->GetName()} rosterName={rosterName} actorWorld={actor->HomeWorld} rosterWorld={member.HomeWorld} draw={actor->DrawObject != null}");
            }
        }
    }

    internal static void CacheOfflineNames(ref UpdatePartyPacket packet, int count, Action<uint, ulong, string, ushort, byte> cache)
    {
        fixed (UpdatePartyPacket* ptr = &packet)
        {
            var entries = (UpdatePartyPacketMember*)ptr;
            for (var i = 0; i < Math.Min(count, UpdatePartyPacket.Capacity); i++)
            {
                var entry = &entries[i];
                var name = new ReadOnlySpan<byte>(entry->Name, UpdatePartyPacketMember.NameLength);
                var end = name.IndexOf((byte)0);
                cache(entry->EntityId, entry->ContentId, System.Text.Encoding.UTF8.GetString(end < 0 ? name : name[..end]), entry->HomeWorld, entry->Sex);
            }
        }
    }

    internal static void WriteEntry(UpdatePartyPacketMember* entry, BattleChara* bc, ushort territory)
    {
        var obj = (GameObject*)bc;
        // The local player keeps its real ids: AgentReadyCheck and other party lookups match on them.
        entry->ContentId = bc->ContentId != 0 ? bc->ContentId : DoppelContentIdTag | obj->EntityId;
        entry->AccountId = bc->AccountId != 0 ? bc->AccountId : DoppelContentIdTag | obj->EntityId;
        entry->EntityId = obj->EntityId;
        entry->PetEntityId = 0xE0000000;
        entry->CompanionEntityId = 0xE0000000;
        entry->CurrentHP = bc->Health;
        entry->MaxHP = bc->MaxHealth;
        entry->CurrentMP = (ushort)bc->Mana;
        entry->MaxMP = (ushort)bc->MaxMana;
        entry->HomeWorld = bc->HomeWorld;
        entry->TerritoryType = territory;
        entry->Valid = 1;
        entry->ClassJob = bc->ClassJob;
        entry->Sex = bc->DrawData.CustomizeData.Sex;
        entry->Level = bc->Level;
        for (var i = 0; i < UpdatePartyPacketMember.NameLength - 1; i++)
        {
            var b = obj->Name[i];
            entry->Name[i] = b;
            if (b == 0) break;
        }
    }

    // The packet the server would send for the party as it stands; all zero when solo, which is
    // what the server sends on leaving a party.
    private static UpdatePartyPacket SnapshotPacket(ref GroupManager.Group grp)
    {
        var packet = new UpdatePartyPacket();
        if (grp.MemberCount == 0) return packet;
        var entries = (UpdatePartyPacketMember*)&packet;
        var count = Math.Min((int)grp.MemberCount, UpdatePartyPacket.Capacity);
        for (var i = 0; i < count; i++)
            SnapshotEntry(&entries[i], ref grp.PartyMembers[i]);
        packet.MemberCount = (byte)count;
        packet.LeaderIndex = (byte)grp.PartyLeaderIndex;
        packet.PartyId = grp.PartyId;
        packet.PartyId_2 = grp.PartyId_2;
        return packet;
    }

    private static void SnapshotEntry(UpdatePartyPacketMember* entry, ref GroupPartyMember slot)
    {
        entry->AccountId = slot.AccountId;
        entry->ContentId = slot.ContentId;
        entry->EntityId = slot.EntityId;
        entry->PetEntityId = slot.PetEntityId;
        entry->CompanionEntityId = slot.CompanionEntityId;
        entry->CurrentHP = slot.CurrentHP;
        entry->MaxHP = slot.MaxHP;
        entry->CurrentMP = slot.CurrentMP;
        entry->MaxMP = slot.MaxMP;
        entry->HomeWorld = slot.HomeWorld;
        entry->TerritoryType = slot.TerritoryType;
        entry->Valid = 1;
        entry->ClassJob = slot.ClassJob;
        entry->Sex = slot.Sex;
        entry->Level = slot.Level;
        entry->SyncedLevel = (byte)((slot.Flags & 0x8) != 0 ? slot.Level : 0);
        for (var i = 0; i < UpdatePartyPacketMember.NameLength - 1; i++)
        {
            var b = slot.Name[i];
            entry->Name[i] = b;
            if (b == 0) break;
        }
        var statuses = (UpdatePartyPacketStatus*)((byte*)entry + UpdatePartyPacketMember.StatusesOffset);
        for (var i = 0; i < UpdatePartyPacketMember.StatusCount; i++)
        {
            ref var s = ref slot.StatusManager.Status[i];
            statuses[i].StatusId = s.StatusId;
            statuses[i].Param = s.Param;
            statuses[i].RemainingTime = s.RemainingTime;
            statuses[i].SourceEntityId = s.SourceObject.ObjectId;
        }
    }
}
