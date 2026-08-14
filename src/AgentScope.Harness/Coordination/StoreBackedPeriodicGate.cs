using System.Collections.Concurrent;

namespace AgentScope.Harness.Coordination;

/// <summary>基于分布式存储的周期性门，对应 Java StoreBackedPeriodicGate</summary>
public sealed class StoreBackedPeriodicGate : IPeriodicGate
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
