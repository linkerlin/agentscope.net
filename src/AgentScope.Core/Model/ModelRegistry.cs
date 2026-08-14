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
/// 按配置名称查找模型的服务中心，线程安全。
/// 支持通过 SPI 提供程序动态注册和创建模型。
/// 对标 Java ModelRegistry。
/// </summary>
public class ModelRegistry
{
    private readonly ConcurrentDictionary<string, IModelProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IModel> _instances = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 注册一个模型提供程序。
    /// </summary>
    public void RegisterProvider(IModelProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _providers[provider.ProviderId] = provider;
    }

    /// <summary>
    /// 注销指定提供程序。
    /// </summary>
    public void UnregisterProvider(string providerId)
    {
        _providers.TryRemove(providerId, out _);
    }

    /// <summary>
    /// 获取已注册的所有提供程序标识符。
    /// </summary>
    public IEnumerable<string> GetProviderIds() => _providers.Keys;

    /// <summary>
    /// 查找支持指定模型标识符的第一个提供程序。
    /// </summary>
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
    /// 创建或获取缓存的模型实例。
    /// </summary>
    public IModel GetOrCreate(string modelId, string apiKey, string? baseUrl = null, string? preferredProvider = null)
    {
        var cacheKey = $"{preferredProvider ?? "*"}:{modelId}:{baseUrl ?? ""}";

        return _instances.GetOrAdd(cacheKey, _ =>
        {
            IModelProvider? provider = null;

            if (preferredProvider != null && _providers.TryGetValue(preferredProvider, out var p))
            {
                provider = p;
            }

            provider ??= FindProvider(modelId);

            if (provider == null)
            {
                throw new NotSupportedException($"没有已注册的提供程序支持模型 '{modelId}'。");
            }

            return provider.Create(modelId, apiKey, baseUrl);
        });
    }

    /// <summary>
    /// 清除所有缓存的模型实例。
    /// </summary>
    public void ClearCache()
    {
        _instances.Clear();
    }
}
