// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Skill;

public class MarkdownSkillParser
{
    public RegisteredSkill ParseFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        var rawContent = File.ReadAllText(path);
        var parsed = Parse(rawContent, path);
        parsed.SourcePath = path;
        return parsed;
    }

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

    private static string ResolveId(Dictionary<string, string> metadata, string name, string? sourcePath)
    {
        if (metadata.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id))
            return id.Trim();

        if (!string.IsNullOrWhiteSpace(sourcePath))
            return NormalizeId(Path.GetFileNameWithoutExtension(sourcePath));

        return NormalizeId(name);
    }

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

    private static bool ResolveIsActiveByDefault(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("active", out var rawActive) || string.IsNullOrWhiteSpace(rawActive))
            return true;

        return !rawActive.Equals("false", StringComparison.OrdinalIgnoreCase)
            && !rawActive.Equals("0", StringComparison.OrdinalIgnoreCase)
            && !rawActive.Equals("no", StringComparison.OrdinalIgnoreCase);
    }

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