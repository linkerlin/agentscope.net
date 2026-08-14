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
using AgentScope.Core.Events;

namespace AgentScope.Core.Agent;

/// <summary>
/// 中间件基类，提供 5 个拦截点
/// 对应 Java: io.agentscope.core.agent.middleware.Middleware
/// </summary>
public abstract class MiddlewareBase
{
    /// <summary>拦截系统提示词构建</summary>
    public virtual Task<string> OnSystemPromptAsync(IAgent agent, RuntimeContext ctx, string prompt)
        => Task.FromResult(prompt);

    /// <summary>拦截 Agent 主调用链</summary>
    public virtual IAsyncEnumerable<Event> OnAgentAsync(
        AgentInput input,
        Func<AgentInput, IAsyncEnumerable<Event>> next)
        => next(input);

    /// <summary>拦截推理阶段</summary>
    public virtual Task<ReasoningInput> OnReasoningAsync(
        ReasoningInput input,
        Func<ReasoningInput, Task<ReasoningInput>> next)
        => next(input);

    /// <summary>拦截行动阶段</summary>
    public virtual Task<ActingInput> OnActingAsync(
        ActingInput input,
        Func<ActingInput, Task<ActingInput>> next)
        => next(input);

    /// <summary>拦截模型调用</summary>
    public virtual Task<ModelCallInput> OnModelCallAsync(
        ModelCallInput input,
        Func<ModelCallInput, Task<ModelCallInput>> next)
        => next(input);
}
