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
/// Anthropic 消息角色
/// Anthropic message role
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnthropicRole
{
    [JsonStringEnumMemberName("user")]
    User,

    [JsonStringEnumMemberName("assistant")]
    Assistant
}

/// <summary>
/// Anthropic 消息
/// Anthropic message
/// </summary>
public record AnthropicMessage
{
    [JsonPropertyName("role")]
    public required AnthropicRole Role { get; init; }

    [JsonPropertyName("content")]
    public required List<AnthropicContentBlock> Content { get; init; }

    public AnthropicMessage()
    {
        Content = new List<AnthropicContentBlock>();
    }

    public AnthropicMessage(AnthropicRole role, List<AnthropicContentBlock> content)
    {
        Role = role;
        Content = content;
    }
}

/// <summary>
/// 系统消息（Anthropic API 使用单独的 system 参数）
/// System message (Anthropic API uses separate system parameter)
/// </summary>
public record AnthropicSystemMessage
{
    [JsonPropertyName("type")]
    public string Type => "text";

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
