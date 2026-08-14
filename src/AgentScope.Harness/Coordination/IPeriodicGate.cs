using System.Collections.Concurrent;

namespace AgentScope.Harness.Coordination;

/// <summary>
/// 周期性门。用于最小间隔节流。对标 Java PeriodicGate。
/// </summary>
public interface IPeriodicGate
{
    bool TryClaim(string name, TimeSpan minGap);
}

/// <summary>
/// 进程内实现。对标 Java LocalPeriodicGate。
/// </summary>
public sealed class LocalPeriodicGate : IPeriodicGate
{
    private readonly ConcurrentDictionary<string, DateTime> _lastClaims = new();

    public bool TryClaim(string name, TimeSpan minGap)
    {
        var now = DateTime.UtcNow;
        if (_lastClaims.TryGetValue(name, out var last) && now - last < minGap)
            return false;

        _lastClaims[name] = now;
        return true;
    }
}
