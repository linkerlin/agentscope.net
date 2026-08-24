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
/// Agent state container, including session context, summary, iteration count, reply ID, and sub-context.
/// Agent 状态容器，包含会话上下文、摘要、迭代计数、回复ID 和子上下文。
/// Corresponds to Java: io.agentscope.core.state.AgentState
/// 对应 Java: io.agentscope.core.state.AgentState
/// </summary>
public class AgentState
{
    /// <summary>
    /// Gets the session identifier.
    /// 获取会话标识。
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Gets the user identifier (optional).
    /// 获取用户标识（可选）。
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Gets or sets the conversation summary text.
    /// 获取或设置对话摘要文本。
    /// </summary>
    public string Summary { get; set; } = "";

    /// <summary>
    /// Gets the list of conversation messages (context).
    /// 获取对话消息列表（上下文）。
    /// </summary>
    public List<Msg> Context { get; } = [];

    /// <summary>
    /// Gets or sets the reply identifier.
    /// 获取或设置回复标识。
    /// </summary>
    public string ReplyId { get; set; } = "";

    /// <summary>
    /// Gets or sets the current iteration count.
    /// 获取或设置当前迭代计数。
    /// </summary>
    public int CurIter { get; set; }

    /// <summary>
    /// Mutable context (only exists in the current scope at runtime, not serialized).
    /// 可变上下文（运行时只存在于当前 scope 中，不序列化）。
    /// </summary>
    [JsonIgnore]
    public List<Msg> ContextMutable { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentState"/> class.
    /// 初始化 <see cref="AgentState"/> 类的新实例。
    /// </summary>
    /// <param name="sessionId">Session identifier / 会话标识</param>
    /// <param name="userId">User identifier (optional) / 用户标识（可选）</param>
    public AgentState(string sessionId, string? userId = null)
    {
        SessionId = sessionId;
        UserId = userId;
    }
}

/// <summary>
/// Versioned state wrapper supporting optimistic concurrency control.
/// 版本化状态包装，支持乐观并发控制。
/// Corresponds to Java: io.agentscope.core.state.VersionedState
/// 对应 Java: io.agentscope.core.state.VersionedState
/// </summary>
public class VersionedState<T>
{
    /// <summary>
    /// Gets the version number for optimistic concurrency control.
    /// 获取用于乐观并发控制的版本号。
    /// </summary>
    public long Version { get; init; }

    /// <summary>
    /// Gets the wrapped state value.
    /// 获取包装的状态值。
    /// </summary>
    public T State { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionedState{T}"/> class.
    /// 初始化 <see cref="VersionedState{T}"/> 类的新实例。
    /// </summary>
    /// <param name="version">Version number / 版本号</param>
    /// <param name="state">State value / 状态值</param>
    public VersionedState(long version, T state)
    {
        Version = version;
        State = state;
    }
}
