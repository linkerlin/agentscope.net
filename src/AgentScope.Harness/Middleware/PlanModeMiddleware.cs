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

namespace AgentScope.Harness.Middleware;

/// <summary>
/// PLAN/BUILD 模式切换中间件。对标 Java PlanModeMiddleware。
/// 在 agent 调用前根据当前模式注入不同的系统提示。
/// </summary>
public sealed class PlanModeMiddleware : IHarnessMiddleware
{
    public int Order => 400;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        var mode = ctx.Items.GetValueOrDefault("plan_mode") as string ?? "build";
        ctx.Items["system_prompt_suffix"] = mode switch
        {
            "plan" => "\n当前模式：规划。请先分解目标为步骤再行动。",
            _ => ""
        };
        await next();
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
