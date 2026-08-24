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

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP content converter: unifies multi-part content into a text representation consumable by the tool chain.
/// Corresponds to Java: io.agentscope.core.mcp.McpContentConverter
/// MCP 内容转换器：将多段内容统一转为当前工具链可消费的文本表示。
/// 对应 Java: io.agentscope.core.mcp.McpContentConverter
/// </summary>
public class McpContentConverter
{
    /// <summary>
    /// Converts a list of content parts into a single text string, joined by newlines.
    /// 将内容项列表转换为单个文本字符串，以换行符连接。
    /// </summary>
    /// <param name="parts">The list of content parts to convert / 要转换的内容项列表</param>
    /// <returns>The combined text representation / 合并后的文本表示</returns>
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

    /// <summary>
    /// Converts an McpCallResult into a text string.
    /// Uses the result's Content property if available, otherwise falls back to converting Parts.
    /// 将 McpCallResult 转换为文本字符串。
    /// 优先使用结果的 Content 属性，否则回退到转换 Parts。
    /// </summary>
    /// <param name="result">The MCP call result to convert / 要转换的 MCP 调用结果</param>
    /// <returns>The text representation / 文本表示</returns>
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

    /// <summary>
    /// Converts a single McpContentItem into its text representation.
    /// For text-type items, returns the text content directly.
    /// For other types (image, resource, etc.), returns a descriptive placeholder like "[type:mimeType]".
    /// 将单个 McpContentItem 转换为其文本表示。
    /// 对于 text 类型项，直接返回文本内容。
    /// 对于其他类型（image、resource 等），返回描述性占位符如 "[type:mimeType]"。
    /// </summary>
    /// <param name="part">The content item to convert / 要转换的内容项</param>
    /// <returns>The text representation / 文本表示</returns>
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