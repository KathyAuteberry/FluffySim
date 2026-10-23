using System;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;

namespace AnoMech.Core.Native.Implementations.Interop;

internal static unsafe class SpectatorPartyProjection
{
    internal readonly record struct StatusIcon(uint Icon, bool CanDispel, byte MaxStacks);

    internal static void PublishMember(BattleChara* actor, ref PartyMember group, ref HudPartyMember hud,
        ref PartyListNumberArray.PartyListMemberNumberArray numbers, byte row, Func<ushort, StatusIcon> statusIcon)
    {
        PublishGroupMember(actor, ref group);
        PublishNumbers(actor, ref numbers, row, statusIcon);
        PartyHud.PublishHudMember(ref hud, actor, ref group, row);
    }

    internal static void PublishGroupMember(BattleChara* actor, ref PartyMember group)
    {
        group.EntityId = actor->EntityId;
        group.ContentId = actor->ContentId != 0 ? actor->ContentId : 0xFF00000000000000UL | actor->EntityId;
        group.AccountId = actor->AccountId != 0 ? actor->AccountId : group.ContentId;
        group.NameOverride = null;
        group.Name.Clear();
        for (var i = 0; i < group.Name.Length - 1 && actor->Name[i] != 0; i++) group.Name[i] = actor->Name[i];
        group.Position = actor->Position;
        group.ClassJob = actor->ClassJob;
        group.Level = actor->Level;
        group.HomeWorld = actor->HomeWorld;
        group.CurrentHP = actor->Health;
        group.MaxHP = actor->MaxHealth;
        group.CurrentMP = (ushort)actor->Mana;
        group.MaxMP = (ushort)actor->MaxMana;
        group.DamageShield = actor->ShieldValue;
        group.StatusManager.NumValidStatuses = actor->StatusManager.NumValidStatuses;
        group.StatusManager.ExtraFlags = actor->StatusManager.ExtraFlags;
        actor->StatusManager.Flags.CopyTo(group.StatusManager.Flags);
        for (var i = 0; i < group.StatusManager.Status.Length; i++)
            group.StatusManager.Status[i] = i < actor->StatusManager.Status.Length ? actor->StatusManager.Status[i] : default;
    }

    private static void PublishNumbers(BattleChara* actor, ref PartyListNumberArray.PartyListMemberNumberArray numbers, byte row, Func<ushort, StatusIcon> statusIcon)
    {
        numbers.EntityId = actor->EntityId;
        numbers.DisplayRow = row;
        numbers.Level = actor->Level;
        numbers.ClassIconId = 62000 + actor->ClassJob;
        numbers.CurrentHealth = (int)actor->Health;
        numbers.MaxHealth = (int)actor->MaxHealth;
        numbers.CurrentMana = (int)actor->Mana;
        numbers.MaxMana = (int)actor->MaxMana;
        numbers.ShieldsPercentage = actor->ShieldValue;
        numbers.Targetable = ((byte)actor->TargetableStatus & 1) != 0;
        numbers.EnmityLevel = 0;
        numbers.EnmityPercent = 0;
        numbers.StatusCount = 0;
        numbers.StatusIconIds.Clear();
        numbers.StatusIsDispellable.Clear();
        foreach (var status in actor->StatusManager.Status)
        {
            if (status.StatusId == 0) continue;
            var icon = statusIcon(status.StatusId);
            if (icon.Icon == 0) continue;
            var slot = numbers.StatusCount++;
            numbers.StatusIconIds[slot] = (int)icon.Icon + Math.Clamp(status.Param & 0xFF, 1, Math.Max(1, (int)icon.MaxStacks)) - 1;
            numbers.StatusIsDispellable[slot] = icon.CanDispel;
            if (numbers.StatusCount == numbers.StatusIconIds.Length) break;
        }
        numbers.CastId = actor->CastInfo.IsCasting ? (int)actor->CastInfo.ActionId : 0;
        // UNVERIFIED: native party cast progress is projected on a 0–100 scale.
        numbers.CastTime = actor->CastInfo.IsCasting && actor->CastInfo.TotalCastTime > 0
            ? (int)Math.Clamp(actor->CastInfo.CurrentCastTime / actor->CastInfo.TotalCastTime * 100, 0, 100) : -1;
    }

    internal static void PublishCount(ref PartyListNumberArray numbers, int count)
    {
        if (count is < 0 or > 8) throw new ArgumentOutOfRangeException(nameof(count));
        numbers.PartyListCount = count;
        numbers.IsVisible = count != 0;
        numbers.PartyHasMembers = count != 0;
        numbers.HideWhenInSoloParty = false;
        numbers.IsCrossRealmParty = false;
        numbers.TrustCount = numbers.ChocoboCount = numbers.PetCount = 0;
        numbers.UsePetSlot = false;
        numbers.EnmityLeaderIndex = -1;
        numbers.HideEnmityLeader = true;
    }
}
