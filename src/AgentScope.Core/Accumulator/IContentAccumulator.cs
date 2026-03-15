// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Message;

namespace AgentScope.Core.Accumulator;

/// <summary>
/// 内容累加器：逐步累积 ContentBlock，用于流式推理/行动阶段的增量展示。
/// </summary>
public interface IContentAccumulator
{
    void Accumulate(ContentBlock block);
    ContentBlock? GetAccumulated();
    void Reset();
}
