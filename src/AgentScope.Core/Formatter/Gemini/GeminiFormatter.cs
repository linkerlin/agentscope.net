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
using AgentScope.Core.Formatter.Gemini.Dto;
using AgentScope.Core.Message;
using AgentScope.Core.Model;

namespace AgentScope.Core.Formatter.Gemini;

/// <summary>
/// Gemini Formatter
/// 
/// 实现 IFormatter 接口，用于 Gemini API 格式转换
/// Implements IFormatter interface for Gemini API format conversion
/// 
/// 使用方式：
/// Usage:
/// <code>
/// var formatter = new GeminiFormatter();
/// var request = new GeminiRequest
/// {
///     Contents = formatter.Format(messages)
/// };
/// </code>
/// </summary>
public class GeminiFormatter : IFormatter<GeminiContent, GeminiResponse, GeminiGenerationConfig>
{
    private readonly GenerateOptions? _defaultOptions;

    /// <summary>
    /// 创建 GeminiFormatter 实例
    /// Create GeminiFormatter instance
    /// </summary>
    /// <param name="defaultOptions">默认生成选项 / Default generation options</param>
    public GeminiFormatter(GenerateOptions? defaultOptions = null)
    {
        _defaultOptions = defaultOptions;
    }

    /// <summary>
    /// 将 Msg 列表格式化为 GeminiContent 列表
    /// Format Msg list to GeminiContent list
    /// </summary>
    public List<GeminiContent> Format(List<Msg> messages)
    {
        if (messages == null || messages.Count == 0)
        {
            return new List<GeminiContent>();
        }

        var contents = new List<GeminiContent>();
        GeminiContent? systemContent = null;

        foreach (var msg in messages)
        {
            var role = msg.Role?.ToLowerInvariant() ?? "user";

            // 处理系统消息 - Gemini 使用单独的 systemInstruction 字段
            // Handle system message - Gemini uses separate systemInstruction field
            if (role == "system")
            {
                systemContent = GeminiMessageConverter.ConvertToContent(msg);
                continue;
            }

            var content = GeminiMessageConverter.ConvertToContent(msg);
            contents.Add(content);
        }

        // 如果有系统消息，将其设置为 systemInstruction（存储在第一个内容的元数据中）
        // If there's a system message, store it for later use
        if (systemContent != null && contents.Count > 0)
        {
            // 返回时附加系统指令信息
            // System instruction will be handled separately in the request
        }

        return contents;
    }

    /// <summary>
    /// 解析 Gemini 响应为 ModelResponse
    /// Parse Gemini response to ModelResponse
    /// </summary>
    public ModelResponse ParseResponse(GeminiResponse response, DateTime startTime)
    {
        return GeminiResponseParser.ParseResponse(response, startTime);
    }

    /// <summary>
    /// 应用生成选项到 GeminiGenerationConfig
    /// Apply generation options to GeminiGenerationConfig
    /// </summary>
    public void ApplyOptions(GeminiGenerationConfig config, GenerateOptions? options, GenerateOptions? defaultOptions)
    {
        var effectiveOptions = options ?? defaultOptions ?? _defaultOptions;
        if (effectiveOptions == null)
        {
            return;
        }

        if (effectiveOptions.Temperature.HasValue)
        {
            config.Temperature = effectiveOptions.Temperature.Value;
        }

        if (effectiveOptions.MaxTokens.HasValue)
        {
            config.MaxOutputTokens = effectiveOptions.MaxTokens.Value;
        }

        if (effectiveOptions.TopP.HasValue)
        {
            config.TopP = effectiveOptions.TopP.Value;
        }

        if (effectiveOptions.TopK.HasValue)
        {
            config.TopK = effectiveOptions.TopK.Value;
        }

        if (effectiveOptions.Stop != null && effectiveOptions.Stop.Count > 0)
        {
            config.StopSequences = effectiveOptions.Stop;
        }

        // 处理响应格式
        // Handle response format
        if (effectiveOptions.ResponseFormat != null)
        {
            var formatType = effectiveOptions.ResponseFormat.Type?.ToLowerInvariant();
            if (formatType == "json_object")
            {
                config.ResponseMimeType = "application/json";
            }
        }
    }

