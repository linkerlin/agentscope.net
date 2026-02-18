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

namespace AgentScope.Core.Message;

/// <summary>
/// Content block base class for multimodal messages
/// 内容块基类，用于多模态消息
/// 
/// Java参考: io.agentscope.core.message.ContentBlock
/// </summary>
public abstract record ContentBlock
{
    public abstract string Type { get; }
}

/// <summary>
/// Text content block
/// 文本内容块
/// </summary>
public record TextBlock : ContentBlock
{
    public override string Type => "text";
    public required string Text { get; set; }
}

/// <summary>
/// Image content block
/// 图片内容块
/// </summary>
public record ImageBlock : ContentBlock
{
    public override string Type => "image";
    public required string Url { get; set; }
    public string? MimeType { get; set; }
    public byte[]? Data { get; set; }
}

/// <summary>
/// Tool use block (represents a tool call from the assistant)
/// 工具使用块（表示助手的工具调用）
/// </summary>
public record ToolUseBlock : ContentBlock
{
    public override string Type => "tool_use";
    public required string Id { get; set; }
    public required string Name { get; set; }
    public Dictionary<string, object>? Input { get; set; }
    public string? Content { get; set; }
}

/// <summary>
/// Tool result block (represents the result of a tool execution)
/// 工具结果块（表示工具执行的结果）
/// </summary>
public record ToolResultBlock : ContentBlock
{
    public override string Type => "tool_result";
    public required string Id { get; set; }
    public object? Output { get; set; }
    public bool IsError { get; set; }
}

/// <summary>
/// Thinking block (for models with extended thinking capability)
/// 思考块（用于具有扩展思考能力的模型）
/// </summary>
public record ThinkingBlock : ContentBlock
{
    public override string Type => "thinking";
    public required string Thinking { get; set; }
    public string? Signature { get; set; }
}
