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

using System.Diagnostics;
using System.Text;
namespace AgentScope.Extensions.Sandbox.Kubernetes;

public sealed class KubernetesSandbox : ISandbox
{
    private readonly string _kubeConfigPath;
    private string? _podName;
    private readonly string _image;
    private readonly string _namespace;

    public KubernetesSandbox(string image = "ubuntu:22.04", string? kubeConfigPath = null, string? ns = null)
    {
        _image = image;
        _kubeConfigPath = kubeConfigPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kube", "config");
        _namespace = ns ?? "default";
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _podName = $"agentscope-{Guid.NewGuid():N}";
        var yaml = $@"
apiVersion: v1
kind: Pod
metadata:
  name: {_podName}
  namespace: {_namespace}
spec:
  containers:
  - name: sandbox
    image: {_image}
    command: [""sleep"", ""infinity""]
  restartPolicy: Never";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, yaml, ct);
        RunKubectl($"apply -f {tempFile}");
        RunKubectl($"wait --for=condition=Ready pod/{_podName} -n {_namespace} --timeout=60s");
        try { File.Delete(tempFile); } catch { }
    }

    public Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null, CancellationToken ct = default)
    {
        if (_podName == null) return Task.FromResult(new ExecResult(-1, "", "Pod not started", false));
        var result = RunKubectl($"exec {_podName} -n {_namespace} -- sh -c \"{command.Replace("\"", "\\\"")}\"");
        return Task.FromResult(new ExecResult(result.ExitCode, result.StdOut, result.StdErr, false));
    }

    public Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default)
    {
        if (_podName == null) return Task.FromResult(Stream.Null);
        // 对标 Java KubernetesSandbox.doPersistWorkspace：tar + base64 打包，再解码还原二进制 tar 流
        var result = RunKubectl($"exec {_podName} -n {_namespace} -- sh -c \"tar -cf - -C /workspace . | base64 -w0\"");
        if (result.ExitCode != 0) return Task.FromResult(Stream.Null);
        var b64 = (result.StdOut ?? "").Replace("\n", "").Replace("\r", "");
        try
        {
            var raw = Convert.FromBase64String(b64);
            return Task.FromResult<Stream>(new MemoryStream(raw));
        }
        catch
        {
            return Task.FromResult(Stream.Null);
        }
    }

    public Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default)
    {
        // 对标 Java KubernetesSandbox.doHydrateWorkspace（exec 路径）：base64 写入后 tar 解包
        if (_podName == null) return Task.CompletedTask;

        var buffer = new MemoryStream();
        archive.CopyTo(buffer);
        var raw = buffer.ToArray();
        if (raw.Length == 0) return Task.CompletedTask;

        var b64 = Convert.ToBase64String(raw);
        RunKubectl($"exec {_podName} -n {_namespace} -- sh -c \"rm -f /tmp/agentscope-ws.b64\"");

        const int chunkSize = 24000;
        for (var i = 0; i < b64.Length; i += chunkSize)
        {
            var chunk = b64.Substring(i, Math.Min(chunkSize, b64.Length - i));
            var lit = System.Text.Json.JsonSerializer.Serialize(chunk);
            var py = $"import pathlib; pathlib.Path('/tmp/agentscope-ws.b64').open('a').write({lit})";
            RunKubectl($"exec {_podName} -n {_namespace} -- sh -c \"python3 -c '{py.Replace("'", "'\\''")}'\"");
        }

        var pyFin = "import base64,pathlib,subprocess; raw=base64.standard_b64decode(pathlib.Path('/tmp/agentscope-ws.b64').read_text()); subprocess.run(['tar','xf','-','-C','/workspace'],input=raw,check=True)";
        RunKubectl($"exec {_podName} -n {_namespace} -- sh -c \"python3 -c '{pyFin.Replace("'", "'\\''")}'\"");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        if (_podName != null)
        {
            RunKubectl($"delete pod {_podName} -n {_namespace} --ignore-not-found=true");
            _podName = null;
        }
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => StopAsync(ct);
    public async ValueTask DisposeAsync() { await StopAsync(); }

    private (int ExitCode, string StdOut, string StdErr) RunKubectl(string args)
    {
        var psi = new ProcessStartInfo("kubectl", args)
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
            Environment = { ["KUBECONFIG"] = _kubeConfigPath }
        };
        using var proc = Process.Start(psi);
        if (proc == null) return (-1, "", "Cannot start kubectl");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }
}

