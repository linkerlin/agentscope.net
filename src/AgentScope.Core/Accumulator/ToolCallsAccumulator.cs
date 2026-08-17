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
using System.Text;
using AgentScope.Core.Message;

namespace AgentScope.Core.Accumulator;

/// <summary>
/// Tool call accumulator: supports streaming fragment concatenation by tool_call_id,
/// using StringBuilder to incrementally merge arguments fragments.
/// 工具调用累加器：支持流式分片拼接，按 tool_call_id 用 StringBuilder 增量拼接 arguments 片段。
/// </summary>
public class ToolCallsAccumulator : IContentAccumulator
{
    // Accumulates argument JSON fragments for each tool call by ID
    // 按 ID 累积每个工具调用的参数 JSON 片段
    private readonly Dictionary<string, StringBuilder> _argsBuilders = new();
    // Maps tool call ID to tool name
    // 将工具调用 ID 映射到工具名称
    private readonly Dictionary<string, string> _names = new();
    // All blocks received (including incomplete ones)
    // 所有已接收的块（包括未完成的）
    private readonly List<ContentBlock> _allBlocks = new();
    // Completed tool calls
    // 已完成的工具调用
    private readonly List<ToolUseBlock> _complete = new();

    /// <inheritdoc />
    public void Accumulate(ContentBlock block)
    {
        _allBlocks.Add(block);

        if (block is ToolUseBlock tb)
        {
            // First fragment for this tool call: initialize builder
            // 该工具调用的第一个分片：初始化构建器
            if (!_argsBuilders.ContainsKey(tb.Id))
            {
                _argsBuilders[tb.Id] = new StringBuilder();
                _names[tb.Id] = tb.Name;
            }

            // Streaming fragment: if Input contains __fragment__, append to this tool call's args
            // 流式分片：如果 Input 包含 __fragment__，拼接到该 tool_call
            if (tb.Input != null && tb.Input.TryGetValue("__fragment__", out var frag) && frag is string fragStr)
            {
                _argsBuilders[tb.Id].Append(fragStr);
            }
            // If Input contains __done__, consider the tool call complete
            // 如果 Input 包含 __done__，认为完成
            else if (tb.Input != null && tb.Input.ContainsKey("__done__"))
            {
                var json = _argsBuilders[tb.Id].ToString();
                _complete.Add(new ToolUseBlock
                {
                    Id = tb.Id,
                    Name = _names[tb.Id],
                    Input = string.IsNullOrWhiteSpace(json) ? tb.Input : ParseArgs(json)
                });
            }
            // No fragment markers: save directly
            // 没有分片标记，直接保存
            else if (tb.Input != null && (tb.Input.ContainsKey("name") || tb.Input.Count > 0))
            {
                _complete.Add(tb);
            }
        }
    }

    /// <inheritdoc />
    public ContentBlock? GetAccumulated()
    {
        // Return the most recent completed tool call, or the latest block
        // 返回最近完成的工具调用，或最新的块
        if (_complete.Count > 0) return _complete[^1];
        return _allBlocks.Count > 0 ? _allBlocks[^1] : null;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _argsBuilders.Clear();
        _names.Clear();
        _allBlocks.Clear();
        _complete.Clear();
    }

    /// <summary>
    /// Gets all accumulated blocks (including incomplete fragments).
    /// 获取所有已累积的块（包括未完成的分片）。
    /// </summary>
    public IReadOnlyList<ContentBlock> GetBlocks() => _allBlocks;

    /// <summary>
    /// Gets only the completed tool calls.
    /// 仅获取已完成的工具调用。
    /// </summary>
    public IReadOnlyList<ToolUseBlock> GetCompleteToolCalls() => _complete;

    /// <summary>
    /// Attempts to parse a JSON string into a dictionary; falls back to wrapping raw text on failure.
    /// 尝试将 JSON 字符串解析为字典；失败时回退为包装原始文本。
    /// </summary>
    private static Dictionary<string, object>? ParseArgs(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return new Dictionary<string, object> { ["__raw__"] = json };
        }
    }
}
