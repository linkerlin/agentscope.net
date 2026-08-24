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
using AgentScope.Harness.Subagent;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 子 Agent 编排中间件。对标 Java SubagentsMiddleware。
/// 在 agent 调用前加载子 agent spec，注入动态子 agent 列表。
/// </summary>
public sealed class SubagentsMiddleware(ISubagentManager manager) : IHarnessMiddleware
{
    public int Order => 300;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        ctx.Items["subagents"] = manager;
        await next();
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
