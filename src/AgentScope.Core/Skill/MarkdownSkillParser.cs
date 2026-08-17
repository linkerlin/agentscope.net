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
/// Parser for Markdown-based skill definitions, extracting front matter metadata and body content.
/// Markdown 技能定义解析器，提取前置元数据和正文内容。
/// Corresponds to Java: io.agentscope.core.skill.MarkdownSkillParser
/// </summary>
public class MarkdownSkillParser
{
    /// <summary>
    /// Parses a Markdown file into a RegisteredSkill.
    /// 将 Markdown 文件解析为 RegisteredSkill。
    /// </summary>
    /// <param name="path">The file path to parse. / 要解析的文件路径。</param>
    /// <returns>The parsed skill registration metadata. / 解析后的技能注册元数据。</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null or empty. / 当 path 为 null 或空时抛出。</exception>
    public RegisteredSkill ParseFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        var rawContent = File.ReadAllText(path);
        var parsed = Parse(rawContent, path);
        parsed.SourcePath = path;
        return parsed;
    }

    /// <summary>
    /// Parses raw Markdown content into a RegisteredSkill.
    /// 将原始 Markdown 内容解析为 RegisteredSkill。
    /// </summary>
    /// <param name="rawContent">The raw Markdown content. / 原始 Markdown 内容。</param>
    /// <param name="sourcePath">Optional source file path. / 可选的源文件路径。</param>
    /// <returns>The parsed skill registration metadata. / 解析后的技能注册元数据。</returns>
    /// <exception cref="ArgumentException">Thrown when rawContent is empty. / 当 rawContent 为空时抛出。</exception>
    public RegisteredSkill Parse(string rawContent, string? sourcePath = null)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            throw new ArgumentException("Markdown skill content cannot be empty.", nameof(rawContent));

        var normalizedContent = rawContent.Replace("\r\n", "\n");
        var frontMatter = ParseFrontMatter(normalizedContent, out var body);

        var resolvedName = ResolveName(frontMatter, body, sourcePath);
        var resolvedId = ResolveId(frontMatter, resolvedName, sourcePath);
        var resolvedDescription = ResolveDescription(frontMatter, body);
        var resolvedToolNames = ResolveToolNames(frontMatter);
        var resolvedDefaultActive = ResolveIsActiveByDefault(frontMatter);

        return new RegisteredSkill
        {
            Id = resolvedId,
            Name = resolvedName,
            Description = resolvedDescription,
            ToolNames = resolvedToolNames,
            IsActiveByDefault = resolvedDefaultActive,
            SourcePath = sourcePath,
            RawContent = rawContent
        };
    }

    /// <summary>
    /// Parses YAML-style front matter (--- delimited) from the raw content.
    /// 从原始内容中解析 YAML 风格的前置元数据（--- 分隔）。
    /// </summary>
    private static Dictionary<string, string> ParseFrontMatter(string rawContent, out string body)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        body = rawContent;

        if (!rawContent.StartsWith("---\n", StringComparison.Ordinal))
            return metadata;

        var closingIndex = rawContent.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (closingIndex < 0)
            return metadata;

        var frontMatterContent = rawContent.Substring(4, closingIndex - 4);
        body = rawContent.Substring(closingIndex + 5).TrimStart('\n');

        foreach (var line in frontMatterContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Length == 0 || trimmedLine.StartsWith('#'))
                continue;

            var separatorIndex = trimmedLine.IndexOf(':');
            if (separatorIndex <= 0)
                continue;

            var key = trimmedLine[..separatorIndex].Trim();
            var value = trimmedLine[(separatorIndex + 1)..].Trim().Trim('"', '\'');
            if (key.Length > 0)
                metadata[key] = value;
        }

        return metadata;
    }

    /// <summary>
    /// Resolves the skill name from front matter, first heading, or file name.
    /// 从前置元数据、第一个标题或文件名中解析技能名称。
    /// </summary>
    private static string ResolveName(Dictionary<string, string> metadata, string body, string? sourcePath)
    {
        if (metadata.TryGetValue("name", out var name) && !string.IsNullOrWhiteSpace(name))
            return name;

        foreach (var line in body.Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("# ", StringComparison.Ordinal))
                return trimmedLine[2..].Trim();
        }

        if (!string.IsNullOrWhiteSpace(sourcePath))
            return Path.GetFileNameWithoutExtension(sourcePath);

        return "unnamed-skill";
    }

    /// <summary>
    /// Resolves the skill ID from front matter, name, or file name.
    /// 从前置元数据、名称或文件名中解析技能 ID。
    /// </summary>
    private static string ResolveId(Dictionary<string, string> metadata, string name, string? sourcePath)
    {
        if (metadata.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id))
            return id.Trim();

        if (!string.IsNullOrWhiteSpace(sourcePath))
            return NormalizeId(Path.GetFileNameWithoutExtension(sourcePath));

        return NormalizeId(name);
    }

    /// <summary>
    /// Resolves the skill description from front matter or body text.
    /// 从前置元数据或正文中解析技能描述。
    /// </summary>
    private static string ResolveDescription(Dictionary<string, string> metadata, string body)
    {
        if (metadata.TryGetValue("description", out var description) && !string.IsNullOrWhiteSpace(description))
            return description;

        var lines = body.Split('\n');
        var collected = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                if (collected.Count > 0)
                    break;

                continue;
            }

            if (line.StartsWith("# ", StringComparison.Ordinal))
                continue;

            collected.Add(line);
        }

        return string.Join(" ", collected);
    }

    /// <summary>
    /// Resolves the list of tool names from front matter.
    /// 从前置元数据中解析工具名称列表。
    /// </summary>
    private static List<string> ResolveToolNames(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("tools", out var rawTools) || string.IsNullOrWhiteSpace(rawTools))
            return new List<string>();

        var normalized = rawTools.Trim();
        if (normalized.StartsWith("[", StringComparison.Ordinal) && normalized.EndsWith("]", StringComparison.Ordinal))
            normalized = normalized[1..^1];

        return normalized
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(toolName => toolName.Trim().Trim('"', '\''))
            .Where(toolName => toolName.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Resolves whether the skill is active by default from front matter.
    /// 从前置元数据中解析技能是否默认激活。
    /// </summary>
    private static bool ResolveIsActiveByDefault(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("active", out var rawActive) || string.IsNullOrWhiteSpace(rawActive))
            return true;

        return !rawActive.Equals("false", StringComparison.OrdinalIgnoreCase)
            && !rawActive.Equals("0", StringComparison.OrdinalIgnoreCase)
            && !rawActive.Equals("no", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a string into a valid skill ID (lowercase, hyphens for separators).
    /// 将字符串规范化为有效的技能 ID（小写，连字符分隔）。
    /// </summary>
    private static string NormalizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "skill";

        var chars = new List<char>(value.Length);
        var lastWasDash = false;

        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                chars.Add(ch);
                lastWasDash = false;
                continue;
            }

            if (lastWasDash)
                continue;

            chars.Add('-');
            lastWasDash = true;
        }

        var normalized = new string(chars.ToArray()).Trim('-');
        return normalized.Length == 0 ? "skill" : normalized;
    }
}
