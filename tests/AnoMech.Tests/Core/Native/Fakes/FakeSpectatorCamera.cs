using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeSpectatorCamera : ISpectatorCamera
{
    public bool Available { get; set; } = true;
    public IBattleCharaProxy? Target { get; private set; }
    private bool visible;
    public void Begin(IBattleCharaProxy actor)
    {
        Target = actor;
        visible = Natives.BattleCharas.LocalPlayer.IsDrawObjectVisible;
        Natives.BattleCharas.LocalPlayer.IsDrawObjectVisible = false;
        Natives.PlayerInput.Steer(null, 0);
        Natives.PlayerInput.SpectatorInput = true;
    }
    public void End()
    {
        if (Target is not null) Natives.BattleCharas.LocalPlayer.IsDrawObjectVisible = visible;
        Target = null;
        Natives.PlayerInput.SpectatorInput = false;
    }
    public void Dispose() => End();
}
