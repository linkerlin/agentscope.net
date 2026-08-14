// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Text;
using AgentScope.Core.Message;

namespace AgentScope.Core.Accumulator;

/// <summary>
/// 工具调用累加器：支持流式分片拼接，按 tool_call_id 用 StringBuilder 增量拼接 arguments 片段
/// </summary>
public class ToolCallsAccumulator : IContentAccumulator
{
    private readonly Dictionary<string, StringBuilder> _argsBuilders = new();
    private readonly Dictionary<string, string> _names = new();
    private readonly List<ContentBlock> _allBlocks = new();
    private readonly List<ToolUseBlock> _complete = new();

    public void Accumulate(ContentBlock block)
    {
        _allBlocks.Add(block);

        if (block is ToolUseBlock tb)
        {
            if (!_argsBuilders.ContainsKey(tb.Id))
            {
                _argsBuilders[tb.Id] = new StringBuilder();
                _names[tb.Id] = tb.Name;
            }

            // 流式分片：如果 Input 包含 __fragment__，拼接到该 tool_call
            if (tb.Input != null && tb.Input.TryGetValue("__fragment__", out var frag) && frag is string fragStr)
            {
                _argsBuilders[tb.Id].Append(fragStr);
            }
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
            // 没有分片标记，直接保存
            else if (tb.Input != null && (tb.Input.ContainsKey("name") || tb.Input.Count > 0))
            {
                _complete.Add(tb);
            }
        }
    }

    public ContentBlock? GetAccumulated()
    {
        if (_complete.Count > 0) return _complete[^1];
        return _allBlocks.Count > 0 ? _allBlocks[^1] : null;
    }

    public void Reset()
    {
        _argsBuilders.Clear();
        _names.Clear();
        _allBlocks.Clear();
        _complete.Clear();
    }

    public IReadOnlyList<ContentBlock> GetBlocks() => _allBlocks;

    public IReadOnlyList<ToolUseBlock> GetCompleteToolCalls() => _complete;

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
