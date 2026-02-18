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

namespace AgentScope.Core.Model;

/// <summary>
/// Chat response with detailed information
/// 聊天响应详细信息
/// 
/// Java参考: io.agentscope.core.model.ChatResponse
/// </summary>
public class ChatResponse : ModelResponse
{
    /// <summary>
    /// Response ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Response content (text)
    /// </summary>
    public new string? Content { get; set; }

    /// <summary>
    /// Tool calls in the response
    /// </summary>
    public List<ToolCallInfo>? ToolCalls { get; set; }

    /// <summary>
    /// Token usage information
    /// </summary>
    public ChatUsage? Usage { get; set; }

    /// <summary>
    /// Model name used
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Stop reason
    /// </summary>
    public string? StopReason { get; set; }

    /// <summary>
    /// Whether this is the final response in a stream
    /// </summary>
    public bool IsComplete { get; set; }
}

/// <summary>
/// Tool call information
/// 工具调用信息
/// </summary>
public class ToolCallInfo
{
    /// <summary>
    /// Tool call ID
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Tool type (usually "function")
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Tool/function name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Tool arguments as JSON string
    /// </summary>
    public string? Arguments { get; set; }
}

/// <summary>
/// Token usage information
/// Token 使用信息
/// </summary>
public class ChatUsage
{
    /// <summary>
    /// Input tokens
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Output tokens
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Response time in seconds
    /// </summary>
    public double TimeSeconds { get; set; }
}
