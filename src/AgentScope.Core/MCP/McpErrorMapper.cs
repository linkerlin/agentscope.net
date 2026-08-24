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

using AgentScope.Core.Tool;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP error mapper: normalizes protocol/client errors into unified exceptions or tool error messages.
/// Corresponds to Java: io.agentscope.core.mcp.McpErrorMapper
/// MCP 错误映射：将协议/客户端错误收敛为统一异常或工具错误消息。
/// 对应 Java: io.agentscope.core.mcp.McpErrorMapper
/// </summary>
public class McpErrorMapper
{
    /// <summary>
    /// Maps a general exception to an McpException with contextual information.
    /// If the exception is already an McpException, it is returned as-is.
    /// 将一般异常映射为带上下文信息的 McpException。
    /// 如果异常已经是 McpException，则直接返回。
    /// </summary>
    /// <param name="exception">The original exception / 原始异常</param>
    /// <param name="operation">The operation being performed / 正在执行的操作</param>
    /// <param name="clientName">Optional MCP client name / 可选的 MCP 客户端名称</param>
    /// <returns>An McpException with contextual information / 带上下文信息的 McpException</returns>
    public virtual McpException MapException(global::System.Exception exception, string operation, string? clientName = null)
    {
        if (exception is McpException mcpException)
        {
            return mcpException;
        }

        var scope = string.IsNullOrWhiteSpace(clientName)
            ? "MCP"
            : $"MCP client '{clientName}' / MCP 客户端 '{clientName}'";

        return new McpException($"{scope} {operation} failed: {exception.Message} / {scope}{operation}失败: {exception.Message}", exception);
    }

    /// <summary>
    /// Maps a tool failure to a descriptive error string.
    /// 将工具失败映射为描述性错误字符串。
    /// </summary>
    /// <param name="toolName">The name of the tool that failed / 失败的工具名称</param>
    /// <param name="clientName">Optional MCP client name / 可选的 MCP 客户端名称</param>
    /// <param name="reason">Optional failure reason / 可选的失败原因</param>
    /// <returns>A descriptive error message / 描述性错误消息</returns>
    public virtual string MapToolFailure(string toolName, string? clientName, string? reason = null)
    {
        var scope = string.IsNullOrWhiteSpace(clientName)
            ? toolName
            : $"{clientName}/{toolName}";

        return string.IsNullOrWhiteSpace(reason)
            ? $"MCP tool call failed: {scope} / MCP 工具调用失败: {scope}"
            : $"MCP tool call failed [{scope}]: {reason} / MCP 工具调用失败 [{scope}]: {reason}";
    }

    /// <summary>
    /// Maps a tool exception to a ToolResult with a failure message.
    /// 将工具异常映射为带失败消息的 ToolResult。
    /// </summary>
    /// <param name="exception">The exception thrown during tool execution / 工具执行期间抛出的异常</param>
    /// <param name="toolName">The name of the tool / 工具名称</param>
    /// <param name="clientName">Optional MCP client name / 可选的 MCP 客户端名称</param>
    /// <returns>A ToolResult with a failure message / 带失败消息的 ToolResult</returns>
    public virtual ToolResult MapToolException(global::System.Exception exception, string toolName, string? clientName = null)
    {
        var mapped = MapException(exception, $"calling tool '{toolName}' / 调用工具 '{toolName}'", clientName);
        return ToolResult.Fail(mapped.Message);
    }
}