using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.Native.Implementations;

internal readonly record struct CameraSnapshot(float Yaw, float Pitch, float Distance, float Interpolated, float Saved, CameraZoomMode Zoom)
{
    public static unsafe CameraSnapshot Capture(Camera* camera)
        => new(camera->DirH, camera->DirV, camera->Distance, camera->InterpDistance, camera->SavedDistance, camera->ZoomMode);

    public unsafe void Restore(Camera* camera)
    {
        camera->DirH = Yaw;
        camera->DirV = Pitch;
        camera->Distance = Distance;
        camera->InterpDistance = Interpolated;
        camera->SavedDistance = Saved;
        camera->ZoomMode = Zoom;
    }
}
