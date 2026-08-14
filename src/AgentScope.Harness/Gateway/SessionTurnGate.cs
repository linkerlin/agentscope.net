using System.Collections.Concurrent;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 会话轮次门。确保同一会话的调用串行执行。对标 Java SessionTurnGate。
/// </summary>
public sealed class SessionTurnGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    public async Task<IDisposable> AcquireAsync(string sessionId, CancellationToken ct = default)
    {
        var sem = _gates.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        return new TurnLease(sem);
    }

    public bool IsRunning(string sessionId) =>
        _gates.TryGetValue(sessionId, out var sem) && sem.CurrentCount == 0;

    private sealed class TurnLease(SemaphoreSlim sem) : IDisposable
    {
        public void Dispose() => sem.Release();
    }
}
