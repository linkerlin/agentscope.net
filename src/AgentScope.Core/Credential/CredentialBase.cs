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
using AgentScope.Core.Model;

namespace AgentScope.Core.Credential;

/// <summary>
/// 凭据抽象基类，封装厂商 API 凭据和关联的模型信息。
/// </summary>
public abstract class CredentialBase
{
    /// <summary>凭据标识符（通常是厂商名称）</summary>
    public string Id { get; }

    /// <summary>API 密钥</summary>
    public string ApiKey { get; }

    /// <summary>API 基础 URL</summary>
    public string? BaseUrl { get; }

    protected CredentialBase(string id, string apiKey, string? baseUrl = null)
    {
        Id = id;
        ApiKey = apiKey;
        BaseUrl = baseUrl;
    }

    /// <summary>
    /// 获取此凭据对应的聊天模型类型。
    /// </summary>
    public abstract System.Type GetChatModelClass();

    /// <summary>
    /// 列出此凭据支持的模型。
    /// </summary>
    public abstract List<ModelCard> ListModels();
}
