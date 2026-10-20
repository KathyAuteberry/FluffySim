using System.Collections.Generic;
using System.Linq;
using AnoMech.Scenarios.Fru.Upstream.P2LightRampant;
using AnoMech.Scenarios.Fru.Upstream.P3Apocalypse;
using AnoMech.Scenarios.Fru.Upstream.P3UltimateRelativity;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;
using AnoMech.Scenarios.Fru.Upstream.P4DarklitDragonsong;
using AnoMech.Scenarios.Fru.Upstream.P5FulgentBlade;
using AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained;
using AnoMech.Scenarios.Top.P2PartySynergy;
using AnoMech.Scenarios.Top.P5Delta;
using AnoMech.Scenarios.Top.P5Omega;
using AnoMech.Scenarios.Top.P5Sigma;
using AnoMech.Scenarios.Top.P6WaveCannon2;
using AnoMech.Scenarios.Ucob.P5Exaflares;
using AnoMech.Scenarios.Umad;
using AnoMech.Scenarios.Umad.P1TeleTrouncing;
using AnoMech.Scenarios.Umad.P2Forsaken;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3LimitCut;
using AnoMech.Scenarios.Umad.P4KefkaSays;
using AnoMech.Scenarios.Umad.P5Celestriad;
using AnoMech.Scenarios.Umad.P5Exaflares;
using AnoMech.Scenarios.Umad.P5Flood;
using AnoMech.Scenarios.Uwu.UltimatePredation;
using AnoMech.Scenarios.Uwu.UltimateSuppression;

namespace AnoMech.Scenarios;

// Every runnable scenario, in menu order, plus the zone -> phase -> scenario tree derived from it in
// first-appearance order. Fresh instances per Create: a scenario holds its run state.
public sealed class ScenarioCatalog
{
    public IReadOnlyList<IScenario> Scenarios { get; }
    public IReadOnlyList<IZone> Zones { get; }
    private readonly Dictionary<IZone, IReadOnlyList<IPhase>> phasesByZone;
    private readonly Dictionary<IPhase, IReadOnlyList<IScenario>> scenariosByPhase;

    private ScenarioCatalog(IReadOnlyList<IScenario> scenarios)
    {
        Scenarios = scenarios;
        var zones = new List<IZone>();
        var phases = new Dictionary<IZone, List<IPhase>>();
        var byPhase = new Dictionary<IPhase, List<IScenario>>();
        foreach (var scenario in scenarios)
        {
            var phase = scenario.Phase;
            var zone = phase.Zone;
            if (!phases.TryGetValue(zone, out var zonePhases))
            {
                zonePhases = [];
                phases[zone] = zonePhases;
                zones.Add(zone);
            }
            if (!zonePhases.Contains(phase)) zonePhases.Add(phase);
            if (!byPhase.TryGetValue(phase, out var phaseScenarios))
            {
                phaseScenarios = [];
                byPhase[phase] = phaseScenarios;
            }
            phaseScenarios.Add(scenario);
        }
        Zones = zones;
        phasesByZone = phases.ToDictionary(kv => kv.Key, IReadOnlyList<IPhase> (kv) => kv.Value);
        scenariosByPhase = byPhase.ToDictionary(kv => kv.Key, IReadOnlyList<IScenario> (kv) => kv.Value);
    }

    public IReadOnlyList<IPhase> PhasesOf(IZone zone) => phasesByZone[zone];
    public IReadOnlyList<IScenario> ScenariosOf(IPhase phase) => scenariosByPhase[phase];

    public static ScenarioCatalog Create() => new(
    [
        new Fru.FallOfFaith.FruFallOfFaithScenario(),
        new Fru.DiamondDust.FruDiamondDustScenario(),
        new Fru.MirrorMirror.FruMirrorMirrorScenario(),
        new Fru.LightRampant.FruLightRampantScenario(),
        new Fru.UltimateRelativity.FruUltimateRelativityScenario(),
        new Fru.Apocalypse.FruApocalypseScenario(),
        new Fru.Darklit.FruDarklitScenario(),
        new Dsr.P1Knights.DsrP1KnightsScenario(),
        new Dsr.Strength.DsrStrengthScenario(),
        new Dsr.Strength2.DsrStrength2Scenario(),
        new Dsr.Sanctity1.DsrSanctity1Scenario(),
        new Dsr.Sanctity2.DsrSanctity2Scenario(),
        new Dsr.P3Nidhogg.DsrP3NidhoggScenario(),
        new Dsr.P4Eyes.DsrP4EyesScenario(),
        new Dsr.P5Wrath.DsrP5WrathScenario(),
        new Dsr.P5Death.DsrP5DeathScenario(),
        new Dsr.P6DoubleDragons.DsrP6DoubleDragonsScenario(),
        new Dsr.P6DoubleDragons.DsrP6CauterizeScenario(),
        new UmadP1TeleTrouncingScenario(),
        new UmadP2ForsakenScenario(),
        new UmadP3LimitCutScenario(),
        new UmadP3BlackHoleScenario(),
        new UmadP4KefkaSaysScenario(),
        new UmadP5FloodScenario(),
        new UmadP5CelestriadScenario(),
        new UmadP5ExaflaresScenario(),
        new UmadP5ForsakenNull(),
        new FruP2LightRampantScenario(),
        new FruP3UltimateRelativityScenario(),
        new FruP3ApocalypseScenario(),
        new FruP4DarklitDragonsongScenario(),
        new FruP4CrystallizeTimeScenario(),
        new FruP5FulgentBladeScenario(),
        new FruP5ParadiseRegainedScenario(),
        new Fru.Pandora.FruPolarizingStrikesScenario(),
        new Fru.Pandora.FruPandorasBoxScenario(),
        new Fru.Pandora.FruPandoraSequenceScenario(),
        new TopP2PartySynergyScenario(),
        new TopP5DeltaScenario(),
        new TopP5SigmaScenario(),
        new TopP5OmegaScenario(),
        new TopP6WaveCannon2Scenario(),
        new UltimatePredationScenario(),
        new UltimateSuppressionScenario(),
        new UcobP5ExaflaresScenario(),
    ]);
}
