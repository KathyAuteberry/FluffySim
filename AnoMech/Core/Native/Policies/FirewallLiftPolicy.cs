namespace AnoMech.Core.Native.Policies;

internal static class FirewallLiftPolicy
{
    public static string? BlockedReason(bool nativeInnReady, double restoredForSeconds, bool pendingAction)
    {
        if (!nativeInnReady) return "the native inn reload is still pending";
        if (restoredForSeconds < 1) return "the inn reload has not settled for one second";
        if (pendingAction) return "a local action is still pending";
        return null;
    }
}
