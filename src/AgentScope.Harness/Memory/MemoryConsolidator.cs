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

using AgentScope.Core.Message;
using AgentScope.Harness.Memory.Compaction;
using AgentScope.Harness.Memory.Session;

namespace AgentScope.Harness.Memory;

/// <summary>
/// Memory consolidator that merges daily session logs into long-term memory summaries.<br />
/// 记忆合并器：将日记录账合并为长期记忆摘要。
/// </summary>
public sealed class MemoryConsolidator
{
    private readonly MemoryConfig _config;
    private readonly SessionTree _sessionTree;
    private readonly ConversationCompactor _compactor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryConsolidator"/> class.<br />
    /// 初始化 <see cref="MemoryConsolidator"/> 的新实例。
    /// </summary>
    /// <param name="config">Memory configuration / 记忆配置</param>
    /// <param name="sessionTree">Session tree for reading/writing entries / 用于读写条目的会话树</param>
    /// <param name="compactor">Optional conversation compactor / 可选的对话压缩器</param>
    public MemoryConsolidator(
        MemoryConfig config,
        SessionTree sessionTree,
        ConversationCompactor? compactor = null)
    {
        _config = config;
        _sessionTree = sessionTree;
        _compactor = compactor ?? new ConversationCompactor();
    }

    /// <summary>
    /// Executes consolidation: loads logs, compresses, and saves context.<br />
    /// 执行合并：读取日志 → 压缩 → 保存上下文。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async Task ConsolidateAsync(CancellationToken ct = default)
    {
        var logEntries = await _sessionTree.LoadLogAsync(ct);
        if (logEntries.Count == 0) return;

        // 提取消息条目 // Extract message entries
        var messages = logEntries
            .OfType<MessageEntry>()
            .Select(e => e.ToMsg())
            .ToList();

        if (messages.Count == 0) return;

        // 压缩消息 // Compress messages
        var compressed = _compactor.ShouldCompact(messages)
            ? _compactor.Compact(messages)
            : messages;

        // 转换为上下文条目 // Convert to context entries
        var contextEntries = compressed
            .Select(m => (SessionEntry)MessageEntry.FromMsg(m))
            .ToList();

        // 写入压缩标记 // Write compaction marker entry
        contextEntries.Insert(0, new CompactionEntry
        {
            Summary = $"Consolidated {logEntries.Count} entries to {contextEntries.Count}",
            OriginalMessageCount = logEntries.Count,
            CompressedMessageCount = contextEntries.Count
        });

        await _sessionTree.SaveContextAsync(contextEntries, ct);
    }

    /// <summary>
    /// Determines whether consolidation is needed based on time gap.<br />
    /// 基于时间间隔判断是否需要合并。
    /// </summary>
    /// <param name="lastConsolidated">Last consolidation timestamp / 上次合并时间戳</param>
    /// <returns>True if consolidation should run / 如需合并则返回 true</returns>
    public bool ShouldConsolidate(DateTime lastConsolidated)
    {
        var gap = _config.ConsolidationMinGap ?? TimeSpan.FromHours(1);
        return DateTime.UtcNow - lastConsolidated > gap;
    }
}
