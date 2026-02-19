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
using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.Gemini.Dto;

/// <summary>
/// Gemini API 请求
/// Gemini API request
/// </summary>
public class GeminiRequest
{
    /// <summary>
    /// 消息内容列表
    /// List of message contents
    /// </summary>
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new();

    /// <summary>
    /// 生成配置
    /// Generation configuration
    /// </summary>
    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig? GenerationConfig { get; set; }

    /// <summary>
    /// 安全设置
    /// Safety settings
    /// </summary>
    [JsonPropertyName("safetySettings")]
    public List<GeminiSafetySetting>? SafetySettings { get; set; }

    /// <summary>
    /// 系统指令
    /// System instruction
    /// </summary>
    [JsonPropertyName("systemInstruction")]
    public GeminiContent? SystemInstruction { get; set; }

    /// <summary>
    /// 工具声明
    /// Function declarations
    /// </summary>
    [JsonPropertyName("tools")]
    public List<GeminiTools>? Tools { get; set; }
}

/// <summary>
/// Gemini 消息内容
/// Gemini message content
/// </summary>
public class GeminiContent
{
    /// <summary>
    /// 角色：user 或 model
    /// Role: user or model
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    /// <summary>
    /// 内容部分列表
    /// List of content parts
    /// </summary>
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

/// <summary>
/// Gemini 内容部分
/// Gemini content part
/// </summary>
public class GeminiPart
{
    /// <summary>
    /// 文本内容
    /// Text content
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// 内联数据（图片等）
    /// Inline data (images, etc.)
    /// </summary>
    [JsonPropertyName("inlineData")]
    public GeminiInlineData? InlineData { get; set; }

    /// <summary>
    /// 函数调用
    /// Function call
    /// </summary>
    [JsonPropertyName("functionCall")]
    public GeminiFunctionCall? FunctionCall { get; set; }

    /// <summary>
    /// 函数响应
    /// Function response
    /// </summary>
    [JsonPropertyName("functionResponse")]
    public GeminiFunctionResponse? FunctionResponse { get; set; }
}

/// <summary>
/// Gemini 内联数据
/// Gemini inline data
/// </summary>
public class GeminiInlineData
{
    /// <summary>
    /// MIME 类型
    /// MIME type
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "";

    /// <summary>
    /// Base64 编码的数据
    /// Base64 encoded data
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = "";
}

/// <summary>
/// Gemini 函数调用
/// Gemini function call
/// </summary>
public class GeminiFunctionCall
{
    /// <summary>
    /// 函数名称
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// 函数参数
    /// Function arguments
    /// </summary>
    [JsonPropertyName("args")]
    public Dictionary<string, object>? Args { get; set; }
}

/// <summary>
/// Gemini 函数响应
/// Gemini function response
/// </summary>
public class GeminiFunctionResponse
{
    /// <summary>
    /// 函数名称
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// 响应内容
    /// Response content
    /// </summary>
    [JsonPropertyName("response")]
    public Dictionary<string, object>? Response { get; set; }
}

/// <summary>
/// Gemini 生成配置
/// Gemini generation configuration
/// </summary>
public class GeminiGenerationConfig
{
    /// <summary>
    /// 温度
    /// Temperature
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Top-P
    /// </summary>
    [JsonPropertyName("topP")]
    public double? TopP { get; set; }

    /// <summary>
    /// Top-K
    /// </summary>
    [JsonPropertyName("topK")]
    public int? TopK { get; set; }

    /// <summary>
    /// 最大输出令牌数
    /// Maximum output tokens
    /// </summary>
    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// 停止序列
    /// Stop sequences
    /// </summary>
    [JsonPropertyName("stopSequences")]
    public List<string>? StopSequences { get; set; }

    /// <summary>
    /// 响应 MIME 类型
    /// Response MIME type
    /// </summary>
    [JsonPropertyName("responseMimeType")]
    public string? ResponseMimeType { get; set; }
}

/// <summary>
/// Gemini 安全设置
/// Gemini safety setting
/// </summary>
public class GeminiSafetySetting
{
    /// <summary>
    /// 安全类别
    /// Safety category
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    /// <summary>
    /// 安全阈值
    /// Safety threshold
    /// </summary>
    [JsonPropertyName("threshold")]
    public string Threshold { get; set; } = "";
}

/// <summary>
/// Gemini 工具
/// Gemini tools
/// </summary>
public class GeminiTools
{
    /// <summary>
    /// 函数声明列表
    /// Function declarations
    /// </summary>
    [JsonPropertyName("functionDeclarations")]
    public List<GeminiFunctionDeclaration>? FunctionDeclarations { get; set; }
}

/// <summary>
/// Gemini 函数声明
/// Gemini function declaration
/// </summary>
public class GeminiFunctionDeclaration
{
    /// <summary>
    /// 函数名称
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// 函数描述
    /// Function description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 参数 Schema
    /// Parameters schema
    /// </summary>
    [JsonPropertyName("parameters")]
    public GeminiSchema? Parameters { get; set; }
}

/// <summary>
/// Gemini Schema
/// </summary>
public class GeminiSchema
{
    /// <summary>
    /// 类型
    /// Type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    /// <summary>
    /// 属性
    /// Properties
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, GeminiSchema>? Properties { get; set; }

    /// <summary>
    /// 必需属性
    /// Required properties
    /// </summary>
    [JsonPropertyName("required")]
    public List<string>? Required { get; set; }

    /// <summary>
    /// 描述
    /// Description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
