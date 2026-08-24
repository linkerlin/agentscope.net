// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tool.Multimodal;
using Xunit;

namespace AgentScope.Core.Tests.Tool.Multimodal;

/// <summary>
/// Tests for OpenAIMultiModalTool
/// OpenAIMultiModalTool 的测试
/// </summary>
public class OpenAIMultiModalToolTests
{
    [Fact]
    /// <summary>
    /// GetSchema returns schema with action and optional parameters
    /// 测试 GetSchema 返回包含 action 和可选参数的 schema
    /// </summary>
    public void GetSchema_IncludesActionAndOptionalParams()
    {
        var tool = new OpenAIMultiModalTool();
        var schema = tool.GetSchema();
        Assert.Equal("openai_multimodal", schema["name"]);
        var parameters = schema["parameters"] as Dictionary<string, object>;
        Assert.NotNull(parameters);
        Assert.True(parameters!.ContainsKey("action"));
        Assert.True(parameters.ContainsKey("prompt"));
    }

    [Fact]
    /// <summary>
    /// ExecuteAsync returns fail when action parameter is missing
    /// 测试 ExecuteAsync 在缺少 action 参数时返回失败
    /// </summary>
    public async Task ExecuteAsync_MissingAction_ReturnsFail()
    {
        var tool = new OpenAIMultiModalTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object>());
        Assert.False(result.Success);
        Assert.Contains("action", result.Error);
    }

    [Fact]
    /// <summary>
    /// ExecuteAsync returns fail when action is unknown
    /// 测试 ExecuteAsync 在 action 未知时返回失败
    /// </summary>
    public async Task ExecuteAsync_UnknownAction_ReturnsFail()
    {
        var tool = new OpenAIMultiModalTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object> { ["action"] = "unknown" });
        Assert.False(result.Success);
    }

    [Fact]
    /// <summary>
    /// ExecuteAsync with text_to_image action returns API placeholder error
    /// 测试 ExecuteAsync 的 text_to_image action 返回 API 占位错误
    /// </summary>
    public async Task ExecuteAsync_TextToImage_ReturnsPlaceholderMessage()
    {
        var tool = new OpenAIMultiModalTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object> { ["action"] = "text_to_image", ["prompt"] = "a cat" });
        Assert.False(result.Success);
        Assert.Contains("API", result.Error);
    }
}
