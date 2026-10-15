using AnoMech.Core.SimObjects;

namespace AnoMech.Core.UserActions;

// Movement utility buttons remain usable when optional combat-action simulation is disabled.
internal static class AntiKnockbackHandler
{
    public static void Apply(SimCharacter? character, uint actionId)
    {
        if (character is null) return;
        if (actionId == 7548) character.AddStatus(1209,6f);
        if (actionId == 7559) character.AddStatus(160,6f);
    }
}
