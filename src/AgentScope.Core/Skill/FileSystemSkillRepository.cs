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

    public FileSystemSkillRepository(string basePath, Func<RegisteredSkill, ISkill>? loader = null)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _loader = loader;
    }

    public IEnumerable<RegisteredSkill> Scan()
    {
        if (!System.IO.Directory.Exists(_basePath))
            return Array.Empty<RegisteredSkill>();
        var list = new List<RegisteredSkill>();
        foreach (var path in System.IO.Directory.EnumerateFiles(_basePath, "*.md", System.IO.SearchOption.TopDirectoryOnly))
        {
            var id = System.IO.Path.GetFileNameWithoutExtension(path);
            list.Add(new RegisteredSkill { Id = id, Name = id, SourcePath = path });
        }
        return list;
    }

    public ISkill Load(RegisteredSkill registered)
    {
        if (_loader != null)
            return _loader(registered);
        throw new NotSupportedException("FileSystemSkillRepository 未配置 Loader，请提供 Func<RegisteredSkill, ISkill>。");
    }
}
