// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Message;

namespace AgentScope.Core.Accumulator;

/// <summary>
/// 工具调用累加器：收集 ToolUseBlock / ToolResultBlock，GetAccumulated 返回最后一个块（或首个）。
/// </summary>
public class ToolCallsAccumulator : IContentAccumulator
{
    private readonly List<ContentBlock> _blocks = new();

    public void Accumulate(ContentBlock block)
    {
        if (block is ToolUseBlock || block is ToolResultBlock)
            _blocks.Add(block);
    }

    public ContentBlock? GetAccumulated() => _blocks.Count > 0 ? _blocks[^1] : null;

    public void Reset() => _blocks.Clear();

    public IReadOnlyList<ContentBlock> GetBlocks() => _blocks;
}
