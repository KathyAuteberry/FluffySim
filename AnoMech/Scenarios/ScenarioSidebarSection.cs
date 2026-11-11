using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Scenarios;

public sealed record ScenarioSidebarSection(string Title, IReadOnlyList<IZone> Zones)
{
    private const uint AlexanderTerritory = 887;

    internal static IReadOnlyList<ScenarioSidebarSection> Build(IReadOnlyList<IZone> zones)
    {
        var ordered = zones.OrderByDescending(ReleasePatch).ToArray();
        return
        [
            new("FluffySim Native", ordered.Where(IsNative).ToArray()),
            new("Upstream", ordered.Where(zone => !IsNative(zone)).ToArray()),
        ];
    }

    private static bool IsNative(IZone zone)
        => zone is Fru.FruZone or Dsr.DsrZone || zone.TerritoryId == AlexanderTerritory;

    private static int ReleasePatch(IZone zone) => zone switch
    {
        Umad.UmadZone => 751,
        Fru.FruZone or Fru.Upstream.FruZone => 711,
        Top.TopZone => 631,
        Dsr.DsrZone => 611,
        { TerritoryId: AlexanderTerritory } => 511,
        Uwu.UwuZone => 431,
        Ucob.UcobZone => 411,
        _ => 0,
    };
}
