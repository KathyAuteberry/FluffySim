namespace AnoMech.Scenarios;

public static class PhaseMusic
{
    public const ushort Heroes = 921;

    public static ushort TrackFor(IScenario scenario) => scenario.EnablePhaseBgm ? scenario.Bgm ?? scenario.Phase.Bgm : (ushort)0;
}
