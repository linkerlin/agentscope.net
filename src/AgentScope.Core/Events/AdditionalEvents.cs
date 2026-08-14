// Copyright 2024-2026 the original author or authors.
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

using System.Collections.Generic;
using AgentScope.Core.Message;

namespace AgentScope.Core.Events;

/// <summary>
/// 用户确认/拒绝结果事件（HITL 反馈）。对应 Java: UserConfirmResultEvent。
/// </summary>
public record UserConfirmResultEvent(string ReplyId, ConfirmResult Result) : AgentEvent(ReplyId);

/// <summary>
/// 用户确认决策：是否批准、可能的参数修改。
/// 对应 Java: io.agentscope.core.event.ConfirmResult
/// </summary>
public record ConfirmResult(bool Approved, string? Reason = null, string? ModifiedToolCallId = null)
{
    public static ConfirmResult Approve() => new(true);
    public static ConfirmResult Deny(string? reason = null) => new(false, reason);
}

/// <summary>请求停止事件（用户/系统主动中止当前回合）。对应 Java: RequestStopEvent。</summary>
public record RequestStopEvent(string ReplyId, string? Reason = null) : AgentEvent(ReplyId);

/// <summary>数据块流式开始事件。对应 Java: DataBlockStartEvent。</summary>
public record DataBlockStartEvent(string ReplyId, string DataId, string? MimeType = null) : AgentEvent(ReplyId);

/// <summary>数据块流式增量事件。对应 Java: DataBlockDeltaEvent。</summary>
public record DataBlockDeltaEvent(string ReplyId, string DataId, string DeltaBase64) : AgentEvent(ReplyId);

/// <summary>数据块流式结束事件。对应 Java: DataBlockEndEvent。</summary>
public record DataBlockEndEvent(string ReplyId, string DataId) : AgentEvent(ReplyId);

/// <summary>子 Agent 被暴露/实例化事件。对应 Java: SubagentExposedEvent。</summary>
public record SubagentExposedEvent(string ReplyId, string SubagentName, string? SubagentId = null) : AgentEvent(ReplyId);

/// <summary>请求外部执行事件（如外部沙箱/人工工具）。对应 Java: RequireExternalExecutionEvent。</summary>
public record RequireExternalExecutionEvent(string ReplyId, string ExternalId, Dictionary<string, object>? Payload = null)
    : AgentEvent(ReplyId);

/// <summary>外部执行结果事件。对应 Java: ExternalExecutionResultEvent。</summary>
public record ExternalExecutionResultEvent(string ReplyId, string ExternalId, bool Success, object? Result = null, string? Error = null)
    : AgentEvent(ReplyId);

/// <summary>
/// Agent 事件发射器：将 AgentEvent 推送给一组观察者回调。
/// 对应 Java: io.agentscope.core.event.AgentEventEmitter
/// </summary>
public class AgentEventEmitter
{
    private readonly List<Action<AgentEvent>> _listeners = new();
    private readonly object _lock = new();

    /// <summary>注册一个事件监听器，返回可用于取消订阅的句柄。</summary>
    public IDisposable OnEvent(Action<AgentEvent> listener)
    {
        lock (_lock)
        {
            _listeners.Add(listener);
        }

        return new Unsubscriber(() =>
        {
            lock (_lock)
            {
                _listeners.Remove(listener);
            }
        });
    }

    /// <summary>发射一个事件给所有监听器。</summary>
    public void Emit(AgentEvent @event)
    {
        List<Action<AgentEvent>> snapshot;
        lock (_lock)
        {
            snapshot = new List<Action<AgentEvent>>(_listeners);
        }

        foreach (var listener in snapshot)
        {
            try
            {
                listener(@event);
            }
            catch
            {
                // 单个监听器异常不影响其它监听器
            }
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly Action _unsubscribe;
        public Unsubscriber(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}
