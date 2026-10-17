using AnoMech.Core.Native.Implementations;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Game.Network;

namespace AnoMech.Tests;

public class NpcAppearanceTests
{
    [Test]
    public void OracleAppearancePopulatesOnlyVerifiedBodyAndWeaponFields()
    {
        var common = new CommonSpawnData { ModelChara = 3057, ModelAttributeFlags = 0, AnimationState = 0 };
        BattleCharas.ApplyAppearance(ref common, new NpcAppearance(65537, 4295237459));
        Assert.That(common.EquipmentModelIds[1].Id, Is.EqualTo(1));
        Assert.That(common.EquipmentModelIds[1].Variant, Is.EqualTo(1));
        Assert.That(common.EquipmentModelIds[1].Stain, Is.Zero);
        Assert.That(common.MainhandWeaponModel.Value, Is.EqualTo(4295237459));
        Assert.That(common.EquipmentModelIds[0].Id, Is.Zero);
        Assert.That(common.OffhandWeaponModel.Value, Is.Zero);
        Assert.That(common.ModelChara, Is.EqualTo(3057));
        Assert.That(common.AnimationState, Is.Zero);
    }

    [Test]
    public void AbsentAppearancePreservesExistingSpawnFields()
    {
        var common = new CommonSpawnData();
        common.EquipmentModelIds[1].Id = 7;
        common.MainhandWeaponModel.Value = 88;
        BattleCharas.ApplyAppearance(ref common, null);
        Assert.That(common.EquipmentModelIds[1].Id, Is.EqualTo(7));
        Assert.That(common.MainhandWeaponModel.Value, Is.EqualTo(88));
    }

    [Test]
    public void DemihumanEquipmentIncludesHeadLegsAndShieldWithoutChangingOtherSlots()
    {
        var common = new CommonSpawnData { ModelChara = 3268 };
        common.EquipmentModelIds[2].Id = 9;
        BattleCharas.ApplyAppearance(ref common, new NpcAppearance(131073, 17183473865, 131073, 131073, 17181900901, 0));
        foreach (var slot in new[] { 0, 1, 3 })
        {
            Assert.That(common.EquipmentModelIds[slot].Id, Is.EqualTo(1));
            Assert.That(common.EquipmentModelIds[slot].Variant, Is.EqualTo(2));
        }
        Assert.That(common.EquipmentModelIds[2].Id, Is.EqualTo(9));
        Assert.That(common.OffhandWeaponModel.Value, Is.EqualTo(17181900901));
        Assert.That(common.MainhandWeaponModel.Value, Is.EqualTo(17183473865));
        Assert.That(common.ModelChara, Is.EqualTo(3268));
    }
    [Test]
    public unsafe void UpstreamAppearanceWritesBoundedEquipmentAndCustomizeWithoutChangingModel()
    {
        var common = new CommonSpawnData { ModelChara = 3057, AnimationState = 7 };
        var equipment = Enumerable.Range(1, 12).Select(i => (uint)(i | 2 << 16 | 3 << 24)).ToArray();
        var customize = Enumerable.Range(1, 30).Select(i => (byte)i).ToArray();
        BattleCharas.ApplyAppearance(ref common, new NpcAppearance(
            MainhandWeaponModel: 123, OffhandWeaponModel: 456, Equipment: equipment, Customize: customize));
        for (var slot = 0; slot < 10; slot++)
        {
            Assert.That(common.EquipmentModelIds[slot].Id, Is.EqualTo(slot + 1));
            Assert.That(common.EquipmentModelIds[slot].Variant, Is.EqualTo(2));
            Assert.That(common.EquipmentModelIds[slot].Stain, Is.EqualTo(3));
        }
        var bytes = (byte*)&common.CustomizeData;
        for (var index = 0; index < 26; index++)
            Assert.That(bytes[index], Is.EqualTo(index + 1));
        Assert.That(common.MainhandWeaponModel.Value, Is.EqualTo(123));
        Assert.That(common.OffhandWeaponModel.Value, Is.EqualTo(456));
        Assert.That(common.ModelChara, Is.EqualTo(3057));
        Assert.That(common.AnimationState, Is.EqualTo(7));
    }

}
