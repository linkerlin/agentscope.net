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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentScope.Core.Tool;

namespace AgentScope.Core.Memory;

/// <summary>
/// Agent 可控的长时记忆工具方法集合。
/// 通过 [ToolAttribute] 标记暴露给 Agent 的 LTM 操作。
/// </summary>
public static class LongTermMemoryTools
{
    private static ILongTermMemory? _memory;

    /// <summary>
    /// 初始化 LTM 工具，设置后端存储
    /// </summary>
    public static void Initialize(ILongTermMemory memory)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
    }

    /// <summary>
    /// 向长时记忆中添加一条事实
    /// </summary>
    [ToolAttribute(Name = "ltm_add", Description = "向长期记忆中存储一条事实文本")]
    public static async Task<string> AddAsync(
        [ToolParamAttribute(Name = "text", Description = "要存储的事实文本")] string text)
    {
        var mem = _memory ?? throw new InvalidOperationException("LTM 未初始化，请先调用 Initialize");
        await mem.AddAsync(text);
        return $"已存储: {text}";
    }

    /// <summary>
    /// 从长时记忆中搜索相关事实
    /// </summary>
    [ToolAttribute(Name = "ltm_search", Description = "从长期记忆中搜索与查询相关的事实")]
    public static async Task<List<string>> SearchAsync(
        [ToolParamAttribute(Name = "query", Description = "搜索查询字符串")] string query,
        [ToolParamAttribute(Name = "topK", Description = "返回结果数量上限", Required = false)] int topK = 5)
    {
        var mem = _memory ?? throw new InvalidOperationException("LTM 未初始化，请先调用 Initialize");
        return await mem.SearchAsync(query, topK);
    }

    /// <summary>
    /// 获取长时记忆的摘要
    /// </summary>
    [ToolAttribute(Name = "ltm_summarize", Description = "获取长期记忆内容的摘要")]
    public static async Task<string> SummarizeAsync()
    {
        var mem = _memory ?? throw new InvalidOperationException("LTM 未初始化，请先调用 Initialize");
        return await mem.SummarizeAsync();
    }
}
