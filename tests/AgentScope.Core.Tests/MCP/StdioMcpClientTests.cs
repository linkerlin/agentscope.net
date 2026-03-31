// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.MCP;

namespace AgentScope.Core.Tests.MCP;

public sealed class StdioMcpClientTests
{
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