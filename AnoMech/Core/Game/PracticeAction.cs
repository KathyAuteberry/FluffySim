namespace AnoMech.Core.Game;

public class PracticeAction(float duration)
{
    public bool Enabled { get; private set; }
    public bool Ready => Enabled && !used;
    public bool Active => Enabled && remaining > 0;
    public float Remaining => remaining;
    public bool Suspended { get; internal set; }
    private bool used;
    private float remaining;

    public void Start() { Enabled = true; used = false; remaining = 0; Suspended = false; }
    public bool Activate(bool eligible)
    {
        if (!eligible || !Ready || Suspended) return false;
        used = true;
        remaining = duration;
        return true;
    }
    public void Tick(float delta) { if (!Suspended) remaining = System.MathF.Max(0, remaining - delta); }
    public void Clear() { Enabled = false; used = false; remaining = 0; Suspended = false; }
}
