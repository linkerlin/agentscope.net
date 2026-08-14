using System.Collections.Concurrent;
using System.Text.Json;

namespace AgentScope.Harness.Sandbox;

/// <summary>沙箱状态存储：基于内存管理沙箱生命周期状态</summary>
public sealed class SessionSandboxStateStore
{
    private readonly ConcurrentDictionary<string, SandboxStateSlot> _store = new();

    /// <summary>加载沙箱状态</summary>
    public Task<SandboxState?> LoadAsync(string sessionId,
        CancellationToken ct = default)
    {
        if (_store.TryGetValue(EncodeSessionId(sessionId, IsolationScope.Session),
            out var slot) && !slot.Deleted)
        {
            return Task.FromResult(JsonSerializer.Deserialize<SandboxState>(slot.Json));
        }
        return Task.FromResult<SandboxState?>(null);
    }

    /// <summary>保存沙箱状态</summary>
    public Task SaveAsync(string sessionId, SandboxState state,
        CancellationToken ct = default)
    {
        var key = EncodeSessionId(sessionId, IsolationScope.Session);
        var json = JsonSerializer.Serialize(state);
        _store[key] = new SandboxStateSlot(json, false);
        return Task.CompletedTask;
    }

    /// <summary>删除沙箱状态（tombstone 模式）</summary>
    public Task DeleteAsync(string sessionId,
        CancellationToken ct = default)
    {
        var key = EncodeSessionId(sessionId, IsolationScope.Session);
        if (_store.TryGetValue(key, out var slot))
            _store[key] = slot with { Deleted = true };
        else
            _store[key] = new SandboxStateSlot("", true);
        return Task.CompletedTask;
    }

    private static string EncodeSessionId(string sessionId, IsolationScope scope)
    {
        var prefix = scope switch
        {
            IsolationScope.Session => "ses",
            IsolationScope.User => "usr",
            IsolationScope.Agent => "agt",
            IsolationScope.Global => "gbl",
            _ => "unk"
        };
        return $"{prefix}::{sessionId}";
    }

    private sealed record SandboxStateSlot(string Json, bool Deleted);
}

// IsolationScope 已在 AgentScope.Harness.IsolationScope 中定义
