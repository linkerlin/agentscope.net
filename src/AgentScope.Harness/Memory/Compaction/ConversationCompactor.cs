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

namespace AgentScope.Harness.Memory.Compaction;

/// <summary>
/// Conversation compactor that reduces conversation history using combined strategies.<br />
/// 对话压缩器，使用组合策略缩减对话历史。
/// </summary>
public sealed class ConversationCompactor
{
    private readonly CompactionConfig _config;
    private readonly ToolResultEvictionConfig _eviction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationCompactor"/> class.<br />
    /// 初始化 <see cref="ConversationCompactor"/> 的新实例。
    /// </summary>
    /// <param name="config">Compaction configuration / 压缩配置</param>
    /// <param name="eviction">Tool result eviction configuration / 工具结果驱逐配置</param>
    public ConversationCompactor(
        CompactionConfig? config = null,
        ToolResultEvictionConfig? eviction = null)
    {
        _config = config ?? new CompactionConfig();
        _eviction = eviction ?? new ToolResultEvictionConfig();
    }

    /// <summary>Gets the compaction configuration / 获取压缩配置</summary>
    public CompactionConfig Config => _config;

    /// <summary>
    /// Determines whether compaction is needed based on message count or token count.<br />
    /// 基于消息数或 token 数判断是否需要压缩。
    /// </summary>
    /// <param name="messages">Messages to evaluate / 待评估的消息列表</param>
    /// <returns>True if compaction should be performed / 如需压缩则返回 true</returns>
    public bool ShouldCompact(IReadOnlyList<Msg> messages)
    {
        if (messages.Count >= _config.TriggerMessageCount) return true;
        var totalTokens = EstimateTokens(messages);
        return totalTokens >= _config.TriggerTokenCount;
    }

    /// <summary>
    /// Performs compaction and returns the compressed message list.<br />
    /// 执行压缩，返回压缩后的消息列表。
    /// </summary>
    /// <param name="messages">Messages to compact / 待压缩的消息列表</param>
    /// <returns>Compressed message list / 压缩后的消息列表</returns>
    public IReadOnlyList<Msg> Compact(IReadOnlyList<Msg> messages)
    {
        if (!ShouldCompact(messages)) return messages;

        var working = messages.ToList();

        if (_config.TruncateArgs?.Enabled == true)
            working = TruncateToolArgs(working);

        if (_config.Prune?.Enabled == true)
            working = PruneToolResults(working);

        // 如果仍然超出目标，丢弃最早的非系统消息
        // Drop earliest non-system messages if target count is still exceeded
        while (working.Count > _config.TargetMessageCount)
        {
            var idx = FindEarliestNonSystem(working);
            if (idx < 0) break;
            working.RemoveAt(idx);
        }

        return working;
    }

    /// <summary>
    /// Truncates tool call arguments. Currently a placeholder for future implementation.<br />
    /// 截断工具调用参数。当前为占位实现，留待后续扩展。
    /// </summary>
    private List<Msg> TruncateToolArgs(List<Msg> messages)
    {
        // C# Msg 使用单一 Content 字段，工具调用参数在 Content 中作为字符串传递
        // C# Msg uses a single Content field; tool call args are passed as strings in Content
        return messages;
    }

    /// <summary>
    /// Prunes tool result content. Currently a placeholder for future implementation.<br />
    /// 剪枝工具结果内容。当前为占位实现，留待后续扩展。
    /// </summary>
    private List<Msg> PruneToolResults(List<Msg> messages)
    {
        // ToolResultBlock 在 C# 中是标记 record，实际结果内容在 Msg.Content 中
        // ToolResultBlock is a marker record in C#; actual result content is in Msg.Content
        return messages;
    }

    /// <summary>
    /// Finds the index of the earliest non-system message.<br />
    /// 查找最早的非系统消息的索引。
    /// </summary>
    private static int FindEarliestNonSystem(IList<Msg> messages)
    {
        for (var i = 0; i < messages.Count; i++)
            if (messages[i].Role != "system") return i;
        return -1;
    }

    /// <summary>
    /// Estimates total token count for all messages.<br />
    /// 估算所有消息的总 token 数。
    /// </summary>
    private int EstimateTokens(IReadOnlyList<Msg> messages)
    {
        return messages.Sum(m =>
            TokenCounterUtil.EstimateTokenCount(m.GetTextContent()));
    }
}
