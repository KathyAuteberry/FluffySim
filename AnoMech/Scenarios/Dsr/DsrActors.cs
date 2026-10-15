using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr;

public static class DsrActors
{
    public static EnemySpawnConfig Config(uint baseId, Placement placement) => baseId switch
    {
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
    private static EnemySpawnConfig Create(uint id, uint name, uint model, NpcAppearance appearance, Placement placement, byte flags = 0)
        => new(id, name, Level: 90, Targetable: id == 12604, EnemyList: EnemyListMode.OnlyWhenVisible,
            Placement: placement, ModelCharaId: model, InitialModeAttributeFlags: flags, Appearance: appearance);
}
