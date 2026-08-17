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

using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tool;

/// <summary>
/// Shell 执行工具。对标 Java ShellExecuteTool。
/// 在沙箱或本地执行 shell 命令。
/// </summary>
public sealed class ShellExecuteTool : ITool
{
    public string Name => "shell_execute";
    public string Description => "在沙箱中执行 shell 命令并获取输出";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var command = parameters.GetValueOrDefault("command")?.ToString();
        if (string.IsNullOrWhiteSpace(command))
            return ToolResult.Fail("需要 command 参数");

        try
        {
            var timeout = 30;
            if (parameters.TryGetValue("timeout", out var t) && t is int ti)
                timeout = ti;

            var psi = new System.Diagnostics.ProcessStartInfo("sh", $"-c '{command}'")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return ToolResult.Fail("无法启动进程");

            var completed = proc.WaitForExit(timeout * 1000);
            if (!completed)
            {
                proc.Kill();
                return ToolResult.Fail("命令执行超时");
            }

            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();

            var result = stdout;
            if (!string.IsNullOrEmpty(stderr))
                result += $"\nSTDERR:\n{stderr}";

            return ToolResult.Ok(result);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"命令执行失败: {ex.Message}");
        }
    }

    public Dictionary<string, object> GetSchema() => new()
    {
        ["name"] = Name,
        ["description"] = Description,
        ["parameters"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["command"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "要执行的 shell 命令" },
                ["timeout"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "超时时间(秒)" }
            },
            ["required"] = new[] { "command" }
        }
    };
}
