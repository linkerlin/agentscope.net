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

using AgentScope.Core.Exception;

namespace AgentScope.Core.Model;

/// <summary>
/// Exception thrown when a model operation fails (e.g., API error, network failure, invalid response).
/// Carries optional model name and provider information for diagnostics.
/// Corresponds to Java: io.agentscope.core.model.ModelException
/// 模型操作异常，在模型操作失败时抛出（如 API 错误、网络故障、无效响应）。
/// 携带可选的模型名称和提供程序信息用于诊断。
/// 对应 Java: io.agentscope.core.model.ModelException
/// </summary>
public class ModelException : AgentScopeException
{
    /// <summary>
    /// Gets the name of the model that caused the exception.
    /// 获取导致异常的模型名称。
    /// </summary>
    public string? ModelName { get; }

    /// <summary>
    /// Gets the provider name (e.g., "openai", "anthropic", "deepseek").
    /// 获取提供程序名称（例如 "openai"、"anthropic"、"deepseek"）。
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Initializes a new instance with an error message.
    /// 使用错误消息初始化新实例。
    /// </summary>
    /// <param name="message">Error message / 错误消息。</param>
    public ModelException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with an error message and inner exception.
    /// 使用错误消息和内部异常初始化新实例。
    /// </summary>
    /// <param name="message">Error message / 错误消息。</param>
    /// <param name="innerException">Inner exception / 内部异常。</param>
    public ModelException(string message, System.Exception innerException) 
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance with full diagnostic information.
    /// 使用完整的诊断信息初始化新实例。
    /// </summary>
    /// <param name="message">Error message / 错误消息。</param>
    /// <param name="innerException">Inner exception / 内部异常。</param>
    /// <param name="modelName">Model name / 模型名称。</param>
    /// <param name="provider">Provider name / 提供程序名称。</param>
    public ModelException(string message, System.Exception innerException, string modelName, string provider) 
        : base(message, innerException)
    {
        ModelName = modelName;
        Provider = provider;
    }
}
