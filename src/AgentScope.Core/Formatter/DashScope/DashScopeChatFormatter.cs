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
using System.Linq;
using AgentScope.Core.Formatter.DashScope.Dto;
using AgentScope.Core.Message;
using AgentScope.Core.Model;

namespace AgentScope.Core.Formatter.DashScope;

/// <summary>
/// Formatter for DashScope Conversation/Generation APIs.
/// Converts between AgentScope Msg objects and DashScope DTO types.
/// DashScope Chat 格式化器
/// 
/// This formatter handles both text and multimodal messages, supporting the DashScope
/// Generation API and MultiModalConversation API.
/// 
/// Java参考: io.agentscope.core.formatter.dashscope.DashScopeChatFormatter
/// </summary>
public class DashScopeChatFormatter
{
    private readonly string _modelName;

    /// <summary>
    /// Create a new DashScopeChatFormatter with default model.
    /// </summary>
    public DashScopeChatFormatter() : this("qwen-plus")
    {
    }

    /// <summary>
    /// Create a new DashScopeChatFormatter with specified model.
    /// </summary>
    /// <param name="modelName">Model name (e.g., "qwen-plus", "qwen-vl-max")</param>
    public DashScopeChatFormatter(string modelName)
    {
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    /// <summary>
    /// Format AgentScope Msg objects to DashScope MultiModal message format.
    /// </summary>
    public List<DashScopeMessage> FormatMultiModal(List<Msg> messages)
    {
        return DashScopeMessageConverter.Convert(messages, useMultimodalFormat: true);
    }

    /// <summary>
    /// Build a complete DashScopeRequest for the API call.
    /// </summary>
    public DashScopeRequest BuildRequest(string model, List<DashScopeMessage> messages, bool stream)
    {
        var parameters = new DashScopeParameters
        {
            ResultFormat = "message",
            IncrementalOutput = stream
        };

        return new DashScopeRequest
        {
            Model = model,
            Input = new DashScopeInput { Messages = messages },
            Parameters = parameters
        };
    }

    /// <summary>
    /// Build a complete DashScopeRequest with full configuration.
    /// </summary>
    public DashScopeRequest BuildRequest(
        string model,
        List<DashScopeMessage> messages,
        bool stream,
        GenerateOptions? options,
        GenerateOptions? defaultOptions,
        List<ToolSchema>? tools,
        ToolChoice? toolChoice)
    {
        var request = BuildRequest(model, messages, stream);

        if (request.Parameters != null)
        {
            // Apply default options first, then override with specific options
            if (defaultOptions != null)
            {
                ApplyOptionsToParameters(request.Parameters, defaultOptions);
            }
            if (options != null)
            {
                ApplyOptionsToParameters(request.Parameters, options);
            }

            if (tools != null && tools.Count > 0)
            {
                request.Parameters.Tools = ConvertTools(tools);
            }

            if (toolChoice != null)
            {
                ApplyToolChoice(request.Parameters, toolChoice);
            }
        }

        return request;
    }

    /// <summary>
    /// Apply options to parameters.
    /// </summary>
    private void ApplyOptionsToParameters(DashScopeParameters parameters, GenerateOptions options)
    {
        if (options.Temperature.HasValue)
            parameters.Temperature = options.Temperature.Value;
        if (options.MaxTokens.HasValue)
            parameters.MaxTokens = options.MaxTokens.Value;
        if (options.TopP.HasValue)
            parameters.TopP = options.TopP.Value;
        if (options.TopK.HasValue)
            parameters.TopK = options.TopK.Value;
        if (options.Seed.HasValue)
            parameters.Seed = options.Seed.Value;
        if (options.FrequencyPenalty.HasValue)
            parameters.FrequencyPenalty = options.FrequencyPenalty.Value;
        if (options.PresencePenalty.HasValue)
            parameters.PresencePenalty = options.PresencePenalty.Value;
        if (options.Stop?.Count > 0)
            parameters.Stop = options.Stop;

        // Handle additional body params
        if (options.AdditionalBodyParams != null)
        {
            if (options.AdditionalBodyParams.TryGetValue("enable_thinking", out var thinkingObj) &&
                thinkingObj is bool thinking)
            {
                parameters.EnableThinking = thinking;
            }
            if (options.AdditionalBodyParams.TryGetValue("thinking_budget", out var budgetObj) &&
                budgetObj is int budget)
            {
                parameters.ThinkingBudget = budget;
            }
            if (options.AdditionalBodyParams.TryGetValue("enable_search", out var searchObj) &&
                searchObj is bool search)
            {
                parameters.EnableSearch = search;
            }
            if (options.AdditionalBodyParams.TryGetValue("repetition_penalty", out var repPenaltyObj) &&
                repPenaltyObj is double repPenalty)
            {
                parameters.RepetitionPenalty = repPenalty;
            }
        }
    }

    /// <summary>
    /// Apply tool choice to parameters.
    /// </summary>
    private void ApplyToolChoice(DashScopeParameters parameters, ToolChoice toolChoice)
    {
        parameters.ToolChoice = toolChoice.Type switch
        {
            ToolChoiceType.Auto => "auto",
            ToolChoiceType.None => "none",
            ToolChoiceType.Required => "auto", // DashScope doesn't have 'required', use 'auto'
            ToolChoiceType.Specific => new { type = "function", function = new { name = toolChoice.ToolName } },
            _ => "auto"
        };
    }

    /// <summary>
    /// Convert ToolSchema list to DashScopeTool list.
    /// </summary>
    private List<DashScopeTool> ConvertTools(List<ToolSchema> tools)
    {
        return tools.Select(t => DashScopeTool.FromFunction(new DashScopeToolFunction
        {
            Name = t.Name,
            Description = t.Description,
            Parameters = t.Parameters
        })).ToList();
    }
}
