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

using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tool;

/// <summary>
/// Web 工具。对标 Java WebTools。
/// 提供网页抓取功能。
/// </summary>
public sealed class WebTools : ITool
{
    private readonly HttpClient _http;

    public WebTools(HttpClient? http = null) => _http = http ?? new HttpClient();

    public string Name => "web_fetch";
    public string Description => "获取网页内容";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var url = parameters.GetValueOrDefault("url")?.ToString();
        if (string.IsNullOrWhiteSpace(url))
            return ToolResult.Fail("需要 url 参数");

        try
        {
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return ToolResult.Ok(content.Length > 10000 ? content[..10000] + "\n... (已截断)" : content);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"获取网页失败: {ex.Message}");
        }
    }

    public Dictionary<string, object> GetSchema() => new()
    {
        ["name"] = Name,
        ["description"] = Description,
        ["parameters"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["url"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "要抓取的网页 URL" }
            },
            ["required"] = new[] { "url" }
        }
    };
}
