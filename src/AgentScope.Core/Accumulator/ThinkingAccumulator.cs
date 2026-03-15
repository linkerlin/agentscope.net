// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Message;

namespace AgentScope.Core.Accumulator;

/// <summary>
/// 思考内容累加器：合并多个 ThinkingBlock。
/// </summary>
public class ThinkingAccumulator : IContentAccumulator
{
    private readonly List<string> _parts = new();

    public void Accumulate(ContentBlock block)
    {
        if (block is ThinkingBlock tb)
            _parts.Add(tb.Thinking ?? "");
    }

    public ContentBlock? GetAccumulated()
    {
        if (_parts.Count == 0) return null;
        return new ThinkingBlock { Thinking = string.Concat(_parts) };
    }

    public void Reset() => _parts.Clear();

    public string GetThinking() => string.Concat(_parts);
}
