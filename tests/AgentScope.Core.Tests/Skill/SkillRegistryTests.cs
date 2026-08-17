// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Skill;
using AgentScope.Core.Tool;
using Xunit;

namespace AgentScope.Core.Tests.Skill;

/// <summary>
/// Unit tests for <see cref="SkillRegistry"/>, verifying registration, retrieval, activation, and metadata storage.
/// 对 <see cref="SkillRegistry"/> 的单元测试，验证注册、检索、激活以及元数据存储。
/// </summary>
public class SkillRegistryTests
{
    /// <summary>
    /// Tests that <see cref="SkillRegistry.Register"/> stores a skill and <see cref="SkillRegistry.Get"/> retrieves it by ID.
    /// 测试 <see cref="SkillRegistry.Register"/> 存储技能后，<see cref="SkillRegistry.Get"/> 能否通过 ID 检索。
    /// </summary>
    [Fact]
    public void Register_And_Get()
    {
        var reg = new SkillRegistry();
        var skill = new SimpleSkill("s1", "Skill1", "Desc", new List<ITool> { new CalculatorTool() });
        reg.Register("s1", skill);
        Assert.Same(skill, reg.Get("s1"));
        Assert.Null(reg.Get("s2"));
    }

    /// <summary>
    /// Tests that <see cref="SkillRegistry.SetActive"/> changes a skill's activation state and
    /// <see cref="SkillRegistry.GetActiveSkills"/> returns only active skills.
    /// 测试 <see cref="SkillRegistry.SetActive"/> 更改技能的激活状态，
    /// 且 <see cref="SkillRegistry.GetActiveSkills"/> 仅返回活跃的技能。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SkillRegistry.Register(ISkill, RegisteredSkill)"/> stores metadata
    /// and <see cref="SkillRegistry.GetRegistered"/> retrieves it.
    /// 测试 <see cref="SkillRegistry.Register(ISkill, RegisteredSkill)"/> 存储元数据后
    /// <see cref="SkillRegistry.GetRegistered"/> 能否正确检索。
    /// </summary>
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

    /// <summary>
    /// A minimal <see cref="ISkill"/> implementation used for testing the <see cref="SkillRegistry"/>.
    /// 一个用于测试 <see cref="SkillRegistry"/> 的最小化 <see cref="ISkill"/> 实现。
    /// </summary>
    private sealed class SimpleSkill : ISkill
    {
        /// <summary>
        /// Initializes a new <see cref="SimpleSkill"/> with the given identity and tool list.
        /// 使用给定的标识和工具列表初始化一个新的 <see cref="SimpleSkill"/>。
        /// </summary>
        public SimpleSkill(string id, string name, string description, IReadOnlyList<ITool> tools)
        {
            Id = id;
            Name = name;
            Description = description;
            Tools = tools;
        }
        /// <summary>Gets the skill identifier. 获取技能标识符。</summary>
        public string Id { get; }
        /// <summary>Gets the display name of the skill. 获取技能的显示名称。</summary>
        public string Name { get; }
        /// <summary>Gets the description of the skill. 获取技能描述。</summary>
        public string Description { get; }
        /// <summary>Gets the list of tools associated with this skill. 获取与此技能关联的工具列表。</summary>
        public IReadOnlyList<ITool> Tools { get; }
        /// <summary>Gets or sets whether this skill is active. 获取或设置技能是否激活。</summary>
        public bool IsActive { get; set; }
    }
}
