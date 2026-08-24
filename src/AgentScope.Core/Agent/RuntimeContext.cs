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

using System.Threading;

namespace AgentScope.Core.Agent;

/// <summary>
/// Runtime context that flows through the agent invocation chain via AsyncLocal.
/// Provides contextual information such as user ID and session ID to all
/// components in the current asynchronous flow.
/// 通过 AsyncLocal 在 Agent 调用链中传递的运行时上下文。
/// 为当前异步流中的所有组件提供用户 ID 和会话 ID 等上下文信息。
/// 对应 Java: io.agentscope.core.agent.RuntimeContext
/// </summary>
public record RuntimeContext(
    string? UserId,
    string? SessionId,
    RuntimeContext? Parent = null)
{
    private static readonly AsyncLocal<RuntimeContext?> _current = new();

    /// <summary>
    /// Gets or sets the current RuntimeContext for the current thread/task flow.
    /// Uses AsyncLocal to ensure proper flow across async operations.
    /// 获取或设置当前线程/任务流中的 RuntimeContext。
    /// 使用 AsyncLocal 确保在异步操作中正确传递。
    /// </summary>
    public static RuntimeContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets an empty RuntimeContext with no user or session information.
    /// 获取一个不包含用户或会话信息的空 RuntimeContext。
    /// </summary>
    public static RuntimeContext Empty => new(null, null);

    /// <summary>
    /// Creates a new RuntimeContext with the specified user ID, copying other fields.
    /// 创建一个具有指定用户 ID 的新 RuntimeContext，复制其他字段。
    /// </summary>
    /// <param name="userId">The new user ID / 新的用户 ID</param>
    /// <returns>A new RuntimeContext with updated user ID / 更新了用户 ID 的新 RuntimeContext</returns>
    public RuntimeContext WithUserId(string userId) => this with { UserId = userId };

    /// <summary>
    /// Creates a new RuntimeContext with the specified session ID, copying other fields.
    /// 创建一个具有指定会话 ID 的新 RuntimeContext，复制其他字段。
    /// </summary>
    /// <param name="sessionId">The new session ID / 新的会话 ID</param>
    /// <returns>A new RuntimeContext with updated session ID / 更新了会话 ID 的新 RuntimeContext</returns>
    public RuntimeContext WithSessionId(string sessionId) => this with { SessionId = sessionId };
}
