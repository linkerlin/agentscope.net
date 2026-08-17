// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.MCP;

namespace AgentScope.Core.Tests.MCP;

/// <summary>
/// Tests for <see cref="StdioMcpClient"/> covering initialization, tool listing,
/// tool invocation, and error mapping against a real Stdio MCP server.
/// <see cref="StdioMcpClient"/> 的初始化、工具列表、工具调用以及针对真实 Stdio MCP 服务器的错误映射测试。
/// </summary>
public sealed class StdioMcpClientTests
{
    /// <summary>
    /// Verifies that <see cref="StdioMcpClient"/> can initialize, list tools, and call a tool
    /// successfully against a real Stdio MCP server.
    /// 验证 StdioMcpClient 能够针对真实的 Stdio MCP 服务器成功初始化、列出工具并调用工具。
    /// </summary>
    [Fact]
    public async Task StdioMcpClient_CanInitialize_ListTools_AndCallTool()
    {
        using var server = new TestStdioMcpServer();
        if (!server.IsAvailable)
        {
            return;
        }

        using var client = server.CreateClient();

        await client.InitializeAsync();

        Assert.True(client.IsInitialized);

        var tools = await client.ListToolsAsync();
        var echoTool = Assert.Single(tools, static tool => tool.Name == "echo");
        Assert.Equal("回显输入文本", echoTool.Description);
        Assert.NotNull(echoTool.InputSchema);

        var result = await client.CallToolAsync("echo", new Dictionary<string, object> { ["text"] = "hello" });

        Assert.False(result.IsError);
        Assert.Equal("echo: hello", result.Content);
        Assert.NotNull(result.Parts);
        Assert.Single(result.Parts!);
        Assert.Equal("text", result.Parts[0].Type);
    }

    /// <summary>
    /// Verifies that when the MCP server returns a tool error,
    /// <see cref="StdioMcpClient"/> maps it to a failed <see cref="McpCallResult"/>.
    /// 验证当 MCP 服务器返回工具错误时，StdioMcpClient 将其映射为失败的 McpCallResult。
    /// </summary>
    [Fact]
    public async Task StdioMcpClient_WhenServerReturnsToolError_MapsToFailedResult()
    {
        using var server = new TestStdioMcpServer();
        if (!server.IsAvailable)
        {
            return;
        }

        using var client = server.CreateClient();

        var result = await client.CallToolAsync("fail", new Dictionary<string, object>());

        Assert.True(result.IsError);
        Assert.Equal("denied", result.Content);
    }

    /// <summary>
    /// Verifies that an <see cref="McpTool"/> backed by a <see cref="StdioMcpClient"/>
    /// can execute successfully against a real Stdio MCP server.
    /// 验证由 StdioMcpClient 支持的 McpTool 能够针对真实 Stdio MCP 服务器成功执行。
    /// </summary>
    [Fact]
    public async Task McpTool_WithStdioMcpClient_CanExecute()
    {
        using var server = new TestStdioMcpServer();
        if (!server.IsAvailable)
        {
            return;
        }

        using var client = server.CreateClient();

        var schema = new McpToolSchema
        {
            Name = "echo",
            Description = "回显输入文本"
        };

        var tool = new McpTool(client, schema);
        var result = await tool.ExecuteAsync(new Dictionary<string, object> { ["text"] = "from-tool" });

        Assert.True(result.Success);
        Assert.Equal("echo: from-tool", result.Result);
    }
}