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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Model;

/// <summary>
/// Abstract base class for chat models, providing a unified abstraction over chat semantics (message list → ChatResponse).
/// 聊天模型抽象基类，在 ModelBase 基础上提供聊天语义（消息列表 → ChatResponse）的统一抽象。
/// 对应 Java: io.agentscope.core.model.ChatModelBase
/// </summary>
public abstract class ChatModelBase : ModelBase
{
    /// <summary>
    /// Gets the provider identifier (e.g., "openai", "anthropic").
    /// 获取提供商标识（如 "openai"、"anthropic"）。
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Gets or sets whether to include structured output reminders in requests.
    /// 获取或设置是否在请求中携带结构化输出提醒。
    /// </summary>
    public bool StructuredOutputEnabled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatModelBase"/> class.
    /// 初始化 <see cref="ChatModelBase"/> 类的新实例。
    /// </summary>
    /// <param name="modelName">Model name / 模型名称。</param>
    /// <param name="provider">Provider identifier / 提供商标识。</param>
    protected ChatModelBase(string modelName, string provider) : base(modelName)
    {
        Provider = provider ?? "";
    }

    /// <summary>
    /// Subclass implementation: sends a chat request and returns the full response.
    /// 子类实现：发送聊天请求并返回完整响应。
    /// </summary>
    /// <param name="messages">List of messages in the conversation / 对话中的消息列表。</param>
    /// <param name="options">Optional request parameters / 可选的请求参数。</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌。</param>
    /// <returns>Chat response / 聊天响应。</returns>
    public abstract Task<ChatResponse> ChatAsync(
        List<Msg> messages,
        Dictionary<string, object>? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Default implementation: delegates <see cref="GenerateAsync"/> to <see cref="ChatAsync"/> and extracts the content.
    /// 默认实现：将 <see cref="GenerateAsync"/> 代理到 <see cref="ChatAsync"/> 并提取 Content 内容。
    /// </summary>
    public override async Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        var chat = await ChatAsync(request.Messages, request.Options).ConfigureAwait(false);
        return new ModelResponse
        {
            Text = chat.Content,
            Metadata = chat.Metadata,
            Success = chat.Success,
            Error = chat.Error
        };
    }
}
