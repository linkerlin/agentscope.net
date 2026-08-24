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
/// Chat response with detailed information including content, tool calls, and usage statistics.
/// 聊天响应详细信息，包含内容、工具调用和用量统计。
///
/// Java reference: io.agentscope.core.model.ChatResponse
/// Java 参考: io.agentscope.core.model.ChatResponse
/// </summary>
public class ChatResponse : ModelResponse
{
    /// <summary>
    /// Gets or sets the response ID.
    /// 获取或设置响应 ID。
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the response content text.
    /// 获取或设置响应内容文本。
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the list of tool calls included in the response.
    /// 获取或设置响应中包含的工具调用列表。
    /// </summary>
    public List<ToolCallInfo>? ToolCalls { get; set; }

    /// <summary>
    /// Gets or sets the token usage information.
    /// 获取或设置 Token 用量信息。
    /// </summary>
    public ChatUsage? Usage { get; set; }

    /// <summary>
    /// Gets or sets the model name that generated this response.
    /// 获取或设置生成此响应的模型名称。
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the stop reason (e.g., "stop", "length", "tool_calls").
    /// 获取或设置停止原因（例如 "stop"、"length"、"tool_calls"）。
    /// </summary>
    public string? StopReason { get; set; }

    /// <summary>
    /// Gets or sets whether this is the final response in a streaming scenario.
    /// 获取或设置是否是流式响应中的最终响应。
    /// </summary>
    public bool IsComplete { get; set; }
}

/// <summary>
/// Tool call information, representing a function/tool invocation in the model response.
/// 工具调用信息，表示模型响应中的函数/工具调用。
/// </summary>
public class ToolCallInfo
{
    /// <summary>
    /// Gets or sets the tool call ID.
    /// 获取或设置工具调用 ID。
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the tool type (typically "function").
    /// 获取或设置工具类型（通常为 "function"）。
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the tool / function name.
    /// 获取或设置工具/函数名称。
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the tool arguments as a JSON string.
    /// 获取或设置工具参数（JSON 字符串）。
    /// </summary>
    public string? Arguments { get; set; }
}

/// <summary>
/// Token usage information for a model response.
/// 模型响应的 Token 用量信息。
/// </summary>
public class ChatUsage
{
    /// <summary>
    /// Gets or sets the number of input tokens.
    /// 获取或设置输入 Token 数量。
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens.
    /// 获取或设置输出 Token 数量。
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens (input + output).
    /// 获取或设置总 Token 数量（输入＋输出）。
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the response time in seconds.
    /// 获取或设置响应时间（秒）。
    /// </summary>
    public double TimeSeconds { get; set; }
}
