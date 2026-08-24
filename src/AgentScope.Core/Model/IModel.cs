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
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Model;

/// <summary>
/// LLM model request, containing the message list and optional parameters.
/// Corresponds to Java: io.agentscope.core.model.ModelRequest
/// LLM 模型请求，包含消息列表和可选参数。
/// 对应 Java: io.agentscope.core.model.ModelRequest
/// </summary>
public class ModelRequest
{
    /// <summary>
    /// Gets or sets the list of messages in the conversation.
    /// 获取或设置对话中的消息列表。
    /// </summary>
    public List<Msg> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets optional request parameters (e.g., temperature, max_tokens, top_p).
    /// These are passed directly to the underlying LLM API.
    /// 获取或设置可选的请求参数（如 temperature、max_tokens、top_p）。
    /// 这些参数会直接传递给底层 LLM API。
    /// </summary>
    public Dictionary<string, object>? Options { get; set; }
}

/// <summary>
/// LLM model response, containing the generated text and metadata.
/// Corresponds to Java: io.agentscope.core.model.ModelResponse
/// LLM 模型响应，包含生成文本和元数据。
/// 对应 Java: io.agentscope.core.model.ModelResponse
/// </summary>
public class ModelResponse
{
    /// <summary>
    /// Gets or sets the generated response text.
    /// 获取或设置生成的响应文本。
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets additional metadata from the response (e.g., model name, timestamp, token counts).
    /// 获取或设置响应中的附加元数据（如模型名称、时间戳、Token 数量）。
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets whether the request was successful.
    /// 获取或设置请求是否成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the request failed.
    /// 获取或设置请求失败时的错误消息。
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// LLM model interface, providing both reactive (observable) and async-await generation patterns.
/// All model implementations must implement this interface.
/// Corresponds to Java: io.agentscope.core.model.IModel
/// LLM 模型接口，提供响应式（Observable）和异步（async-await）两种生成模式。
/// 所有模型实现都必须实现此接口。
/// 对应 Java: io.agentscope.core.model.IModel
/// </summary>
public interface IModel
{
    /// <summary>
    /// Gets the model name (e.g., "gpt-4", "claude-3-opus", "deepseek-chat").
    /// 获取模型名称（例如 "gpt-4"、"claude-3-opus"、"deepseek-chat"）。
    /// </summary>
    string ModelName { get; }
    
    /// <summary>
    /// Generates a response using the reactive (observable) pattern.
    /// Useful for integrating with reactive frameworks like System.Reactive.
    /// 使用响应式（Observable）模式生成响应。
    /// 适用于与 System.Reactive 等响应式框架集成。
    /// </summary>
    IObservable<ModelResponse> Generate(ModelRequest request);
    
    /// <summary>
    /// Generates a response asynchronously using the Task-based async pattern.
    /// This is the primary method for most use cases.
    /// 使用基于 Task 的异步模式生成响应。
    /// 这是大多数用例的主要方法。
    /// </summary>
    Task<ModelResponse> GenerateAsync(ModelRequest request);
}

/// <summary>
/// Unified streaming chat model interface.
/// Adapts providers' GenerateStreamAsync capability for unified consumption by the agent layer.
/// Corresponds to Java: io.agentscope.core.model.IStreamingChatModel
/// 统一的流式聊天模型接口，适配已有 Provider 的流式生成能力，供 Agent 层统一消费。
/// 对应 Java: io.agentscope.core.model.IStreamingChatModel
/// </summary>
public interface IStreamingChatModel
{
    /// <summary>
    /// Generates a streaming chat response as an async-enumerable sequence of ChatResponse chunks.
    /// Each chunk represents a partial or complete response from the model.
    /// 以异步可枚举序列形式生成流式聊天响应，每个 ChatResponse 块代表模型的部分或完整响应。
    /// </summary>
    /// <param name="messages">List of conversation messages / 对话消息列表。</param>
    /// <param name="cancellationToken">Cancellation token for aborting the stream / 用于中止流的取消令牌。</param>
    /// <returns>Async-enumerable sequence of chat response chunks / 聊天响应块的异步可枚举序列。</returns>
    IAsyncEnumerable<ChatResponse> GenerateStreamAsync(List<Msg> messages, CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstract base class for all models, implementing <see cref="IModel"/>.
/// Provides common infrastructure like model name storage and validation.
/// Corresponds to Java: io.agentscope.core.model.ModelBase
/// 所有模型的抽象基类，实现 <see cref="IModel"/> 接口。
/// 提供模型名称存储和验证等公共基础设施。
/// 对应 Java: io.agentscope.core.model.ModelBase
/// </summary>
public abstract class ModelBase : IModel
{
    private readonly string _modelName;
    
    /// <inheritdoc />
    public string ModelName 
    { 
        get => _modelName;
        protected set => throw new InvalidOperationException("ModelName cannot be changed after construction");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelBase"/> class.
    /// 初始化 <see cref="ModelBase"/> 类的新实例。
    /// </summary>
    /// <param name="modelName">Model name (e.g., "gpt-4", "claude-3") / 模型名称（如 "gpt-4"、"claude-3"）。</param>
    /// <exception cref="ArgumentNullException">Thrown when modelName is null / 当 modelName 为 null 时抛出。</exception>
    protected ModelBase(string modelName)
    {
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    /// <inheritdoc />
    public abstract IObservable<ModelResponse> Generate(ModelRequest request);

    /// <inheritdoc />
    public abstract Task<ModelResponse> GenerateAsync(ModelRequest request);
}
