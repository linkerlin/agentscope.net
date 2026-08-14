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
/// 工具流式推送事件参数
/// </summary>
public class ToolEmitEventArgs : EventArgs
{
    /// <summary>工具使用块</summary>
    public ToolUseBlock? ToolUse { get; init; }

    /// <summary>工具结果块</summary>
    public ToolResultBlock? ToolResult { get; init; }

    /// <summary>是否为此工具的最终事件</summary>
    public bool IsComplete { get; init; }

    /// <summary>错误信息</summary>
    public string? Error { get; init; }
}

/// <summary>
/// 工具流式推送接口，允许在工具执行过程中实时推送进度事件。
/// Agent 可监听这些事件以实现流式 UI 或中间态处理。
/// </summary>
public interface IToolEmitter
{
    /// <summary>
    /// 工具执行进度事件
    /// </summary>
    event EventHandler<ToolEmitEventArgs>? OnToolEmit;

    /// <summary>
    /// 订阅工具事件
    /// </summary>
    void Subscribe(EventHandler<ToolEmitEventArgs> handler);

    /// <summary>
    /// 取消订阅工具事件
    /// </summary>
    void Unsubscribe(EventHandler<ToolEmitEventArgs> handler);
}

/// <summary>
/// 默认的工具推送实现，支持同步和异步推送。
/// 线程安全。
/// </summary>
public class ToolEmitter : IToolEmitter
{
    private readonly object _lock = new();
    private event EventHandler<ToolEmitEventArgs>? _onToolEmit;

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

    public void Subscribe(EventHandler<ToolEmitEventArgs> handler)
    {
        OnToolEmit += handler;
    }

    public void Unsubscribe(EventHandler<ToolEmitEventArgs> handler)
    {
        OnToolEmit -= handler;
    }

    /// <summary>
    /// 推送工具调用开始事件
    /// </summary>
    public void EmitToolUse(ToolUseBlock toolUse)
    {
        Emit(new ToolEmitEventArgs { ToolUse = toolUse });
    }

    /// <summary>
    /// 推送工具结果事件
    /// </summary>
    public void EmitToolResult(ToolResultBlock result)
    {
        Emit(new ToolEmitEventArgs { ToolResult = result });
    }

    /// <summary>
    /// 推送工具完成事件
    /// </summary>
    public void EmitComplete(ToolUseBlock? toolUse = null, ToolResultBlock? result = null)
    {
        Emit(new ToolEmitEventArgs { ToolUse = toolUse, ToolResult = result, IsComplete = true });
    }

    /// <summary>
    /// 推送工具错误事件
    /// </summary>
    public void EmitError(string error, ToolUseBlock? toolUse = null)
    {
        Emit(new ToolEmitEventArgs { ToolUse = toolUse, Error = error, IsComplete = true });
    }

    /// <summary>
    /// 执行实际的事件推送
    /// </summary>
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
    /// 清理所有订阅
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _onToolEmit = null;
        }
    }
}
