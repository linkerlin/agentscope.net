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

namespace AgentScope.Harness.Filesystem;

/// <summary>
/// Two-layer overlay filesystem (Copy-on-Write). Counterpart to Java OverlayFilesystem.
/// 双层叠加文件系统（Copy-on-Write）。对标 Java OverlayFilesystem。
/// Upper layer: per-user R/W; lower layer: shared R/O.
/// 上层：per-user R/W；下层：shared R/O。
/// Writes to lower-layer files trigger CoW: copy to upper layer first, then modify.
/// 对下层文件的写操作触发 CoW：先复制到上层再修改。
/// </summary>
public sealed class OverlayFilesystem(IFilesystem upper, IFilesystem lower) : IFilesystem
{
    /// <inheritdoc />
    public async Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null,
        CancellationToken ct = default)
    {
        var upperResult = await upper.ReadAsync(filePath, offset, limit, ct);
        if (upperResult.Found) return upperResult;
        return await lower.ReadAsync(filePath, offset, limit, ct);
    }

    /// <inheritdoc />
    public async Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default)
        => await upper.WriteAsync(filePath, content, ct);

    /// <inheritdoc />
    public async Task<EditResult> EditAsync(string filePath, string oldString, string newString,
        bool replaceAll = false, CancellationToken ct = default)
    {
        if (await upper.ExistsAsync(filePath, ct))
            return await upper.EditAsync(filePath, oldString, newString, replaceAll, ct);

        if (await lower.ExistsAsync(filePath, ct))
        {
            var lowerContent = await lower.ReadAsync(filePath, ct: ct);
            if (lowerContent.Found)
            {
                await upper.WriteAsync(filePath, lowerContent.Content!, ct);
                return await upper.EditAsync(filePath, oldString, newString, replaceAll, ct);
            }
        }

        return new EditResult(false, "文件不存在");
    }

    /// <inheritdoc />
    public async Task<LsResult> ListAsync(string path, CancellationToken ct = default)
    {
        var upperResult = await upper.ListAsync(path, ct);
        var lowerResult = await lower.ListAsync(path, ct);
        var merged = new List<FileInfo>();
        var upperNames = new HashSet<string>();

        if (upperResult.Files != null)
        {
            merged.AddRange(upperResult.Files);
            upperNames.UnionWith(upperResult.Files.Select(f => f.Name));
        }

        if (lowerResult.Files != null)
            merged.AddRange(lowerResult.Files.Where(f => !upperNames.Contains(f.Name)));

        return new LsResult(merged);
    }

    /// <inheritdoc />
    public async Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default)
    {
        var upperResult = await upper.GlobAsync(pattern, path, ct);
        var lowerResult = await lower.GlobAsync(pattern, path, ct);
        var upperSet = upperResult.Paths?.ToHashSet() ?? [];
        var all = (upperResult.Paths ?? []).Concat(lowerResult.Paths?.Where(p => !upperSet.Contains(p)) ?? []);
        return new GlobResult(all.ToList());
    }

    /// <inheritdoc />
    public async Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null,
        CancellationToken ct = default)
    {
        var upperResult = await upper.GrepAsync(pattern, path, glob, ct);
        var lowerResult = await lower.GrepAsync(pattern, path, glob, ct);
        return new GrepResult(
            (upperResult.Matches ?? []).Concat(lowerResult.Matches ?? []).ToList());
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string path, CancellationToken ct = default) =>
        await upper.ExistsAsync(path, ct) || await lower.ExistsAsync(path, ct);

    /// <inheritdoc />
    public async Task DeleteAsync(string path, CancellationToken ct = default)
        => await upper.DeleteAsync(path, ct);

    /// <inheritdoc />
    public async Task MoveAsync(string from, string to, CancellationToken ct = default)
        => await upper.MoveAsync(from, to, ct);
}
