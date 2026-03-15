// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Events;

/// <summary>
/// 流式事件类型，与 Java 版 Event 体系对齐。
/// </summary>
public enum EventType
{
    /// <summary>推理开始</summary>
    ReasoningStart,

    /// <summary>推理内容块</summary>
    ReasoningChunk,

    /// <summary>推理结束</summary>
    ReasoningFinish,

    /// <summary>工具调用开始</summary>
    ToolCallStart,

    /// <summary>工具调用内容块</summary>
    ToolCallChunk,

    /// <summary>工具调用结束</summary>
    ToolCallFinish,

    /// <summary>行动开始</summary>
    ActingStart,

    /// <summary>行动内容块</summary>
    ActingChunk,

    /// <summary>行动结束</summary>
    ActingFinish,

    /// <summary>摘要开始</summary>
    SummaryStart,

    /// <summary>摘要内容块</summary>
    SummaryChunk,

    /// <summary>摘要结束</summary>
    SummaryFinish,

    /// <summary>错误事件</summary>
    Error
}
