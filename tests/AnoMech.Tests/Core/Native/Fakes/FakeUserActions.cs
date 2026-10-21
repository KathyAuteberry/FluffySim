using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

// Off by default, as when the user hasn't enabled the module.
internal sealed class FakeUserActions : IUserActions
{
    public bool Enabled { get; set; }
    public void Enable() => Enabled = true;
    public void Disable() => Enabled = false;
    public void OnSessionStart() { }
    public Action? ScenarioStarting { get; set; }
    public void OnScenarioStart() => ScenarioStarting?.Invoke();
    public void OnSessionEnd() { }
    public int Pauses { get; private set; }
    public int Resumes { get; private set; }
    public bool ThrowOnResume { get; set; }
    public Action? Resuming { get; set; }
    public void OnResume() { if (ThrowOnResume) throw new InvalidOperationException("Resume failed"); Resumes++; Resuming?.Invoke(); }
    public void OnPause() => Pauses++;
    public void Tick(float deltaSeconds) { }

    public void Dispose() { }
}
