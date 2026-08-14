using System.Diagnostics;
using AgentScope.Extensions.Sandbox;

namespace AgentScope.Extensions.Sandbox.Docker;

/// <summary>
/// Docker 沙箱。对标 Java DockerSandbox。
/// 通过 Docker CLI（docker exec/run/stop/rm）管理容器。
/// 实现 IVectorStore 接口（ISandbox 在伞工程 AgentScope.Extensions 中）。
/// </summary>
public sealed class DockerSandbox(string image = "ubuntu:22.04", string? containerName = null) : ISandbox
{
    private string? _containerId;
    private string _containerName = containerName ?? $"agentscope-{Guid.NewGuid():N}";
    private string _workspaceRoot = "/workspace";

    public Task StartAsync(CancellationToken ct = default) => InitializeContainerAsync(ct);

    public async Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null,
        CancellationToken ct = default)
    {
        if (_containerId == null)
            return new ExecResult(-1, "", "容器未运行", false);

        var psi = new ProcessStartInfo("docker", $"exec -w {_workspaceRoot} {_containerId} sh -c '{command.Replace("'", "'\\''")}'")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var proc = Process.Start(psi);
        if (proc == null) return new ExecResult(-1, "", "无法启动进程", false);

        var completed = proc.WaitForExit((timeoutSeconds ?? 30) * 1000);
        if (!completed) { proc.Kill(); return new ExecResult(-1, "", "超时", false); }

        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
        var stderr = await proc.StandardError.ReadToEndAsync(ct);
        return new ExecResult(proc.ExitCode, stdout, stderr, false);
    }

    public async Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo("docker", $"exec {_containerId} tar -cf - -C {_workspaceRoot} .")
        {
            RedirectStandardOutput = true
        };
        using var proc = Process.Start(psi);
        if (proc == null) return Stream.Null;
        var ms = new MemoryStream();
        await proc.StandardOutput.BaseStream.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    public async Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default)
    {
        // 对标 Java DockerSandbox.doHydrateWorkspace：docker exec -i <id> tar -xf - -C <root>
        if (_containerId == null)
            throw new InvalidOperationException("容器未运行");

        // 确保工作区目录存在
        await RunDockerBlockingAsync(30, "exec", _containerId, "mkdir", "-p", _workspaceRoot);

        var psi = new ProcessStartInfo("docker", $"exec -i {_containerId} tar -xf - -C {_workspaceRoot}")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var proc = Process.Start(psi);
        if (proc == null) throw new InvalidOperationException("无法启动 docker tar 进程");

        // 后台把 archive 写入 stdin，同时读取 stderr
        var writeTask = archive.CopyToAsync(proc.StandardInput.BaseStream, ct);
        var stderrTask = proc.StandardError.ReadToEndAsync(ct);

        await writeTask.ConfigureAwait(false);
        proc.StandardInput.Close();

        var completed = proc.WaitForExit(120 * 1000);
        if (!completed)
        {
            proc.Kill();
            throw new TimeoutException("docker tar 解包超时");
        }

        var stderr = await stderrTask.ConfigureAwait(false);
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"docker tar 解包失败 (exit={proc.ExitCode}): {stderr}");
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        if (_containerId == null) return Task.CompletedTask;
        Process.Start("docker", $"stop {_containerId}")?.WaitForExit();
        Process.Start("docker", $"rm {_containerId}")?.WaitForExit();
        _containerId = null;
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => StopAsync(ct);

    private async Task InitializeContainerAsync(CancellationToken ct)
    {
        var psi = new ProcessStartInfo("docker", $"run -d --name {_containerName} {image} sleep infinity")
        {
            RedirectStandardOutput = true
        };
        using var proc = Process.Start(psi);
        if (proc == null) throw new InvalidOperationException("无法启动 Docker 容器");
        _containerId = (await proc.StandardOutput.ReadToEndAsync(ct)).Trim();
    }

    public async ValueTask DisposeAsync() { await StopAsync(); }

    private async Task RunDockerBlockingAsync(int timeoutSeconds, params string[] args)
    {
        var psi = new ProcessStartInfo("docker", string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var proc = Process.Start(psi);
        if (proc == null) throw new InvalidOperationException("无法启动 docker 进程");

        var completed = proc.WaitForExit(timeoutSeconds * 1000);
        if (!completed)
        {
            proc.Kill();
            throw new TimeoutException($"docker 命令超时: {args[0]}");
        }
        if (proc.ExitCode != 0)
        {
            var stderr = await proc.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"docker 命令失败 (exit={proc.ExitCode}): {stderr}");
        }
    }
}
