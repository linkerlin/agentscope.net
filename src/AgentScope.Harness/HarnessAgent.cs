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

using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;
using AgentScope.Core.State;
using AgentScope.Core.Tool;
using AgentScope.Harness.Bus;
using AgentScope.Harness.Filesystem;
using AgentScope.Harness.Gateway;
using AgentScope.Harness.Middleware;
using AgentScope.Harness.Subagent;
using AgentScope.Harness.Team;

namespace AgentScope.Harness;

/// <summary>
/// Harness main agent. Counterpart to Java HarnessAgent.
/// Harness 主 Agent。对标 Java HarnessAgent。
/// Composes EnhancedReActAgent with subsystems (bus/filesystem/team/gateway/middleware)
/// to provide a complete agent runtime environment.
/// 组合 EnhancedReActAgent + 各子系统（总线/文件系统/团队/网关/中间件），
/// 提供完整的 Agent 运行时环境。
/// </summary>
public sealed class HarnessAgent : IAgent
{
    private readonly EnhancedReActAgent _inner;
    private readonly List<IHarnessMiddleware> _middlewares = [];
    private readonly IMessageBus _bus;
    private readonly IFilesystem _filesystem;
    private readonly IGateway _gateway;

    /// <summary>Agent unique identifier. / Agent 唯一标识。</summary>
    public string AgentId => _inner.AgentId;
    /// <summary>Agent display name. / Agent 显示名称。</summary>
    public string Name => _inner.Name;
    /// <summary>Agent description. / Agent 描述。</summary>
    public string Description => _inner.Description;

    /// <summary>
    /// Internal constructor. Use <see cref="HarnessAgentBuilder"/> to create instances.
    /// 内部构造函数。请使用 <see cref="HarnessAgentBuilder"/> 创建实例。
    /// </summary>
    /// <param name="inner">The inner EnhancedReActAgent. / 内部的 EnhancedReActAgent。</param>
    /// <param name="bus">Message bus. / 消息总线。</param>
    /// <param name="filesystem">Filesystem abstraction. / 文件系统抽象。</param>
    /// <param name="gateway">Gateway. / 网关。</param>
    /// <param name="middlewares">Optional middleware collection. / 可选的中间件集合。</param>
    internal HarnessAgent(
        EnhancedReActAgent inner,
        IMessageBus bus,
        IFilesystem filesystem,
        IGateway gateway,
        IEnumerable<IHarnessMiddleware>? middlewares = null)
    {
        _inner = inner;
        _bus = bus;
        _filesystem = filesystem;
        _gateway = gateway;
        if (middlewares != null) _middlewares.AddRange(middlewares);
    }

    /// <summary>
    /// Sends messages to the agent and returns a response, running through the middleware pipeline.
    /// 向 Agent 发送消息并返回响应，经过中间件管道处理。
    /// </summary>
    /// <param name="messages">Input messages. / 输入消息列表。</param>
    /// <param name="context">Optional runtime context. / 可选的运行时上下文。</param>
    /// <returns>The response message. / 响应消息。</returns>
    public Task<Msg> CallAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
        => ExecuteWithMiddlewareAsync(messages, () => _inner.CallAsync(messages, context), context);

    /// <summary>
    /// Sends a single message to the agent.
    /// 向 Agent 发送单条消息。
    /// </summary>
    /// <param name="message">The message to send. / 要发送的消息。</param>
    /// <param name="context">Optional runtime context. / 可选的运行时上下文。</param>
    /// <returns>The response message. / 响应消息。</returns>
    public Task<Msg> CallAsync(Msg message, RuntimeContext? context = null)
        => CallAsync([message], context);

    /// <summary>
    /// Sends a plain text message to the agent.
    /// 向 Agent 发送纯文本消息。
    /// </summary>
    /// <param name="text">The text content. / 文本内容。</param>
    /// <param name="context">Optional runtime context. / 可选的运行时上下文。</param>
    /// <returns>The response message. / 响应消息。</returns>
    public Task<Msg> CallAsync(string text, RuntimeContext? context = null)
    {
        var msg = Msg.Builder().Role("user").TextContent(text).Build();
        return CallAsync([msg], context);
    }

    /// <summary>
    /// Streams events generated from the given messages.
    /// 从给定消息流式获取事件。
    /// </summary>
    /// <param name="messages">Input messages. / 输入消息列表。</param>
    /// <param name="context">Optional runtime context. / 可选的运行时上下文。</param>
    /// <returns>An async sequence of events. / 事件的异步序列。</returns>
    public async IAsyncEnumerable<Event> StreamEventsAsync(IReadOnlyList<Msg> messages,
        RuntimeContext? context = null)
    {
        await EnhanceSystemPromptAsync(messages, context).ConfigureAwait(false);
        await foreach (var evt in _inner.StreamEventsAsync(messages, context))
            yield return evt;
    }

