// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.MCP;
using AgentScope.Core.Tool;
using Xunit;

namespace AgentScope.Core.Tests.MCP;

/// <summary>
/// Tests for <see cref="McpTool"/> covering name/description mapping,
/// execution success/failure, and <see cref="McpClientWrapper"/> disposal.
/// <see cref="McpTool"/> 的名称/描述映射、执行成功/失败以及 McpClientWrapper 释放的测试。
/// </summary>
public class McpToolTests
{
    /// <summary>
    /// Verifies that <see cref="McpTool"/> reads Name and Description from the schema.
    /// 验证 McpTool 从架构中读取 Name 和 Description。
    /// </summary>
    [Fact]
    public void McpTool_Name_And_Description_FromSchema()
    {
        var schema = new McpToolSchema { Name = "test_tool", Description = "A test" };
        var client = new StubMcpClient();
        var tool = new McpTool(client, schema);
        Assert.Equal("test_tool", tool.Name);
        Assert.Equal("A test", tool.Description);
    }

    /// <summary>
    /// Verifies that <see cref="McpTool.ExecuteAsync"/> calls the client and returns a successful result.
    /// 验证 McpTool.ExecuteAsync 调用客户端并返回成功结果。
    /// </summary>
    [Fact]
    public async Task McpTool_ExecuteAsync_CallsClient_ReturnsOk()
    {
        var schema = new McpToolSchema { Name = "echo", Description = "Echo" };
        var client = new StubMcpClient { CallResult = McpCallResult.Ok("hello") };
        var tool = new McpTool(client, schema);
        var result = await tool.ExecuteAsync(new Dictionary<string, object> { ["x"] = "1" });
        Assert.True(result.Success);
        Assert.Equal("hello", result.Result);
    }

    /// <summary>
    /// Verifies that <see cref="McpTool.ExecuteAsync"/> returns a failed result
    /// when the client returns an error.
    /// 验证当客户端返回错误时，McpTool.ExecuteAsync 返回失败结果。
    /// </summary>
    [Fact]
    public async Task McpTool_ExecuteAsync_WhenClientReturnsError_ReturnsFail()
    {
        var schema = new McpToolSchema { Name = "fail", Description = "" };
        var client = new StubMcpClient { CallResult = McpCallResult.Fail("denied") };
        var tool = new McpTool(client, schema);
        var result = await tool.ExecuteAsync(new Dictionary<string, object>());
        Assert.False(result.Success);
        Assert.Contains("denied", result.Error);
    }

    /// <summary>
    /// Verifies that disposing an <see cref="McpClientWrapper"/> does not throw any exception.
    /// 验证释放 McpClientWrapper 不会抛出任何异常。
    /// </summary>
    [Fact]
    public void McpClientWrapper_Dispose_DoesNotThrow()
    {
        var w = new ConcreteMcpClientWrapper();
        w.Dispose();
    }

    /// <summary>
    /// A stub <see cref="IMcpClient"/> for testing that returns a configurable <see cref="CallResult"/>.
    /// 用于测试的 IMcpClient 存根，返回可配置的 CallResult。
    /// </summary>
    private sealed class StubMcpClient : IMcpClient
    {
        public bool IsInitialized { get; set; } = true;
        public string Name => "Stub";
        public McpCallResult? CallResult { get; set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<McpToolSchema>>(new List<McpToolSchema>());
        public Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default) =>
            Task.FromResult(CallResult ?? McpCallResult.Ok());
        public void Dispose() { }
    }

    /// <summary>
    /// A concrete implementation of <see cref="McpClientWrapper"/> for testing disposal behavior.
    /// 用于测试释放行为的 McpClientWrapper 具体实现。
    /// </summary>
    private sealed class ConcreteMcpClientWrapper : McpClientWrapper
    {
        public override string Name => "Concrete";
        public override Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public override Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<McpToolSchema>>(Array.Empty<McpToolSchema>());
        public override Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default) =>
            Task.FromResult(McpCallResult.Ok());
    }
}
