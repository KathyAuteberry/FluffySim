using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeBgm(SimTimeline timeline) : IBgm
{
    public FakeBgm() : this(new SimTimeline()) { }
    public List<(ushort Track, float Offset)> Plays { get; } = [];
    public ushort CurrentTrack { get; private set; }
    public int Releases { get; private set; }
    public void Play(ushort bgmId, float secondsIn = 0f)
    {
        Plays.Add((bgmId, secondsIn));
        CurrentTrack = bgmId;
        timeline.Add("bgm", id: bgmId.ToString(), name: "BGM", detail: $"seconds_in={SimTimeline.F(secondsIn)}");
    }
    public void Sync(float secondsIn) { }
    public void Reset() { if (CurrentTrack != 0) Releases++; CurrentTrack = 0; }
    public void Tick(float deltaSeconds) { }
    public void LogPosition(string label) { }

    public void Dispose() { }
}
