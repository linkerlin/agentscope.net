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
/// Ollama model provider for local LLM inference.
/// Ollama 模型提供者 - 本地 LLM 推理
/// 
/// Ollama API is compatible with OpenAI API format.
/// Ollama API 兼容 OpenAI API 格式。
/// 
/// Features:
/// - Local LLM inference (no API key required)
/// - Support for popular models (llama2, mistral, codellama, etc.)
/// - GPU acceleration support
/// - No rate limits
/// 
/// Environment variables:
/// - OLLAMA_BASE_URL: Ollama server URL (default: http://localhost:11434)
/// - OLLAMA_MODEL: Model name (default: llama2)
/// 
/// Popular models:
/// - llama2: Meta's Llama 2
/// - llama3: Meta's Llama 3
/// - mistral: Mistral AI's Mistral
/// - codellama: Meta's Code Llama
/// - deepseek-coder: DeepSeek Coder
/// - phi3: Microsoft's Phi-3
/// </summary>
public class OllamaModel : OpenAIModel
{
    /// <summary>
    /// Default Ollama API base URL
    /// </summary>
    public const string DefaultBaseUrl = "http://localhost:11434/v1";

    /// <summary>
    /// Default Ollama model
    /// </summary>
    public const string DefaultModel = "llama2";

    /// <summary>
    /// Available popular Ollama models
    /// </summary>
    public static class Models
    {
        /// <summary>
        /// Meta Llama 2
        /// </summary>
        public const string Llama2 = "llama2";

        /// <summary>
        /// Meta Llama 3
        /// </summary>
        public const string Llama3 = "llama3";

        /// <summary>
        /// Meta Llama 3.1
        /// </summary>
        public const string Llama31 = "llama3.1";

        /// <summary>
        /// Mistral AI Mistral
        /// </summary>
        public const string Mistral = "mistral";

        /// <summary>
        /// Mistral AI Mixtral
        /// </summary>
        public const string Mixtral = "mixtral";

        /// <summary>
        /// Meta Code Llama
        /// </summary>
        public const string CodeLlama = "codellama";

        /// <summary>
        /// DeepSeek Coder
        /// </summary>
        public const string DeepSeekCoder = "deepseek-coder";

        /// <summary>
        /// Microsoft Phi-3
        /// </summary>
        public const string Phi3 = "phi3";

        /// <summary>
        /// Google Gemma
        /// </summary>
        public const string Gemma = "gemma";

        /// <summary>
        /// Alibaba Qwen
        /// </summary>
        public const string Qwen = "qwen";
    }

    /// <summary>
    /// Creates a new Ollama model instance.
    /// Note: Ollama typically doesn't require an API key for local usage.
    /// </summary>
    /// <param name="modelName">Model name (default: llama2)</param>
    /// <param name="baseUrl">Ollama server URL (default: http://localhost:11434/v1)</param>
    public OllamaModel(
        string modelName = DefaultModel,
        string? baseUrl = null)
        : base(
            modelName,
            apiKey: "ollama", // Ollama doesn't require real API key
            baseUrl: baseUrl ?? GetOllamaBaseUrl())
    {
    }

    /// <summary>
    /// Get Ollama base URL from environment variable or default.
    /// </summary>
    private static string GetOllamaBaseUrl()
    {
        var envUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL");
        if (!string.IsNullOrEmpty(envUrl))
        {
            // Ensure URL ends with /v1 for OpenAI compatibility
            if (!envUrl.EndsWith("/v1") && !envUrl.EndsWith("/v1/"))
            {
                return envUrl.TrimEnd('/') + "/v1";
            }
            return envUrl;
        }
        return DefaultBaseUrl;
    }

    /// <summary>
    /// Create a builder for OllamaModel.
    /// </summary>
    public static new OllamaModelBuilder Builder()
    {
        return new OllamaModelBuilder();
    }
}

/// <summary>
/// Builder for OllamaModel.
/// </summary>
public class OllamaModelBuilder
{
    private string _modelName = OllamaModel.DefaultModel;
    private string? _baseUrl;

    /// <summary>
    /// Set the model name.
    /// </summary>
    public OllamaModelBuilder ModelName(string modelName)
    {
        _modelName = modelName;
        return this;
    }

    /// <summary>
    /// Use Llama 2 model.
    /// </summary>
    public OllamaModelBuilder UseLlama2()
    {
        _modelName = OllamaModel.Models.Llama2;
        return this;
    }

    /// <summary>
    /// Use Llama 3 model.
    /// </summary>
    public OllamaModelBuilder UseLlama3()
    {
        _modelName = OllamaModel.Models.Llama3;
        return this;
    }

    /// <summary>
    /// Use Llama 3.1 model.
    /// </summary>
    public OllamaModelBuilder UseLlama31()
    {
        _modelName = OllamaModel.Models.Llama31;
        return this;
    }

    /// <summary>
    /// Use Mistral model.
    /// </summary>
    public OllamaModelBuilder UseMistral()
    {
        _modelName = OllamaModel.Models.Mistral;
        return this;
    }

    /// <summary>
    /// Use Code Llama model.
    /// </summary>
    public OllamaModelBuilder UseCodeLlama()
    {
        _modelName = OllamaModel.Models.CodeLlama;
        return this;
    }

    /// <summary>
    /// Use DeepSeek Coder model.
    /// </summary>
    public OllamaModelBuilder UseDeepSeekCoder()
    {
        _modelName = OllamaModel.Models.DeepSeekCoder;
        return this;
    }

    /// <summary>
    /// Use Phi-3 model.
    /// </summary>
    public OllamaModelBuilder UsePhi3()
    {
        _modelName = OllamaModel.Models.Phi3;
        return this;
    }

    /// <summary>
    /// Set the base URL for Ollama server.
    /// </summary>
    public OllamaModelBuilder BaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    /// <summary>
    /// Build the OllamaModel instance.
    /// </summary>
    public OllamaModel Build()
    {
        return new OllamaModel(_modelName, _baseUrl);
    }
}
