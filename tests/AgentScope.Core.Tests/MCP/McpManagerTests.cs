// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Generic;
using System.Reactive.Linq;
using AgentScope.Core.MCP;
using AgentScope.Core.Message;
using AgentScope.Core.Model;

namespace AgentScope.Core.Tests.MCP;

public class McpManagerTests
{
    [Fact]
    public async Task McpManager_CreateToolsAsync_FromStdioClient_ReturnsExecutableTools()
    {
        using var server = new TestStdioMcpServer();
        if (!server.IsAvailable)
        {
            return;
        }

        using var manager = new McpManager();
        manager.RegisterClient(server.CreateClient());

        var tools = await manager.CreateToolsAsync();
        var echoTool = Assert.Single(tools, static tool => tool.Name == "echo");

        var result = await echoTool.ExecuteAsync(new Dictionary<string, object> { ["text"] = "from-manager" });

        Assert.True(result.Success);
        Assert.Equal("echo: from-manager", result.Result);
    }

    [Fact]
    public async Task McpManager_DiscoverToolsAsync_WhenToolNamesConflict_Throws()
    {
        using var manager = new McpManager();
        manager.RegisterClient(new StubMcpClient("client-a", new McpToolSchema { Name = "echo", Description = "A" }));
        manager.RegisterClient(new StubMcpClient("client-b", new McpToolSchema { Name = "echo", Description = "B" }));

        var exception = await Assert.ThrowsAsync<McpException>(() => manager.DiscoverToolsAsync());

        Assert.Contains("工具名冲突", exception.Message);
        Assert.Contains("client-a", exception.Message);
        Assert.Contains("client-b", exception.Message);
    }

    [Fact]
    public async Task ReActAgent_WithManagedMcpTools_CanExecuteRealTool()
    {
        using var server = new TestStdioMcpServer();
        if (!server.IsAvailable)
        {
            return;
        }

        using var manager = new McpManager();
        manager.RegisterClient(server.CreateClient());
        var tools = await manager.CreateToolsAsync();

        var model = new ScriptedModel(
            "Thought: 需要调用 MCP 工具\nAction: echo\nAction Input: {\"text\":\"via-agent\"}",
            "Thought: 已获得工具结果\nAction: finish\nAction Input: MCP finished");

        var agent = ReActAgent.Builder()
            .Name("McpManagedAgent")
            .Model(model)
            .Tools(tools)
            .MaxIterations(3)
            .Build();

        var response = await agent.CallAsync(Msg.Builder().TextContent("test mcp").Build());

        Assert.Equal("MCP finished", response.GetTextContent());
        Assert.NotNull(response.Metadata);
        Assert.True(response.Metadata!.TryGetValue("thoughts", out var thoughts));
        Assert.Contains("echo: via-agent", thoughts?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpTool_WhenClientThrows_UsesMappedErrorMessage()
    {
        var tool = new McpTool(
            new ThrowingMcpClient("broken-client", new TimeoutException("request timed out")),
            new McpToolSchema { Name = "echo", Description = "Echo" });

        var result = await tool.ExecuteAsync(new Dictionary<string, object>());

        Assert.False(result.Success);
        Assert.Contains("broken-client", result.Error, StringComparison.Ordinal);
        Assert.Contains("echo", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void McpContentConverter_ConvertsMixedPartsToText()
    {
        var converter = new McpContentConverter();

        var text = converter.ConvertPartsToText(
            new List<McpContentItem>
            {
                new() { Type = "text", Text = "hello" },
                new() { Type = "image", MimeType = "image/png" }
            });

        Assert.Equal("hello\n[image:image/png]", text);
    }

    private sealed class StubMcpClient : IMcpClient
    {
        private readonly IReadOnlyList<McpToolSchema> _toolSchemas;

        public StubMcpClient(string name, params McpToolSchema[] toolSchemas)
        {
            Name = name;
            _toolSchemas = toolSchemas;
        }

        public string Name { get; }
        public bool IsInitialized { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_toolSchemas);
        }

        public Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(McpCallResult.Ok(toolName));
        }

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingMcpClient : IMcpClient
    {
        private readonly global::System.Exception _exception;

        public ThrowingMcpClient(string name, global::System.Exception exception)
        {
            Name = name;
            _exception = exception;
        }

        public string Name { get; }
        public bool IsInitialized => true;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<McpToolSchema>>(Array.Empty<McpToolSchema>());

        public Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default)
            => throw _exception;

        public void Dispose()
        {
        }
    }

    private sealed class ScriptedModel : IModel
    {
        private readonly Queue<string> _responses;

        public ScriptedModel(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public string ModelName => "mcp-scripted";

        public IObservable<ModelResponse> Generate(ModelRequest request)
        {
            _ = request;
            return Observable.Return(CreateResponse());
        }

        public Task<ModelResponse> GenerateAsync(ModelRequest request)
        {
            _ = request;
            return Task.FromResult(CreateResponse());
        }

        private ModelResponse CreateResponse()
        {
            var text = _responses.Count > 0 ? _responses.Dequeue() : string.Empty;
            return new ModelResponse
            {
                Success = true,
                Text = text
            };
        }
    }
}