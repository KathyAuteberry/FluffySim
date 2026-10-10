using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.Strength;
using AnoMech.Scenarios.Dsr.Strength2;
using AnoMech.Scenarios.Fru.LightRampant;
using AnoMech.Scenarios.Fru.UltimateRelativity;
using AnoMech.Core.SimObjects;
using AnoMech.Core.Game.Ai;

namespace AnoMech.Tests;

public class ScenarioMusicTests
{
    [TestCase(typeof(FruLightRampantScenario), (ushort)230)]
    [TestCase(typeof(FruUltimateRelativityScenario), (ushort)802)]
    [TestCase(typeof(DsrStrengthScenario), (ushort)921)]
    [TestCase(typeof(DsrStrength2Scenario), (ushort)921)]
    public void PlayableScenarioStartsPhaseMusicAndCompletionReleasesIt(Type scenario, ushort track)
    {
        var music = new FakeBgm();
        var releasedAtEnd = false;
        var run = ScenarioRun.Execute(scenario, 0, 42, new()
        {
            ConfigureNative = _ => Natives.Bgm = music,
            Probe = p =>
            {
                if (p.Game.HasScenarioSucceeded) releasedAtEnd = music.CurrentTrack == 0;
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(music.Plays, Is.EqualTo(new[] { (track, 0f) }));
        Assert.That(music.CurrentTrack, Is.Zero);
        Assert.That(music.Releases, Is.EqualTo(1));
        Assert.That(releasedAtEnd, Is.True);
    }

    [Test]
    public void DisabledOptionDoesNotStartMusic()
    {
        var music = new FakeBgm();
        var run = ScenarioRun.Execute(typeof(DsrStrength2Scenario), 0, 42, new()
        {
            ConfigureNative = _ => Natives.Bgm = music,
            ScenarioFactory = () => { Plugin.Config.SuppressBgm = true; return new DsrStrength2Scenario(); },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(music.Plays, Is.Empty);
        Assert.That(music.Releases, Is.Zero);
    }

    [Test]
    public void MidMechanicCancellationAndDisposalReleaseMusic()
    {
        foreach (var cancel in new[] { false, true })
        {
            var music = new FakeBgm();
            ScenarioRun.Execute(typeof(DsrStrength2Scenario), 0, 42, new()
            {
                ConfigureNative = _ => Natives.Bgm = music,
                StopAt = 5f,
                Probe = p => { if (cancel && p.Crossed(4f)) p.Game.Reset(); },
                WriteArtifactsOnFailure = false,
            });
            Assert.That(music.Plays, Has.Count.EqualTo(1));
            Assert.That(music.Releases, Is.EqualTo(1));
            Assert.That(music.CurrentTrack, Is.Zero);
        }
    }

    [Test]
    public void FailedInitializationReleasesPreviousOverride()
    {
        var music = new FakeBgm();
        ScenarioRun.Execute(typeof(FailingScenario), 0, 42, new()
        {
            ConfigureNative = _ => { Natives.Bgm = music; music.Play(230); },
            ScenarioFactory = () => new FailingScenario(),
            WriteArtifactsOnFailure = false,
        });
        Assert.That(music.Releases, Is.EqualTo(1));
        Assert.That(music.CurrentTrack, Is.Zero);
        Assert.That(music.Plays, Has.Count.EqualTo(1));
    }

    private sealed class FailingScenario : IScenario
    {
        public string Name => "Failed initialization";
        public IPhase Phase => DsrZone.P2;
        public IReadOnlyList<IScenarioAi> AiStrats => [];
        public void Run(SimWorld world, int? selectedAi) => throw new InvalidOperationException("Initialization failed");
    }
}
