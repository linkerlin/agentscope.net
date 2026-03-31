// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AgentScope.Core.MCP;

namespace AgentScope.Core.Tests.MCP;

internal sealed class TestStdioMcpServer : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public bool IsAvailable => ResolvePowerShellExecutable() != null;

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

    private static string? ResolvePowerShellExecutable()
    {
        var executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pwsh.exe" : "pwsh";
        if (CanRun(executable))
        {
            return executable;
        }

        return null;
    }

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