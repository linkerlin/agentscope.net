// Copyright 2024-2026 the original author or authors.
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

namespace AgentScope.Harness.Sandbox;

/// <summary>
/// 沙箱租约：带过期时间的沙箱持有权，到期自动释放（防止泄漏）。
/// 对应 Java: io.agentscope.harness.agent.sandbox.SandboxLease
/// </summary>
public sealed class SandboxLease : IDisposable
{
    private readonly Timer? _timer;
    private readonly Action<SandboxLease>? _onExpire;
    private bool _disposed;

    /// <summary>租约ID。</summary>
    public string LeaseId { get; }
    /// <summary>沙箱ID。</summary>
    public string SandboxId { get; }
    /// <summary>租约获取时间。</summary>
    public DateTimeOffset AcquiredAt { get; }
    /// <summary>租约有效期。</summary>
    public TimeSpan Ttl { get; }
    /// <summary>是否已过期。</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= AcquiredAt + Ttl;

    public SandboxLease(string leaseId, string sandboxId, TimeSpan ttl, Action<SandboxLease>? onExpire = null)
    {
        LeaseId = leaseId;
        SandboxId = sandboxId;
        Ttl = ttl;
        AcquiredAt = DateTimeOffset.UtcNow;
        _onExpire = onExpire;

        if (ttl > TimeSpan.Zero)
        {
            _timer = new Timer(_ => Expire(), null, ttl, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>续租。</summary>
    public void Renew(TimeSpan additional)
    {
        _timer?.Change(additional, Timeout.InfiniteTimeSpan);
    }

    private void Expire()
    {
        if (_disposed) return;
        _onExpire?.Invoke(this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _timer?.Dispose();
        _disposed = true;
    }
}
