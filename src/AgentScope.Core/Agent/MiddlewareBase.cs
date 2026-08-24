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
/// Abstract base class for middleware, providing 5 interception points
/// in the agent execution pipeline: system prompt, agent main chain,
/// reasoning, acting, and model call stages.
/// Middleware can be chained together using MiddlewareChain to form
/// a pipeline of cross-cutting concerns (e.g., logging, monitoring, caching).
/// 中间件抽象基类，提供 Agent 执行管道中的 5 个拦截点：
/// 系统提示词、Agent 主调用链、推理、行动和模型调用阶段。
/// 中间件可以通过 MiddlewareChain 链接在一起，形成横切关注点
/// （如日志记录、监控、缓存）的管道。
/// 对应 Java: io.agentscope.core.agent.middleware.Middleware
/// </summary>
public abstract class MiddlewareBase
{
    /// <summary>
    /// Intercepts the system prompt construction phase.
    /// Allows modification of the system prompt before it is sent to the model.
    /// 拦截系统提示词构建阶段。
    /// 允许在系统提示词发送给模型之前进行修改。
    /// </summary>
    /// <param name="agent">The target agent / 目标 Agent</param>
    /// <param name="ctx">Runtime context / 运行时上下文</param>
    /// <param name="prompt">The original system prompt / 原始系统提示词</param>
    /// <returns>The modified system prompt / 修改后的系统提示词</returns>
    public virtual Task<string> OnSystemPromptAsync(IAgent agent, RuntimeContext ctx, string prompt)
        => Task.FromResult(prompt);

    /// <summary>
    /// Intercepts the main agent invocation chain.
    /// This is the primary interception point for wrapping the entire agent execution.
    /// 拦截 Agent 主调用链。
    /// 这是包装整个 Agent 执行过程的主要拦截点。
    /// </summary>
    /// <param name="input">Agent input data / Agent 输入数据</param>
    /// <param name="next">The next handler in the chain / 链中的下一个处理器</param>
    /// <returns>Async enumerable of events / 事件的异步枚举</returns>
    public virtual IAsyncEnumerable<Event> OnAgentAsync(
        AgentInput input,
        Func<AgentInput, IAsyncEnumerable<Event>> next)
        => next(input);

    /// <summary>
    /// Intercepts the reasoning stage of agent execution.
    /// Allows modification of reasoning inputs before the model is called.
    /// 拦截 Agent 执行的推理阶段。
    /// 允许在调用模型之前修改推理输入。
    /// </summary>
    /// <param name="input">Reasoning input data / 推理输入数据</param>
    /// <param name="next">The next handler in the chain / 链中的下一个处理器</param>
    /// <returns>The processed reasoning input / 处理后的推理输入</returns>
    public virtual Task<ReasoningInput> OnReasoningAsync(
        ReasoningInput input,
        Func<ReasoningInput, Task<ReasoningInput>> next)
        => next(input);

    /// <summary>
    /// Intercepts the acting (tool execution) stage of agent execution.
    /// Allows modification of tool calls before they are executed.
    /// 拦截 Agent 执行的行动（工具调用）阶段。
    /// 允许在工具调用执行之前修改工具调用。
    /// </summary>
    /// <param name="input">Acting input data containing tool calls / 包含工具调用的行动输入数据</param>
    /// <param name="next">The next handler in the chain / 链中的下一个处理器</param>
    /// <returns>The processed acting input / 处理后的行动输入</returns>
    public virtual Task<ActingInput> OnActingAsync(
        ActingInput input,
        Func<ActingInput, Task<ActingInput>> next)
        => next(input);

    /// <summary>
    /// Intercepts the model call stage.
    /// Allows modification of messages and options before they are sent to the LLM.
    /// 拦截模型调用阶段。
    /// 允许在消息发送给 LLM 之前修改消息和选项。
    /// </summary>
    /// <param name="input">Model call input data / 模型调用输入数据</param>
    /// <param name="next">The next handler in the chain / 链中的下一个处理器</param>
    /// <returns>The processed model call input / 处理后的模型调用输入</returns>
    public virtual Task<ModelCallInput> OnModelCallAsync(
        ModelCallInput input,
        Func<ModelCallInput, Task<ModelCallInput>> next)
        => next(input);
}
