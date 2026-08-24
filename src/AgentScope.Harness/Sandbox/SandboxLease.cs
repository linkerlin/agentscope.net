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

namespace AgentScope.Harness.Sandbox;

/// <summary>
/// 沙箱租约：带过期时间的沙箱持有权，到期自动释放（防止泄漏）。
/// Sandbox lease: time-bound sandbox ownership that auto-releases on expiry (prevents leaks).
/// 对应 Java: io.agentscope.harness.agent.sandbox.SandboxLease
/// </summary>
public sealed class SandboxLease : IDisposable
{
    private readonly Timer? _timer;
    private readonly Action<SandboxLease>? _onExpire;
    private bool _disposed;

    /// <summary>
    /// 租约ID。
    /// Lease ID.
    /// </summary>
    public string LeaseId { get; }

    /// <summary>
    /// 沙箱ID。
    /// Sandbox ID.
    /// </summary>
    public string SandboxId { get; }

    /// <summary>
    /// 租约获取时间。
    /// Lease acquisition time.
    /// </summary>
    public DateTimeOffset AcquiredAt { get; }

    /// <summary>
    /// 租约有效期。
    /// Lease time-to-live.
    /// </summary>
    public TimeSpan Ttl { get; }

    /// <summary>
    /// 是否已过期。
    /// Whether the lease has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= AcquiredAt + Ttl;

    /// <summary>
    /// 创建沙箱租约。
    /// Create a sandbox lease.
    /// </summary>
    /// <param name="leaseId">租约ID / Lease ID</param>
    /// <param name="sandboxId">沙箱ID / Sandbox ID</param>
    /// <param name="ttl">租约有效期 / Lease time-to-live</param>
    /// <param name="onExpire">过期回调（可选） / Expiry callback (optional)</param>
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

    /// <summary>
    /// 续租。
    /// Renew the lease.
    /// </summary>
    /// <param name="additional">额外延长时间 / Additional time to extend</param>
    public void Renew(TimeSpan additional)
    {
        _timer?.Change(additional, Timeout.InfiniteTimeSpan);
    }

    private void Expire()
    {
        if (_disposed) return;
        _onExpire?.Invoke(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _timer?.Dispose();
        _disposed = true;
    }
}
