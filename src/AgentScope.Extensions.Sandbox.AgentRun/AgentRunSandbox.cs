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

using System.Net.Http.Json;
namespace AgentScope.Extensions.Sandbox.AgentRun;

/// <summary>
/// Alibaba Cloud AgentRun sandbox implementation.
/// Communicates with the AgentRun data plane via REST API to manage sandbox lifecycle,
/// execute commands, and persist/hydrate workspace snapshots.
/// Counterpart of Java AgentRunSandbox.
/// <br/>
/// 阿里云 AgentRun 沙箱实现。
/// 通过 REST API 与 AgentRun 数据面通信，管理沙箱生命周期、执行命令、持久化/恢复工作区快照。
/// 对标 Java AgentRunSandbox。
/// </summary>
public sealed class AgentRunSandbox : ISandbox
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private string? _sandboxId;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRunSandbox"/> class.
    /// 初始化 AgentRunSandbox 实例。
    /// </summary>
    /// <param name="http">HttpClient for API communication / 用于 API 通信的 HttpClient</param>
    /// <param name="baseUrl">Data plane base URL / 数据面基础地址</param>
    public AgentRunSandbox(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Starts the sandbox by creating a new sandbox session on the data plane.
    /// 通过向数据面发起请求创建新沙箱会话来启动沙箱。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async Task StartAsync(CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"{_baseUrl}/sandboxes", null, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
        _sandboxId = json.GetProperty("id").GetString();
    }

    /// <summary>
    /// Executes a command inside the sandbox and returns the result.
    /// 在沙箱中执行命令并返回结果。
    /// </summary>
    /// <param name="command">Command to execute / 要执行的命令</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds / 可选超时秒数</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>Execution result including exit code, stdout, stderr / 执行结果，包含退出码、标准输出、标准错误</returns>
    public async Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null, CancellationToken ct = default)
    {
        if (_sandboxId == null) return new ExecResult(-1, "", "Sandbox not started", false);
        var url = $"{_baseUrl}/sandboxes/{_sandboxId}/exec";
        var resp = await _http.PostAsJsonAsync(url, new { command, timeout = timeoutSeconds ?? 30 }, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ExecResult>(ct);
        return result;
    }

    /// <summary>
    /// Persists the workspace into a tar archive stream.
    /// Counterpart of Java AgentRunSandbox.doPersistWorkspace: tars the workspace and base64-encodes it.
    /// <br/>
    /// 将工作区持久化为 tar 归档流。
    /// 对标 Java AgentRunSandbox.doPersistWorkspace：tar 打包 + base64 编码工作区。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>Stream containing the tar archive / 包含 tar 归档的流</returns>
    public async Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default)
    {
        // 对标 Java AgentRunSandbox.doPersistWorkspace：tar + base64 打包工作区
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

    /// <summary>
    /// Hydrates the workspace from a tar archive stream.
    /// Counterpart of Java AgentRunSandbox.doHydrateWorkspace: writes base64 chunks then extracts tar.
    /// <br/>
    /// 从 tar 归档流恢复工作区。
    /// 对标 Java AgentRunSandbox.doHydrateWorkspace：base64 分块写入后 tar 解包。
    /// </summary>
    /// <param name="archive">Stream containing the tar archive / 包含 tar 归档的流</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default)
    {
        // 对标 Java AgentRunSandbox.doHydrateWorkspace：base64 分块写入后 tar 解包
        if (_sandboxId == null) return;

        var buffer = new MemoryStream();
        await archive.CopyToAsync(buffer, ct);
        var raw = buffer.ToArray();
        if (raw.Length == 0) return;

        var b64 = Convert.ToBase64String(raw);
        await ExecAsync("rm -f /tmp/agentscope-ws.b64", 30, ct);

        const int chunkSize = 24000;
        // 分块写入 base64 数据到容器的临时文件
        // Write base64 data to the container's temp file in chunks
        for (var i = 0; i < b64.Length; i += chunkSize)
        {
            var chunk = b64.Substring(i, Math.Min(chunkSize, b64.Length - i));
            var lit = System.Text.Json.JsonSerializer.Serialize(chunk);
            var py = $"import pathlib; pathlib.Path('/tmp/agentscope-ws.b64').open('a').write({lit})";
            await ExecAsync($"python3 -c '{py.Replace("'", "'\\''")}'", 120, ct);
        }

        // 最终调用 python 解码并解包 tar 到工作区
        // Final step: decode base64 and extract tar to workspace via python
        var pyFin = "import base64,pathlib,subprocess; raw=base64.standard_b64decode(pathlib.Path('/tmp/agentscope-ws.b64').read_text()); subprocess.run(['tar','xf','-','-C','/workspace'],input=raw,check=True)";
        await ExecAsync($"python3 -c '{pyFin.Replace("'", "'\\''")}'", 120, ct);
    }

    /// <summary>
    /// Stops the sandbox and releases the remote sandbox resource.
    /// 停止沙箱并释放远程沙箱资源。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_sandboxId != null)
        {
            try { await _http.DeleteAsync($"{_baseUrl}/sandboxes/{_sandboxId}", ct); } catch { }
            _sandboxId = null;
        }
    }

    /// <summary>
    /// Shuts down the sandbox (alias for StopAsync).
    /// 关闭沙箱（StopAsync 的别名）。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public Task ShutdownAsync(CancellationToken ct = default) => StopAsync(ct);

    /// <summary>
    /// Disposes the sandbox resources asynchronously.
    /// 异步释放沙箱资源。
    /// </summary>
    public async ValueTask DisposeAsync() { await StopAsync(); }
}
