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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Events;

namespace AgentScope.Core.Agent;

/// <summary>
/// Defines the types of events that can be emitted by a sub-agent during its lifecycle.
/// 定义子 Agent 在其生命周期中可以发出的事件类型。
/// </summary>
public enum SubagentEventType
{
    /// <summary>Sub-agent has been created / 子 Agent 已创建</summary>
    Created,
    /// <summary>Sub-agent has started execution / 子 Agent 已开始执行</summary>
    Started,
    /// <summary>Sub-agent has completed execution successfully / 子 Agent 已成功完成执行</summary>
    Completed,
    /// <summary>Sub-agent execution has failed / 子 Agent 执行失败</summary>
    Failed,
    /// <summary>Sub-agent execution was cancelled / 子 Agent 执行被取消</summary>
    Cancelled,
    /// <summary>Progress update from the sub-agent / 子 Agent 的进度更新</summary>
    Progress,
    /// <summary>Tool call event from the sub-agent / 子 Agent 的工具调用事件</summary>
    ToolCall,
    /// <summary>Tool result event from the sub-agent / 子 Agent 的工具结果事件</summary>
    ToolResult,
    /// <summary>Custom event type for extensibility / 用于扩展的自定义事件类型</summary>
    Custom
}

/// <summary>
/// Event arguments for sub-agent events, containing the sub-agent ID, event type,
/// associated data, and optional AgentEvent reference.
/// 子 Agent 事件参数，包含子 Agent ID、事件类型、关联数据和可选的 AgentEvent 引用。
/// </summary>
public class SubagentEventArgs : EventArgs
{
    /// <summary>
    /// Gets the ID of the sub-agent that emitted the event.
    /// 获取发出事件的子 Agent ID。
    /// </summary>
    public string SubagentId { get; }

    /// <summary>
    /// Gets the type of the event.
    /// 获取事件类型。
    /// </summary>
    public SubagentEventType EventType { get; }

    /// <summary>
    /// Gets the optional data associated with the event.
    /// 获取与事件关联的可选数据。
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Gets the optional AgentEvent associated with this sub-agent event.
    /// 获取与此子 Agent 事件关联的可选 AgentEvent。
    /// </summary>
    public AgentEvent? AgentEvent { get; }

    /// <summary>
    /// Initializes a new instance of the SubagentEventArgs class.
    /// 初始化 SubagentEventArgs 类的新实例。
    /// </summary>
    /// <param name="subagentId">The sub-agent ID / 子 Agent ID</param>
    /// <param name="eventType">The event type / 事件类型</param>
    /// <param name="data">Optional associated data / 可选的关联数据</param>
    /// <param name="agentEvent">Optional associated AgentEvent / 可选的关联 AgentEvent</param>
    public SubagentEventArgs(
        string subagentId,
        SubagentEventType eventType,
        object? data = null,
        AgentEvent? agentEvent = null)
    {
        SubagentId = subagentId ?? throw new ArgumentNullException(nameof(subagentId));
        EventType = eventType;
        Data = data;
        AgentEvent = agentEvent;
    }
}

/// <summary>
/// Event bus for sub-agent lifecycle and execution events.
/// Enables parent agents to subscribe to and wait for events from child agents.
/// Supports both synchronous subscription and async await patterns.
/// 子 Agent 事件总线，用于父 Agent 监听子 Agent 的生命周期与执行事件。
/// 支持同步订阅和异步等待两种模式。
/// </summary>
public class SubagentEventBus
{
    private readonly ConcurrentDictionary<string, List<EventHandler<SubagentEventArgs>>> _handlers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<SubagentEventArgs>> _waiters = new();

    /// <summary>
    /// Subscribes to all events from a specific sub-agent.
    /// 订阅指定子 Agent 的所有事件。
    /// </summary>
    /// <param name="subagentId">The sub-agent ID to subscribe to / 要订阅的子 Agent ID</param>
    /// <param name="handler">The event handler / 事件处理器</param>
    public void Subscribe(string subagentId, EventHandler<SubagentEventArgs> handler)
    {
        _handlers.AddOrUpdate(
            subagentId,
            _ => new List<EventHandler<SubagentEventArgs>> { handler },
            (_, list) =>
            {
                lock (list) { list.Add(handler); }
                return list;
            });
    }

