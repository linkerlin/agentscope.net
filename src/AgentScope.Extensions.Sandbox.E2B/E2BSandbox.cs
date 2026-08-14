using System.Net.Http.Json;
namespace AgentScope.Extensions.Sandbox.E2B;

public sealed class E2BSandbox : ISandbox
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private string? _sandboxId;

    public E2BSandbox(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.e2b.dev/v1";
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/sandboxes");
        req.Headers.Add("X-API-Key", _apiKey);
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
        _sandboxId = json.GetProperty("sandbox_id").GetString();
    }

    public async Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null, CancellationToken ct = default)
    {
        if (_sandboxId == null) return new ExecResult(-1, "", "Sandbox not started", false);
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/sandboxes/{_sandboxId}/exec");
        req.Headers.Add("X-API-Key", _apiKey);
        req.Content = JsonContent.Create(new { command, timeout = timeoutSeconds ?? 30 });
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ExecResult>(ct);
    }

    public async Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default)
    {
        // 对标 Java E2bSandbox.doPersistWorkspace：tar 打包工作区 + base64，解码还原 tar 流
        if (_sandboxId == null) return Stream.Null;
        var result = await ExecAsync("tar -cf - -C /workspace . | base64 -w0", 120, ct);
        if (result.ExitCode != 0) return Stream.Null;

        var b64 = (result.StdOut ?? "").Replace("\n", "").Replace("\r", "");
        try
        {
            var raw = Convert.FromBase64String(b64);
            return new MemoryStream(raw);
        }
        catch
        {
            return Stream.Null;
        }
    }

    public async Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default)
    {
        // 对标 Java E2bSandbox.doHydrateWorkspace：base64 分块写入容器内临时文件后 tar 解包
        if (_sandboxId == null) return;

        var buffer = new MemoryStream();
        await archive.CopyToAsync(buffer, ct);
        var raw = buffer.ToArray();
        if (raw.Length == 0) return;

        var b64 = Convert.ToBase64String(raw);
        await ExecAsync("rm -f /tmp/agentscope-ws.b64", 30, ct);

        const int chunkSize = 24000;
        for (var i = 0; i < b64.Length; i += chunkSize)
        {
            var chunk = b64.Substring(i, Math.Min(chunkSize, b64.Length - i));
            var lit = System.Text.Json.JsonSerializer.Serialize(chunk);
            var py = $"import pathlib; pathlib.Path('/tmp/agentscope-ws.b64').open('a').write({lit})";
            await ExecAsync($"python3 -c '{py.Replace("'", "'\\''")}'", 120, ct);
        }

        var pyFin = "import base64,pathlib,subprocess; raw=base64.standard_b64decode(pathlib.Path('/tmp/agentscope-ws.b64').read_text()); subprocess.run(['tar','xf','-','-C','/workspace'],input=raw,check=True)";
        await ExecAsync($"python3 -c '{pyFin.Replace("'", "'\\''")}'", 120, ct);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_sandboxId != null)
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/sandboxes/{_sandboxId}");
            req.Headers.Add("X-API-Key", _apiKey);
            try { await _http.SendAsync(req, ct); } catch { }
            _sandboxId = null;
        }
    }

    public Task ShutdownAsync(CancellationToken ct = default) => StopAsync(ct);
    public async ValueTask DisposeAsync() { await StopAsync(); }
}

