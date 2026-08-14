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
/// 组合接口 IAgent：继承 ICallableAgent + IStreamableAgent + IObservableAgent
/// 对应 Java: io.agentscope.core.agent.Agent
/// </summary>
public interface IAgent : ICallableAgent, IStreamableAgent, IObservableAgent
{
    /// <summary>全局唯一 Agent ID</summary>
    string AgentId { get; }

    /// <summary>Agent 名称</summary>
    string Name { get; }

    /// <summary>Agent 描述</summary>
    string Description { get; }

    /// <summary>中断当前操作</summary>
    void Interrupt();

    /// <summary>带消息的中断</summary>
    void Interrupt(Msg message);
}

/// <summary>
/// Agent 抽象基类，实现完整的生命周期管理
/// 对应 Java: io.agentscope.core.agent.AgentBase
/// </summary>
public abstract class AgentBase : IAgent
{
    private readonly List<IHook> _hooks = new();

    public string AgentId { get; } = Guid.NewGuid().ToString();
    public string Name { get; protected set; }
    public string Description { get; protected set; }

    protected IReadOnlyList<IHook> Hooks => _hooks.AsReadOnly();

    protected AgentBase(string name, string? description = null)
    {
        Name = name;
        Description = description ?? $"Agent({AgentId}) {name}";
    }

    public void AddHook(IHook hook)
    {
        _hooks.Add(hook);
    }

    /// <summary>CallAsync 入口（消息列表）</summary>
    public virtual async Task<Msg> CallAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        return await RunLifecycleAsync(messages, DoCallAsync, context);
    }

    /// <summary>CallAsync 入口（单条消息）</summary>
    public virtual async Task<Msg> CallAsync(Msg message, RuntimeContext? context = null)
    {
        return await CallAsync(new[] { message }, context);
    }

    /// <summary>CallAsync 入口（纯文本）</summary>
    public virtual async Task<Msg> CallAsync(string text, RuntimeContext? context = null)
    {
        var msg = Msg.Builder().Role("user").TextContent(text).Build();
        return await CallAsync(new[] { msg }, context);
    }

    /// <summary>子类实现的核心调用逻辑</summary>
    protected abstract Task<Msg> DoCallAsync(IReadOnlyList<Msg> messages);

    /// <summary>前置钩子</summary>
    protected virtual Task<object?> BeforeAgentExecutionAsync(IReadOnlyList<Msg> msgs, RuntimeContext? rc)
        => Task.FromResult<object?>(null);

    /// <summary>后置钩子</summary>
    protected virtual Task AfterAgentExecutionAsync()
        => Task.CompletedTask;

    /// <summary>生命周期编排</summary>
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

    /// <summary>中断当前操作</summary>
    public virtual void Interrupt()
    {
    }

    /// <summary>带消息的中断</summary>
    public virtual void Interrupt(Msg message)
    {
    }

    // IStreamableAgent 默认实现
    public virtual IAsyncEnumerable<Event> StreamEventsAsync(
        IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        throw new NotSupportedException("此 Agent 不支持流式输出");
    }

    public virtual IAsyncEnumerable<Event> StreamEventsAsync(
        Msg message, RuntimeContext? context = null)
    {
        return StreamEventsAsync(new[] { message }, context);
    }

    // IObservableAgent 默认实现
    public async Task ObserveAsync(Msg message, RuntimeContext? context = null)
    {
        await CallAsync(message, context);
    }

    public async Task ObserveAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        await CallAsync(messages, context);
    }
}
