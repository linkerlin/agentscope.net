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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.DashScope.Dto;

/// <summary>
/// DashScope tool definition DTO.
/// DashScope 工具定义
/// 
/// Java参考: io.agentscope.core.formatter.dashscope.dto.DashScopeTool
/// </summary>
public class DashScopeTool
{
    /// <summary>
    /// Tool type, always "function"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// The function definition
    /// </summary>
    [JsonPropertyName("function")]
    public required DashScopeToolFunction Function { get; set; }

    /// <summary>
    /// Create a function tool
    /// </summary>
    public static DashScopeTool FromFunction(DashScopeToolFunction function) => new() { Function = function };
}

/// <summary>
/// DashScope tool function definition.
/// DashScope 工具函数定义
/// </summary>
public class DashScopeToolFunction
{
    /// <summary>
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Function description
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Function parameters schema
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// DashScope tool call DTO.
/// DashScope 工具调用
/// </summary>
public class DashScopeToolCall
{
    /// <summary>
    /// Tool call ID
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    /// <summary>
    /// Tool type, always "function"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// The function call
    /// </summary>
    [JsonPropertyName("function")]
    public required DashScopeFunction Function { get; set; }
}

/// <summary>
/// DashScope function call DTO.
/// DashScope 函数调用
/// </summary>
public class DashScopeFunction
{
    /// <summary>
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Function arguments as JSON string
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Arguments { get; set; }
}
