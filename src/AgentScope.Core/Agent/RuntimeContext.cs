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
/// 调用上下文，通过 AsyncLocal 在调用链中传递
/// 对应 Java: io.agentscope.core.agent.RuntimeContext
/// </summary>
public record RuntimeContext(
    string? UserId,
    string? SessionId,
    RuntimeContext? Parent = null)
{
    private static readonly AsyncLocal<RuntimeContext?> _current = new();

    /// <summary>当前线程/任务流中的 RuntimeContext</summary>
    public static RuntimeContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public static RuntimeContext Empty => new(null, null);

    public RuntimeContext WithUserId(string userId) => this with { UserId = userId };

    public RuntimeContext WithSessionId(string sessionId) => this with { SessionId = sessionId };
}
