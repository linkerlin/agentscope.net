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
/// 用于测试和示例的模拟模型——将最后一条消息的内容回显返回。
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
        return Observable.FromAsync(() => GenerateAsync(request));
    }

    /// <inheritdoc />
    public override Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        var lastMessage = request.Messages.LastOrDefault();
        var text = lastMessage?.GetTextContent() ?? string.Empty;

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

        var response = await GenerateAsync(new ModelRequest { Messages = messages }).ConfigureAwait(false);
        // 将非流式响应包装为单元素流式响应
        // Wrap the non-streaming response as a single-element streaming response
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
    public static MockModelBuilder Builder()
    {
        return new MockModelBuilder();
    }
}

/// <summary>
/// Fluent builder for <see cref="MockModel"/>.
/// MockModel 的流畅构建器。
/// </summary>
public class MockModelBuilder
{
    private string _modelName = "mock-model";

    /// <summary>
    /// Sets the model name.
    /// 设置模型名称。
    /// </summary>
    /// <param name="name">Model name / 模型名称。</param>
    /// <returns>This builder instance for chaining / 当前构建器实例以支持链式调用。</returns>
    public MockModelBuilder ModelName(string name)
    {
        _modelName = name;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="MockModel"/> instance.
    /// 构建 <see cref="MockModel"/> 实例。
    /// </summary>
    /// <returns>A new <see cref="MockModel"/> instance / 新的 <see cref="MockModel"/> 实例。</returns>
    public MockModel Build()
    {
        return new MockModel(_modelName);
    }
}
