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

namespace AgentScope.Harness.Coordination;

/// <summary>
/// Periodic gate interface for minimum-interval throttling, corresponding to Java PeriodicGate.<br />
/// 周期性门接口，用于最小间隔节流，对标 Java PeriodicGate。
/// </summary>
public interface IPeriodicGate
{
    /// <summary>
    /// Try to claim the gate for the given name. Returns <c>true</c> only if the minimum gap has elapsed.<br />
    /// 尝试获取指定名称的门。仅当最小间隔已过时返回 <c>true</c>。
    /// </summary>
    /// <param name="name">Gate identifier / 门标识符</param>
    /// <param name="minGap">Minimum time span between two successive claims / 两次成功获取之间的最小时间间隔</param>
    /// <returns><c>true</c> if the claim succeeds; <c>false</c> if throttled / 获取成功返回 true，被节流返回 false</returns>
    bool TryClaim(string name, TimeSpan minGap);
}

/// <summary>
/// In-process periodic gate implementation, corresponding to Java LocalPeriodicGate.<br />
/// 进程内周期性门实现，对标 Java LocalPeriodicGate。
/// </summary>
public sealed class LocalPeriodicGate : IPeriodicGate
{
    /// <summary>Last successful claim time per gate name / 每个门名称上次成功获取的时间</summary>
    private readonly ConcurrentDictionary<string, DateTime> _lastClaims = new();

    /// <inheritdoc />
    public bool TryClaim(string name, TimeSpan minGap)
    {
        var now = DateTime.UtcNow;
        // 检查是否仍在最小间隔内
        if (_lastClaims.TryGetValue(name, out var last) && now - last < minGap)
            return false;

        // 更新上次获取时间并返回成功
        _lastClaims[name] = now;
        return true;
    }
}
