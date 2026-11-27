using System;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.EnemyActions;

// The statuses a tank's limit break 3 leaves on the party: Last Bastion, Land Waker, Dark Force, Gunmetal Soul.
public static class TankLimitBreak
{
    public static readonly ushort[] LevelThreeStatuses = [196, 863, 864, 1931];

    public static bool HasLevelThree(SimCharacter member) => Array.Exists(LevelThreeStatuses, member.HasStatus);

    public static VulnSpec ProtectedByLevelThree(this VulnSpec spec)
    {
        foreach (var status in LevelThreeStatuses) spec = spec.ProtectedBy(status);
        return spec;
    }
}
