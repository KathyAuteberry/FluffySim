using System;

namespace AnoMech.Core.Native.Interfaces;

public interface ISpectatorCamera : IDisposable
{
    bool Available { get; }
    void Begin(IBattleCharaProxy actor);
    void End();
}
