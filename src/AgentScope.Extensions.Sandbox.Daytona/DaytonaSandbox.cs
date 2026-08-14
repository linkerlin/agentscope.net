using System.Net.Http.Json;
namespace AgentScope.Extensions.Sandbox.Daytona;

public sealed class DaytonaSandbox : ISandbox
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private string? _sessionId;

    public DaytonaSandbox(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"{_baseUrl}/sessions", null, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
        _sessionId = json.GetProperty("session_id").GetString();
    }

    public async Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null, CancellationToken ct = default)
    {
        if (_sessionId == null) return new ExecResult(-1, "", "Session not started", false);
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/sessions/{_sessionId}/exec", new { command, timeout = timeoutSeconds ?? 30 }, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ExecResult>(ct);
    }

    public async Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default)
    {
        // 对标 Java DaytonaSandbox.doPersistWorkspace：tar + base64 打包工作区
        if (_sessionId == null) return Stream.Null;
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
        // 对标 Java DaytonaSandbox.doHydrateWorkspace：base64 分块写入后 tar 解包
        if (_sessionId == null) return;

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
        if (_sessionId != null) { try { await _http.DeleteAsync($"{_baseUrl}/sessions/{_sessionId}", ct); } catch { } _sessionId = null; }
    }

    public Task ShutdownAsync(CancellationToken ct = default) => StopAsync(ct);
    public async ValueTask DisposeAsync() { await StopAsync(); }
}

