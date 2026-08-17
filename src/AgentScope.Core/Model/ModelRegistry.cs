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

using System.Collections.Concurrent;
using AgentScope.Core.Model.Spi;

namespace AgentScope.Core.Model;

/// <summary>
/// Thread-safe model registry that resolves model instances by configuration name.
/// Supports dynamic registration and creation of models via SPI providers.
/// Corresponds to Java: io.agentscope.core.model.ModelRegistry
/// 按配置名称查找模型的线程安全服务中心。
/// 支持通过 SPI 提供程序动态注册和创建模型。
/// 对应 Java: io.agentscope.core.model.ModelRegistry
/// </summary>
public class ModelRegistry
{
    /// <summary>
    /// Dictionary of registered model providers, keyed by provider ID (case-insensitive).
    /// 已注册的模型提供程序字典，键为提供程序 ID（不区分大小写）。
    /// </summary>
    private readonly ConcurrentDictionary<string, IModelProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Cache of created model instances, keyed by a composite cache key.
    /// 已创建的模型实例缓存，键为复合缓存键。
    /// </summary>
    private readonly ConcurrentDictionary<string, IModel> _instances = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a model provider for later model creation.
    /// 注册一个模型提供程序，用于后续的模型创建。
    /// </summary>
    /// <param name="provider">The model provider to register / 要注册的模型提供程序。</param>
    /// <exception cref="ArgumentNullException">Thrown when provider is null / 当 provider 为 null 时抛出。</exception>
    public void RegisterProvider(IModelProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _providers[provider.ProviderId] = provider;
    }

    /// <summary>
    /// Unregisters a previously registered provider by its ID.
    /// 根据提供程序 ID 注销之前注册的提供程序。
    /// </summary>
    /// <param name="providerId">Provider identifier / 提供程序标识符。</param>
    public void UnregisterProvider(string providerId)
    {
        _providers.TryRemove(providerId, out _);
    }

    /// <summary>
    /// Gets all registered provider identifiers.
    /// 获取所有已注册的提供程序标识符。
    /// </summary>
    /// <returns>Collection of provider IDs / 提供程序 ID 集合。</returns>
    public IEnumerable<string> GetProviderIds() => _providers.Keys;

    /// <summary>
    /// Finds the first provider that supports the specified model identifier.
    /// 查找支持指定模型标识符的第一个提供程序。
    /// </summary>
    /// <param name="modelId">Model identifier to check / 要检查的模型标识符。</param>
    /// <returns>The first matching provider, or null if none found / 第一个匹配的提供程序，如果未找到则返回 null。</returns>
    public IModelProvider? FindProvider(string modelId)
    {
        foreach (var provider in _providers.Values)
        {
            if (provider.Supports(modelId))
            {
                return provider;
            }
        }
        return null;
    }

    /// <summary>
    /// Creates a new model instance or retrieves a cached one.
    /// Uses a composite cache key combining provider, model ID, and base URL.
    /// 创建新的模型实例或获取缓存的实例。
    /// 使用组合缓存键（提供程序 + 模型 ID + 基础 URL）。
    /// </summary>
    /// <param name="modelId">Model identifier (e.g., "gpt-4", "claude-3") / 模型标识符。</param>
    /// <param name="apiKey">API key for authentication / 用于认证的 API 密钥。</param>
    /// <param name="baseUrl">Optional custom base URL / 可选的自定义基础 URL。</param>
    /// <param name="preferredProvider">Optional preferred provider ID / 可选的首选提供程序 ID。</param>
    /// <returns>A model instance / 模型实例。</returns>
    /// <exception cref="NotSupportedException">Thrown when no provider supports the model / 当没有提供程序支持该模型时抛出。</exception>
    public IModel GetOrCreate(string modelId, string apiKey, string? baseUrl = null, string? preferredProvider = null)
    {
        var cacheKey = $"{preferredProvider ?? "*"}:{modelId}:{baseUrl ?? ""}";

        return _instances.GetOrAdd(cacheKey, _ =>
        {
            IModelProvider? provider = null;

            // Try preferred provider first, then fall back to auto-detection
            // 先尝试首选提供程序，然后回退到自动检测
            if (preferredProvider != null && _providers.TryGetValue(preferredProvider, out var p))
            {
                provider = p;
            }

            provider ??= FindProvider(modelId);

            if (provider == null)
            {
                throw new NotSupportedException($"没有已注册的提供程序支持模型 '{modelId}'。/ No registered provider supports model '{modelId}'.");
            }

            return provider.Create(modelId, apiKey, baseUrl);
        });
    }

    /// <summary>
    /// Clears all cached model instances. Providers are not affected.
    /// 清除所有缓存的模型实例。提供程序不受影响。
    /// </summary>
    public void ClearCache()
    {
        _instances.Clear();
    }
}
