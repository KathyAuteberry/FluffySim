using AnoMech.Core.SimObjects;

namespace AnoMech.Core.UserActions;

internal static class PracticeDefensiveActions
{
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
