using AnoMech.Core.Native.Implementations;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.Strength;
using AnoMech.Scenarios.Dsr.Strength2;
using AnoMech.Scenarios.Fru;
using AnoMech.Scenarios.Fru.DiamondDust;

namespace AnoMech.Tests;

public class BgmOverrideTests
{
    private BgmSceneState? state;
    private List<ushort> plays = null!;
    private int releases;
    private BgmOverride playback = null!;

    [SetUp]
    public void Setup()
    {
        state = new(0, 1238); plays = []; releases = 0;
        playback = new(() => state, track => { plays.Add(track); state = new(track, state!.Value.Territory); },
            () => { releases++; state = new(0, state!.Value.Territory); });
    }

    [Test]
    public void RestartSamePhaseIsIdempotentAndSwitchUsesNewTrack()
    {
        Assert.That(playback.Play(230), Is.True);
        Assert.That(playback.Play(230), Is.True);
        Assert.That(playback.Play(802), Is.True);
        Assert.That(plays, Is.EqualTo(new ushort[] { 230, 802 }));
        playback.Reset(); playback.Reset();
        Assert.That(releases, Is.EqualTo(1));
        Assert.That(playback.Play(921), Is.True);
        playback.Reset();
        Assert.That(releases, Is.EqualTo(2));
    }

    [Test]
    public void ExternalControllerRetainsItsSelectionOnPollAndCleanup()
    {
        playback.Play(230); state = new(999, 1238);
        Assert.That(playback.Poll(), Is.False);
        Assert.That(playback.Play(230), Is.False);
        playback.Reset();
        Assert.That(releases, Is.Zero);
        Assert.That(state!.Value.Track, Is.EqualTo(999));
        Assert.That(playback.Play(921), Is.False);
        Assert.That(plays, Has.Count.EqualTo(1));
    }

    [Test]
    public void TerritoryChangeReleasesOnlyOwnedSlotAndNeverReplaysPreviousMusic()
    {
        playback.Play(802); state = new(802, 155);
        Assert.That(playback.Poll(), Is.False);
        Assert.That(releases, Is.EqualTo(1));
        Assert.That(playback.Play(802), Is.False);
        playback.Reset();
        Assert.That(plays, Is.EqualTo(new ushort[] { 802 }));
    }

    [Test]
    public void TerritoryChangeWithNewDirectorMusicDoesNotResetIt()
    {
        playback.Play(921); state = new(5, 155);
        playback.Reset();
        Assert.That(releases, Is.Zero);
        Assert.That(state!.Value.Track, Is.EqualTo(5));
    }

    [Test]
    public void UnavailableNativeSystemCannotAcquireOrRelease()
    {
        state = null;
        Assert.That(playback.Play(230), Is.False);
        playback.Reset();
        Assert.That(plays, Is.Empty); Assert.That(releases, Is.Zero);
    }

    [Test]
    public void DisappearingSystemDropsOwnershipWithoutStaleWrites()
    {
        playback.Play(230); state = null;
        Assert.That(playback.Poll(), Is.False);
        playback.Reset();
        Assert.That(releases, Is.Zero);
    }

    [Test]
    public void DiagnosticsAndIncompleteScenariosHaveNoMusic()
    {
        foreach (var scenario in new IScenario[] { new DsrArenaDiagnosticScenario(), new FruArenaDiagnosticScenario(), new FruP3ArenaDiagnosticScenario(), new FruDiamondDustScenario() })
            Assert.That(PhaseMusic.TrackFor(scenario), Is.Zero);
        Assert.That(PhaseMusic.TrackFor(new DsrStrengthScenario()), Is.EqualTo(921));
        Assert.That(PhaseMusic.TrackFor(new DsrStrength2Scenario()), Is.EqualTo(921));
        Assert.That(FruZone.P2.Bgm, Is.EqualTo(230));
        Assert.That(FruZone.P3.Bgm, Is.EqualTo(802));
    }

    [Test]
    public void ExistingSuppressionPreferenceSurvivesAndNewConfigsEnableMusic()
    {
        Assert.That(new Configuration().SuppressBgm, Is.False);
        Assert.That(Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>("{\"SuppressBgm\":true}")!.SuppressBgm, Is.True);
    }
}
