using System.Collections.Generic;

namespace AnoMech.Core.UserActions;

internal static class TankMitigationPractice
{
    public static IReadOnlyList<uint> ActionsForJob(byte job) => job switch
    {
        19 => [36920, 3540, 22, 7531, 7535],
        21 => [36923, 7388, 40, 7531, 7535],
        32 => [36927, 16471, 3634, 7531, 7535],
        37 => [36935, 16160, 16140, 7531, 7535],
        _ => [],
    };
}
