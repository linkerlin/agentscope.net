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

namespace AgentScope.Harness.Memory.Session;

/// <summary>会话新鲜度评估器：判断会话是否过期或应重置</summary>
public sealed class SessionFreshnessEvaluator
{
    private readonly TimeSpan _idleTimeout;
    private readonly bool _resetDaily;

    public SessionFreshnessEvaluator(
        TimeSpan? idleTimeout = null,
        bool resetDaily = true)
    {
        _idleTimeout = idleTimeout ?? TimeSpan.FromHours(24);
        _resetDaily = resetDaily;
    }

    /// <summary>判断会话是否因空闲超时而过期</summary>
    public bool IsIdleExpired(DateTime lastActivityTime)
    {
        return DateTime.UtcNow - lastActivityTime > _idleTimeout;
    }

    /// <summary>判断会话是否需要每日重置</summary>
    public bool ShouldResetDaily(DateTime lastActivityTime)
    {
        if (!_resetDaily) return false;
        return lastActivityTime.Date < DateTime.UtcNow.Date;
    }

    /// <summary>获取下次重置时间</summary>
    public DateTime GetNextResetTime() =>
        DateTime.UtcNow.Date.AddDays(1);
}
