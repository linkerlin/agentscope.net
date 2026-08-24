// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Generic;
using System.Reactive.Linq;
using AgentScope.Core.MCP;
using AgentScope.Core.Message;
using AgentScope.Core.Model;

namespace AgentScope.Core.Tests.MCP;

/// <summary>
/// Tests for <see cref="McpManager"/> covering tool creation, conflict detection,
/// and integration with <see cref="ReActAgent"/>.
/// <see cref="McpManager"/> 的工具创建、冲突检测以及与 ReActAgent 集成的测试。
/// </summary>
public class McpManagerTests
{
    /// <summary>
    /// Verifies that <see cref="McpManager.CreateToolsAsync"/> returns executable tools
    /// when using a real Stdio MCP server client.
    /// 验证使用真实 Stdio MCP 服务器客户端时，CreateToolsAsync 返回可执行工具。
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="McpManager.DiscoverToolsAsync"/> throws <see cref="McpException"/>
    /// when two registered clients expose tools with the same name.
    /// 验证当两个已注册客户端暴露同名工具时，DiscoverToolsAsync 抛出 McpException。
    /// </summary>
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

    /// <summary>
    /// Verifies that a <see cref="ReActAgent"/> can use MCP tools managed by <see cref="McpManager"/>
    /// to execute a real tool via the Stdio MCP server.
    /// 验证 ReActAgent 能够使用 McpManager 管理的 MCP 工具，通过 Stdio MCP 服务器执行真实工具。
    /// </summary>
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

    /// <summary>
    /// Verifies that when an <see cref="McpTool"/> client throws an exception,
    /// the error message is properly mapped to a failed <see cref="ToolResult"/>.
    /// 验证当 McpTool 客户端抛出异常时，错误信息被正确映射到失败的 ToolResult。
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="McpContentConverter.ConvertPartsToText"/> correctly
    /// converts mixed text and image content parts into a single text representation.
    /// 验证 McpContentConverter.ConvertPartsToText 正确地将混合的文本和图片内容部分转换为单一文本表示。
    /// </summary>
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

    /// <summary>
    /// A stub implementation of <see cref="IMcpClient"/> for testing purposes,
    /// returning pre-configured tool schemas and a default OK result.
    /// 用于测试的 IMcpClient 存根实现，返回预配置的工具架构和默认的成功结果。
    /// </summary>
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

    /// <summary>
    /// A stub implementation of <see cref="IMcpClient"/> that throws a pre-configured
    /// exception when <see cref="CallToolAsync"/> is invoked.
    /// 在 CallToolAsync 被调用时抛出预配置异常的 IMcpClient 存根实现。
    /// </summary>
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

    /// <summary>
    /// A scripted <see cref="IModel"/> implementation that returns pre-defined responses
    /// in sequence, used for testing agent workflows without a real model.
    /// 一个脚本化的 IModel 实现，按顺序返回预定义的响应，用于在没有真实模型的情况下测试 agent 工作流。
    /// </summary>
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