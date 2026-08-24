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

namespace AgentScope.Core.Credential;

/// <summary>
/// xAI (Grok) 凭据。
/// </summary>
public class XAICredential : CredentialBase
{
    /// <summary>
    /// xAI API 默认基础 URL。
    /// </summary>
    public const string DefaultBaseUrl = "https://api.x.ai";

    /// <summary>
    /// 使用指定的 API 密钥创建 xAI 凭据。
    /// </summary>
    /// <param name="apiKey">xAI API 密钥</param>
    /// <param name="baseUrl">API 基础 URL，默认为 <see cref="DefaultBaseUrl"/></param>
    public XAICredential(string apiKey, string? baseUrl = null)
        : base("xai", apiKey, baseUrl ?? DefaultBaseUrl)
    {
    }

    /// <summary>
    /// XAIChatModel 尚未实现，返回 <c>null</c>。
    /// </summary>
    public override Type? GetChatModelClass() => null;

    /// <summary>
    /// 列出此凭据支持的 xAI 模型。
    /// </summary>
    public override List<ModelCard> ListModels()
    {
        return new List<ModelCard>
        {
            new()
            {
                ModelId = "grok-2",
                DisplayName = "Grok 2",
                ProviderId = Id,
                ContextWindow = 131072,
                Description = "xAI Grok-2 对话模型",
                SupportsStreaming = true,
                SupportsToolUse = true,
            },
            new()
            {
                ModelId = "grok-2-vision",
                DisplayName = "Grok 2 Vision",
                ProviderId = Id,
                ContextWindow = 131072,
                Description = "xAI Grok-2 Vision 多模态模型",
                SupportsStreaming = true,
                SupportsToolUse = true,
            },
        };
    }
}
