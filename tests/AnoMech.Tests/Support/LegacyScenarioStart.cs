using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;

namespace AnoMech.Tests;

internal static class LegacyScenarioStart
{
    public static void RunScenario(this Game game, LegacyRunScenarioParams p)
    {
        game.ZoneIn(p.Scenario.Phase.Zone, SessionMode.Local);
        game.StartScenario(new RunScenarioParams(p.Scenario, new LocalPartyProvider { RoleOverride = p.Role }, p.Ai, p.Waymark, p.Seed));
    }
}
internal sealed record LegacyRunScenarioParams(IScenario Scenario, PartyRole? Role, int? Ai, int Waymark, int? Seed = null);
