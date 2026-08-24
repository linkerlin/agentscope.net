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
using AgentScope.Core.Hook;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// Composite interface IAgent that combines ICallableAgent, IStreamableAgent, and IObservableAgent.
/// This is the core abstraction for all agents in the AgentScope framework.
/// 组合接口 IAgent：继承 ICallableAgent（可调用）、IStreamableAgent（可流式输出）和 IObservableAgent（可观察）。
/// 这是 AgentScope 框架中所有 Agent 的核心抽象接口，对应 Java: io.agentscope.core.agent.Agent。
/// </summary>
public interface IAgent : ICallableAgent, IStreamableAgent, IObservableAgent
{
    /// <summary>
    /// Gets the globally unique identifier for this agent instance.
    /// 获取当前 Agent 实例的全局唯一标识符。
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Gets the human-readable name of this agent.
    /// 获取当前 Agent 的可读名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this agent, explaining its purpose and capabilities.
    /// 获取当前 Agent 的描述信息，说明其用途和能力。
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Interrupts the current operation of this agent.
    /// 中断当前 Agent 正在执行的操作。
    /// </summary>
    void Interrupt();

    /// <summary>
    /// Interrupts the current operation with a specific message providing context.
    /// 使用指定的消息中断当前操作，提供中断原因上下文。
    /// </summary>
    /// <param name="message">The message providing interruption context / 提供中断上下文的消息</param>
    void Interrupt(Msg message);
}

/// <summary>
/// Abstract base class for agents, implementing the complete lifecycle management.
/// Provides default implementations for IAgent interface methods including
/// call, stream, observe, and interrupt operations.
/// 抽象 Agent 基类，实现完整的生命周期管理。
/// 提供 IAgent 接口方法的默认实现，包括调用、流式输出、观察和中断操作。
/// 对应 Java: io.agentscope.core.agent.AgentBase
/// </summary>
public abstract class AgentBase : IAgent
{
    /// <summary>
    /// Internal list of lifecycle hooks attached to this agent.
    /// 附加到当前 Agent 的生命周期钩子列表。
    /// </summary>
    private readonly List<IHook> _hooks = new();

    /// <summary>
    /// Gets the globally unique identifier for this agent instance, auto-generated as a GUID.
    /// 获取当前 Agent 实例的全局唯一标识符，自动生成为 GUID。
    /// </summary>
    public string AgentId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the human-readable name of this agent.
    /// 获取或设置当前 Agent 的可读名称。
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    /// Gets or sets the description of this agent.
    /// 获取或设置当前 Agent 的描述信息。
    /// </summary>
    public string Description { get; protected set; }

    /// <summary>
    /// Gets the read-only list of hooks attached to this agent.
    /// 获取附加到当前 Agent 的只读钩子列表。
    /// </summary>
    protected IReadOnlyList<IHook> Hooks => _hooks.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the AgentBase class.
    /// 初始化 AgentBase 类的新实例。
    /// </summary>
    /// <param name="name">Agent name / Agent 名称</param>
    /// <param name="description">Optional description; defaults to "Agent({AgentId}) {name}" if null / 可选的描述信息，为 null 时使用默认值</param>
    protected AgentBase(string name, string? description = null)
    {
        Name = name;
        Description = description ?? $"Agent({AgentId}) {name}";
    }

    /// <summary>
    /// Adds a lifecycle hook to this agent.
    /// 向当前 Agent 添加一个生命周期钩子。
    /// </summary>
    /// <param name="hook">The hook to add / 要添加的钩子</param>
    public void AddHook(IHook hook)
    {
        _hooks.Add(hook);
    }

