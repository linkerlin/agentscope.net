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
/// Core filesystem interface. Counterpart to Java AbstractFilesystem.
/// 核心文件系统接口。对标 Java AbstractFilesystem。
/// RuntimeContext is implicitly passed via AsyncLocal, not in method signatures.
/// RuntimeContext 通过 AsyncLocal 隐式传递，不在方法签名中显式传递。
/// </summary>
public interface IFilesystem
{
    /// <summary>
    /// Read file content asynchronously.
    /// 异步读取文件内容。
    /// </summary>
    Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null, CancellationToken ct = default);
    /// <summary>
    /// Write content to a file asynchronously.
    /// 异步写入文件内容。
    /// </summary>
    Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default);
    /// <summary>
    /// Edit file content by replacing text asynchronously.
    /// 异步编辑文件内容（文本替换）。
    /// </summary>
    Task<EditResult> EditAsync(string filePath, string oldString, string newString, bool replaceAll = false, CancellationToken ct = default);
    /// <summary>
    /// List directory contents asynchronously.
    /// 异步列出目录内容。
    /// </summary>
    Task<LsResult> ListAsync(string path, CancellationToken ct = default);
    /// <summary>
    /// Glob files matching a pattern asynchronously.
    /// 异步按通配符模式搜索文件。
    /// </summary>
    Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default);
    /// <summary>
    /// Grep file contents by pattern asynchronously.
    /// 异步按正则搜索文件内容。
    /// </summary>
    Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null, CancellationToken ct = default);
    /// <summary>
    /// Check if a file or directory exists asynchronously.
    /// 异步检查文件或目录是否存在。
    /// </summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    /// <summary>
    /// Delete a file or directory asynchronously.
    /// 异步删除文件或目录。
    /// </summary>
    Task DeleteAsync(string path, CancellationToken ct = default);
    /// <summary>
    /// Move/rename a file or directory asynchronously.
    /// 异步移动/重命名文件或目录。
    /// </summary>
    Task MoveAsync(string from, string to, CancellationToken ct = default);
}

// ── DTO records ──

/// <summary>Result of a read operation. / 读取操作结果。</summary>
public readonly record struct ReadResult(string? Content, bool Found, string? Error = null);
/// <summary>Result of a write operation. / 写入操作结果。</summary>
public readonly record struct WriteResult(bool Success, string? Error = null);
/// <summary>Result of an edit operation. / 编辑操作结果。</summary>
public readonly record struct EditResult(bool Success, string? Error = null);

/// <summary>Result of a list operation. / 列出目录操作结果。</summary>
public readonly record struct LsResult(IReadOnlyList<FileInfo> Files, string? Error = null);
/// <summary>File info metadata. / 文件信息元数据。</summary>
public readonly record struct FileInfo(string Name, string Path, bool IsDirectory, long Size, DateTime LastModified);

/// <summary>Result of a glob operation. / 通配符搜索操作结果。</summary>
public readonly record struct GlobResult(IReadOnlyList<string> Paths, string? Error = null);
/// <summary>Result of a grep operation. / 内容搜索操作结果。</summary>
public readonly record struct GrepResult(IReadOnlyList<GrepMatch> Matches, string? Error = null);
/// <summary>A single grep match. / 单条内容匹配结果。</summary>
public readonly record struct GrepMatch(string File, int LineNumber, string Line);
