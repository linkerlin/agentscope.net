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
using AgentScope.Core.Model.OpenAI;

namespace AgentScope.Core.Model.DeepSeek;

/// <summary>
/// DeepSeek model provider for the AgentScope framework.
/// DeepSeek API is fully compatible with the OpenAI API format, so this class
/// extends OpenAIModel and simply configures the base URL and API key.
/// Corresponds to Java: io.agentscope.core.model.DeepSeekChatModel
/// AgentScope 框架的 DeepSeek 模型提供者。
/// DeepSeek API 完全兼容 OpenAI API 格式，因此此类
/// 继承 OpenAIModel 并仅配置基础 URL 和 API 密钥。
/// 对应 Java: io.agentscope.core.model.DeepSeekChatModel
///
/// Available models / 可用模型:
/// - deepseek-chat: General conversation model / 通用对话模型
/// - deepseek-reasoner: Reasoning model (R1) / 推理模型 (R1)
///
/// Environment variables / 环境变量:
/// - DEEPSEEK_API_KEY: DeepSeek API key / DeepSeek API 密钥
/// - DEEPSEEK_MODEL: Model name (default: deepseek-chat) / 模型名称（默认：deepseek-chat）
/// </summary>
public class DeepSeekModel : OpenAIModel
{
    /// <summary>
    /// DeepSeek API base URL.
    /// DeepSeek API 的基础 URL。
    /// </summary>
    public const string DefaultBaseUrl = "https://api.deepseek.com";

    /// <summary>
    /// Default DeepSeek model name.
    /// 默认 DeepSeek 模型名称。
    /// </summary>
    public const string DefaultModel = "deepseek-chat";

    /// <summary>
    /// Predefined DeepSeek model identifiers.
    /// 预定义的 DeepSeek 模型标识符。
    /// </summary>
    public static class Models
    {
        /// <summary>
        /// General conversation model (deepseek-chat).
        /// 通用对话模型 (deepseek-chat)。
        /// </summary>
        public const string Chat = "deepseek-chat";

        /// <summary>
        /// Reasoning model (deepseek-reasoner, R1).
        /// 推理模型 (deepseek-reasoner, R1)。
        /// </summary>
        public const string Reasoner = "deepseek-reasoner";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeepSeekModel"/> class.
    /// Uses the OpenAI-compatible API format with DeepSeek's base URL.
    /// 初始化 <see cref="DeepSeekModel"/> 类的新实例。
    /// 使用 DeepSeek 基础 URL 的 OpenAI 兼容 API 格式。
    /// </summary>
    /// <param name="modelName">Model name (default: deepseek-chat) / 模型名称（默认：deepseek-chat）。</param>
    /// <param name="apiKey">API key (optional, will use DEEPSEEK_API_KEY env var if not provided) / API 密钥（可选，未提供则读取 DEEPSEEK_API_KEY 环境变量）。</param>
    public DeepSeekModel(
        string modelName = DefaultModel,
        string? apiKey = null)
        : base(
            modelName,
            apiKey ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY"),
            DefaultBaseUrl)
    {
    }

    /// <summary>
    /// Creates a new builder for DeepSeekModel with fluent configuration.
    /// 创建一个新的 DeepSeekModel 构建器，支持流畅配置。
    /// </summary>
    /// <returns>A new DeepSeekModelBuilder instance / 一个新的 DeepSeekModelBuilder 实例。</returns>
    public static new DeepSeekModelBuilder Builder()
    {
        return new DeepSeekModelBuilder();
    }
}

/// <summary>
/// Fluent builder for creating DeepSeekModel instances.
/// Provides convenient methods for selecting predefined models and configuring the API key.
/// 用于创建 DeepSeekModel 实例的流畅构建器。
/// 提供选择预定义模型和配置 API 密钥的便捷方法。
/// </summary>
public class DeepSeekModelBuilder
{
    /// <summary>
    /// The model name to use.
    /// 要使用的模型名称。
    /// </summary>
    private string _modelName = DeepSeekModel.DefaultModel;

    /// <summary>
    /// The API key for authentication.
    /// 用于身份验证的 API 密钥。
    /// </summary>
    private string? _apiKey;

    /// <summary>
    /// Sets the model name.
    /// 设置模型名称。
    /// </summary>
    /// <param name="modelName">Model name / 模型名称。</param>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public DeepSeekModelBuilder ModelName(string modelName)
    {
        _modelName = modelName;
        return this;
    }

    /// <summary>
    /// Uses the general chat model (deepseek-chat).
    /// 使用通用对话模型 (deepseek-chat)。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public DeepSeekModelBuilder UseChat()
    {
        _modelName = DeepSeekModel.Models.Chat;
        return this;
    }

    /// <summary>
    /// Uses the reasoning model (deepseek-reasoner, R1).
    /// 使用推理模型 (deepseek-reasoner, R1)。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public DeepSeekModelBuilder UseReasoner()
    {
        _modelName = DeepSeekModel.Models.Reasoner;
        return this;
    }

    /// <summary>
    /// Sets the API key for authentication.
    /// 设置用于身份验证的 API 密钥。
    /// </summary>
    /// <param name="apiKey">DeepSeek API key / DeepSeek API 密钥。</param>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public DeepSeekModelBuilder ApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    /// <summary>
    /// Builds the DeepSeekModel instance with the configured settings.
    /// 使用已配置的设置构建 DeepSeekModel 实例。
    /// </summary>
    /// <returns>A configured DeepSeekModel instance / 一个已配置的 DeepSeekModel 实例。</returns>
    public DeepSeekModel Build()
    {
        return new DeepSeekModel(_modelName, _apiKey);
    }
}
