// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP 内容转换器：将多段内容统一转为当前工具链可消费的文本表示。
/// </summary>
public class McpContentConverter
{
    public virtual string ConvertPartsToText(IReadOnlyList<McpContentItem>? parts)
    {
        if (parts == null || parts.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "\n",
            parts.Select(ConvertPartToText)
                .Where(static text => !string.IsNullOrWhiteSpace(text)));
    }

    public virtual string ConvertResultToText(McpCallResult result)
    {
        if (result == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(result.Content))
        {
            return result.Content!;
        }

        return ConvertPartsToText(result.Parts);
    }

    public virtual string ConvertPartToText(McpContentItem part)
    {
        if (part == null)
        {
            return string.Empty;
        }

        if (string.Equals(part.Type, "text", StringComparison.OrdinalIgnoreCase))
        {
            return part.Text ?? string.Empty;
        }

        var type = string.IsNullOrWhiteSpace(part.Type) ? "content" : part.Type;
        var mimeType = string.IsNullOrWhiteSpace(part.MimeType) ? "unknown" : part.MimeType;
        return $"[{type}:{mimeType}]";
    }
}