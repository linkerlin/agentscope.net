using SandboxBase = AgentScope.Harness.Sandbox.SandboxBase;
using SBExecResult = AgentScope.Harness.Sandbox.ExecResult;

namespace AgentScope.Harness.Filesystem.Sandbox;

/// <summary>
/// 沙箱代理文件系统。将对 IFilesystem 的操作转发到活跃的 Sandbox.exec()。
/// 对标 Java SandboxBackedFilesystem。
/// </summary>
public sealed class SandboxBackedFilesystem(
    SandboxBase sandbox,
    string id) : SandboxFilesystemBase
{
    public override string Id => id;

    public override async Task<SBExecResult> ExecuteAsync(string command, int? timeout = null,
        CancellationToken ct = default)
    {
        var result = await sandbox.ExecAsync(command, timeout, ct);
        return new SBExecResult(result.ExitCode, result.StdOut, result.StdErr, result.Truncated);
    }
}
