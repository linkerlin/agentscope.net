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

namespace AgentScope.Core.Model.Spi;

/// <summary>
/// Model provider SPI (Service Provider Interface) for the AgentScope framework.
/// Each model vendor implements this interface to support model lookup and creation via ModelRegistry.
/// Corresponds to Java: io.agentscope.core.model.spi.IModelProvider
/// 模型提供程序 SPI（服务提供程序接口）。
/// 各厂商实现此接口以支持通过 ModelRegistry 按配置名称查找和创建模型。
/// 对应 Java: io.agentscope.core.model.spi.IModelProvider
/// </summary>
public interface IModelProvider
{
    /// <summary>
    /// Gets the provider identifier (e.g., "openai", "anthropic", "deepseek").
    /// 获取提供商标识符（例如 "openai"、"anthropic"、"deepseek"）。
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Determines whether this provider supports the specified model identifier.
    /// Typically uses prefix matching (e.g., "gpt-4" matches "gpt-4o", "gpt-4-turbo").
    /// 判断此提供程序是否支持指定的模型标识符。
    /// 通常使用前缀匹配（例如 "gpt-4" 匹配 "gpt-4o"、"gpt-4-turbo"）。
    /// </summary>
    /// <param name="modelId">Model identifier (e.g., "gpt-4o", "claude-3") / 模型标识符。</param>
    /// <returns>True if this provider supports the model / 如果此提供程序支持该模型则返回 true。</returns>
    bool Supports(string modelId);

    /// <summary>
    /// Creates a model instance for the specified model identifier.
    /// 为指定的模型标识符创建模型实例。
    /// </summary>
    /// <param name="modelId">Model identifier / 模型标识符。</param>
    /// <param name="apiKey">API key for authentication / API 密钥。</param>
    /// <param name="baseUrl">Optional custom base URL / 可选的自定义基础 URL。</param>
    /// <returns>A new model instance / 新的模型实例。</returns>
    IModel Create(string modelId, string apiKey, string? baseUrl = null);
}
