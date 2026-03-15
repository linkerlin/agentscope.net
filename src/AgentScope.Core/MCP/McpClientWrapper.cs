// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP 客户端包装器抽象基类，与 Java McpClientWrapper 对应。具体实现可委托给 C# MCP SDK。
/// </summary>
public abstract class McpClientWrapper : IMcpClient
{
    public abstract string Name { get; }
    public virtual bool IsInitialized { get; protected set; }

    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);
    public abstract Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default);
    public abstract Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default);

    public virtual void Dispose() => GC.SuppressFinalize(this);
}
