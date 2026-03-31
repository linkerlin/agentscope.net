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
using AgentScope.Core.Tool.Coding;
using AgentScope.Core.Tool.File;
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
    public void Create_ReadFile_ShouldReturnReadFileTool()
    {
        var tool = ToolFactory.Create("read_file");

        Assert.NotNull(tool);
        Assert.IsType<ReadFileTool>(tool);
    }

    [Fact]
    public void Create_WriteFile_ShouldReturnWriteFileTool()
    {
        var tool = ToolFactory.Create("write_file");

        Assert.NotNull(tool);
        Assert.IsType<WriteFileTool>(tool);
    }

    [Fact]
    public void Create_ShellCommand_ShouldReturnShellCommandTool()
    {
        var tool = ToolFactory.Create("shell_command");

        Assert.NotNull(tool);
        Assert.IsType<ShellCommandTool>(tool);
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
    public void CreateAdvanced_ShouldReturnOnlyAdvancedTools()
    {
        var tools = ToolFactory.CreateAdvanced();

        Assert.NotNull(tools);
        Assert.Equal(3, tools.Count);

        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("read_file", toolNames);
        Assert.Contains("write_file", toolNames);
        Assert.Contains("shell_command", toolNames);
        Assert.DoesNotContain("calculator", toolNames);
    }

    [Fact]
    public void CreateAll_ShouldReturnDefaultAndAdvancedTools()
    {
        var tools = ToolFactory.CreateAll();

        Assert.NotNull(tools);
        Assert.Equal(7, tools.Count);

        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("calculator", toolNames);
        Assert.Contains("shell_command", toolNames);
    }

    [Fact]
    public void CreatePreset_Default_ShouldMatchCreateDefaults()
    {
        var presetTools = ToolFactory.CreatePreset(ToolPreset.Default).Select(t => t.Name).ToList();
        var defaultTools = ToolFactory.CreateDefaults().Select(t => t.Name).ToList();

        Assert.Equal(defaultTools, presetTools);
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
    [InlineData("read_file", true)]
    [InlineData("write_file", true)]
    [InlineData("shell_command", true)]
    [InlineData("unknown", false)]
    [InlineData("", false)]
    public void IsSupportedTool_ShouldReturnCorrectValue(string toolType, bool expected)
    {
        var result = ToolFactoryExtensions.IsSupportedTool(toolType);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("calculator", true)]
    [InlineData("web_search", true)]
    [InlineData("shell_command", false)]
    [InlineData("unknown", false)]
    public void IsDefaultTool_ShouldReturnCorrectValue(string toolType, bool expected)
    {
        var result = ToolFactoryExtensions.IsDefaultTool(toolType);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("read_file", true)]
    [InlineData("shell_command", true)]
    [InlineData("calculator", false)]
    [InlineData("unknown", false)]
    public void IsAdvancedTool_ShouldReturnCorrectValue(string toolType, bool expected)
    {
        var result = ToolFactoryExtensions.IsAdvancedTool(toolType);
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
        Assert.Contains("read_file", tools);
        Assert.Contains("write_file", tools);
        Assert.Contains("shell_command", tools);
        Assert.Equal(7, tools.Count);
    }

    [Fact]
    public void GetDefaultTools_ShouldReturnOnlyDefaultTools()
    {
        var tools = ToolFactoryExtensions.GetDefaultTools();

        Assert.Equal(4, tools.Count);
        Assert.Contains("calculator", tools);
        Assert.Contains("code_execution", tools);
        Assert.DoesNotContain("read_file", tools);
    }

    [Fact]
    public void GetAdvancedTools_ShouldReturnOnlyAdvancedTools()
    {
        var tools = ToolFactoryExtensions.GetAdvancedTools();

        Assert.Equal(3, tools.Count);
        Assert.Contains("read_file", tools);
        Assert.Contains("write_file", tools);
        Assert.Contains("shell_command", tools);
        Assert.DoesNotContain("calculator", tools);
    }
}
