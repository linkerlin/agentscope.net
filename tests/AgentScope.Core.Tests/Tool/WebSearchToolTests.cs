// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tool;
using Xunit;

namespace AgentScope.Core.Tests.Tool;

public class WebSearchToolTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var tool = new WebSearchTool();

        // Assert
        Assert.Equal("web_search", tool.Name);
        Assert.Equal(10, tool.MaxResults);
        Assert.True(tool.IncludeSnippets);
        Assert.Equal(TimeSpan.FromSeconds(30), tool.Timeout);
    }

    [Fact]
    public void GetSchema_ReturnsValidSchema()
    {
        // Arrange
        var tool = new WebSearchTool();

        // Act
        var schema = tool.GetSchema();

        // Assert
        Assert.Equal("web_search", schema["name"]);
        Assert.NotNull(schema["description"]);
        var parameters = schema["parameters"] as Dictionary<string, object>;
        Assert.NotNull(parameters);
        Assert.Contains("query", parameters.Keys);
    }

    [Fact]
    public async Task ExecuteAsync_WithQuery_ReturnsResults()
    {
        // Arrange
        var tool = new WebSearchTool();
        var parameters = new Dictionary<string, object>
        {
            ["query"] = "test search"
        };

        // Act
        var result = await tool.ExecuteAsync(parameters);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.Contains("Found", result.Result?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithoutQuery_ReturnsFailure()
    {
        // Arrange
        var tool = new WebSearchTool();
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await tool.ExecuteAsync(parameters);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Missing required parameter", result.Error);
    }

    [Fact]
    public async Task SearchAsync_ReturnsSimulatedResults()
    {
        // Arrange
        var tool = new WebSearchTool();

        // Act
        var results = await tool.SearchAsync("test query");

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r =>
        {
            Assert.NotNull(r.Title);
            Assert.NotNull(r.Url);
        });
    }

    [Fact]
    public async Task SearchAsync_WithMaxResults_LimitsResults()
    {
        // Arrange
        var tool = new WebSearchTool { MaxResults = 2 };

        // Act
        var results = await tool.SearchAsync("test query");

        // Assert
        Assert.True(results.Count <= 2);
    }

    [Fact]
    public async Task MockWebSearchTool_ReturnsMockResults()
    {
        // Arrange
        var mockResults = new List<WebSearchResult>
        {
            new() { Title = "Test 1", Url = "https://test1.com", Snippet = "Snippet 1" },
            new() { Title = "Test 2", Url = "https://test2.com", Snippet = "Snippet 2" }
        };
        var tool = new MockWebSearchTool(mockResults);

        // Act
        var results = await tool.SearchAsync("query");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Test 1", results[0].Title);
    }

    [Fact]
    public async Task MockWebSearchTool_WithoutMockResults_ReturnsDefault()
    {
        // Arrange
        var tool = new MockWebSearchTool();

        // Act
        var results = await tool.SearchAsync("query");

        // Assert
        Assert.Single(results);
        Assert.Contains("Mock", results[0].Title);
    }
}
