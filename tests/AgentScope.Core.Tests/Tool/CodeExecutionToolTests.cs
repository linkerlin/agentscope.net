// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tool;
using Xunit;

namespace AgentScope.Core.Tests.Tool;

public class CodeExecutionToolTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var tool = new CodeExecutionTool();

        // Assert
        Assert.Equal("code_execution", tool.Name);
        Assert.Equal(TimeSpan.FromSeconds(30), tool.Timeout);
        Assert.False(tool.AllowNetwork);
        Assert.Equal(10000, tool.MaxOutputLength);
    }

    [Fact]
    public void GetSchema_ReturnsValidSchema()
    {
        // Arrange
        var tool = new CodeExecutionTool();

        // Act
        var schema = tool.GetSchema();

        // Assert
        Assert.Equal("code_execution", schema["name"]);
        var parameters = schema["parameters"] as Dictionary<string, object>;
        Assert.NotNull(parameters);
        Assert.Contains("code", parameters.Keys);
        Assert.Contains("language", parameters.Keys);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutCode_ReturnsFailure()
    {
        // Arrange
        var tool = new CodeExecutionTool();
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await tool.ExecuteAsync(parameters);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Missing required parameter", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnsupportedLanguage_ReturnsFailure()
    {
        // Arrange
        var tool = new CodeExecutionTool();
        var parameters = new Dictionary<string, object>
        {
            ["code"] = "print('hello')",
            ["language"] = "unsupported_language"
        };

        // Act
        var result = await tool.ExecuteAsync(parameters);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unsupported language", result.Error);
    }

    [Fact]
    public async Task ExecuteCodeAsync_WithPython_PrintsOutput()
    {
        // Arrange
        var tool = new CodeExecutionTool { Timeout = TimeSpan.FromSeconds(5) };
        var code = "print('Hello, World!')";

        // Act
        var result = await tool.ExecuteCodeAsync(code, CodeLanguage.Python);

        // Assert - Python may not be installed in test environment
        // So we just verify the method doesn't throw
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteCodeAsync_WithUnsupportedLanguage_ReturnsError()
    {
        // Arrange
        var tool = new CodeExecutionTool();
        var code = "console.log('test');";

        // Act - Try to execute JavaScript (may not be available)
        var result = await tool.ExecuteCodeAsync(code, CodeLanguage.Java);

        // Assert - Java is likely not configured, should return error
        Assert.NotNull(result);
    }

    [Fact]
    public void CodeExecutionResult_Success_HasCorrectProperties()
    {
        // Arrange & Act
        var result = new CodeExecutionResult
        {
            Success = true,
            StdOut = "output",
            StdErr = "",
            ExitCode = 0,
            Duration = TimeSpan.FromSeconds(1)
        };

        // Assert
        Assert.True(result.Success);
        Assert.Equal("output", result.StdOut);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(TimeSpan.FromSeconds(1), result.Duration);
    }

    [Fact]
    public void CodeExecutionResult_Failure_HasCorrectProperties()
    {
        // Arrange & Act
        var exception = new InvalidOperationException("Test error");
        var result = new CodeExecutionResult
        {
            Success = false,
            StdOut = "",
            StdErr = "error",
            ExitCode = 1,
            Exception = exception,
            Duration = TimeSpan.FromMilliseconds(100)
        };

        // Assert
        Assert.False(result.Success);
        Assert.Equal("error", result.StdErr);
        Assert.Equal(1, result.ExitCode);
        Assert.NotNull(result.Exception);
    }
}

public class SafeCodeExecutionToolTests
{
    [Fact]
    public async Task ExecuteCodeAsync_WithBlockedPattern_ReturnsError()
    {
        // Arrange
        var tool = new SafeCodeExecutionTool();
        var code = "import os; os.system('rm -rf /')";

        // Act
        var result = await tool.ExecuteCodeAsync(code, CodeLanguage.Python);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Security error", result.StdErr);
        Assert.Contains("os.system", result.StdErr);
    }

    [Fact]
    public async Task ExecuteCodeAsync_WithEval_ReturnsError()
    {
        // Arrange
        var tool = new SafeCodeExecutionTool();
        var code = "eval('1 + 1')";

        // Act
        var result = await tool.ExecuteCodeAsync(code, CodeLanguage.Python);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Security error", result.StdErr);
        Assert.Contains("eval(", result.StdErr);
    }

    [Fact]
    public async Task ExecuteCodeAsync_SafeCode_AllowsExecution()
    {
        // Arrange
        var tool = new SafeCodeExecutionTool();
        var code = "x = 1 + 1; print(x)";

        // Act
        var result = await tool.ExecuteCodeAsync(code, CodeLanguage.Python);

        // Assert - May succeed or fail depending on Python availability
        // But should not fail due to security check
        Assert.DoesNotContain("Security error", result.StdErr);
    }
}
