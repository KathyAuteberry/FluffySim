using System.Threading;

namespace AnoMech.Core.Native.Policies;

internal sealed class PacketFilterPolicy(ushort heartbeat)
{
    private long allowed, blocked, invalid;
    public long Allowed => Interlocked.Read(ref allowed);
    public long Blocked => Interlocked.Read(ref blocked);
    public long Invalid => Interlocked.Read(ref invalid);
    public long Intercepted => Allowed + Blocked;

    public bool Allow(ushort? opcode)
    {
        if (heartbeat != 0 && opcode == heartbeat)
        {
            Interlocked.Increment(ref allowed);
            return true;
        }
        Interlocked.Increment(ref blocked);
        if (opcode is null) Interlocked.Increment(ref invalid);
        return false;
    }
}
