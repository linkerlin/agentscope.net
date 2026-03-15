// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tool.Multimodal;
using Xunit;

namespace AgentScope.Core.Tests.Tool.Multimodal;

public class OpenAIMultiModalToolTests
{
    [Fact]
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
    public async Task ExecuteAsync_MissingAction_ReturnsFail()
    {
        var tool = new OpenAIMultiModalTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object>());
        Assert.False(result.Success);
        Assert.Contains("action", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownAction_ReturnsFail()
    {
        var tool = new OpenAIMultiModalTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object> { ["action"] = "unknown" });
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_TextToImage_ReturnsPlaceholderMessage()
    {
        var tool = new OpenAIMultiModalTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object> { ["action"] = "text_to_image", ["prompt"] = "a cat" });
        Assert.False(result.Success);
        Assert.Contains("API", result.Error);
    }
}
