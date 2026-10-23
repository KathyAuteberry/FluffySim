using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NUnit.Framework;

namespace AnoMech.SafetyTests.Contract;

public class SpectatorPartyContractTests
{
    [Test]
    public void DalamudPartyEnumerationReadsTheProjectedMainGroup()
    {
        var party = RealDalamud.Type("Dalamud.dll", "Dalamud.Game.ClientState.Party.PartyList");
        var address = RealDalamud.Method(party, "get_GroupListAddress");
        Assert.That(address.Body.Instructions.Any(i => i.Operand is FieldReference f && f.Name == "MainGroup"), Is.True);
        var length = RealDalamud.Method(party, "get_Length");
        Assert.That(length.Body.Instructions.Any(i => i.Operand is FieldReference f && f.Name == "MemberCount"), Is.True);
    }

    [Test]
    public void InstalledDelvUiUsesTheSameHudIdentityLayoutAndDalamudMembers()
    {
        var path = Environment.GetEnvironmentVariable("FLUFFYSIM_DELVUI_DLL");
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) Assert.Ignore("Set FLUFFYSIM_DELVUI_DLL to check an installed DelvUI without loading it.");
        using var module = ModuleDefinition.ReadModule(path!);
        var manager = module.GetType("DelvUI.Interface.Party.PartyManager");
        Assert.That(manager, Is.Not.Null);
        var map = manager.Methods.Single(m => m.Name == "GetMembersDataMap");
        var hud = RealDalamud.Type("FFXIVClientStructs.dll", "FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentHUD");
        var member = RealDalamud.Type("FFXIVClientStructs.dll", "FFXIVClientStructs.FFXIV.Client.UI.Agent.HudPartyMember");
        var offset = hud.Fields.Single(f => f.Name == "_partyMembers").Offset + member.Fields.Single(f => f.Name == "Name").Offset;
        Assert.That(map.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldc_I4 && i.Operand is int n && n == offset), Is.True,
            "DelvUI raw party records no longer start at the projected HUD Name field.");
        var update = manager.Methods.Single(m => m.Name == "UpdateRegularParty");
        Assert.That(update.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "GetDalamudPartyMember"), Is.True);
    }
}