    /// <summary>
    /// Unsubscribes a handler from a specific sub-agent.
    /// 取消订阅指定子 Agent 的事件处理器。
    /// </summary>
    /// <param name="subagentId">The sub-agent ID / 子 Agent ID</param>
    /// <param name="handler">The event handler to remove / 要移除的事件处理器</param>
    public void Unsubscribe(string subagentId, EventHandler<SubagentEventArgs> handler)
    {
        if (_handlers.TryGetValue(subagentId, out var list))
        {
            lock (list) { list.Remove(handler); }
        }
    }

    /// <summary>
    /// Publishes an event to all subscribers of the specified sub-agent.
    /// Also notifies any async waiters waiting for this event type.
    /// 发布事件到指定子 Agent 的所有订阅者。
    /// 同时通知正在等待此事件类型的异步等待者。
    /// </summary>
    /// <param name="subagentId">The sub-agent ID / 子 Agent ID</param>
    /// <param name="args">The event arguments / 事件参数</param>
    public void Publish(string subagentId, SubagentEventArgs args)
    {
        if (_handlers.TryGetValue(subagentId, out var list))
        {
            List<EventHandler<SubagentEventArgs>> snapshot;
            lock (list) { snapshot = new List<EventHandler<SubagentEventArgs>>(list); }

            foreach (var handler in snapshot)
            {
                try
                {
                    handler.Invoke(this, args);
                }
                catch
                {
                    // Ignore exceptions from individual subscribers
                    // 忽略单个订阅者的异常
                }
            }
        }

        // Notify async waiters
        // 通知异步等待者
        if (_waiters.TryRemove($"{subagentId}:{args.EventType}", out var tcs))
        {
            tcs.TrySetResult(args);
        }
    }

    /// <summary>
    /// Waits asynchronously for a specific event type from a sub-agent.
    /// 异步等待指定子 Agent 的特定事件类型。
    /// </summary>
    /// <param name="subagentId">The sub-agent ID / 子 Agent ID</param>
    /// <param name="eventType">The event type to wait for / 要等待的事件类型</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>A task that completes when the event is received / 事件接收时完成的任务</returns>
    public Task<SubagentEventArgs> WaitForEventAsync(
        string subagentId,
        SubagentEventType eventType,
        CancellationToken ct = default)
    {
        var key = $"{subagentId}:{eventType}";
        var tcs = new TaskCompletionSource<SubagentEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _waiters[key] = tcs;

        ct.Register(() =>
        {
            if (_waiters.TryRemove(key, out var pending))
            {
                pending.TrySetCanceled(ct);
            }
        });

        return tcs.Task;
    }

    /// <summary>
    /// Waits for a sub-agent to complete (Completed, Failed, or Cancelled).
    /// Returns the first terminal event that occurs.
    /// 等待子 Agent 完成（Completed / Failed / Cancelled）。
    /// 返回第一个发生的终结事件。
    /// </summary>
    /// <param name="subagentId">The sub-agent ID / 子 Agent ID</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>The terminal event / 终结事件</returns>
    public async Task<SubagentEventArgs> WaitForCompletionAsync(
        string subagentId,
        CancellationToken ct = default)
    {
        // Use Task.WhenAny to wait for any terminal event
        // 使用 Task.WhenAny 等待任意一个终结事件
        var completed = WaitForEventAsync(subagentId, SubagentEventType.Completed, ct);
        var failed = WaitForEventAsync(subagentId, SubagentEventType.Failed, ct);
        var cancelled = WaitForEventAsync(subagentId, SubagentEventType.Cancelled, ct);

        var done = await Task.WhenAny(completed, failed, cancelled).ConfigureAwait(false);
        return await done.ConfigureAwait(false);
    }

    /// <summary>
    /// Clears all subscriptions for a specific sub-agent.
    /// 清除指定子 Agent 的所有订阅。
    /// </summary>
    /// <param name="subagentId">The sub-agent ID / 子 Agent ID</param>
    public void Clear(string subagentId)
    {
        _handlers.TryRemove(subagentId, out _);
    }

    /// <summary>
    /// Clears all subscriptions for all sub-agents.
    /// 清除所有子 Agent 的所有订阅。
    /// </summary>
    public void ClearAll()
    {
        _handlers.Clear();
    }
}
