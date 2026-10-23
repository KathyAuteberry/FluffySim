#pragma warning disable CA1416
using AnoMech.Core.Native.Implementations;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Tests;

public class SpectatorCameraTests
{
    [Test]
    public unsafe void SnapshotRestoresCameraAnglesAndZoomWithoutChangingOtherCameraSettings()
    {
        var camera = new Camera { DirH = 1.2f, DirV = -.45f, Distance = 18, InterpDistance = 17,
            SavedDistance = 19, ZoomMode = CameraZoomMode.ThirdPerson, FoV = .8f };
        var snapshot = CameraSnapshot.Capture(&camera);
        camera.DirH = -2;
        camera.DirV = .3f;
        camera.Distance = 5;
        camera.InterpDistance = 6;
        camera.SavedDistance = 7;
        camera.ZoomMode = CameraZoomMode.FirstPerson;
        snapshot.Restore(&camera);
        Assert.That(CameraSnapshot.Capture(&camera), Is.EqualTo(snapshot));
        Assert.That(camera.FoV, Is.EqualTo(.8f));
    }
}
