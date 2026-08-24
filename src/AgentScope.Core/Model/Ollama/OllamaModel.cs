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

namespace AgentScope.Core.Model.Ollama;

/// <summary>
/// Ollama model provider for local LLM inference in the AgentScope framework.
/// Ollama API is compatible with the OpenAI API format, so this class
/// extends OpenAIModel and configures the local server URL.
/// No API key is required for local usage.
/// Corresponds to Java: io.agentscope.core.model.OllamaChatModel
/// AgentScope 框架的 Ollama 模型提供者，用于本地 LLM 推理。
/// Ollama API 兼容 OpenAI API 格式，因此此类
/// 继承 OpenAIModel 并配置本地服务器 URL。
/// 本地使用无需 API 密钥。
/// 对应 Java: io.agentscope.core.model.OllamaChatModel
///
/// Features / 功能特性:
/// - Local LLM inference (no API key required) / 本地 LLM 推理（无需 API 密钥）
/// - Support for popular models (llama2, mistral, codellama, etc.) / 支持流行模型
/// - GPU acceleration support / GPU 加速支持
/// - No rate limits / 无速率限制
///
/// Environment variables / 环境变量:
/// - OLLAMA_BASE_URL: Ollama server URL (default: http://localhost:11434) / Ollama 服务器 URL（默认：http://localhost:11434）
/// - OLLAMA_MODEL: Model name (default: llama2) / 模型名称（默认：llama2）
///
/// Popular models / 流行模型:
/// - llama2: Meta's Llama 2 / Meta 的 Llama 2
/// - llama3: Meta's Llama 3 / Meta 的 Llama 3
/// - mistral: Mistral AI's Mistral / Mistral AI 的 Mistral
/// - codellama: Meta's Code Llama / Meta 的 Code Llama
/// - deepseek-coder: DeepSeek Coder / DeepSeek 编程模型
/// - phi3: Microsoft's Phi-3 / 微软的 Phi-3
/// </summary>
public class OllamaModel : OpenAIModel
{
    /// <summary>
    /// Default Ollama API base URL (OpenAI-compatible endpoint).
    /// 默认 Ollama API 基础 URL（OpenAI 兼容端点）。
    /// </summary>
    public const string DefaultBaseUrl = "http://localhost:11434/v1";

    /// <summary>
    /// Default Ollama model name.
    /// 默认 Ollama 模型名称。
    /// </summary>
    public const string DefaultModel = "llama2";

    /// <summary>
    /// Predefined popular Ollama model identifiers.
    /// 预定义的流行 Ollama 模型标识符。
    /// </summary>
    public static class Models
    {
        /// <summary>
        /// Meta Llama 2 - General purpose model.
        /// Meta Llama 2 - 通用模型。
        /// </summary>
        public const string Llama2 = "llama2";

        /// <summary>
        /// Meta Llama 3 - Latest generation Llama model.
        /// Meta Llama 3 - 最新一代 Llama 模型。
        /// </summary>
        public const string Llama3 = "llama3";

        /// <summary>
        /// Meta Llama 3.1 - Updated Llama 3 with extended context.
        /// Meta Llama 3.1 - 更新的 Llama 3，支持扩展上下文。
        /// </summary>
        public const string Llama31 = "llama3.1";

        /// <summary>
        /// Mistral AI Mistral - Efficient 7B model.
        /// Mistral AI Mistral - 高效的 7B 模型。
        /// </summary>
        public const string Mistral = "mistral";

        /// <summary>
        /// Mistral AI Mixtral - Mixture of Experts model (8x7B).
        /// Mistral AI Mixtral - 专家混合模型 (8x7B)。
        /// </summary>
        public const string Mixtral = "mixtral";

        /// <summary>
        /// Meta Code Llama - Specialized for code generation.
        /// Meta Code Llama - 专为代码生成优化。
        /// </summary>
        public const string CodeLlama = "codellama";

        /// <summary>
        /// DeepSeek Coder - Specialized code model.
        /// DeepSeek Coder - 专为代码优化的模型。
        /// </summary>
        public const string DeepSeekCoder = "deepseek-coder";

        /// <summary>
        /// Microsoft Phi-3 - Small but capable model (3.8B).
        /// 微软 Phi-3 - 小型但功能强大的模型 (3.8B)。
        /// </summary>
        public const string Phi3 = "phi3";

        /// <summary>
        /// Google Gemma - Lightweight model by Google.
        /// Google Gemma - Google 的轻量级模型。
        /// </summary>
        public const string Gemma = "gemma";

        /// <summary>
        /// Alibaba Qwen - Bilingual (Chinese/English) model.
        /// 阿里通义千问 - 双语（中文/英文）模型。
        /// </summary>
        public const string Qwen = "qwen";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaModel"/> class.
    /// Uses the OpenAI-compatible API format with Ollama's local server URL.
    /// Note: Ollama typically doesn't require an API key for local usage.
    /// 初始化 <see cref="OllamaModel"/> 类的新实例。
    /// 使用 Ollama 本地服务器 URL 的 OpenAI 兼容 API 格式。
    /// 注意：Ollama 本地使用通常不需要 API 密钥。
    /// </summary>
    /// <param name="modelName">Model name (default: llama2) / 模型名称（默认：llama2）。</param>
    /// <param name="baseUrl">Ollama server URL (default: http://localhost:11434/v1) / Ollama 服务器 URL（默认：http://localhost:11434/v1）。</param>
    public OllamaModel(
        string modelName = DefaultModel,
        string? baseUrl = null)
        : base(
            modelName,
            apiKey: "ollama", // Ollama doesn't require a real API key / Ollama 不需要真实的 API 密钥
            baseUrl: baseUrl ?? GetOllamaBaseUrl())
    {
    }

