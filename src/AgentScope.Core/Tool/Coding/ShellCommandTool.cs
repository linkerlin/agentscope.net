// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AgentScope.Core.Tool.Coding;

/// <summary>
/// 执行 Shell 命令工具；依赖 ICommandValidator 做安全检查，带超时与输出截断。
/// </summary>
public class ShellCommandTool : ToolBase
{
    private readonly ICommandValidator _validator;
    private readonly TimeSpan _defaultTimeout;
    private readonly int _maxOutputChars;

    public ShellCommandTool(
        ICommandValidator? validator = null,
        TimeSpan? defaultTimeout = null,
        int maxOutputChars = 32_000)
        : base("shell_command", "执行一条 Shell 命令。参数: command(必填)。命令须通过安全检查与超时限制。")
    {
        _validator = validator ?? CreatePlatformValidator();
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
        _maxOutputChars = Math.Max(1024, Math.Min(maxOutputChars, 100_000));
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("command", out var cmdObj) || cmdObj is not string command)
            return ToolResult.Fail("缺少必需参数: command");

        if (!_validator.Validate(command))
            return ToolResult.Fail("命令未通过安全检查。");

        var timeout = _defaultTimeout;
        if (parameters.TryGetValue("timeout_seconds", out var to) && to is int sec && sec > 0 && sec <= 300)
            timeout = TimeSpan.FromSeconds(sec);

        try
        {
            var (stdout, stderr, exitCode) = await RunWithTimeoutAsync(command, timeout).ConfigureAwait(false);
            var outTrunc = Truncate(stdout);
            var errTrunc = Truncate(stderr);
            var msg = exitCode == 0
                ? outTrunc + (string.IsNullOrEmpty(errTrunc) ? "" : "\n[stderr]\n" + errTrunc)
                : $"[exit code {exitCode}]\n{outTrunc}\n[stderr]\n{errTrunc}";
            return ToolResult.Ok(msg);
        }
        catch (TimeoutException)
        {
            return ToolResult.Fail("命令执行超时。");
        }
        catch (System.Exception ex)
        {
            return ToolResult.Fail("执行失败: " + ex.Message);
        }
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["command"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "要执行的命令", ["required"] = true },
                ["timeout_seconds"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "超时秒数（可选）", ["required"] = false }
            }
        };
    }

    private static ICommandValidator CreatePlatformValidator()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsCommandValidator();
        return new UnixCommandValidator();
    }

    private string Truncate(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= _maxOutputChars ? s : s.Substring(0, _maxOutputChars) + "\n...[输出已截断]";
    }

    private async Task<(string StdOut, string StdErr, int ExitCode)> RunWithTimeoutAsync(string command, TimeSpan timeout)
    {
        var useShell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var psi = new ProcessStartInfo
        {
            FileName = useShell ? "cmd.exe" : "/bin/sh",
            Arguments = useShell ? "/c " + command : "-c " + command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = new Process { StartInfo = psi };
        process.Start();
        var exitTask = process.WaitForExitAsync();
        var delayTask = Task.Delay(timeout);
        var completed = await Task.WhenAny(exitTask, delayTask).ConfigureAwait(false);
        if (completed == delayTask)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException();
        }
        var stdout = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        return (stdout, stderr, process.ExitCode);
    }
}
