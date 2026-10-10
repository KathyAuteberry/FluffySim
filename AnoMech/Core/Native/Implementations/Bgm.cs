using System;
using System.IO;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Sound;
using Lumina.Excel.Sheets;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.Native.Implementations;

// Thin wrapper around Client::Game::BGMSystem for scenario music. SetBGM
// writes into the Content scene (sceneId 3 = instance music) and starts
// the SCD from the beginning. Reset hands the slot back so the territory/director BGM
// can resume.
//
// BGMSystem can't start a track mid-way. Tried and inert: the scene's "disable restart" resume
// entry (a timer, not a position) and ISoundData.SetSpeed on the music sound (reads back, still
// plays at 1x). A track that must start mid-way plays on our own output instead.
internal sealed unsafe class Bgm : IBgm
{
    private const uint ContentSceneId = 3;
    private const ushort SilentBgmId = 1;

    private readonly BgmOverride playback = new(ReadContentScene,
        track => BGMSystem.SetBGM(track, ContentSceneId),
        () => { var system = BGMSystem.Instance(); if (system != null) system->ResetBGM(ContentSceneId); });
    private ushort current;
    private MusicPlayer? player;
    private float trackVolume = 1f;

    public void Play(ushort bgmId, float secondsIn = 0f)
    {
        if (bgmId == 0) return; // 0 = use Reset
        if (BGMSystem.Instance() == null) return;
        if (secondsIn <= 0f && bgmId == current && player == null && playback.Poll()) return;
        if (!playback.Play(secondsIn > 0f ? SilentBgmId : bgmId))
        {
            StopPlayer();
            current = 0;
            DiagnosticLog.Info("[Bgm] Playback unavailable or owned by another controller; leaving its selection unchanged.");
            return;
        }
        StopPlayer();
        var started = secondsIn > 0f && StartPlayer(bgmId, secondsIn);
        if (!started && secondsIn > 0f) playback.Play(bgmId);
        current = bgmId;
    }

    public void Sync(float secondsIn)
    {
        if (current == 0) { DiagnosticLog.Info("[Bgm] Sync: no scenario track is playing."); return; }
        Play(current, secondsIn);
    }

    public void Reset()
    {
        StopPlayer();
        playback.Reset();
        current = 0;
    }

    public void Tick(float deltaSeconds)
    {
        if (current != 0 && !playback.Poll())
        {
            StopPlayer();
            current = 0;
        }
        if (player != null) player.Volume = trackVolume * MusicVolume();
    }

    public void LogPosition(string label)
        => DiagnosticLog.Info($"[Bgm] {label}: track {current} {(player != null ? $"on our output at {player.PositionSeconds:F2}s, volume {player.Volume:F2}" : "on the game's own output")}.");

    private static BgmSceneState? ReadContentScene()
    {
        var system = BGMSystem.Instance();
        if (system == null) return null;
        foreach (ref var scene in system->Scenes)
            if (scene.SceneId == ContentSceneId)
                return new(scene.BgmId, Plugin.ClientState.TerritoryType);
        return null;
    }

    private bool StartPlayer(ushort bgmId, float secondsIn)
    {
        var path = Plugin.DataManager.GetExcelSheet<BGM>()?.GetRowOrDefault(bgmId)?.File.ExtractText();
        var scd = string.IsNullOrEmpty(path) ? null : Plugin.DataManager.GetFile(path)?.Data;
        if (scd == null) { DiagnosticLog.Warn($"[Bgm] Track {bgmId} ({path}) has no readable file; playing it from the top instead."); return false; }
        var ogg = ScdOgg.Extract(scd, out var error);
        if (ogg == null) { DiagnosticLog.Warn($"[Bgm] Track {bgmId} ({path}): {error}; playing it from the top instead."); return false; }
        trackVolume = ScdOgg.SoundVolume(scd);
        var volume = MusicVolume();
        try
        {
            player = MusicPlayer.Start(ogg, secondsIn, trackVolume * volume, out error);
        }
        catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException)
        {
            // The NAudio/NVorbis dlls are missing from the plugin folder or failed to load.
            error = $"{e.GetType().Name}: {e.Message}";
        }
        if (player == null) { DiagnosticLog.Warn($"[Bgm] Track {bgmId} ({path}) failed to start on our output ({error}); playing it from the top instead."); return false; }
        DiagnosticLog.Info($"[Bgm] Track {bgmId} ({path}) started on our output at {secondsIn:F2}s, track mix {trackVolume:F2}, music volume {volume:F2}; the content scene holds the silent track.");
        return true;
    }

    private void StopPlayer()
    {
        if (player == null) return;
        player.Dispose();
        player = null;
    }

    // GetEffectiveVolume throws if its signature didn't resolve.
    private static bool volumeFallbackLogged;

    // The Music bus carries the BGM slider, the BGM mute and the background-window rule; the game
    // applies the master volume and mute at its output instead, so they're multiplied in here.
    private static float MusicVolume()
    {
        var manager = SoundManager.Instance();
        if (manager == null || manager->Disabled || manager->IsSndMaster) return 0f;
        var master = manager->MasterVolume * manager->ActiveVolume;
        try
        {
            return manager->GetEffectiveVolume(SoundBus.Music) * master;
        }
        catch (InvalidOperationException e)
        {
            if (!volumeFallbackLogged) DiagnosticLog.Warn($"[Bgm] Music volume unreadable ({e.Message}); our output follows the master volume only.");
            volumeFallbackLogged = true;
            return master;
        }
    }

    public void Dispose() => Reset();
}
