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
using AgentScope.Core;
using AgentScope.Core.Tool;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AgentScope.Core.Tests.Tool;

public class ToolFactoryTests
{
    [Fact]
    public void Create_Calculator_ShouldReturnCalculatorTool()
    {
        var tool = ToolFactory.Create("calculator");

        Assert.NotNull(tool);
        Assert.IsType<CalculatorTool>(tool);
        Assert.Equal("calculator", tool.Name);
    }

    [Fact]
    public void Create_GetTime_ShouldReturnGetTimeTool()
    {
        var tool = ToolFactory.Create("get_time");

        Assert.NotNull(tool);
        Assert.IsType<GetTimeTool>(tool);
        Assert.Equal("get_time", tool.Name);
    }

    [Fact]
    public void Create_WebSearch_ShouldReturnWebSearchTool()
    {
        var tool = ToolFactory.Create("web_search");

        Assert.NotNull(tool);
        Assert.IsType<WebSearchTool>(tool);
        Assert.Equal("web_search", tool.Name);
    }

    [Fact]
    public void Create_CodeExecution_ShouldReturnCodeExecutionTool()
    {
        var tool = ToolFactory.Create("code_execution");

        Assert.NotNull(tool);
        Assert.IsType<CodeExecutionTool>(tool);
    }

    [Fact]
    public void Create_UnsupportedTool_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            ToolFactory.Create("unknown_tool"));
    }

    [Fact]
    public void Create_CaseInsensitive_ShouldWork()
    {
        var tool1 = ToolFactory.Create("CALCULATOR");
        var tool2 = ToolFactory.Create("Calculator");
        var tool3 = ToolFactory.Create("calculator");

        Assert.NotNull(tool1);
        Assert.NotNull(tool2);
        Assert.NotNull(tool3);
    }

    [Fact]
    public void CreateDefaults_ShouldReturnAllDefaultTools()
    {
        var tools = ToolFactory.CreateDefaults();

        Assert.NotNull(tools);
        Assert.Equal(4, tools.Count);

        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("calculator", toolNames);
        Assert.Contains("get_time", toolNames);
        Assert.Contains("web_search", toolNames);
    }

    [Fact]
    public void Create_WithNullConfig_ShouldWork()
    {
        var tool = ToolFactory.Create("calculator", null);

        Assert.NotNull(tool);
        Assert.IsType<CalculatorTool>(tool);
    }
}

public class ToolFactoryExtensionsTests
{
    [Theory]
    [InlineData("calculator", true)]
    [InlineData("CALCULATOR", true)]
    [InlineData("get_time", true)]
    [InlineData("GET_TIME", true)]
    [InlineData("web_search", true)]
    [InlineData("code_execution", true)]
    [InlineData("unknown", false)]
    [InlineData("", false)]
    public void IsSupportedTool_ShouldReturnCorrectValue(string toolType, bool expected)
    {
        var result = ToolFactoryExtensions.IsSupportedTool(toolType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSupportedTools_ShouldReturnAllTools()
    {
        var tools = ToolFactoryExtensions.GetSupportedTools();

        Assert.Contains("calculator", tools);
        Assert.Contains("get_time", tools);
        Assert.Contains("web_search", tools);
        Assert.Contains("code_execution", tools);
        Assert.Equal(4, tools.Count);
    }
}
