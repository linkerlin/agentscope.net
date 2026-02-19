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
/// DeepSeek model provider.
/// DeepSeek 模型提供者
/// 
/// DeepSeek API is compatible with OpenAI API format.
/// DeepSeek API 兼容 OpenAI API 格式。
/// 
/// Available models:
/// - deepseek-chat: General conversation model
/// - deepseek-reasoner: Reasoning model (R1)
/// 
/// Environment variables:
/// - DEEPSEEK_API_KEY: DeepSeek API key
/// - DEEPSEEK_MODEL: Model name (default: deepseek-chat)
/// </summary>
public class DeepSeekModel : OpenAIModel
{
    /// <summary>
    /// DeepSeek API base URL
    /// </summary>
    public const string DefaultBaseUrl = "https://api.deepseek.com";

    /// <summary>
    /// Default DeepSeek model
    /// </summary>
    public const string DefaultModel = "deepseek-chat";

    /// <summary>
    /// Available DeepSeek models
    /// </summary>
    public static class Models
    {
        /// <summary>
        /// General conversation model
        /// </summary>
        public const string Chat = "deepseek-chat";

        /// <summary>
        /// Reasoning model (R1)
        /// </summary>
        public const string Reasoner = "deepseek-reasoner";
    }

    /// <summary>
    /// Creates a new DeepSeek model instance.
    /// </summary>
    /// <param name="modelName">Model name (default: deepseek-chat)</param>
    /// <param name="apiKey">API key (optional, will use DEEPSEEK_API_KEY env var if not provided)</param>
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
    /// Create a builder for DeepSeekModel.
    /// </summary>
    public static new DeepSeekModelBuilder Builder()
    {
        return new DeepSeekModelBuilder();
    }
}

/// <summary>
/// Builder for DeepSeekModel.
/// </summary>
public class DeepSeekModelBuilder
{
    private string _modelName = DeepSeekModel.DefaultModel;
    private string? _apiKey;

    /// <summary>
    /// Set the model name.
    /// </summary>
    public DeepSeekModelBuilder ModelName(string modelName)
    {
        _modelName = modelName;
        return this;
    }

    /// <summary>
    /// Use the chat model (deepseek-chat).
    /// </summary>
    public DeepSeekModelBuilder UseChat()
    {
        _modelName = DeepSeekModel.Models.Chat;
        return this;
    }

    /// <summary>
    /// Use the reasoner model (deepseek-reasoner).
    /// </summary>
    public DeepSeekModelBuilder UseReasoner()
    {
        _modelName = DeepSeekModel.Models.Reasoner;
        return this;
    }

    /// <summary>
    /// Set the API key.
    /// </summary>
    public DeepSeekModelBuilder ApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    /// <summary>
    /// Build the DeepSeekModel instance.
    /// </summary>
    public DeepSeekModel Build()
    {
        return new DeepSeekModel(_modelName, _apiKey);
    }
}
