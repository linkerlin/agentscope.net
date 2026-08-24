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

namespace AgentScope.Core.Skill;

/// <summary>
/// File system-based skill repository that scans a directory for Markdown (.md) skill files.
/// 基于文件系统的技能仓库，扫描指定目录中的 Markdown (.md) 技能文件。
/// Corresponds to Java: io.agentscope.core.skill.FileSystemSkillRepository
/// </summary>
public class FileSystemSkillRepository : ISkillRepository
{
    /// <summary>
    /// Base directory path to scan for skill files.
    /// 扫描技能文件的基础目录路径。
    /// </summary>
    private readonly string _basePath;

    /// <summary>
    /// Optional custom loader function to convert a RegisteredSkill into an ISkill instance.
    /// 可选的自定义加载器函数，用于将 RegisteredSkill 转换为 ISkill 实例。
    /// </summary>
    private readonly Func<RegisteredSkill, ISkill>? _loader;

    /// <summary>
    /// Markdown parser used to parse skill files.
    /// 用于解析技能文件的 Markdown 解析器。
    /// </summary>
    private readonly MarkdownSkillParser _parser;

    /// <summary>
    /// Initializes a new instance of the FileSystemSkillRepository.
    /// 初始化 FileSystemSkillRepository 的新实例。
    /// </summary>
    /// <param name="basePath">Base directory path to scan for skill files / 扫描技能文件的基础目录路径</param>
    /// <param name="loader">Optional custom loader function / 可选的自定义加载器函数</param>
    /// <param name="parser">Optional Markdown parser; uses default if not provided / 可选的 Markdown 解析器，未提供时使用默认解析器</param>
    /// <exception cref="ArgumentNullException">Thrown when basePath is null / 当 basePath 为 null 时抛出</exception>
    public FileSystemSkillRepository(string basePath, Func<RegisteredSkill, ISkill>? loader = null, MarkdownSkillParser? parser = null)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _loader = loader;
        _parser = parser ?? new MarkdownSkillParser();
    }

    /// <summary>
    /// Scans the base directory for Markdown skill files and returns their registered metadata.
    /// 扫描基础目录中的 Markdown 技能文件，返回其注册元数据。
    /// </summary>
    /// <returns>
    /// Collection of RegisteredSkill entries parsed from .md files in the base directory.
    /// 从基础目录中的 .md 文件解析出的 RegisteredSkill 条目集合。
    /// </returns>
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

    /// <summary>
    /// Loads a skill from its registered metadata, using the custom loader or parsing the source file/content.
    /// 从注册元数据加载技能，使用自定义加载器或解析源文件/内容。
    /// </summary>
    /// <param name="registered">The registered skill metadata to load / 要加载的注册技能元数据</param>
    /// <returns>
    /// An ISkill instance representing the loaded skill.
    /// 表示已加载技能的 ISkill 实例。
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when registered is null / 当 registered 为 null 时抛出</exception>
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
