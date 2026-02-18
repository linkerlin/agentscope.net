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
public class ChatResponse : 模型响应
{
    /// <summary>
    /// 响应ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 响应内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 响应中的工具调用
    /// </summary>
    public List<ToolCallInfo>? ToolCalls { get; set; }

    /// <summary>
    /// Token使用信息
    /// </summary>
    public ChatUsage? Usage { get; set; }

    /// <summary>
    /// 使用的模型名称
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 停止原因
    /// </summary>
    public string? StopReason { get; set; }

    /// <summary>
    /// 是否是流式响应中的最终响应
    /// </summary>
    public bool IsComplete { get; set; }
}

/// <summary>
/// 工具调用信息
/// Tool call information
/// </summary>
public class ToolCallInfo
{
    /// <summary>
    /// 工具调用ID
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 工具类型（通常为"function"）
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 工具/函数名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 工具参数（JSON字符串）
    /// </summary>
    public string? Arguments { get; set; }
}

/// <summary>
/// 聊天用量
/// Token usage information
/// </summary>
public class ChatUsage
{
    /// <summary>
    /// 输入Token数量
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// 输出Token数量
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// 总Token数量
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// 响应时间（秒）
    /// </summary>
    public double TimeSeconds { get; set; }
}
