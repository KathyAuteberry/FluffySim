namespace AnoMech.Core.Native.Interfaces;

public sealed class LocalInputLock
{
    public bool ZeroMovement { get; set; }
    public bool DisableAllActions { get; set; }
    public bool PauseInput { get; set; }
    public bool TransitionInput { get; set; }
    public bool MovementBlocked => ZeroMovement || PauseInput || TransitionInput;
    public bool CancelAutoAttacks => DisableAllActions && !PauseInput && !TransitionInput;
    public bool RejectAction(bool stoppingAutoAttacks) => PauseInput || TransitionInput || (DisableAllActions && !stoppingAutoAttacks);

    public void SuppressMovement(ref float left, ref float forward, ref float turn, ref byte backwardOrStrafe)
    {
        if (!MovementBlocked) return;
        left = forward = 0;
        backwardOrStrafe = 0;
        if (PauseInput || TransitionInput) turn = 0;
    }
}
