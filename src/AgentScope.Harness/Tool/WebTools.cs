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
