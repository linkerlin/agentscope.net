// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP 工具定义（与 MCP 协议 ListTools 返回的 Tool 对应）。
/// </summary>
public class McpToolSchema
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Dictionary<string, object>? InputSchema { get; set; }
}
