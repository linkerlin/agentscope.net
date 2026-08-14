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
/// 子 Agent 事件类型
/// </summary>
public enum SubagentEventType
{
    Created,
    Started,
    Completed,
    Failed,
    Cancelled,
    Progress,
    ToolCall,
    ToolResult,
    Custom
}

/// <summary>
/// 子 Agent 事件参数
/// </summary>
public class SubagentEventArgs : EventArgs
{
    /// <summary>子 Agent ID</summary>
    public string SubagentId { get; }

    /// <summary>事件类型</summary>
    public SubagentEventType EventType { get; }

    /// <summary>关联数据</summary>
    public object? Data { get; }

    /// <summary>关联 Agent 事件（可选）</summary>
    public AgentEvent? AgentEvent { get; }

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
/// 子 Agent 事件总线，用于父 Agent 监听子 Agent 的生命周期与执行事件。
/// 支持同步订阅和异步等待。
/// </summary>
public class SubagentEventBus
{
    private readonly ConcurrentDictionary<string, List<EventHandler<SubagentEventArgs>>> _handlers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<SubagentEventArgs>> _waiters = new();

    /// <summary>
    /// 订阅指定子 Agent 的所有事件
    /// </summary>
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
    /// 取消订阅
    /// </summary>
    public void Unsubscribe(string subagentId, EventHandler<SubagentEventArgs> handler)
    {
        if (_handlers.TryGetValue(subagentId, out var list))
        {
            lock (list) { list.Remove(handler); }
        }
    }

    /// <summary>
    /// 发布事件到指定子 Agent 的订阅者
    /// </summary>
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
                    // 忽略单个订阅者的异常
                }
            }
        }

        // 通知等待者
        if (_waiters.TryRemove($"{subagentId}:{args.EventType}", out var tcs))
        {
            tcs.TrySetResult(args);
        }
    }

    /// <summary>
    /// 等待指定子 Agent 的特定事件类型
    /// </summary>
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
    /// 等待子 Agent 完成（Completed / Failed / Cancelled）
    /// </summary>
    public async Task<SubagentEventArgs> WaitForCompletionAsync(
        string subagentId,
        CancellationToken ct = default)
    {
        // 使用 Task.WhenAny 等待任意一个终结事件
        var completed = WaitForEventAsync(subagentId, SubagentEventType.Completed, ct);
        var failed = WaitForEventAsync(subagentId, SubagentEventType.Failed, ct);
        var cancelled = WaitForEventAsync(subagentId, SubagentEventType.Cancelled, ct);

        var done = await Task.WhenAny(completed, failed, cancelled).ConfigureAwait(false);
        return await done.ConfigureAwait(false);
    }

    /// <summary>
    /// 清除指定子 Agent 的所有订阅
    /// </summary>
    public void Clear(string subagentId)
    {
        _handlers.TryRemove(subagentId, out _);
    }

    /// <summary>
    /// 清除所有订阅
    /// </summary>
    public void ClearAll()
    {
        _handlers.Clear();
    }
}
