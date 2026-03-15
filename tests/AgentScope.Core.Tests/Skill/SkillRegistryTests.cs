// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Skill;
using AgentScope.Core.Tool;
using Xunit;

namespace AgentScope.Core.Tests.Skill;

public class SkillRegistryTests
{
    [Fact]
    public void Register_And_Get()
    {
        var reg = new SkillRegistry();
        var skill = new SimpleSkill("s1", "Skill1", "Desc", new List<ITool> { new CalculatorTool() });
        reg.Register("s1", skill);
        Assert.Same(skill, reg.Get("s1"));
        Assert.Null(reg.Get("s2"));
    }

    [Fact]
    public void SetActive_GetActiveSkills()
    {
        var reg = new SkillRegistry();
        var a = new SimpleSkill("a", "A", "", new List<ITool>()) { IsActive = true };
        var b = new SimpleSkill("b", "B", "", new List<ITool>()) { IsActive = false };
        reg.Register("a", a);
        reg.Register("b", b);
        var active = reg.GetActiveSkills().ToList();
        Assert.Single(active);
        Assert.Same(a, active[0]);
        reg.SetActive("b", true);
        active = reg.GetActiveSkills().ToList();
        Assert.Equal(2, active.Count);
    }

    [Fact]
    public void Register_WithMetadata_GetRegistered()
    {
        var reg = new SkillRegistry();
        var meta = new RegisteredSkill { Id = "x", Name = "X", SourcePath = "/p/x.md" };
        reg.Register("x", new SimpleSkill("x", "X", "", new List<ITool>()), meta);
        var r = reg.GetRegistered("x");
        Assert.NotNull(r);
        Assert.Equal("/p/x.md", r!.SourcePath);
    }

    private sealed class SimpleSkill : ISkill
    {
        public SimpleSkill(string id, string name, string description, IReadOnlyList<ITool> tools)
        {
            Id = id;
            Name = name;
            Description = description;
            Tools = tools;
        }
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<ITool> Tools { get; }
        public bool IsActive { get; set; }
    }
}
