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

namespace AgentScope.Core.Formatter.Anthropic.Dto;

/// <summary>
/// Anthropic 工具定义
/// Anthropic tool definition
/// </summary>
public record AnthropicTool
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("input_schema")]
    public required Dictionary<string, object> InputSchema { get; init; }
}

/// <summary>
/// 工具选择类型
/// Tool choice type
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnthropicToolChoiceType
{
    [JsonStringEnumMemberName("auto")]
    Auto,

    [JsonStringEnumMemberName("any")]
    Any,

    [JsonStringEnumMemberName("tool")]
    Tool
}

/// <summary>
/// 工具选择配置
/// Tool choice configuration
/// </summary>
public record AnthropicToolChoice
{
    [JsonPropertyName("type")]
    public required AnthropicToolChoiceType Type { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }  // Required when Type is Tool
}

/// <summary>
/// 缓存控制（Prompt Caching）
/// Cache control for prompt caching
/// </summary>
public record CacheControl
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }  // "ephemeral"
}
