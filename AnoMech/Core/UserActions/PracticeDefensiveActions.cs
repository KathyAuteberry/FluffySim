using AnoMech.Core.SimObjects;

namespace AnoMech.Core.UserActions;

internal static class PracticeDefensiveActions
{
    public static uint[] RecastsForJob(uint job) => job switch
    {
        19 => [3, 30, 17, 36920, 3540, 22, 3542, 25746, 7382, 7531, 7535, 7538, 7533, 7548],
        21 => [3, 43, 44, 36923, 7388, 40, 3551, 25751, 16464, 7531, 7535, 7538, 7533, 7548],
        32 => [3, 3638, 3636, 36927, 16471, 3634, 7393, 25754, 7531, 7535, 7538, 7533, 7548],
        37 => [3, 16152, 16148, 36935, 16160, 16140, 16161, 25758, 7531, 7535, 7538, 7533, 7548],
        23 or 31 or 38 => [3, 7548, 7551],
        20 or 22 or 30 or 34 or 39 or 41 => [3, 7548],
        _ => [3, 7559],
    };

    public static uint TankAction(uint job) => job switch { 19 => 30, 21 => 43, 32 => 3638, 37 => 16152, _ => 0 };
    public static bool Apply(SimCharacter? player, uint action)
    {
        if (player is null) return false;
        var status = action switch { 30 => 82, 43 => 409, 3638 => 810, 16152 => 1836, 7548 => 1209, 7559 => 160, _ => 0 };
        if (status == 0) return false;
        player.AddStatus((ushort)status, action is 7548 or 7559 ? 6f : 10f);
        return true;
    }
}
