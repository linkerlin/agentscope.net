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
/// 聊天模型抽象基类：在 ModelBase 基础上提供聊天语义（消息列表 -> ChatResponse）的统一抽象。
/// 对应 Java: io.agentscope.core.model.ChatModelBase
/// </summary>
public abstract class ChatModelBase : ModelBase
{
    /// <summary>提供商标识（如 openai/anthropic）。</summary>
    public string Provider { get; }

    /// <summary>是否在请求中携带结构化输出提醒。</summary>
    public bool StructuredOutputEnabled { get; set; }

    protected ChatModelBase(string modelName, string provider) : base(modelName)
    {
        Provider = provider ?? "";
    }

    /// <summary>
    /// 子类实现：发送聊天请求并返回完整响应。
    /// </summary>
    public abstract Task<ChatResponse> ChatAsync(
        List<Msg> messages,
        Dictionary<string, object>? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 默认把 GenerateAsync 代理到 ChatAsync 并取 Content。
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
