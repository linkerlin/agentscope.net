// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Message;

namespace AgentScope.Core.Accumulator;

/// <summary>
/// 文本内容累加器：合并多个 TextBlock 为一段文本。
/// </summary>
public class TextAccumulator : IContentAccumulator
{
    private readonly List<string> _parts = new();

    public void Accumulate(ContentBlock block)
    {
        if (block is TextBlock tb)
            _parts.Add(tb.Text ?? "");
    }

    public ContentBlock? GetAccumulated()
    {
        if (_parts.Count == 0) return null;
        return new TextBlock { Text = string.Concat(_parts) };
    }

    public void Reset() => _parts.Clear();

    public string GetText() => string.Concat(_parts);
}
