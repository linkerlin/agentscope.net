// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Xunit;
using AgentScope.Core.Tool;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentScope.Core.Tests.Tool;

public class ExampleToolsTests
{
    [Fact]
    public async Task CalculatorTool_Execute_ShouldAddNumbers()
    {
        // Arrange
        var tool = new CalculatorTool();
        var parameters = new Dictionary<string, object>
        {
            ["a"] = 5,
            ["b"] = 3
        };

        // Act
        var result = await tool.ExecuteAsync(parameters);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(8.0, Convert.ToDouble(result.Result));
    }

    [Fact]
    public async Task CalculatorTool_WithMissingParameter_ShouldFail()
    {
        // Arrange
        var tool = new CalculatorTool();
        var parameters = new Dictionary<string, object>
        {
            ["a"] = 5
        };

        // Act
        var result = await tool.ExecuteAsync(parameters);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void CalculatorTool_GetSchema_ShouldReturnValidSchema()
    {
        // Arrange
        var tool = new CalculatorTool();

        // Act
        var schema = tool.GetSchema();

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("function", schema.Keys);
        Assert.Equal("calculator", tool.Name);
    }

    [Fact]
    public async Task GetTimeTool_Execute_ShouldReturnCurrentTime()
    {
        // Arrange
        var tool = new GetTimeTool();
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await tool.ExecuteAsync(parameters);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        var timeStr = result.Result.ToString();
        Assert.Contains("-", timeStr); // Date separator
        Assert.Contains(":", timeStr); // Time separator
    }

    [Fact]
    public void GetTimeTool_GetSchema_ShouldReturnValidSchema()
    {
        // Arrange
        var tool = new GetTimeTool();

        // Act
        var schema = tool.GetSchema();

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("function", schema.Keys);
        Assert.Equal("get_time", tool.Name);
    }

    [Fact]
    public void ToolResult_Ok_ShouldCreateSuccessResult()
    {
        // Arrange & Act
        var result = ToolResult.Ok("test result");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("test result", result.Result);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ToolResult_Fail_ShouldCreateFailureResult()
    {
        // Arrange & Act
        var result = ToolResult.Fail("test error");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("test error", result.Error);
        Assert.Null(result.Result);
    }
}
