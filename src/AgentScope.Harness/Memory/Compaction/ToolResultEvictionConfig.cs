namespace AgentScope.Harness.Memory.Compaction;

/// <summary>
/// 大型工具结果驱逐配置，与对话压缩正交。
/// 对标 Java <c>io.agentscope.harness.agent.memory.compaction.ToolResultEvictionConfig</c>。
/// </summary>
public sealed record ToolResultEvictionConfig
{
    /// <summary>是否启用驱逐</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>触发驱逐的结果字节阈值（内存截断路径使用）</summary>
    public int MaxResultBytes { get; init; } = 4096;

    /// <summary>驱逐后保留的头字节数</summary>
    public int HeadBytes { get; init; } = 256;

    /// <summary>驱逐后保留的尾字节数</summary>
    public int TailBytes { get; init; } = 256;

    /// <summary>替换占位文本</summary>
    public string Placeholder { get; init; } = "... [结果已截断] ...";

    // ── 以下为落盘驱逐参数，对标 Java 版 ──

    /// <summary>触发落盘驱逐的字符数阈值。对标 Java <c>getMaxResultChars()</c>。</summary>
    public int MaxResultChars { get; init; } = 4000;

    /// <summary>占位符中保留的首尾预览字符数。对标 Java <c>getPreviewChars()</c>。</summary>
    public int PreviewChars { get; init; } = 500;

    /// <summary>驱逐文件写入的工作区相对目录。对标 Java <c>getEvictionPath()</c>。</summary>
    public string EvictionPath { get; init; } = ".evicted";

    /// <summary>
    /// 永不驱逐的工具名集合。对标 Java <c>getExcludedToolNames()</c>。
    /// 默认排除读文件类工具——驱逐它们会造成"再读一次"的死循环。
    /// </summary>
    public IReadOnlySet<string> ExcludedToolNames { get; init; } =
        new HashSet<string>(StringComparer.Ordinal) { "read_file", "readFile" };

    /// <summary>执行驱逐：对大结果保留 head + tail（纯内存截断，不落盘）</summary>
    public string Evict(string? result)
    {
        if (string.IsNullOrEmpty(result)) return result ?? string.Empty;
        var bytes = System.Text.Encoding.UTF8.GetByteCount(result);
        if (bytes <= MaxResultBytes) return result;

        // 按字符截断 head + tail
        var headLen = Math.Min(HeadBytes, result.Length);
        var tailLen = Math.Min(TailBytes, result.Length - headLen);
        return result[..headLen] + Placeholder + result[^tailLen..];
    }
}
