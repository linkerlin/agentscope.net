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
using AgentScope.Core.Model.Spi;

namespace AgentScope.Core.Model;

/// <summary>
/// Abstract base class for model provider SPI support.
/// Provides common capability declaration and model ID prefix matching for IModelProvider implementations.
/// Subclasses only need to define ProviderId, SupportedPrefixes, and the Create method.
/// Corresponds to Java: io.agentscope.core.model.ModelProviderSupport
/// 模型提供程序 SPI 支持基类：为 IModelProvider 提供通用的能力声明与模型标识前缀匹配。
/// 子类只需定义 ProviderId、SupportedPrefixes 和 Create 方法。
/// 对应 Java: io.agentscope.core.model.ModelProviderSupport
/// </summary>
public abstract class ModelProviderSupport : IModelProvider
{
    /// <inheritdoc />
    public abstract string ProviderId { get; }

    /// <summary>
    /// Gets the collection of model ID prefixes supported by this provider (lowercase).
    /// Used by the default Supports() implementation for prefix-based matching.
    /// 获取此提供程序支持的模型标识前缀集合（小写）。
    /// 由默认的 Supports() 实现用于基于前缀的匹配。
    /// </summary>
    protected abstract IReadOnlyCollection<string> SupportedPrefixes { get; }

    /// <inheritdoc />
    public virtual bool Supports(string modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return false;
        var id = modelId.ToLowerInvariant();
        foreach (var prefix in SupportedPrefixes)
        {
            if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public abstract IModel Create(string modelId, string apiKey, string? baseUrl = null);
}
