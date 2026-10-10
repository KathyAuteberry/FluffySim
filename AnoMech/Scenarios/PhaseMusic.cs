namespace AnoMech.Scenarios;

public static class PhaseMusic
{
    public const ushort FootstepsInTheSnow = 230;
    public const ushort TheExtreme = 802;
    public const ushort Heroes = 921;

    public static ushort TrackFor(IScenario scenario) => scenario.EnablePhaseBgm ? scenario.Phase.Bgm : (ushort)0;
}
