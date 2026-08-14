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

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 会话ID 工具：组合/解析（用户, 渠道, 线程）三元组为稳定会话ID。
/// 对应 Java: io.agentscope.harness.agent.gateway.SessionIdUtils
/// </summary>
public static class SessionIdUtils
{
    private const char Sep = '/';

    /// <summary>组合会话ID。</summary>
    public static string Compose(string userId, string channelId, string threadId)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId 必填", nameof(userId));
        return $"{userId}{Sep}{channelId ?? ""}{Sep}{threadId ?? ""}";
    }

    /// <summary>尝试解析会话ID为三元组。</summary>
    public static (string UserId, string ChannelId, string ThreadId) Parse(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return ("", "", "");
        var parts = sessionId.Split(Sep);
        return (
            parts.Length > 0 ? parts[0] : "",
            parts.Length > 1 ? parts[1] : "",
            parts.Length > 2 ? parts[2] : "");
    }

    /// <summary>生成默认会话ID（基于 GUID）。</summary>
    public static string NewId() => Guid.NewGuid().ToString("N");
}