    /// <summary>
    /// Calls the agent with a list of messages. Entry point for message list invocation.
    /// 使用消息列表调用 Agent，消息列表调用的入口点。
    /// </summary>
    public virtual async Task<Msg> CallAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        return await RunLifecycleAsync(messages, DoCallAsync, context);
    }

    /// <summary>
    /// Calls the agent with a single message. Entry point for single message invocation.
    /// 使用单条消息调用 Agent，单条消息调用的入口点。
    /// </summary>
    public virtual async Task<Msg> CallAsync(Msg message, RuntimeContext? context = null)
    {
        return await CallAsync(new[] { message }, context);
    }

    /// <summary>
    /// Calls the agent with plain text, automatically wrapped as a user message.
    /// 使用纯文本调用 Agent，自动包装为用户消息。
    /// </summary>
    public virtual async Task<Msg> CallAsync(string text, RuntimeContext? context = null)
    {
        var msg = Msg.Builder().Role("user").TextContent(text).Build();
        return await CallAsync(new[] { msg }, context);
    }

    /// <summary>
    /// Core call logic to be implemented by subclasses. This is the actual processing method.
    /// 由子类实现的核心调用逻辑，是实际的处理方法。
    /// </summary>
    /// <param name="messages">Input messages / 输入消息列表</param>
    /// <returns>Result message / 结果消息</returns>
    protected abstract Task<Msg> DoCallAsync(IReadOnlyList<Msg> messages);

    /// <summary>
    /// Pre-execution hook, called before the core logic runs.
    /// 前置钩子，在核心逻辑执行之前调用。
    /// </summary>
    /// <param name="msgs">Input messages / 输入消息</param>
    /// <param name="rc">Runtime context / 运行时上下文</param>
    /// <returns>Optional result object / 可选的返回对象</returns>
    protected virtual Task<object?> BeforeAgentExecutionAsync(IReadOnlyList<Msg> msgs, RuntimeContext? rc)
        => Task.FromResult<object?>(null);

    /// <summary>
    /// Post-execution hook, called after the core logic completes (in finally block).
    /// 后置钩子，在核心逻辑执行完成后调用（在 finally 块中执行）。
    /// </summary>
    protected virtual Task AfterAgentExecutionAsync()
        => Task.CompletedTask;

    /// <summary>
    /// Lifecycle orchestrator that manages pre/post hooks and runtime context.
    /// 生命周期编排方法，管理前置/后置钩子和运行时上下文。
    /// </summary>
    /// <param name="messages">Input messages / 输入消息</param>
    /// <param name="coreFn">Core processing function / 核心处理函数</param>
    /// <param name="context">Runtime context / 运行时上下文</param>
    /// <returns>Result message / 结果消息</returns>
    protected async Task<Msg> RunLifecycleAsync(
        IReadOnlyList<Msg> messages,
        Func<IReadOnlyList<Msg>, Task<Msg>> coreFn,
        RuntimeContext? context)
    {
        RuntimeContext.Current = context;
        await BeforeAgentExecutionAsync(messages, context);
        try
        {
            return await coreFn(messages);
        }
        finally
        {
            await AfterAgentExecutionAsync();
            RuntimeContext.Current = null;
        }
    }

    /// <summary>
    /// Interrupts the current operation. Default implementation is a no-op.
    /// 中断当前操作。默认实现为空操作。
    /// </summary>
    public virtual void Interrupt()
    {
    }

    /// <summary>
    /// Interrupts the current operation with a specific message. Default implementation is a no-op.
    /// 使用指定消息中断当前操作。默认实现为空操作。
    /// </summary>
    /// <param name="message">Interruption context message / 中断上下文消息</param>
    public virtual void Interrupt(Msg message)
    {
    }

    /// <summary>
    /// Default implementation of IStreamableAgent.StreamEventsAsync for message list.
    /// Throws NotSupportedException by default; override in subclasses that support streaming.
    /// IStreamableAgent.StreamEventsAsync 的默认实现（消息列表版本）。
    /// 默认抛出 NotSupportedException；在支持流式输出的子类中重写。
    /// </summary>
    public virtual IAsyncEnumerable<Event> StreamEventsAsync(
        IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        throw new NotSupportedException("此 Agent 不支持流式输出 / This agent does not support streaming output");
    }

    /// <summary>
    /// Default implementation of IStreamableAgent.StreamEventsAsync for single message.
    /// Delegates to the list overload.
    /// IStreamableAgent.StreamEventsAsync 的默认实现（单条消息版本）。
    /// 委托给列表重载方法。
    /// </summary>
    public virtual IAsyncEnumerable<Event> StreamEventsAsync(
        Msg message, RuntimeContext? context = null)
    {
        return StreamEventsAsync(new[] { message }, context);
    }

    /// <summary>
    /// Default implementation of IObservableAgent.ObserveAsync for single message.
    /// Delegates to CallAsync by default.
    /// IObservableAgent.ObserveAsync 的默认实现（单条消息版本）。
    /// 默认委托给 CallAsync 处理。
    /// </summary>
    public async Task ObserveAsync(Msg message, RuntimeContext? context = null)
    {
        await CallAsync(message, context);
    }

    /// <summary>
    /// Default implementation of IObservableAgent.ObserveAsync for multiple messages.
    /// Delegates to CallAsync by default.
    /// IObservableAgent.ObserveAsync 的默认实现（多条消息版本）。
    /// 默认委托给 CallAsync 处理。
    /// </summary>
    public async Task ObserveAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        await CallAsync(messages, context);
    }
}
