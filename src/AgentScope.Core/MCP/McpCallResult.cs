// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP CallTool 返回结果（简化：文本内容或错误）。
/// </summary>
public class McpCallResult
{
    public bool IsError { get; set; }
    public string? Content { get; set; }
    public IReadOnlyList<McpContentItem>? Parts { get; set; }

    public static McpCallResult Ok(string? content = null, IReadOnlyList<McpContentItem>? parts = null) =>
        new() { IsError = false, Content = content, Parts = parts };

    public static McpCallResult Fail(string content) =>
        new() { IsError = true, Content = content };
}

/// <summary>
/// MCP 内容项（文本/图片等）。
/// </summary>
public class McpContentItem
{
    public string Type { get; set; } = "text";
    public string? Text { get; set; }
    public string? Data { get; set; }
    public string? MimeType { get; set; }
}
