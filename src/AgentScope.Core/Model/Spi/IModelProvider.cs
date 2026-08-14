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
/// 模型提供程序 SPI 接口。
/// 各厂商实现此接口以支持通过 ModelRegistry 按配置名称查找和创建模型。
/// </summary>
public interface IModelProvider
{
    /// <summary>提供商标识符，例如 "openai", "anthropic", "deepseek"</summary>
    string ProviderId { get; }

    /// <summary>
    /// 判断此提供程序是否支持指定的模型标识符。
    /// </summary>
    /// <param name="modelId">模型标识符，例如 "gpt-4o", "claude-3"</param>
    bool Supports(string modelId);

    /// <summary>
    /// 创建一个模型实例。
    /// </summary>
    /// <param name="modelId">模型标识符</param>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="baseUrl">可选的基础 URL</param>
    IModel Create(string modelId, string apiKey, string? baseUrl = null);
}
