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
using AgentScope.Core.Message;

namespace AgentScope.Core.Tool;

/// <summary>
/// Event arguments for tool streaming emit events, carrying tool use blocks, result blocks,
/// completion status, and error information.
/// 工具流式推送事件参数，携带工具使用块、结果块、完成状态及错误信息。
/// </summary>
public class ToolEmitEventArgs : EventArgs
{
    /// <summary>
    /// Tool use block representing the tool invocation.
    /// 工具使用块，表示工具调用。
    /// </summary>
    public ToolUseBlock? ToolUse { get; init; }

    /// <summary>
    /// Tool result block representing the execution result.
    /// 工具结果块，表示执行结果。
    /// </summary>
    public ToolResultBlock? ToolResult { get; init; }

    /// <summary>
    /// Whether this is the final event for this tool execution.
    /// 是否为此工具的最终事件。
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Error message if the tool execution failed.
    /// 工具执行失败时的错误信息。
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Tool streaming emit interface that allows real-time progress events during tool execution.
/// Agents can listen to these events for streaming UI or intermediate state handling.
/// 工具流式推送接口，允许在工具执行过程中实时推送进度事件。
/// Agent 可监听这些事件以实现流式 UI 或中间态处理。
/// </summary>
public interface IToolEmitter
{
    /// <summary>
    /// Tool execution progress event.
    /// 工具执行进度事件。
    /// </summary>
    event EventHandler<ToolEmitEventArgs>? OnToolEmit;

    /// <summary>
    /// Subscribe to tool events.
    /// 订阅工具事件。
    /// </summary>
    /// <param name="handler">Event handler / 事件处理器</param>
    void Subscribe(EventHandler<ToolEmitEventArgs> handler);

    /// <summary>
    /// Unsubscribe from tool events.
    /// 取消订阅工具事件。
    /// </summary>
    /// <param name="handler">Event handler / 事件处理器</param>
    void Unsubscribe(EventHandler<ToolEmitEventArgs> handler);
}

/// <summary>
/// Default tool emit implementation supporting both synchronous and asynchronous push.
/// Thread-safe via locking.
/// 默认的工具推送实现，支持同步和异步推送。线程安全。
/// </summary>
public class ToolEmitter : IToolEmitter
{
    // 线程同步锁，保护事件处理器的添加/移除/触发
    private readonly object _lock = new();
    private event EventHandler<ToolEmitEventArgs>? _onToolEmit;

    /// <summary>
    /// Tool execution progress event. Thread-safe add/remove.
    /// 工具执行进度事件，线程安全的添加/移除。
    /// </summary>
    public event EventHandler<ToolEmitEventArgs>? OnToolEmit
    {
        add
        {
            lock (_lock) { _onToolEmit += value; }
        }
        remove
        {
            lock (_lock) { _onToolEmit -= value; }
        }
    }

    /// <inheritdoc />
    public void Subscribe(EventHandler<ToolEmitEventArgs> handler)
    {
        OnToolEmit += handler;
    }

    /// <inheritdoc />
    public void Unsubscribe(EventHandler<ToolEmitEventArgs> handler)
    {
        OnToolEmit -= handler;
    }

    /// <summary>
    /// Emit a tool use start event.
    /// 推送工具调用开始事件。
    /// </summary>
    /// <param name="toolUse">Tool use block / 工具使用块</param>
    public void EmitToolUse(ToolUseBlock toolUse)
    {
        Emit(new ToolEmitEventArgs { ToolUse = toolUse });
    }

    /// <summary>
    /// Emit a tool result event.
    /// 推送工具结果事件。
    /// </summary>
    /// <param name="result">Tool result block / 工具结果块</param>
    public void EmitToolResult(ToolResultBlock result)
    {
        Emit(new ToolEmitEventArgs { ToolResult = result });
    }

    /// <summary>
    /// Emit a tool completion event.
    /// 推送工具完成事件。
    /// </summary>
    /// <param name="toolUse">Optional tool use block / 可选的工具使用块</param>
    /// <param name="result">Optional tool result block / 可选的工具结果块</param>
    public void EmitComplete(ToolUseBlock? toolUse = null, ToolResultBlock? result = null)
    {
        Emit(new ToolEmitEventArgs { ToolUse = toolUse, ToolResult = result, IsComplete = true });
    }

    /// <summary>
    /// Emit a tool error event.
    /// 推送工具错误事件。
    /// </summary>
    /// <param name="error">Error message / 错误信息</param>
    /// <param name="toolUse">Optional tool use block / 可选的工具使用块</param>
    public void EmitError(string error, ToolUseBlock? toolUse = null)
    {
        Emit(new ToolEmitEventArgs { ToolUse = toolUse, Error = error, IsComplete = true });
    }

    /// <summary>
    /// Perform the actual event dispatch to all subscribers.
    /// 执行实际的事件推送，分发给所有订阅者。
    /// </summary>
    /// <param name="args">Event arguments / 事件参数</param>
    protected virtual void Emit(ToolEmitEventArgs args)
    {
        EventHandler<ToolEmitEventArgs>? handler;
        lock (_lock)
        {
            handler = _onToolEmit;
        }
        handler?.Invoke(this, args);
    }

    /// <summary>
    /// Clear all subscriptions. Used for cleanup.
    /// 清理所有订阅，用于资源清理。
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _onToolEmit = null;
        }
    }
}
