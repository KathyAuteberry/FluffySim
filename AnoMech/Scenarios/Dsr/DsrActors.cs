using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr;

public static class DsrActors
{
    public static EnemySpawnConfig Config(uint baseId, Placement placement) => baseId switch
    {
        13117 => new(baseId, 1455, Level: 90, Targetable: false, EnemyList: EnemyListMode.Never, Placement: placement, IsHostile: false, ModelCharaId: 3289, Appearance: new(MainhandWeaponModel: 21474902218UL, OffhandWeaponModel: 8590524517UL, Equipment: [0, 10223631, 11927563, 4587535, 10485775, 0, 0, 0, 0, 0], Customize: [2, 0, 1, 75, 3, 2, 2, 1, 0, 146, 154, 25, 96, 6, 2, 146, 2, 5, 3, 0, 0, 50, 1, 50, 0, 0])),
        13118 => new(baseId, 11313, Level: 90, Targetable: false, EnemyList: EnemyListMode.Never, Placement: placement, IsHostile: false, ModelCharaId: 3290, Appearance: new(MainhandWeaponModel: 85899412357UL, OffhandWeaponModel: 0UL, Equipment: [0, 74620, 393336, 327799, 393336, 0, 0, 0, 0, 0], Customize: [2, 1, 1, 50, 4, 202, 202, 1, 88, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 128, 195, 50, 1, 50, 0, 0])),
        13119 => new(baseId, 11312, Level: 90, Targetable: false, EnemyList: EnemyListMode.Never, Placement: placement, IsHostile: false, ModelCharaId: 3291, Appearance: new(MainhandWeaponModel: 4295040841UL, OffhandWeaponModel: 0UL, Equipment: [140177, 74651, 74651, 458788, 458788, 0, 0, 0, 0, 0], Customize: [2, 0, 1, 50, 3, 208, 5, 1, 74, 6, 161, 0, 0, 1, 2, 6, 129, 4, 0, 1, 0, 50, 1, 50, 0, 1])),
        13769 => new(baseId, 4130, Level: 90, Targetable: false, EnemyList: EnemyListMode.Never, Placement: placement, IsHostile: false, ModelCharaId: 3499, Appearance: new(MainhandWeaponModel: 4295034535UL, OffhandWeaponModel: 0UL, Equipment: [74639, 74639, 75439, 74639, 75439, 0, 0, 0, 0, 0], Customize: [2, 0, 4, 50, 3, 201, 201, 1, 0, 146, 0, 0, 0, 0, 0, 146, 0, 0, 0, 0, 0, 50, 1, 50, 0, 0])),
        12611 => Create(baseId, 3632, 3269, new(131073, 4297590237, AnimationState: 1), placement, 16),
        12592 => Create(baseId, 3633, 3268, new(65537, 12887197149, 65537, 65537), placement),
        13070 => Create(baseId, 4385, 3501, new(0, 0), placement),
        12604 => Create(baseId, 3632, 3269, new(65537, 4297590237, AnimationState: 1), placement, 16),
        12633 => Create(baseId, 3636, 3268, new(65539, 17181180405, 65539, 458753), placement),
        12634 => Create(baseId, 3637, 3268, new(131075, 34360066549, 131075, 524289), placement),
        12635 => Create(baseId, 3638, 3268, new(196611, 12885492213, 196611, 589825), placement),
        12637 => Create(baseId, 3641, 3268, new(196610, 8593080721, 196610, 393217), placement),
        12601 => Create(baseId, 3634, 3268, new(131073, 17183473865, 131073, 131073, 17181900901), placement),
        12632 => Create(baseId, 3635, 3268, new(196609, 17183604937, 196609, 196609, 17181900901), placement),
        12602 => Create(baseId, 3639, 3268, new(65538, 21474902420, 65538, 262145), placement),
        12603 => Create(baseId, 3642, 3268, new(65540, 17180787689, 65540, 655361), placement),
        12638 => Create(baseId, 3643, 3268, new(131076, 12887261985, 131076, 720897), placement),
        12639 => Create(baseId, 3644, 3268, new(196612, 17181311777, 196612, 786433), placement),
        12591 => new(baseId, 0, Level: 90, EnemyList: EnemyListMode.Never, Placement: placement, ModelCharaId: 629),
        12636 => Create(baseId, 3640, 3268, new(131074, 17181245841, 131074, 327681), placement),
        _ => throw new System.ArgumentOutOfRangeException(nameof(baseId)),
    };
    public static EnemySpawnConfig WithNativeAppearance(EnemySpawnConfig config)
    {
        if (config.BNpcBaseId is not (12592 or 12601 or 12602 or 12603 or 12604 or 12611 or 12632 or 12633 or 12634 or 12635 or 12636 or 12637 or 12638 or 12639 or 13117 or 13118 or 13119 or 13769)) return config;
        var native = Config(config.BNpcBaseId, config.Placement);
        return config with { ModelCharaId = native.ModelCharaId, Appearance = native.Appearance, InitialModeAttributeFlags = native.InitialModeAttributeFlags };
    }

    private static EnemySpawnConfig Create(uint id, uint name, uint model, NpcAppearance appearance, Placement placement, byte flags = 0)
        => new(id, name, Level: 90, Targetable: id == 12604, EnemyList: EnemyListMode.OnlyWhenVisible,
            Placement: placement, ModelCharaId: model, InitialModeAttributeFlags: flags, Appearance: appearance);
}
