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

using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// Defines the contract for agents that support structured output generation,
/// such as deserializing LLM responses into strongly-typed objects (e.g., JSON).
/// This enables type-safe interaction with language models for structured data extraction.
/// 支持结构化输出（如将 LLM 响应反序列化为强类型对象，例如 JSON）的 Agent 接口。
/// 这实现了与语言模型进行类型安全的结构化数据交互。
/// </summary>
public interface IStructuredOutputCapableAgent : IAgent
{
    /// <summary>
    /// Generates a structured output and deserializes it into the specified type T.
    /// Typically used for JSON object extraction from LLM responses.
    /// 生成结构化输出并将其反序列化为指定类型 T。
    /// 通常用于从 LLM 响应中提取 JSON 对象。
    /// </summary>
    /// <typeparam name="T">The target type for deserialization / 反序列化的目标类型</typeparam>
    /// <param name="messages">Input messages to process / 要处理的输入消息</param>
    /// <returns>The deserialized structured output / 反序列化后的结构化输出</returns>
    Task<T> GenerateStructuredOutputAsync<T>(IEnumerable<Msg> messages);

    /// <summary>
    /// Streams structured output generation while producing an event stream.
    /// Enables real-time processing of structured data as it's being generated.
    /// 流式生成结构化输出的同时产出事件流。
    /// 支持在结构化数据生成过程中进行实时处理。
    /// </summary>
    /// <typeparam name="T">The target type for deserialization / 反序列化的目标类型</typeparam>
    /// <param name="messages">Input messages to process / 要处理的输入消息</param>
    /// <param name="options">Streaming options and configuration / 流式选项和配置</param>
    /// <returns>An async enumerable of events produced during structured output generation / 结构化输出生成过程中产生的事件异步枚举</returns>
    IAsyncEnumerable<Event> StreamStructuredOutputAsync<T>(IEnumerable<Msg> messages, StreamOptions options);
}
