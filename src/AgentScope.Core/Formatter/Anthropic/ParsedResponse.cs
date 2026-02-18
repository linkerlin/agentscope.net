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

namespace AgentScope.Core.Formatter.Anthropic;

/// <summary>
/// 解析后的响应
/// Parsed response from Anthropic API
/// </summary>
public class ParsedResponse
{
    /// <summary>
    /// 响应ID
    /// Response ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 模型名称
    /// Model name
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 文本内容
    /// Text content
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// 停止原因
    /// Stop reason
    /// </summary>
    public string? StopReason { get; set; }

    /// <summary>
    /// 工具调用列表
    /// Tool calls
    /// </summary>
    public List<ToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Token 使用情况
    /// Token usage
    /// </summary>
    public UsageInfo? Usage { get; set; }
}

/// <summary>
/// 工具调用信息
/// Tool call information
/// </summary>
public class ToolCall
{
    /// <summary>
    /// 工具调用ID
    /// Tool call ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 工具名称
    /// Tool name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 输入参数JSON
    /// Input parameters JSON
    /// </summary>
    public string? InputJson { get; set; }
}

/// <summary>
/// Token 使用信息
/// Token usage information
/// </summary>
public class UsageInfo
{
    /// <summary>
    /// 输入token数
    /// Input tokens
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// 输出token数
    /// Output tokens
    /// </summary>
    public int OutputTokens { get; set; }
}
