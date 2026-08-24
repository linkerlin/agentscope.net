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

namespace AgentScope.Core.Shutdown;

/// <summary>
/// Session binding for shutdown: associates active sessions/requests with the shutdown process,
/// allowing batch persistence or notification per session during shutdown.
/// 关闭与会话绑定：把活跃会话/请求与关闭流程关联，关闭时按会话批量持久化或通知。
/// Corresponds to Java: io.agentscope.core.shutdown.ShutdownSessionBinding
/// </summary>
public class ShutdownSessionBinding
{
    /// <summary>
    /// Thread-safe dictionary mapping request IDs to their bound session information.
    /// 线程安全的字典，将请求ID映射到绑定的会话信息。
    /// </summary>
    private readonly ConcurrentDictionary<string, BoundSession> _bindings = new();

    /// <summary>
    /// Binds a request to a session for shutdown tracking.
    /// 绑定一个会话到关闭流程。
    /// </summary>
    /// <param name="requestId">The request identifier. / 请求标识符。</param>
    /// <param name="sessionId">The session identifier. / 会话标识符。</param>
    /// <param name="userId">Optional user identifier. / 可选用户标识符。</param>
    public void Bind(string requestId, string sessionId, string? userId = null)
    {
        _bindings[requestId] = new BoundSession
        {
            RequestId = requestId,
            SessionId = sessionId,
            UserId = userId,
            BoundAt = System.DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Unbinds a request from the shutdown process.
    /// 解绑请求与关闭流程的关联。
    /// </summary>
    /// <param name="requestId">The request identifier to unbind. / 要解绑的请求标识符。</param>
    /// <returns>True if successfully unbound; false if the request was not found. / 成功解绑返回 true，未找到返回 false。</returns>
    public bool Unbind(string requestId) => _bindings.TryRemove(requestId, out _);

    /// <summary>
    /// Gets the bound session for a given request.
    /// 查询指定请求的绑定会话信息。
    /// </summary>
    /// <param name="requestId">The request identifier. / 请求标识符。</param>
    /// <returns>The bound session if found; otherwise null. / 找到则返回绑定会话，否则返回 null。</returns>
    public BoundSession? Get(string requestId) =>
        _bindings.TryGetValue(requestId, out var b) ? b : null;

    /// <summary>
    /// Gets a snapshot of all currently bound sessions.
    /// 获取当前所有已绑定会话的快照。
    /// </summary>
    /// <returns>A read-only collection of all bound sessions. / 所有绑定会话的只读集合。</returns>
    public System.Collections.Generic.IReadOnlyCollection<BoundSession> All() => _bindings.Values.ToArray();

    /// <summary>
    /// Represents a session bound to the shutdown process.
    /// 表示与关闭流程绑定的会话。
    /// </summary>
    public sealed class BoundSession
    {
        /// <summary>The request identifier. / 请求标识符。</summary>
        public string RequestId { get; set; } = "";
        /// <summary>The session identifier. / 会话标识符。</summary>
        public string SessionId { get; set; } = "";
        /// <summary>Optional user identifier. / 可选用户标识符。</summary>
        public string? UserId { get; set; }
        /// <summary>The UTC time when the binding was created. / 绑定创建时的 UTC 时间。</summary>
        public System.DateTimeOffset BoundAt { get; set; }
    }
}
