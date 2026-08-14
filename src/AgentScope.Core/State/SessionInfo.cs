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

namespace AgentScope.Core.State;

/// <summary>
/// 会话只读信息快照（用户、会话、来源、创建/更新时间）。
/// 对应 Java: io.agentscope.core.state.SessionInfo
/// </summary>
public class SessionInfo : IState
{
    public string UserId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string? Source { get; set; }
    public System.DateTimeOffset CreatedAt { get; set; } = System.DateTimeOffset.UtcNow;
    public System.DateTimeOffset UpdatedAt { get; set; } = System.DateTimeOffset.UtcNow;

    public SessionInfo() { }

    public SessionInfo(string userId, string sessionId, string? source = null)
    {
        UserId = userId;
        SessionId = sessionId;
        Source = source;
    }

    /// <summary>更新时间戳（保存前调用）</summary>
    public void Touch() => UpdatedAt = System.DateTimeOffset.UtcNow;
}
