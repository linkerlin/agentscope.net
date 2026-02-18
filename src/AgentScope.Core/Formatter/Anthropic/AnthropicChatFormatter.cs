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
using AgentScope.Core.Formatter.Anthropic.Dto;
using AgentScope.Core.Message;
using AgentScope.Core.Model;

// Use the global GenerateOptions from Formatter namespace
using GenerateOptions = AgentScope.Core.Formatter.GenerateOptions;

namespace AgentScope.Core.Formatter.Anthropic;

/// <summary>
/// Formatter for Anthropic Messages API.
/// Converts between AgentScope Msg objects and Anthropic SDK types.
/// Anthropic Chat 格式化器
/// 
/// Important: Anthropic API has special requirements:
/// - Only the first message can be a system message (handled via system parameter)
/// - Tool results must be in separate user messages
/// - Supports thinking blocks natively (extended thinking feature)
/// 
/// Java参考: io.agentscope.core.formatter.anthropic.AnthropicChatFormatter
/// </summary>
public class AnthropicChatFormatter : AnthropicBaseFormatter
{
    private readonly string _modelName;

    /// <summary>
    /// Create a new AnthropicChatFormatter with default model.
    /// </summary>
    public AnthropicChatFormatter() : this("claude-3-5-sonnet-20241022")
    {
    }

    /// <summary>
    /// Create a new AnthropicChatFormatter with specified model.
    /// </summary>
    /// <param name="modelName">Model name (e.g., "claude-3-opus-20240229", "claude-3-5-sonnet-20241022")</param>
    public AnthropicChatFormatter(string modelName)
    {
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    /// <summary>
    /// Get model name.
    /// </summary>
    protected override string GetModelName(GenerateOptions? options)
    {
        // Check for model in options first
        if (options?.AdditionalBodyParams?.TryGetValue("model", out var modelObj) == true &&
            modelObj is string modelStr)
        {
            return modelStr;
        }

        return _modelName;
    }

    /// <summary>
    /// Merge source options into target.
    /// </summary>
    private void MergeOptions(GenerateOptions target, GenerateOptions source)
    {
        if (source.Temperature.HasValue)
            target.Temperature = source.Temperature;
        if (source.MaxTokens.HasValue)
            target.MaxTokens = source.MaxTokens;
        if (source.TopP.HasValue)
            target.TopP = source.TopP;
        if (source.TopK.HasValue)
            target.TopK = source.TopK;
        if (source.FrequencyPenalty.HasValue)
            target.FrequencyPenalty = source.FrequencyPenalty;
        if (source.PresencePenalty.HasValue)
            target.PresencePenalty = source.PresencePenalty;
        if (source.Seed.HasValue)
            target.Seed = source.Seed;
        if (source.ResponseFormat != null)
            target.ResponseFormat = source.ResponseFormat;
        if (source.Stop?.Count > 0)
        {
            target.Stop ??= new List<string>();
            target.Stop.AddRange(source.Stop);
        }
        if (source.AdditionalBodyParams?.Count > 0)
        {
            target.AdditionalBodyParams ??= new Dictionary<string, object>();
            foreach (var kvp in source.AdditionalBodyParams)
            {
                target.AdditionalBodyParams[kvp.Key] = kvp.Value;
            }
        }
    }
}
