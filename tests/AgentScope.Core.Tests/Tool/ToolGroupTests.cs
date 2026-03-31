// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Formatter;
using AgentScope.Core.Tool;
using Xunit;

namespace AgentScope.Core.Tests.Tool;

public class ToolGroupTests
{
    [Fact]
    public void ToolGroup_AddAndContains()
    {
        var g = new ToolGroup("g1", "desc");
        g.AddTool("tool_a");
        g.AddTool("tool_b");
        Assert.True(g.ContainsTool("tool_a"));
        Assert.True(g.ContainsTool("TOOL_B"));
        Assert.False(g.ContainsTool("tool_c"));
        Assert.Equal(2, g.GetTools().Count);
    }

    [Fact]
    public void ToolGroup_RemoveTool()
    {
        var g = new ToolGroup("g1");
        g.AddTool("x");
        g.RemoveTool("x");
        Assert.False(g.ContainsTool("x"));
    }

    [Fact]
    public void ToolGroupManager_ActivateDeactivate_GetActiveToolNames()
    {
        var m = new ToolGroupManager();
        var g1 = new ToolGroup("admin", "Admin tools") { IsActive = true };
        g1.AddTool("read_file");
        g1.AddTool("write_file");
        var g2 = new ToolGroup("user", "User tools") { IsActive = false };
        g2.AddTool("calculator");
        m.RegisterGroup(g1);
        m.RegisterGroup(g2);
        var names = m.GetActiveToolNames().ToList();
        Assert.Equal(2, names.Count);
        Assert.Contains("read_file", names);
        Assert.Contains("write_file", names);
        m.DeactivateGroup("admin");
        m.ActivateGroup("user");
        names = m.GetActiveToolNames().ToList();
        Assert.Single(names);
        Assert.Contains("calculator", names);
    }

    [Fact]
    public void ToolGroupManager_GetActiveToolSchemas_RespectsActiveGroups()
    {
        var m = new ToolGroupManager();
        var g = new ToolGroup("g1") { IsActive = true };
        g.AddTool("calc");
        m.RegisterGroup(g);
        var tools = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase)
        {
            ["calc"] = new CalculatorTool(),
            ["other"] = new GetTimeTool()
        };
        var schemas = m.GetActiveToolSchemas(tools);
        Assert.Single(schemas);
        Assert.Equal("calculator", schemas[0].Name);
    }

    [Fact]
    public void ToolGroupManager_FilterActiveTools_WithoutGroups_ReturnsAllTools()
    {
        var m = new ToolGroupManager();
        var tools = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase)
        {
            ["calc"] = new CalculatorTool(),
            ["time"] = new GetTimeTool()
        };

        var filtered = m.FilterActiveTools(tools);

        Assert.Equal(2, filtered.Count);
        Assert.Contains("calc", filtered.Keys);
        Assert.Contains("time", filtered.Keys);
    }

    [Fact]
    public void ToolGroupManager_FilterActiveTools_WithActiveGroups_ReturnsSubset()
    {
        var m = new ToolGroupManager();
        var g = new ToolGroup("readonly") { IsActive = true };
        g.AddTool("calc");
        m.RegisterGroup(g);

        var tools = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase)
        {
            ["calc"] = new CalculatorTool(),
            ["time"] = new GetTimeTool()
        };

        var filtered = m.FilterActiveTools(tools);

        Assert.Single(filtered);
        Assert.Contains("calc", filtered.Keys);
        Assert.DoesNotContain("time", filtered.Keys);
    }
}
