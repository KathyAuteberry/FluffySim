using System.Collections.Generic;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game;

public sealed record PausedCharacterStatuses(SimCharacter Character, Placement Pose,
    float NativeRotation, IReadOnlyList<SimStatusSnapshot> Statuses);
