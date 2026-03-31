// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Exception;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP 客户端或协议错误。
/// </summary>
public class McpException : AgentScopeException
{
    public McpException(string message)
        : base(message)
    {
    }

    public McpException(string message, global::System.Exception innerException)
        : base(message, innerException)
    {
    }

    public int? Code { get; init; }
}