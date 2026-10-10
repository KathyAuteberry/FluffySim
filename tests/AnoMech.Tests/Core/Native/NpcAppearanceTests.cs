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
}