    /// <summary>
    /// Retrieves the Ollama base URL from the OLLAMA_BASE_URL environment variable,
    /// or returns the default URL. Ensures the URL ends with /v1 for OpenAI compatibility.
    /// 从 OLLAMA_BASE_URL 环境变量获取 Ollama 基础 URL，
    /// 或返回默认 URL。确保 URL 以 /v1 结尾以兼容 OpenAI 格式。
    /// </summary>
    /// <returns>The Ollama server URL with /v1 suffix / 带有 /v1 后缀的 Ollama 服务器 URL。</returns>
    private static string GetOllamaBaseUrl()
    {
        var envUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL");
        if (!string.IsNullOrEmpty(envUrl))
        {
            // Ensure URL ends with /v1 for OpenAI compatibility
            // 确保 URL 以 /v1 结尾以兼容 OpenAI 格式
            if (!envUrl.EndsWith("/v1") && !envUrl.EndsWith("/v1/"))
            {
                return envUrl.TrimEnd('/') + "/v1";
            }
            return envUrl;
        }
        return DefaultBaseUrl;
    }

    /// <summary>
    /// Creates a new builder for OllamaModel with fluent configuration.
    /// 创建一个新的 OllamaModel 构建器，支持流畅配置。
    /// </summary>
    /// <returns>A new OllamaModelBuilder instance / 一个新的 OllamaModelBuilder 实例。</returns>
    public static new OllamaModelBuilder Builder()
    {
        return new OllamaModelBuilder();
    }
}

/// <summary>
/// Fluent builder for creating OllamaModel instances.
/// Provides convenient methods for selecting popular local models and configuring the server URL.
/// 用于创建 OllamaModel 实例的流畅构建器。
/// 提供选择流行本地模型和配置服务器 URL 的便捷方法。
/// </summary>
public class OllamaModelBuilder
{
    /// <summary>
    /// The model name to use.
    /// 要使用的模型名称。
    /// </summary>
    private string _modelName = OllamaModel.DefaultModel;

    /// <summary>
    /// The Ollama server base URL.
    /// Ollama 服务器基础 URL。
    /// </summary>
    private string? _baseUrl;

    /// <summary>
    /// Sets the model name.
    /// 设置模型名称。
    /// </summary>
    /// <param name="modelName">Model name / 模型名称。</param>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public OllamaModelBuilder ModelName(string modelName)
    {
        _modelName = modelName;
        return this;
    }

    /// <summary>
    /// Uses the Llama 2 model (Meta's general purpose model).
    /// 使用 Llama 2 模型（Meta 的通用模型）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public OllamaModelBuilder UseLlama2()
    {
        _modelName = OllamaModel.Models.Llama2;
        return this;
    }

    /// <summary>
    /// Uses the Llama 3 model (Meta's latest generation).
    /// 使用 Llama 3 模型（Meta 的最新一代）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public OllamaModelBuilder UseLlama3()
    {
        _modelName = OllamaModel.Models.Llama3;
        return this;
    }

    /// <summary>
    /// Uses the Llama 3.1 model (updated with extended context).
    /// 使用 Llama 3.1 模型（更新版，支持扩展上下文）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public OllamaModelBuilder UseLlama31()
    {
        _modelName = OllamaModel.Models.Llama31;
        return this;
    }

    /// <summary>
    /// Uses the Mistral model (efficient 7B model).
    /// 使用 Mistral 模型（高效的 7B 模型）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public OllamaModelBuilder UseMistral()
    {
        _modelName = OllamaModel.Models.Mistral;
        return this;
    }

    /// <summary>
    /// Uses the Code Llama model (specialized for code generation).
    /// 使用 Code Llama 模型（专为代码生成优化）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public OllamaModelBuilder UseCodeLlama()
    {
        _modelName = OllamaModel.Models.CodeLlama;
        return this;
    }

    /// <summary>
    /// Uses the DeepSeek Coder model (specialized code model).
    /// 使用 DeepSeek Coder 模型（专为代码优化的模型）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public OllamaModelBuilder UseDeepSeekCoder()
    {
        _modelName = OllamaModel.Models.DeepSeekCoder;
        return this;
    }

    /// <summary>
    /// Uses the Phi-3 model (Microsoft's small but capable 3.8B model).
    /// 使用 Phi-3 模型（微软的小型但功能强大的 3.8B 模型）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public OllamaModelBuilder UsePhi3()
    {
        _modelName = OllamaModel.Models.Phi3;
        return this;
    }

    /// <summary>
    /// Sets the base URL for the Ollama server.
    /// 设置 Ollama 服务器的基础 URL。
    /// </summary>
    /// <param name="baseUrl">Ollama server URL / Ollama 服务器 URL。</param>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public OllamaModelBuilder BaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    /// <summary>
    /// Builds the OllamaModel instance with the configured settings.
    /// 使用已配置的设置构建 OllamaModel 实例。
    /// </summary>
    /// <returns>A configured OllamaModel instance / 一个已配置的 OllamaModel 实例。</returns>
    public OllamaModel Build()
    {
        return new OllamaModel(_modelName, _baseUrl);
    }
}
