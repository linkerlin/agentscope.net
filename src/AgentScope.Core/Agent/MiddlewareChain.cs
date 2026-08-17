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
/// Middleware chain that links multiple MiddlewareBase instances in reverse order
/// to form a pipeline. Middleware are chained using the Chain of Responsibility pattern,
/// where each middleware can process or modify the input before passing it to the next.
/// 中间件链，将多个 MiddlewareBase 按注册顺序反向链接为调用管道。
/// 使用责任链模式链接中间件，每个中间件可以在将输入传递给下一个之前进行处理或修改。
/// 对应 Java: io.agentscope.core.middleware.MiddlewareChain
/// </summary>
public sealed class MiddlewareChain
{
    private readonly List<MiddlewareBase> _middlewares = [];

    /// <summary>
    /// Adds a middleware to the chain. Supports fluent API chaining.
    /// 向链中添加一个中间件。支持流畅 API 链式调用。
    /// </summary>
    /// <param name="mw">The middleware to add / 要添加的中间件</param>
    /// <returns>This chain instance for chaining / 当前链实例，用于链式调用</returns>
    public MiddlewareChain Add(MiddlewareBase mw)
    {
        _middlewares.Add(mw);
        return this;
    }

    /// <summary>
    /// Adds a range of middlewares to the chain.
    /// 向链中添加一组中间件。
    /// </summary>
    /// <param name="mws">The middlewares to add / 要添加的中间件集合</param>
    /// <returns>This chain instance for chaining / 当前链实例，用于链式调用</returns>
    public MiddlewareChain AddRange(IEnumerable<MiddlewareBase> mws)
    {
        _middlewares.AddRange(mws);
        return this;
    }

    /// <summary>
    /// Gets the read-only list of registered middlewares.
    /// 获取已注册中间件的只读列表。
    /// </summary>
    public IReadOnlyList<MiddlewareBase> Middlewares => _middlewares;

    /// <summary>
    /// Builds the main agent invocation chain.
    /// Middleware are wrapped in reverse order so the first registered middleware
    /// executes first (outermost layer).
    /// 构建 Agent 主调用链。
    /// 中间件按反向顺序包装，因此最先注册的中间件最先执行（最外层）。
    /// </summary>
    /// <param name="coreHandler">The core agent handler / 核心 Agent 处理器</param>
    /// <returns>The composed chain function / 组合后的链函数</returns>
    public Func<AgentInput, IAsyncEnumerable<Event>> BuildAgentChain(
        Func<AgentInput, IAsyncEnumerable<Event>> coreHandler)
    {
        Func<AgentInput, IAsyncEnumerable<Event>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = input => mw.OnAgentAsync(input, next);
        }
        return chain;
    }

    /// <summary>
    /// Builds the reasoning stage chain.
    /// 构建推理阶段链。
    /// </summary>
    public Func<ReasoningInput, Task<ReasoningInput>> BuildReasoningChain(
        Func<ReasoningInput, Task<ReasoningInput>> coreHandler)
    {
        Func<ReasoningInput, Task<ReasoningInput>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = input => mw.OnReasoningAsync(input, next);
        }
        return chain;
    }

    /// <summary>
    /// Builds the acting (tool execution) stage chain.
    /// 构建行动（工具调用）阶段链。
    /// </summary>
    public Func<ActingInput, Task<ActingInput>> BuildActingChain(
        Func<ActingInput, Task<ActingInput>> coreHandler)
    {
        Func<ActingInput, Task<ActingInput>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = input => mw.OnActingAsync(input, next);
        }
        return chain;
    }

    /// <summary>
    /// Builds the model call stage chain.
    /// 构建模型调用阶段链。
    /// </summary>
    public Func<ModelCallInput, Task<ModelCallInput>> BuildModelCallChain(
        Func<ModelCallInput, Task<ModelCallInput>> coreHandler)
    {
        Func<ModelCallInput, Task<ModelCallInput>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = input => mw.OnModelCallAsync(input, next);
        }
        return chain;
    }

    /// <summary>
    /// Builds the system prompt chain.
    /// 构建系统提示词链。
    /// </summary>
    public Func<IAgent, RuntimeContext, string, Task<string>> BuildSystemPromptChain(
        Func<IAgent, RuntimeContext, string, Task<string>> coreHandler)
    {
        Func<IAgent, RuntimeContext, string, Task<string>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = (agent, ctx, prompt) => mw.OnSystemPromptAsync(agent, ctx, prompt)
                .ContinueWith(t => next(agent, ctx, t.Result)).Unwrap();
        }
        return chain;
    }
}
