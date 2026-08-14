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

using System.Collections.Generic;
using System.Text.Json.Serialization;
using AgentScope.Core.Message;

namespace AgentScope.Core.State;

/// <summary>
/// Agent 状态容器，包含会话上下文、摘要、迭代计数、回复ID 和子上下文
/// 对应 Java: io.agentscope.core.state.AgentState
/// </summary>
public class AgentState
{
    public string SessionId { get; }
    public string? UserId { get; }
    public string Summary { get; set; } = "";
    public List<Msg> Context { get; } = [];
    public string ReplyId { get; set; } = "";
    public int CurIter { get; set; }

    /// <summary>可变上下文（运行时只存在于当前 scope 中）</summary>
    [JsonIgnore]
    public List<Msg> ContextMutable { get; set; } = [];

    public AgentState(string sessionId, string? userId = null)
    {
        SessionId = sessionId;
        UserId = userId;
    }
}

/// <summary>
/// 版本化状态包装，支持乐观并发控制
/// </summary>
public class VersionedState<T>
{
    public long Version { get; init; }
    public T State { get; init; }

    public VersionedState(long version, T state)
    {
        Version = version;
        State = state;
    }
}
