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

namespace AgentScope.Harness.Filesystem.Sandbox;

using ExecResult = AgentScope.Harness.Sandbox.ExecResult;

/// <summary>
/// 沙箱文件系统抽象基类。对标 Java AbstractSandboxFilesystem。
/// 通过沙箱的 shell 执行提供文件操作。
/// </summary>
public abstract class SandboxFilesystemBase : IFilesystem
{
    public abstract string Id { get; }

    public abstract Task<ExecResult> ExecuteAsync(string command, int? timeout = null, CancellationToken ct = default);

    public async Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null,
        CancellationToken ct = default)
    {
        var cmd = offset.HasValue
            ? $"sed -n '{offset.Value},{offset.Value + (limit ?? 1)}p' '{filePath}'"
            : $"cat '{filePath}'";
        var result = await ExecuteAsync(cmd, ct: ct);
        return result.ExitCode == 0
            ? new ReadResult(result.StdOut, true)
            : new ReadResult(null, false, result.StdErr);
    }

    public async Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default)
    {
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));
        var cmd = $"echo '{base64}' | base64 -d > '{filePath}'";
        var result = await ExecuteAsync(cmd, ct: ct);
        return new WriteResult(result.ExitCode == 0, result.StdErr);
    }

    public async Task<EditResult> EditAsync(string filePath, string oldString, string newString,
        bool replaceAll = false, CancellationToken ct = default)
    {
        var oldB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(oldString));
        var newB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(newString));
        var flag = replaceAll ? "g" : "";
        var cmd = $"sed -i 's/$(echo '{oldB64}' | base64 -d)/$(echo '{newB64}' | base64 -d)/{flag}' '{filePath}'";
        var result = await ExecuteAsync(cmd, ct: ct);
        return new EditResult(result.ExitCode == 0, result.StdErr);
    }

    public async Task<LsResult> ListAsync(string path, CancellationToken ct = default)
    {
        var result = await ExecuteAsync($"ls -la '{path}'", ct: ct);
        if (result.ExitCode != 0) return new LsResult([], result.StdErr);

        var files = result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1) // skip total line
            .Select<string, AgentScope.Harness.Filesystem.FileInfo?>(line =>
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 9
                    ? new FileInfo(parts[^1], Path.Combine(path, parts[^1]),
                        parts[0].StartsWith('d'), 0, DateTime.MinValue)
                    : null;
            })
            .Where(f => f != null)
            .Select(f => f!.Value)
            .ToList();
        return new LsResult(files);
    }

    public Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default)
        => throw new NotSupportedException("沙箱环境不支持 Glob");
    public Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null, CancellationToken ct = default)
        => throw new NotSupportedException("沙箱环境不支持 Grep");
    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
        => throw new NotSupportedException("沙箱环境不支持 Exists");
    public Task DeleteAsync(string path, CancellationToken ct = default)
        => throw new NotSupportedException("沙箱环境不支持 Delete");
    public Task MoveAsync(string from, string to, CancellationToken ct = default)
        => throw new NotSupportedException("沙箱环境不支持 Move");

}
