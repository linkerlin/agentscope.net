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

using AgentScope.Core.Agent;
using AgentScope.Core.Events;
namespace AgentScope.Harness.Middleware;

/// <summary>
/// Middleware for Harness skill integration and lifecycle management.
/// Harness 技能集成与生命周期管理中间件。
/// </summary>
public sealed class HarnessSkillMiddleware : IHarnessMiddleware
{
    /// <summary>
    /// Execution order (150).
    /// 执行顺序（150）。
    /// </summary>
    public int Order => 150;

    /// <summary>
    /// Pass-through for agent events.
    /// Agent 事件直通。
    /// </summary>
    /// <inheritdoc cref="IHarnessMiddleware.OnAgentAsync" path="/param"/>
    public async ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => await next();

    /// <summary>
    /// Pass-through for model call events.
    /// 模型调用事件直通。
    /// </summary>
    /// <inheritdoc cref="IHarnessMiddleware.OnModelCallAsync" path="/param"/>
    public async ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => await next();

    /// <summary>
    /// Pass-through for tool execution events.
    /// 工具执行事件直通。
    /// </summary>
    /// <inheritdoc cref="IHarnessMiddleware.OnToolExecutionAsync" path="/param"/>
    public async ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => await next();
}
