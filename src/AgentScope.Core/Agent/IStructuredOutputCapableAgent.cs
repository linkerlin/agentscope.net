// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// 支持结构化输出（如 JSON 反序列化为 T）的 Agent 接口。
/// </summary>
public interface IStructuredOutputCapableAgent : IAgent
{
    /// <summary>
    /// 生成并反序列化为指定类型 T（如 JSON object）。
    /// </summary>
    Task<T> GenerateStructuredOutputAsync<T>(IEnumerable<Msg> messages);

    /// <summary>
    /// 流式生成结构化输出的同时产出事件流。
    /// </summary>
    IAsyncEnumerable<Event> StreamStructuredOutputAsync<T>(IEnumerable<Msg> messages, StreamOptions options);
}
