// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;

namespace AgentScope.Core.Shutdown;

/// <summary>
/// 优雅关闭中间件：在 Agent 主调用链中检查关闭状态
/// 对应 Java: io.agentscope.core.shutdown.GracefulShutdownMiddleware
/// </summary>
public class GracefulShutdownMiddleware : MiddlewareBase
{
    private readonly GracefulShutdownManager _manager;

    public GracefulShutdownMiddleware(GracefulShutdownManager? manager = null)
    {
        _manager = manager ?? GracefulShutdownManager.Instance;
    }

    public override IAsyncEnumerable<Event> OnAgentAsync(
        AgentInput input,
        Func<AgentInput, IAsyncEnumerable<Event>> next)
    {
        _manager.EnsureAcceptingRequests();
        return next(input);
    }

    public override Task<ReasoningInput> OnReasoningAsync(
        ReasoningInput input,
        Func<ReasoningInput, Task<ReasoningInput>> next)
    {
        _manager.EnsureAcceptingRequests();
        return next(input);
    }

    public override Task<ActingInput> OnActingAsync(
        ActingInput input,
        Func<ActingInput, Task<ActingInput>> next)
    {
        _manager.EnsureAcceptingRequests();
        return next(input);
    }
}
