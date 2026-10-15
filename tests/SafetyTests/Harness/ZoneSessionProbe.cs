using System;
using System.Reflection;
using AnoMech.Core.Map;
using AnoMech.Core.Native.Implementations;

namespace AnoMech.SafetyTests.Harness;

// Read access to ZoneControl's private state, for judging the harness's invariants. A renamed
// field fails loudly here rather than silently weakening a check.
internal static class ZoneControlProbe
{
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static FieldInfo Field(string name)
        => typeof(ZoneControl).GetField(name, Any)
           ?? throw new InvalidOperationException($"ZoneControl no longer has a field '{name}'; update ZoneControlProbe.");

    public static bool GuardArmed(ZoneControl z) => (bool)Field("guardArmed").GetValue(z)!;
    public static bool SendHoldActive(ZoneControl z) => (bool)Field("sendHoldActive").GetValue(z)!;
    public static string? TripReason(ZoneControl z) => (string?)Field("tripReason").GetValue(z);
    public static string? PendingLift(ZoneControl z) => (string?)Field("pendingLift").GetValue(z);
    public static int StayId(ZoneControl z) => (int)Field("stayId").GetValue(z)!;

    // The start gate's timers are static and outlive a ZoneControl; every test starts clean.
    public static void ResetStatics()
    {
        foreach (var f in typeof(ZoneControl).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (f.IsLiteral || f.IsInitOnly) continue;
            f.SetValue(null, f.FieldType.IsValueType ? Activator.CreateInstance(f.FieldType) : null);
        }
    }
}
