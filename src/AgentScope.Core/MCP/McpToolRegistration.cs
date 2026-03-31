// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP 工具注册信息：记录发现到的客户端工具及其运行时暴露名。
/// </summary>
public class McpToolRegistration
{
    public string ClientName { get; set; } = string.Empty;
    public string ExposedName { get; set; } = string.Empty;
    public string RemoteName { get; set; } = string.Empty;
    public McpToolSchema Schema { get; set; } = new();
}