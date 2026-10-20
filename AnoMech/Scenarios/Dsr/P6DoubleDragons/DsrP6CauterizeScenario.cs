using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
namespace AnoMech.Scenarios.Dsr.P6DoubleDragons;
public sealed class DsrP6CauterizeScenario : IScenario
{
    private readonly DsrP6DoubleDragonsScenario scenario = new(true);
    public string Name => scenario.Name;
    public bool IsIncomplete => true;
    public bool EnablePhaseBgm => true;
    public float BgmSecondsAtStart => DsrP6DoubleDragonsScenario.SecondHalfStart;
    public IPhase Phase => scenario.Phase;
    public IReadOnlyList<IScenarioAi> AiStrats => scenario.AiStrats;
    public void DrawSettings() => scenario.DrawSettings();
    public object SettingsOverrides => scenario.SettingsOverrides;
    public void Run(SimWorld world, int? selectedAi) => scenario.Run(world, selectedAi);
    public void Tick(float delta, float elapsed) => scenario.Tick(delta, elapsed);
}
