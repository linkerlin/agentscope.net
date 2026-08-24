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
/// MCP CallTool result (simplified: text content or error).
/// Represents the response from calling a tool on an MCP server.
/// Corresponds to Java: io.agentscope.core.mcp.McpCallResult
/// MCP CallTool 返回结果（简化：文本内容或错误）。
/// 表示在 MCP 服务器上调用工具后的响应。
/// 对应 Java: io.agentscope.core.mcp.McpCallResult
/// </summary>
public class McpCallResult
{
    /// <summary>
    /// Gets or sets whether the tool call resulted in an error.
    /// 获取或设置工具调用是否出错。
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// Gets or sets the text content of the result.
    /// 获取或设置结果的文本内容。
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the list of content parts (text, image, etc.).
    /// 获取或设置内容项列表（文本、图片等）。
    /// </summary>
    public IReadOnlyList<McpContentItem>? Parts { get; set; }

    /// <summary>
    /// Creates a successful result with optional content and parts.
    /// 创建一个成功结果，包含可选的内容和内容项。
    /// </summary>
    /// <param name="content">Optional text content / 可选的文本内容</param>
    /// <param name="parts">Optional list of content parts / 可选的内容项列表</param>
    /// <returns>A successful McpCallResult / 一个成功的 McpCallResult</returns>
    public static McpCallResult Ok(string? content = null, IReadOnlyList<McpContentItem>? parts = null) =>
        new() { IsError = false, Content = content, Parts = parts };

    /// <summary>
    /// Creates a failed result with an error message.
    /// 创建一个失败结果，包含错误消息。
    /// </summary>
    /// <param name="content">The error message / 错误消息</param>
    /// <returns>A failed McpCallResult / 一个失败的 McpCallResult</returns>
    public static McpCallResult Fail(string content) =>
        new() { IsError = true, Content = content };
}

/// <summary>
/// MCP content item representing a piece of content (text, image, etc.).
/// Corresponds to Java: io.agentscope.core.mcp.McpContentItem
/// MCP 内容项：表示一段内容（文本、图片等）。
/// 对应 Java: io.agentscope.core.mcp.McpContentItem
/// </summary>
public class McpContentItem
{
    /// <summary>
    /// Gets or sets the content type (e.g., "text", "image", "resource").
    /// 获取或设置内容类型（例如 "text"、"image"、"resource"）。
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// Gets or sets the text content (for text type).
    /// 获取或设置文本内容（用于 text 类型）。
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the binary data (for image/resource types, base64-encoded).
    /// 获取或设置二进制数据（用于 image/resource 类型，base64 编码）。
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the content (e.g., "image/png", "text/plain").
    /// 获取或设置内容的 MIME 类型（例如 "image/png"、"text/plain"）。
    /// </summary>
    public string? MimeType { get; set; }
}