    /// <summary>
    /// Streams events from a single message.
    /// 从单条消息流式获取事件。
    /// </summary>
    /// <param name="message">The message. / 消息。</param>
    /// <param name="context">Optional runtime context. / 可选的运行时上下文。</param>
    /// <returns>An async sequence of events. / 事件的异步序列。</returns>
    public async IAsyncEnumerable<Event> StreamEventsAsync(Msg message,
        RuntimeContext? context = null)
    {
        await EnhanceSystemPromptAsync(new[] { message }, context).ConfigureAwait(false);
        await foreach (var evt in _inner.StreamEventsAsync(message, context))
            yield return evt;
    }

    /// <inheritdoc cref="CallAsync(Msg, RuntimeContext?)" />
    public Task ObserveAsync(Msg message, RuntimeContext? context = null) => CallAsync(message, context);
    /// <inheritdoc cref="CallAsync(IReadOnlyList{Msg}, RuntimeContext?)" />
    public Task ObserveAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null) => CallAsync(messages, context);
    /// <summary>Interrupts the current agent execution. / 中断当前 Agent 执行。</summary>
    public void Interrupt() => _inner.Interrupt();
    /// <summary>Interrupts the current agent execution with a message. / 用一条消息中断当前 Agent 执行。</summary>
    public void Interrupt(Msg message) => _inner.Interrupt(message);

    /// <summary>
    /// 构建中间件上下文。对标 ExecuteWithMiddlewareAsync 中的上下文构造逻辑。
    /// </summary>
    private MiddlewareContext BuildMiddlewareContext(IReadOnlyList<Msg> messages, RuntimeContext? context)
    {
        var mctx = new MiddlewareContext
        {
            AgentName = Name,
            Runtime = context,
            Messages = messages.ToList()
        };
        mctx.Items["filesystem"] = _filesystem;
        mctx.Items["bus"] = _bus;
        mctx.Items["session_id"] = context?.SessionId ?? "default";
        return mctx;
    }

    /// <summary>
    /// 按 Order 运行所有中间件的系统提示词改写链，将结果写回内层 Agent。
    /// 供 CallAsync（通过 ExecuteWithMiddlewareAsync）和 StreamEventsAsync 共享。
    /// </summary>
    private async Task EnhanceSystemPromptAsync(IReadOnlyList<Msg> messages, RuntimeContext? context)
    {
        var mctx = BuildMiddlewareContext(messages, context);
        var sorted = _middlewares.OrderBy(m => m.Order).ToList();
        if (sorted.Count == 0) return;

        var prompt = _inner.SystemPrompt;
        foreach (var mw in sorted)
        {
            try
            {
                prompt = await mw.OnSystemPromptAsync(mctx, prompt).ConfigureAwait(false);
            }
            catch
            {
                // 提示词注入失败不得中断主流程 // Prompt injection failure must not break the main flow
            }
        }
        _inner.SystemPrompt = prompt;
    }

    /// <summary>
    /// Executes the core agent call wrapped in the middleware pipeline (onion model).
    /// 在中间件管道（洋葱模型）包裹下执行核心 Agent 调用。
    /// </summary>
    private async Task<Msg> ExecuteWithMiddlewareAsync(IReadOnlyList<Msg> messages,
        Func<Task<Msg>> coreFn, RuntimeContext? context)
    {
        await EnhanceSystemPromptAsync(messages, context).ConfigureAwait(false);

        var sorted = _middlewares.OrderBy(m => m.Order).ToList();
        if (sorted.Count == 0) return await coreFn().ConfigureAwait(false);

        var mctx = BuildMiddlewareContext(messages, context);

        // 洋葱模型：每个中间件真正包裹核心调用，因此可以在 next() 前后做事，
        // 也可以选择不调用 next() 来短路整个回合。
        // Onion model: each middleware wraps the core call, so it can act before/after next(),
        // or skip next() entirely to short-circuit the round.
        Msg? result = null;
        var coreInvoked = false;

        async ValueTask RunChain(int index)
        {
            if (index >= sorted.Count)
            {
                coreInvoked = true;
                result = await coreFn().ConfigureAwait(false);
                return;
            }
            await sorted[index].OnAgentAsync(mctx, () => RunChain(index + 1)).ConfigureAwait(false);
        }

        await RunChain(0).ConfigureAwait(false);

        // 有中间件短路了链条：回退为直接执行核心，保持既有调用语义不被破坏。
        // Middleware short-circuited the chain: fall back to direct core execution.
        if (!coreInvoked) result = await coreFn().ConfigureAwait(false);

        return result!;
    }
}
