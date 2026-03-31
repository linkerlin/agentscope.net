// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Skill;

/// <summary>
/// 从文件系统扫描 Skill 的仓库实现（最小实现：可扩展为扫描目录下 .md 等）。
/// </summary>
public class FileSystemSkillRepository : ISkillRepository
{
    private readonly string _basePath;
    private readonly Func<RegisteredSkill, ISkill>? _loader;
    private readonly MarkdownSkillParser _parser;

    public FileSystemSkillRepository(string basePath, Func<RegisteredSkill, ISkill>? loader = null, MarkdownSkillParser? parser = null)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _loader = loader;
        _parser = parser ?? new MarkdownSkillParser();
    }

    public IEnumerable<RegisteredSkill> Scan()
    {
        if (!System.IO.Directory.Exists(_basePath))
            return Array.Empty<RegisteredSkill>();
        var list = new List<RegisteredSkill>();
        foreach (var path in System.IO.Directory.EnumerateFiles(_basePath, "*.md", System.IO.SearchOption.TopDirectoryOnly))
        {
            list.Add(_parser.ParseFile(path));
        }
        return list;
    }

    public ISkill Load(RegisteredSkill registered)
    {
        if (registered == null)
            throw new ArgumentNullException(nameof(registered));

        if (_loader != null)
            return _loader(registered);

        RegisteredSkill resolvedSkill;

        if (!string.IsNullOrWhiteSpace(registered.SourcePath) && File.Exists(registered.SourcePath))
        {
            resolvedSkill = _parser.ParseFile(registered.SourcePath);
        }
        else if (!string.IsNullOrWhiteSpace(registered.RawContent))
        {
            resolvedSkill = _parser.Parse(registered.RawContent, registered.SourcePath);
        }
        else
        {
            resolvedSkill = registered;
        }

        return new MarkdownSkill(resolvedSkill, isActive: resolvedSkill.IsActiveByDefault);
    }
}
