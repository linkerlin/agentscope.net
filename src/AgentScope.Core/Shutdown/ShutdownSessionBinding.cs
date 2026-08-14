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

using System.Collections.Concurrent;

namespace AgentScope.Core.Shutdown;

/// <summary>
/// 关闭与会话绑定：把活跃会话/请求与关闭流程关联，关闭时按会话批量持久化或通知。
/// 对应 Java: io.agentscope.core.shutdown.ShutdownSessionBinding
/// </summary>
public class ShutdownSessionBinding
{
    private readonly ConcurrentDictionary<string, BoundSession> _bindings = new();

    /// <summary>绑定一个会话到关闭流程。</summary>
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

    /// <summary>解绑。</summary>
    public bool Unbind(string requestId) => _bindings.TryRemove(requestId, out _);

    /// <summary>查询绑定。</summary>
    public BoundSession? Get(string requestId) =>
        _bindings.TryGetValue(requestId, out var b) ? b : null;

    /// <summary>当前所有已绑定会话快照。</summary>
    public System.Collections.Generic.IReadOnlyCollection<BoundSession> All() => _bindings.Values.ToArray();

    public sealed class BoundSession
    {
        public string RequestId { get; set; } = "";
        public string SessionId { get; set; } = "";
        public string? UserId { get; set; }
        public System.DateTimeOffset BoundAt { get; set; }
    }
}
