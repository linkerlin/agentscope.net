using AgentScope.Harness.Subagent.Protocol;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>Agent Protocol HTTP 客户端，对应 Java AgentProtocolTaskClient</summary>
public sealed class AgentProtocolTaskClient
{
    private readonly HttpClient _http;

    public AgentProtocolTaskClient(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
    }

    public async Task SubmitTaskAsync(string baseUrl,
        Dictionary<string, string>? headers, string taskId,
        string agentId, string input,
        RemoteSubmitContext? context = null,
        CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/tasks")
        {
            Content = JsonContent.Create(new
            {
                task_id = taskId,
                agent_id = agentId,
                input,
                context
            })
        };
        ApplyHeaders(req, headers);
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task<RemoteTaskStatus> GetStatusAsync(string baseUrl,
        Dictionary<string, string>? headers, string taskId,
        CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/tasks/{taskId}");
        ApplyHeaders(req, headers);
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<RemoteTaskStatus>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty status response");
    }

    public async Task<string?> WaitForResultAsync(string baseUrl,
        Dictionary<string, string>? headers, string taskId,
        long timeoutSeconds, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/tasks/{taskId}/wait");
        ApplyHeaders(req, headers);
        using var res = await _http.SendAsync(req, cts.Token);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }

    public async Task CancelTaskAsync(string baseUrl,
        Dictionary<string, string>? headers, string taskId,
        CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/tasks/{taskId}/cancel");
        ApplyHeaders(req, headers);
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task ResumeTaskAsync(string baseUrl,
        Dictionary<string, string>? headers, string taskId,
        List<RemoteConfirmDecision> decisions,
        CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/tasks/{taskId}/resume")
        {
            Content = JsonContent.Create(new { decisions })
        };
        ApplyHeaders(req, headers);
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    private static void ApplyHeaders(HttpRequestMessage req,
        Dictionary<string, string>? headers)
    {
        if (headers == null) return;
        foreach (var (k, v) in headers)
            req.Headers.TryAddWithoutValidation(k, v);
    }
}


