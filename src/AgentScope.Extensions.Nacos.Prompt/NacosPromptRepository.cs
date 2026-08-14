using System.Text.Json;

namespace AgentScope.Extensions.Nacos.Prompt;

public sealed class NacosPromptRepository
{
    private readonly string _serverAddr;
    private readonly string _namespaceId;
    private readonly string _group;
    private readonly HttpClient _http;

    public NacosPromptRepository(string serverAddr, string? namespaceId = null, string? group = null, HttpClient? http = null)
    {
        _serverAddr = serverAddr.TrimEnd('/');
        _namespaceId = namespaceId ?? "public";
        _group = group ?? "DEFAULT_GROUP";
        _http = http ?? new HttpClient();
    }

    public async Task<string?> GetPromptAsync(string promptId, CancellationToken ct = default)
    {
        var url = $"{_serverAddr}/nacos/v1/cs/configs?dataId={promptId}&group={_group}&tenant={_namespaceId}";
        var resp = await _http.GetAsync(url, ct);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync(ct) : null;
    }

    public async Task PublishPromptAsync(string promptId, string content, CancellationToken ct = default)
    {
        var url = $"{_serverAddr}/nacos/v1/cs/configs";
        var form = new Dictionary<string, string>
        {
            ["dataId"] = promptId,
            ["group"] = _group,
            ["tenant"] = _namespaceId,
            ["content"] = content
        };
        var resp = await _http.PostAsync(url, new FormUrlEncodedContent(form), ct);
        resp.EnsureSuccessStatusCode();
    }
}
