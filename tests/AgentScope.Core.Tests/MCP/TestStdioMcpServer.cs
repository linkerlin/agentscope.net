// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AgentScope.Core.MCP;

namespace AgentScope.Core.Tests.MCP;

/// <summary>
/// An in-process test helper that starts a fake MCP server via PowerShell
/// for integration testing of <see cref="StdioMcpClient"/>.
/// 一个进程内测试辅助工具，通过 PowerShell 启动一个模拟 MCP 服务器，用于 StdioMcpClient 的集成测试。
/// </summary>
internal sealed class TestStdioMcpServer : IDisposable
{
    /// <summary>
    /// Tracks temporary script files to clean up on dispose.
    /// 跟踪临时脚本文件以在释放时清理。
    /// </summary>
    private readonly List<string> _tempFiles = new();

    /// <summary>
    /// Gets whether PowerShell (pwsh) is available on the current system.
    /// 获取当前系统上是否可用 PowerShell (pwsh)。
    /// </summary>
    public bool IsAvailable => ResolvePowerShellExecutable() != null;

    /// <summary>
    /// Creates a new <see cref="StdioMcpClient"/> connected to the fake MCP server.
    /// 创建一个连接到模拟 MCP 服务器的新 StdioMcpClient。
    /// </summary>
    /// <param name="name">The client name / 客户端名称。</param>
    /// <param name="requestTimeout">Optional request timeout / 可选的请求超时时间。</param>
    /// <returns>A configured <see cref="StdioMcpClient"/> / 已配置的 StdioMcpClient。</returns>
    public StdioMcpClient CreateClient(string name = "fake-mcp", TimeSpan? requestTimeout = null)
    {
        var executable = ResolvePowerShellExecutable();
        if (executable == null)
        {
            throw new InvalidOperationException("未找到 pwsh，无法启动测试 MCP server。");
        }

        var scriptPath = CreateServerScript();
        return new StdioMcpClient(
            name,
            executable,
            $"-NoProfile -File \"{scriptPath}\"",
            requestTimeout: requestTimeout ?? TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Cleans up all temporary PowerShell script files created during testing.
    /// 清理测试期间创建的所有临时 PowerShell 脚本文件。
    /// </summary>
    public void Dispose()
    {
        foreach (var tempFile in _tempFiles)
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // 忽略测试清理异常。
            }
        }
    }

