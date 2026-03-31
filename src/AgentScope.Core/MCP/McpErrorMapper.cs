// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tool;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP 错误映射：将协议/客户端错误收敛为统一异常或工具错误消息。
/// </summary>
public class McpErrorMapper
{
    public virtual McpException MapException(global::System.Exception exception, string operation, string? clientName = null)
    {
        if (exception is McpException mcpException)
        {
            return mcpException;
        }

        var scope = string.IsNullOrWhiteSpace(clientName)
            ? "MCP"
            : $"MCP 客户端 '{clientName}'";

        return new McpException($"{scope}{operation}失败: {exception.Message}", exception);
    }

    public virtual string MapToolFailure(string toolName, string? clientName, string? reason = null)
    {
        var scope = string.IsNullOrWhiteSpace(clientName)
            ? toolName
            : $"{clientName}/{toolName}";

        return string.IsNullOrWhiteSpace(reason)
            ? $"MCP 工具调用失败: {scope}"
            : $"MCP 工具调用失败 [{scope}]: {reason}";
    }

    public virtual ToolResult MapToolException(global::System.Exception exception, string toolName, string? clientName = null)
    {
        var mapped = MapException(exception, $"调用工具 '{toolName}'", clientName);
        return ToolResult.Fail(mapped.Message);
    }
}