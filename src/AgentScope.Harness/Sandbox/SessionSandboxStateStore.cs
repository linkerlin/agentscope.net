// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
