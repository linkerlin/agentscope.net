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
