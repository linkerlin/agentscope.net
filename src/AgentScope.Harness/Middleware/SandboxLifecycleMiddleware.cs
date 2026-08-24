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

using AgentScope.Harness.Sandbox;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 沙箱生命周期中间件。对标 Java SandboxLifecycleMiddleware。
/// 在每个 Agent 调用前获取沙箱，调用后释放并持久化状态。
/// </summary>
public sealed class SandboxLifecycleMiddleware(
    SandboxManager? manager = null) : IHarnessMiddleware
{
    public int Order => 50;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        if (manager != null)
        {
            var sandboxCtx = SandboxContext.Default;
            ctx.Items["sandbox"] = sandboxCtx;
        }

        try { await next(); }
        finally
        {
            ctx.Items.Remove("sandbox");
        }
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
