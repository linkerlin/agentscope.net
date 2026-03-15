// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// 支持流式事件输出的 Agent 接口，与 Java StreamableAgent 对齐。
/// 增量引入，不破坏现有 IAgent 用法。
/// </summary>
public interface IStreamableAgent : IAgent
{
    /// <summary>
    /// 流式调用，按事件序列产出（推理/工具调用/行动/摘要等）。
    /// </summary>
    /// <param name="messages">输入消息</param>
    /// <param name="options">流选项</param>
    /// <returns>事件流</returns>
    IAsyncEnumerable<Event> StreamAsync(IEnumerable<Msg> messages, StreamOptions options);

    /// <summary>
    /// 单条消息流式调用（使用默认选项）
    /// </summary>
    IAsyncEnumerable<Event> StreamAsync(Msg message, StreamOptions? options = null);
}
