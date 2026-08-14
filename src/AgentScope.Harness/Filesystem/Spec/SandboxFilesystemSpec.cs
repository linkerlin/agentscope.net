using System.Formats.Tar;
using AgentScope.Harness.Filesystem.Sandbox;
using AgentScope.Harness.Sandbox;
using SbExec = AgentScope.Harness.Sandbox;

namespace AgentScope.Harness.Filesystem.Spec;

/// <summary>
/// ?????????????? Java SandboxFilesystemSpec?
/// </summary>
public abstract class SandboxFilesystemSpec
{
    protected abstract string SandboxId { get; }
    protected abstract WorkspaceSpec CreateWorkspaceSpec();

    public async Task<(SandboxBackedFilesystem FS, SandboxContext Ctx)> BuildAsync(
        string hostWorkspaceRoot, CancellationToken ct = default)
    {
        var workspaceSpec = CreateWorkspaceSpec();
        var ctx = new SandboxContext(IsolationScope.Session, workspaceSpec);

        var sandbox = new LocalHarnessSandbox(hostWorkspaceRoot);
        await sandbox.StartAsync(workspaceSpec, ct: ct);

        var fs = new SandboxBackedFilesystem(sandbox, SandboxId);
        return (fs, ctx);
    }
}

/// <summary>
/// ??????????
/// </summary>
internal sealed class LocalHarnessSandbox(string workspaceRoot) : SandboxBase
{
    protected override string WorkspaceRoot => workspaceRoot;

    public override async Task<SbExec.ExecResult> ExecAsync(string command, int? timeoutSeconds = null,
        CancellationToken ct = default)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("sh", $"-c '{command}'")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workspaceRoot
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return new SbExec.ExecResult(-1, "", "??????", false);

            var timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : TimeSpan.FromSeconds(30);
            var completed = proc.WaitForExit((int)timeout.TotalMilliseconds);
            if (!completed) { proc.Kill(); return new SbExec.ExecResult(-1, "", "??", false); }

            var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
            var stderr = await proc.StandardError.ReadToEndAsync(ct);
            return new SbExec.ExecResult(proc.ExitCode, stdout, stderr, false);
        }
        catch (Exception ex)
        {
            return new SbExec.ExecResult(-1, "", ex.Message, false);
        }
    }

    public override async Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default)
    {
        // 对标 Java doPersistWorkspace：将工作区打包为 tar 归档
        var ms = new MemoryStream();
        await Task.Run(() =>
        {
            TarFile.CreateFromDirectory(workspaceRoot, ms, includeBaseDirectory: false);
        }, ct).ConfigureAwait(false);
        ms.Position = 0;
        return ms;
    }

    public override Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default)
    {
        // 对标 Java doHydrateWorkspace：解包 tar 归档到工作区
        Directory.CreateDirectory(workspaceRoot);
        TarFile.ExtractToDirectory(archive, workspaceRoot, overwriteFiles: true);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
        // 本地目录沙箱：停止时保留工作区（对标 Java doSetupWorkspace 后保留语义）
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