    /// <summary>
    /// Resolves the PowerShell executable path (pwsh) on the current platform.
    /// 解析当前平台上的 PowerShell 可执行文件路径 (pwsh)。
    /// </summary>
    /// <returns>
    /// The executable name if found; otherwise null / 如果找到则返回可执行文件名，否则返回 null。
    /// </returns>
    private static string? ResolvePowerShellExecutable()
    {
        var executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pwsh.exe" : "pwsh";
        if (CanRun(executable))
        {
            return executable;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the given executable can be launched successfully.
    /// 检查给定的可执行文件是否能成功启动。
    /// </summary>
    /// <param name="fileName">The executable name / 可执行文件名。</param>
    /// <returns>true if the executable runs and exits with code 0 / 如果可执行文件运行并返回退出码 0 则为 true。</returns>
    private static bool CanRun(string fileName)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = "-NoProfile -Command \"exit 0\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(2000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a temporary PowerShell script that acts as a fake MCP server
    /// supporting initialize, tools/list, and tools/call methods.
    /// 创建一个充当模拟 MCP 服务器的临时 PowerShell 脚本，支持 initialize、tools/list 和 tools/call 方法。
    /// </summary>
    /// <returns>The path to the created script file / 创建的脚本文件路径。</returns>
    private string CreateServerScript()
    {
        const string script = @"
$utf8 = [System.Text.UTF8Encoding]::new($false)
$stdin = [Console]::OpenStandardInput()
$stdout = [Console]::OpenStandardOutput()

function Read-Line {
    param([System.IO.Stream]$Stream)

    $bytes = New-Object System.Collections.Generic.List[byte]
    while ($true) {
        $value = $Stream.ReadByte()
        if ($value -lt 0) {
            if ($bytes.Count -eq 0) {
                return $null
            }

            break
        }

        if ($value -eq 10) {
            break
        }

        [void]$bytes.Add([byte]$value)
    }

    return $utf8.GetString($bytes.ToArray()).TrimEnd(""`r"")
}

function Read-Message {
    $headers = @{}
    while ($true) {
        $line = Read-Line -Stream $stdin
        if ($null -eq $line) {
            return $null
        }

        if ($line.Length -eq 0) {
            break
        }

        $parts = $line.Split(':', 2)
        if ($parts.Length -eq 2) {
            $headers[$parts[0].Trim()] = $parts[1].Trim()
        }
    }

    $length = [int]$headers['Content-Length']
    $buffer = New-Object byte[] $length
    $offset = 0
    while ($offset -lt $length) {
        $read = $stdin.Read($buffer, $offset, $length - $offset)
        if ($read -le 0) {
            throw 'failed to read body'
        }

        $offset += $read
    }

    $json = $utf8.GetString($buffer)
    return $json | ConvertFrom-Json -AsHashtable
}

function Write-Message {
    param($Payload)

    $json = $Payload | ConvertTo-Json -Depth 20 -Compress
    $body = $utf8.GetBytes($json)
    $header = [System.Text.Encoding]::ASCII.GetBytes(""Content-Length: $($body.Length)`r`n`r`n"")
    $stdout.Write($header, 0, $header.Length)
    $stdout.Write($body, 0, $body.Length)
    $stdout.Flush()
}

while ($true) {
    $message = Read-Message
    if ($null -eq $message) {
        break
    }

    switch ($message.method) {
        'initialize' {
            Write-Message @{
                jsonrpc = '2.0'
                id = $message.id
                result = @{
                    protocolVersion = '2024-11-05'
                    capabilities = @{ tools = @{} }
                    serverInfo = @{ name = 'fake-mcp'; version = '1.0.0' }
                }
            }
        }
        'notifications/initialized' {
        }
        'tools/list' {
            Write-Message @{
                jsonrpc = '2.0'
                id = $message.id
                result = @{
                    tools = @(
                        @{
                            name = 'echo'
                            description = '回显输入文本'
                            inputSchema = @{
                                type = 'object'
                                properties = @{
                                    text = @{ type = 'string' }
                                }
                            }
                        },
                        @{
                            name = 'fail'
                            description = '返回错误结果'
                            inputSchema = @{
                                type = 'object'
                                properties = @{}
                            }
                        }
                    )
                }
            }
        }
        'tools/call' {
            $toolName = $message.params.name
            if ($toolName -eq 'echo') {
                $text = [string]$message.params.arguments.text
                Write-Message @{
                    jsonrpc = '2.0'
                    id = $message.id
                    result = @{
                        isError = $false
                        content = @(
                            @{
                                type = 'text'
                                text = ""echo: $text""
                            }
                        )
                    }
                }
            }
            elseif ($toolName -eq 'fail') {
                Write-Message @{
                    jsonrpc = '2.0'
                    id = $message.id
                    result = @{
                        isError = $true
                        content = @(
                            @{
                                type = 'text'
                                text = 'denied'
                            }
                        )
                    }
                }
            }
            else {
                Write-Message @{
                    jsonrpc = '2.0'
                    id = $message.id
                    error = @{
                        code = -32601
                        message = 'method not found'
                    }
                }
            }
        }
        default {
            Write-Message @{
                jsonrpc = '2.0'
                id = $message.id
                error = @{
                    code = -32601
                    message = 'method not found'
                }
            }
        }
    }
}
";

        var path = Path.Combine(Path.GetTempPath(), $"agentscope_fake_mcp_{Guid.NewGuid():N}.ps1");
        File.WriteAllText(path, script, new UTF8Encoding(false));
        _tempFiles.Add(path);
        return path;
    }
}