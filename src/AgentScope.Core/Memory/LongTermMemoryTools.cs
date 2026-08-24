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

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AgentScope.Core.Memory;

/// <summary>
/// Tool functions that expose LongTermMemory operations to LLM agents,
/// enabling agents to store and retrieve long-term memories autonomously.
///
/// 将 LongTermMemory 操作暴露给 LLM Agent 的工具函数，
/// 使 Agent 能够自主存储和检索长期记忆。
/// Corresponds to Java: io.agentscope.core.memory.LongTermMemoryTools
/// </summary>
public static class LongTermMemoryTools
{
    /// <summary>
    /// Stores a memory entry into long-term memory.
    /// 将一条记忆条目存入长期记忆。
    /// </summary>
    /// <param name="memory">The ILongTermMemory instance. / ILongTermMemory 实例。</param>
    /// <param name="content">Memory content to store. / 要存储的记忆内容。</param>
    /// <param name="tags">Optional comma-separated tags for categorization. / 可选的逗号分隔标签，用于分类。</param>
    /// <returns>Confirmation message. / 确认消息。</returns>
    [Description("存储一条长期记忆")]
    public static string StoreMemory(ILongTermMemory memory, string content, string? tags = null)
    {
        // Build optional metadata dictionary with tags if provided.
        // 如果提供了标签，构建包含标签的可选元数据字典。
        Dictionary<string, object>? metadata = null;
        if (!string.IsNullOrWhiteSpace(tags))
        {
            metadata = new Dictionary<string, object>
            {
                { "tags", tags }
            };
        }

        // Call the async AddAsync method synchronously (acceptable for tool functions).
        // 同步调用异步 AddAsync 方法（工具函数中可接受）。
        memory.AddAsync(content, metadata).GetAwaiter().GetResult();
        return $"记忆已存储: {content}";
    }

    /// <summary>
    /// Searches long-term memory by query text.
    /// 根据查询文本搜索长期记忆。
    /// </summary>
    /// <param name="memory">The ILongTermMemory instance. / ILongTermMemory 实例。</param>
    /// <param name="query">Search query. / 搜索查询。</param>
    /// <param name="topK">Maximum number of results to return (default 5). / 返回的最大结果数（默认 5）。</param>
    /// <returns>Search results as a formatted string. / 格式化的搜索结果字符串。</returns>
    [Description("搜索长期记忆")]
    public static string SearchMemory(ILongTermMemory memory, string query, int topK = 5)
    {
        // Call the async SearchAsync method synchronously.
        // 同步调用异步 SearchAsync 方法。
        var results = memory.SearchAsync(query, topK).GetAwaiter().GetResult();
        if (results.Count == 0)
            return "未找到相关记忆。";

        var lines = new System.Text.StringBuilder();
        lines.AppendLine("找到以下相关记忆：");
        for (int i = 0; i < results.Count; i++)
        {
            lines.AppendLine($"{i + 1}. {results[i]}");
        }

        return lines.ToString();
    }

    /// <summary>
    /// Retrieves memories matching a specific tag by using SearchAsync with the tag as query.
    /// 通过将标签作为查询传递给 SearchAsync 来检索匹配特定标签的记忆。
    /// </summary>
    /// <param name="memory">The ILongTermMemory instance. / ILongTermMemory 实例。</param>
    /// <param name="tag">Tag to filter by. / 用于过滤的标签。</param>
    /// <returns>Formatted list of matching memories. / 格式化的匹配记忆列表。</returns>
    [Description("按标签检索长期记忆")]
    public static string GetMemoriesByTag(ILongTermMemory memory, string tag)
    {
        // Use SearchAsync with the tag as query text to find matching entries.
        // 使用 SearchAsync 以标签作为查询文本来查找匹配的条目。
        var results = memory.SearchAsync(tag, topK: 50).GetAwaiter().GetResult();
        if (results.Count == 0)
            return $"未找到标签为 '{tag}' 的记忆。";

        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"标签 '{tag}' 的记忆：");
        for (int i = 0; i < results.Count; i++)
        {
            lines.AppendLine($"{i + 1}. {results[i]}");
        }

        return lines.ToString();
    }

    /// <summary>
    /// Deletes a specific memory by its content text (uses SearchAsync to find and
    /// notes that deletion is not directly supported by ILongTermMemory).
    /// 根据内容文本删除指定的记忆（使用 SearchAsync 查找，
    /// 注意 ILongTermMemory 不直接支持删除操作）。
    /// </summary>
    /// <param name="memory">The ILongTermMemory instance. / ILongTermMemory 实例。</param>
    /// <param name="memoryId">Content text of the memory to delete. / 要删除的记忆内容文本。</param>
    /// <returns>Confirmation message. / 确认消息。</returns>
    [Description("删除一条长期记忆（通过内容文本匹配）")]
    public static string DeleteMemory(ILongTermMemory memory, string memoryId)
    {
        // ILongTermMemory does not have a Delete method; search for the content
        // and report whether it was found.
        // ILongTermMemory 没有 Delete 方法；搜索内容并报告是否找到。
        var results = memory.SearchAsync(memoryId, topK: 1).GetAwaiter().GetResult();
        return results.Count > 0
            ? $"找到匹配记忆 '{memoryId}'，但 ILongTermMemory 不支持直接删除。"
            : $"未找到记忆 '{memoryId}'。";
    }
}
