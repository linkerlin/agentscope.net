// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Skill;

namespace AgentScope.Core.Tests.Skill;

/// <summary>
/// Unit tests for <see cref="MarkdownSkillParser"/> and <see cref="FileSystemSkillRepository"/>,
/// verifying parsing of markdown front matter, fallback behavior, and file-system scanning.
/// 对 <see cref="MarkdownSkillParser"/> 和 <see cref="FileSystemSkillRepository"/> 的单元测试，
/// 验证 Markdown 前置元数据解析、回退行为以及文件系统扫描。
/// </summary>
public class MarkdownSkillParserTests
{
    /// <summary>
    /// Tests that parsing markdown with YAML front matter produces a correctly populated <see cref="RegisteredSkill"/>.
    /// 测试解析包含 YAML 前置元数据的 Markdown 是否能正确生成填充好的 <see cref="RegisteredSkill"/>。
    /// </summary>
    [Fact]
    public void Parse_WithFrontMatter_ReturnsRegisteredSkill()
    {
        var parser = new MarkdownSkillParser();

        var registered = parser.Parse("""
---
id: shell-helper
name: Shell Helper
description: Executes shell-related workflows.
    tools: shell_command
    active: false
---
# Shell Helper

Use this skill when shell work is required.
""");

        Assert.Equal("shell-helper", registered.Id);
        Assert.Equal("Shell Helper", registered.Name);
        Assert.Equal("Executes shell-related workflows.", registered.Description);
        Assert.Single(registered.ToolNames);
        Assert.Equal("shell_command", registered.ToolNames[0]);
        Assert.False(registered.IsActiveByDefault);
        Assert.Contains("Use this skill", registered.RawContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that parsing markdown without front matter falls back to the heading and source path for metadata.
    /// 测试解析没有前置元数据的 Markdown 时，是否回退使用标题和源路径作为元数据。
    /// </summary>
    [Fact]
    public void Parse_WithoutFrontMatter_UsesHeadingAndSourcePathFallback()
    {
        var parser = new MarkdownSkillParser();

        var registered = parser.Parse("""
# Repo Explorer

Inspect repository structure and summarize key files.
""", @"C:\skills\Repo Explorer.md");

        Assert.Equal("repo-explorer", registered.Id);
        Assert.Equal("Repo Explorer", registered.Name);
        Assert.Equal("Inspect repository structure and summarize key files.", registered.Description);
        Assert.True(registered.IsActiveByDefault);
        Assert.Empty(registered.ToolNames);
        Assert.Equal(@"C:\skills\Repo Explorer.md", registered.SourcePath);
    }

    /// <summary>
    /// Tests that <see cref="FileSystemSkillRepository.Scan"/> uses the parser to extract metadata from markdown files.
    /// 测试 <see cref="FileSystemSkillRepository.Scan"/> 是否使用解析器从 Markdown 文件中提取元数据。
    /// </summary>
    [Fact]
    public void FileSystemSkillRepository_Scan_UsesParserMetadata()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"skill-repo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var skillPath = Path.Combine(tempDirectory, "planner.md");
            File.WriteAllText(skillPath, """
---
name: Planner Skill
description: Helps plan tasks.
---
# Planner Skill

Create clear execution plans.
""");

            var repository = new FileSystemSkillRepository(tempDirectory);

            var skills = repository.Scan().ToList();

            Assert.Single(skills);
            Assert.Equal("planner", skills[0].Id);
            Assert.Equal("Planner Skill", skills[0].Name);
            Assert.Equal("Helps plan tasks.", skills[0].Description);
            Assert.True(skills[0].IsActiveByDefault);
            Assert.Empty(skills[0].ToolNames);
            Assert.Equal(skillPath, skills[0].SourcePath);
            Assert.Contains("Planner Skill", skills[0].RawContent, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Tests that <see cref="FileSystemSkillRepository.Load"/> returns a <see cref="MarkdownSkill"/>
    /// when no custom loader is registered, loading content from the file system.
    /// 测试 <see cref="FileSystemSkillRepository.Load"/> 在没有注册自定义加载器时，
    /// 是否从文件系统加载内容并返回 <see cref="MarkdownSkill"/>。
    /// </summary>
    [Fact]
    public void FileSystemSkillRepository_Load_WithoutLoader_ReturnsMarkdownSkill()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"skill-load-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var skillPath = Path.Combine(tempDirectory, "repo-helper.md");
            File.WriteAllText(skillPath, """
---
name: Repo Helper
description: Reads repository-oriented instructions.
---
# Repo Helper

Inspect files and summarize project structure.
""");

            var repository = new FileSystemSkillRepository(tempDirectory);
            var registered = repository.Scan().Single();

            var skill = repository.Load(registered);

            var markdownSkill = Assert.IsType<MarkdownSkill>(skill);
            Assert.Equal("repo-helper", markdownSkill.Id);
            Assert.Equal("Repo Helper", markdownSkill.Name);
            Assert.Equal("Reads repository-oriented instructions.", markdownSkill.Description);
            Assert.Equal(skillPath, markdownSkill.SourcePath);
            Assert.Contains("Inspect files", markdownSkill.RawContent, StringComparison.Ordinal);
            Assert.Empty(markdownSkill.ToolNames);
            Assert.Empty(markdownSkill.Tools);
            Assert.True(markdownSkill.IsActive);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}