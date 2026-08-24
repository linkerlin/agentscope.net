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
/// Kimi (Moonshot AI) 凭据。
/// </summary>
public class KimiCredential : CredentialBase
{
    /// <summary>
    /// Kimi API 默认基础 URL。
    /// </summary>
    public const string DefaultBaseUrl = "https://api.moonshot.cn/v1";

    /// <summary>
    /// 使用指定的 API 密钥创建 Kimi 凭据。
    /// </summary>
    /// <param name="apiKey">Kimi API 密钥</param>
    /// <param name="baseUrl">API 基础 URL，默认为 <see cref="DefaultBaseUrl"/></param>
    public KimiCredential(string apiKey, string? baseUrl = null)
        : base("kimi", apiKey, baseUrl ?? DefaultBaseUrl)
    {
    }

    /// <summary>
    /// KimiChatModel 尚未实现，返回 <c>null</c>。
    /// </summary>
    public override Type? GetChatModelClass() => null;

    /// <summary>
    /// 列出此凭据支持的 Kimi 模型。
    /// </summary>
    public override List<ModelCard> ListModels()
    {
        return new List<ModelCard>
        {
            new()
            {
                ModelId = "moonshot-v1-8k",
                DisplayName = "Moonshot v1 8K",
                ProviderId = Id,
                ContextWindow = 8192,
                Description = "Kimi Moonshot v1 8K 对话模型",
                SupportsStreaming = true,
                SupportsToolUse = true,
            },
            new()
            {
                ModelId = "moonshot-v1-32k",
                DisplayName = "Moonshot v1 32K",
                ProviderId = Id,
                ContextWindow = 32768,
                Description = "Kimi Moonshot v1 32K 对话模型",
                SupportsStreaming = true,
                SupportsToolUse = true,
            },
            new()
            {
                ModelId = "moonshot-v1-128k",
                DisplayName = "Moonshot v1 128K",
                ProviderId = Id,
                ContextWindow = 131072,
                Description = "Kimi Moonshot v1 128K 长上下文对话模型",
                SupportsStreaming = true,
                SupportsToolUse = true,
            },
        };
    }
}
