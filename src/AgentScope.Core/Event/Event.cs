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

using AgentScope.Core.Message;

namespace AgentScope.Core.Events;

/// <summary>
/// 流式事件，用于统一表示推理/工具调用/行动/摘要等阶段的事件。
/// 与 Java 版 Event 语义对齐，支持 IsLast 终止语义。
/// </summary>
public class Event
{
    /// <summary>事件类型</summary>
    public EventType Type { get; }

    /// <summary>关联消息（可为 null）</summary>
    public Msg? Message { get; }

    /// <summary>是否为该流中的最后一个事件</summary>
    public bool IsLast { get; }

    /// <summary>扩展元数据</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    public Event(EventType type, Msg? message, bool isLast = false, IReadOnlyDictionary<string, object>? metadata = null)
    {
        Type = type;
        Message = message;
        IsLast = isLast;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>是否为推理相关事件</summary>
    public bool IsReasoning =>
        Type == EventType.ReasoningChunk || Type == EventType.ReasoningFinish || Type == EventType.ReasoningStart;

    /// <summary>是否为工具调用相关事件</summary>
    public bool IsToolCall =>
        Type == EventType.ToolCallStart || Type == EventType.ToolCallChunk || Type == EventType.ToolCallFinish;

    /// <summary>是否为行动相关事件</summary>
    public bool IsActing =>
        Type == EventType.ActingStart || Type == EventType.ActingChunk || Type == EventType.ActingFinish;

    /// <summary>是否为摘要相关事件</summary>
    public bool IsSummary =>
        Type == EventType.SummaryStart || Type == EventType.SummaryChunk || Type == EventType.SummaryFinish;

    /// <summary>是否为错误事件</summary>
    public bool IsError => Type == EventType.Error;

    /// <summary>创建错误事件</summary>
    public static Event ErrorEvent(Msg? message, string? errorMessage = null, bool isLast = true)
    {
        var meta = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(errorMessage))
            meta["error"] = errorMessage;
        return new Event(EventType.Error, message, isLast, meta);
    }
}
