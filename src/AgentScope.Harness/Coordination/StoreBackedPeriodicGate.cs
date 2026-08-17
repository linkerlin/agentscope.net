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
/// Store-backed periodic gate for distributed throttling, corresponding to Java StoreBackedPeriodicGate.<br />
/// 基于存储的周期性门，用于分布式节流控制，对标 Java StoreBackedPeriodicGate。
/// </summary>
public sealed class StoreBackedPeriodicGate : IPeriodicGate
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
