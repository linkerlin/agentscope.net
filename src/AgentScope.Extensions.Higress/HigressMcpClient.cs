using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Higress;

public sealed class HigressMcpClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public HigressMcpClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<JsonElement> CallToolAsync(string toolName, JsonElement args, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/mcp/tools/{toolName}", new { arguments = args }, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    public async Task<List<string>> ListToolsAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/mcp/tools", ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var tools = new List<string>();
        foreach (var t in json.GetProperty("tools").EnumerateArray())
            tools.Add(t.GetProperty("name").GetString() ?? "");
        return tools;
    }
}