    /// <summary>
    /// 应用工具模式到 GeminiGenerationConfig
    /// Apply tool schemas to GeminiGenerationConfig
    /// </summary>
    public void ApplyTools(GeminiGenerationConfig config, List<ToolSchema>? tools)
    {
        // Gemini uses tools at the request level, not in generation config
        // This method is provided for interface compatibility
    }

    /// <summary>
    /// 将工具模式转换为 Gemini 工具声明
    /// Convert tool schemas to Gemini function declarations
    /// </summary>
    public List<GeminiTools>? ConvertTools(List<ToolSchema>? tools)
    {
        if (tools == null || tools.Count == 0)
        {
            return null;
        }

        var declarations = new List<GeminiFunctionDeclaration>();
        foreach (var tool in tools)
        {
            declarations.Add(new GeminiFunctionDeclaration
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = ConvertSchema(tool.Parameters)
            });
        }

        return new List<GeminiTools>
        {
            new GeminiTools
            {
                FunctionDeclarations = declarations
            }
        };
    }

    /// <summary>
    /// 转换参数 Schema
    /// Convert parameter schema
    /// </summary>
    private GeminiSchema? ConvertSchema(Dictionary<string, object>? parameters)
    {
        if (parameters == null)
        {
            return null;
        }

        var schema = new GeminiSchema();

        if (parameters.TryGetValue("type", out var type))
        {
            schema.Type = type?.ToString() ?? "object";
        }

        if (parameters.TryGetValue("description", out var description))
        {
            schema.Description = description?.ToString();
        }

        if (parameters.TryGetValue("required", out var required) && required is List<string> requiredList)
        {
            schema.Required = requiredList;
        }

        if (parameters.TryGetValue("properties", out var properties) && properties is Dictionary<string, object> props)
        {
            schema.Properties = new Dictionary<string, GeminiSchema>();
            foreach (var prop in props)
            {
                if (prop.Value is Dictionary<string, object> propDef)
                {
                    schema.Properties[prop.Key] = new GeminiSchema
                    {
                        Type = propDef.TryGetValue("type", out var propType) ? propType?.ToString() ?? "string" : "string",
                        Description = propDef.TryGetValue("description", out var propDesc) ? propDesc?.ToString() : null
                    };
                }
            }
        }

        return schema;
    }

    /// <summary>
    /// 创建完整的 Gemini 请求
    /// Create complete Gemini request
    /// </summary>
    public GeminiRequest CreateRequest(
        List<Msg> messages,
        GenerateOptions? options = null,
        List<ToolSchema>? tools = null,
        Msg? systemInstruction = null)
    {
        // 提取系统消息
        // Extract system message
        Msg? systemMsg = systemInstruction;
        var filteredMessages = new List<Msg>(messages);
        
        if (systemMsg == null)
        {
            var systemMsgIndex = messages.FindIndex(m => m.Role?.ToLowerInvariant() == "system");
            if (systemMsgIndex >= 0)
            {
                systemMsg = messages[systemMsgIndex];
                filteredMessages.RemoveAt(systemMsgIndex);
            }
        }

        var request = new GeminiRequest
        {
            Contents = Format(filteredMessages),
            GenerationConfig = new GeminiGenerationConfig()
        };

        // 应用选项
        // Apply options
        ApplyOptions(request.GenerationConfig, options, _defaultOptions);

        // 应用系统指令
        // Apply system instruction
        if (systemMsg != null)
        {
            request.SystemInstruction = GeminiMessageConverter.ConvertToContent(systemMsg);
        }

        // 应用工具
        // Apply tools
        if (tools != null && tools.Count > 0)
        {
            request.Tools = ConvertTools(tools);
        }

        return request;
    }
}
