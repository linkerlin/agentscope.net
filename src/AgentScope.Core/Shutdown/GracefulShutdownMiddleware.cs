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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;

namespace AgentScope.Core.Shutdown;

/// <summary>
/// Graceful shutdown middleware that checks shutdown status in the Agent main invocation chain.
/// 优雅关闭中间件：在 Agent 主调用链中检查关闭状态，确保系统正在接受请求。
/// Corresponds to Java: io.agentscope.core.shutdown.GracefulShutdownMiddleware
/// </summary>
public class GracefulShutdownMiddleware : MiddlewareBase
{
    /// <summary>
    /// The graceful shutdown manager instance used to check shutdown status.
    /// 用于检查关闭状态的优雅关闭管理器实例。
    /// </summary>
    private readonly GracefulShutdownManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GracefulShutdownMiddleware"/> class.
    /// 初始化 <see cref="GracefulShutdownMiddleware"/> 类的新实例。
    /// </summary>
    /// <param name="manager">Optional shutdown manager; defaults to the singleton instance. / 可选的关闭管理器；默认为单例实例。</param>
    public GracefulShutdownMiddleware(GracefulShutdownManager? manager = null)
    {
        _manager = manager ?? GracefulShutdownManager.Instance;
    }

    /// <inheritdoc />
    public override IAsyncEnumerable<Event> OnAgentAsync(
        AgentInput input,
        Func<AgentInput, IAsyncEnumerable<Event>> next)
    {
        // 检查是否仍接受请求，拒绝则抛出 ShutdownException
        // Ensure the system is still accepting requests; throws ShutdownException if shutting down
        _manager.EnsureAcceptingRequests();
        return next(input);
    }

    /// <inheritdoc />
    public override Task<ReasoningInput> OnReasoningAsync(
        ReasoningInput input,
        Func<ReasoningInput, Task<ReasoningInput>> next)
    {
        // 推理阶段同样检查关闭状态
        // Check shutdown status during the reasoning phase as well
        _manager.EnsureAcceptingRequests();
        return next(input);
    }

    /// <inheritdoc />
    public override Task<ActingInput> OnActingAsync(
        ActingInput input,
        Func<ActingInput, Task<ActingInput>> next)
    {
        // 执行阶段检查关闭状态
        // Check shutdown status during the acting phase
        _manager.EnsureAcceptingRequests();
        return next(input);
    }
}
