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
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.Model;

/// <summary>
/// Mock model for testing and sample purposes — echoes back the last message content.
/// Useful for unit tests, integration tests, and demo scenarios where no real LLM API is needed.
/// Supports both synchronous (IObservable) and streaming (IAsyncEnumerable) generation.
/// Corresponds to Java: io.agentscope.core.model.MockModel
/// 用于测试和示例的模拟模型——将最后一条消息的内容回显返回。
/// 适用于单元测试、集成测试和无需真实 LLM API 的演示场景。
/// 支持同步（IObservable）和流式（IAsyncEnumerable）两种生成方式。
/// 对应 Java: io.agentscope.core.model.MockModel
/// </summary>
public class MockModel : ModelBase, IStreamingChatModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MockModel"/> class.
    /// 初始化 <see cref="MockModel"/> 类的新实例。
    /// </summary>
    /// <param name="modelName">Model name (default "mock-model") / 模型名称（默认为 "mock-model"）。</param>
    public MockModel(string modelName = "mock-model") : base(modelName)
    {
    }

    /// <inheritdoc />
    public override IObservable<ModelResponse> Generate(ModelRequest request)
    {
        // Wrap the async method as an observable for Rx-style consumption
        // 将异步方法包装为可观察对象以支持 Rx 风格消费
        return Observable.FromAsync(() => GenerateAsync(request));
    }

    /// <inheritdoc />
    public override Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        // Extract the last message text from the request as the echo source
        // 从请求中提取最后一条消息的文本作为回显源
        var lastMessage = request.Messages.LastOrDefault();
        var text = lastMessage?.GetTextContent() ?? string.Empty;

        // Build a successful response with the echoed text and metadata
        // 构建包含回显文本和元数据的成功响应
        var response = new ModelResponse
        {
            Success = true,
            Text = $"Echo: {text}",
            Metadata = new System.Collections.Generic.Dictionary<string, object>
            {
                ["model"] = ModelName,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponse> GenerateStreamAsync(
        List<AgentScope.Core.Message.Msg> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Delegate to the non-streaming GenerateAsync and wrap as a single-element stream
        // 委托给非流式 GenerateAsync 并包装为单元素流
        var response = await GenerateAsync(new ModelRequest { Messages = messages }).ConfigureAwait(false);

        // Yield a single ChatResponse with IsComplete=true to signal stream end
        // 生成一个 IsComplete=true 的 ChatResponse 以标识流结束
        yield return new ChatResponse
        {
            Success = response.Success,
            Error = response.Error,
            Text = response.Text,
            Content = response.Text,
            Metadata = response.Metadata,
            Model = ModelName,
            IsComplete = true
        };
    }

    /// <summary>
    /// Creates a new <see cref="MockModelBuilder"/> for fluent construction.
    /// 创建新的 <see cref="MockModelBuilder"/> 以支持流畅构建。
    /// </summary>
    /// <returns>A new builder instance / 新的构建器实例。</returns>
    public static MockModelBuilder Builder()
    {
        return new MockModelBuilder();
    }
}

/// <summary>
/// Fluent builder for <see cref="MockModel"/> using the builder pattern.
/// Allows configuring model properties before construction.
/// Corresponds to Java: io.agentscope.core.model.MockModelBuilder
/// MockModel 的流畅构建器，使用构建器模式。
/// 允许在构造前配置模型属性。
/// 对应 Java: io.agentscope.core.model.MockModelBuilder
/// </summary>
public class MockModelBuilder
{
    /// <summary>
    /// Internal model name storage with default value.
    /// 内部模型名称存储，带有默认值。
    /// </summary>
    private string _modelName = "mock-model";

    /// <summary>
    /// Sets the model name for the mock model.
    /// 设置模拟模型的模型名称。
    /// </summary>
    /// <param name="name">Model name / 模型名称。</param>
    /// <returns>This builder instance for method chaining / 当前构建器实例以支持链式调用。</returns>
    public MockModelBuilder ModelName(string name)
    {
        _modelName = name;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="MockModel"/> instance with the configured properties.
    /// 使用已配置的属性构建 <see cref="MockModel"/> 实例。
    /// </summary>
    /// <returns>A new <see cref="MockModel"/> instance / 新的 <see cref="MockModel"/> 实例。</returns>
    public MockModel Build()
    {
        return new MockModel(_modelName);
    }
}
