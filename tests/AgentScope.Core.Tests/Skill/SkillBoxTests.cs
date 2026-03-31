// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Skill;
using AgentScope.Core.Tool;

namespace AgentScope.Core.Tests.Skill;

public class SkillBoxTests
{
    [Fact]
    public void Discover_And_Load_BindsToolsAndRespectsDefaultActiveState()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"skill-box-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var skillPath = Path.Combine(tempDirectory, "ops-helper.md");
            File.WriteAllText(skillPath, """
---
name: Ops Helper
description: Handles operational helper tasks.
tools: calculator, get_time
active: false
---
# Ops Helper

Use this skill for simple operational assistance.
""");

            var skillBox = new SkillBox();
            skillBox.AddRepository(new FileSystemSkillRepository(tempDirectory));
            skillBox.AddTools(new ITool[] { new CalculatorTool(), new GetTimeTool() });

            var discovered = skillBox.Discover();
            Assert.Single(discovered);
            Assert.Equal("ops-helper", discovered[0].Id);
            Assert.Equal(2, discovered[0].ToolNames.Count);
            Assert.False(discovered[0].IsActiveByDefault);

            var loaded = skillBox.Load("ops-helper");
            var markdownSkill = Assert.IsType<MarkdownSkill>(loaded);

            Assert.Equal("Ops Helper", markdownSkill.Name);
            Assert.Equal(2, markdownSkill.Tools.Count);
            Assert.Contains(markdownSkill.Tools, tool => tool.Name == "calculator");
            Assert.Contains(markdownSkill.Tools, tool => tool.Name == "get_time");
            Assert.False(markdownSkill.IsActive);
            Assert.Empty(skillBox.GetActiveSkills());
            Assert.Empty(skillBox.GetActiveTools());

            skillBox.Activate("ops-helper");

            Assert.Single(skillBox.GetActiveSkills());
            Assert.Equal(2, skillBox.GetActiveTools().Count);
            Assert.Contains(skillBox.GetActiveTools(), tool => tool.Name == "calculator");
            Assert.Contains(skillBox.GetActiveTools(), tool => tool.Name == "get_time");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void LoadAll_LoadsEveryRegisteredSkill_AndDeduplicatesActiveTools()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"skill-box-all-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            File.WriteAllText(Path.Combine(tempDirectory, "math.md"), """
---
name: Math Skill
tools: calculator
---
# Math Skill

Do calculations.
""");

            File.WriteAllText(Path.Combine(tempDirectory, "time.md"), """
---
name: Time Skill
tools: get_time, calculator
---
# Time Skill

Read current time.
""");

            var skillBox = new SkillBox();
            skillBox.AddRepository(new FileSystemSkillRepository(tempDirectory));
            skillBox.AddTools(new ITool[] { new CalculatorTool(), new GetTimeTool() });

            var loaded = skillBox.LoadAll();

            Assert.Equal(2, loaded.Count);
            Assert.Equal(2, skillBox.GetActiveSkills().Count);
            Assert.Equal(2, skillBox.GetActiveTools().Count);
            Assert.Contains(skillBox.GetActiveTools(), tool => tool.Name == "calculator");
            Assert.Contains(skillBox.GetActiveTools(), tool => tool.Name == "get_time");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}