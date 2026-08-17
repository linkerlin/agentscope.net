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

namespace AgentScope.Harness.Memory.Compaction;

/// <summary>
/// Configuration for evicting large tool results, orthogonal to conversation compaction.<br />
/// Counterpart of Java <c>io.agentscope.harness.agent.memory.compaction.ToolResultEvictionConfig</c>.<br />
/// 大型工具结果驱逐配置，与对话压缩正交。对标 Java 同名类。
/// </summary>
public sealed record ToolResultEvictionConfig
{
    /// <summary>Whether eviction is enabled / 是否启用驱逐</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Byte threshold to trigger eviction (used by in-memory truncation path) / 触发驱逐的字节阈值（内存截断路径使用）</summary>
    public int MaxResultBytes { get; init; } = 4096;

    /// <summary>Bytes to keep from the head after eviction / 驱逐后保留的头字节数</summary>
    public int HeadBytes { get; init; } = 256;

    /// <summary>Bytes to keep from the tail after eviction / 驱逐后保留的尾字节数</summary>
    public int TailBytes { get; init; } = 256;

    /// <summary>Placeholder text for truncated content / 替换占位文本</summary>
    public string Placeholder { get; init; } = "... [结果已截断] ...";

    // ── 以下为落盘驱逐参数，对标 Java 版 // Disk-eviction parameters, matching the Java version ──

    /// <summary>Character count threshold to trigger disk eviction. Counterpart of Java <c>getMaxResultChars()</c>. / 触发落盘驱逐的字符数阈值</summary>
    public int MaxResultChars { get; init; } = 4000;

    /// <summary>Preview character count kept at head/tail in placeholder. Counterpart of Java <c>getPreviewChars()</c>. / 占位符中保留的首尾预览字符数</summary>
    public int PreviewChars { get; init; } = 500;

    /// <summary>Relative workspace directory for evicted files. Counterpart of Java <c>getEvictionPath()</c>. / 驱逐文件写入的工作区相对目录</summary>
    public string EvictionPath { get; init; } = ".evicted";

    /// <summary>
    /// Tool names that are never evicted. Counterpart of Java <c>getExcludedToolNames()</c>.<br />
    /// By default excludes file-read tools — evicting them would cause a "read again" infinite loop.<br />
    /// 永不驱逐的工具名集合。默认排除读文件类工具——驱逐它们会造成"再读一次"的死循环。
    /// </summary>
    public IReadOnlySet<string> ExcludedToolNames { get; init; } =
        new HashSet<string>(StringComparer.Ordinal) { "read_file", "readFile" };

    /// <summary>
    /// Performs eviction: keeps head + tail for large results (in-memory truncation only, no disk write).<br />
    /// 执行驱逐：对大结果保留 head + tail（纯内存截断，不落盘）。
    /// </summary>
    /// <param name="result">Tool result to evict / 待驱逐的工具结果</param>
    /// <returns>Evicted (truncated) result string / 驱逐（截断）后的结果字符串</returns>
    public string Evict(string? result)
    {
        if (string.IsNullOrEmpty(result)) return result ?? string.Empty;
        var bytes = System.Text.Encoding.UTF8.GetByteCount(result);
        if (bytes <= MaxResultBytes) return result;

        // 按字符截断 head + tail // Truncate by characters: head + placeholder + tail
        var headLen = Math.Min(HeadBytes, result.Length);
        var tailLen = Math.Min(TailBytes, result.Length - headLen);
        return result[..headLen] + Placeholder + result[^tailLen..];
    }
}
