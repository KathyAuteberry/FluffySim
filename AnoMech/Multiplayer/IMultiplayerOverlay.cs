namespace AnoMech.Multiplayer;

// A scenario's own on-screen overlay, which a peer draws without running the scenario.
public interface IMultiplayerOverlay
{
    // Host-only, polled every tick: goes out whenever it differs from the last one sent.
    MpMessage? BuildOverlayMessage();

    // Peer-only, every frame once the host's message is here.
    void ApplyOverlayMessage(MpMessage message);

    // Peer-only, every frame, on the run's clock.
    void TickOverlay(float elapsed);
}
