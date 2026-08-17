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

namespace AgentScope.Core.Accumulator;

/// <summary>
/// Thinking content accumulator: merges multiple ThinkingBlocks into a single thinking string.
/// 思考内容累加器：合并多个 ThinkingBlock。
/// </summary>
public class ThinkingAccumulator : IContentAccumulator
{
    private readonly List<string> _parts = new();

    /// <inheritdoc />
    public void Accumulate(ContentBlock block)
    {
        // Only accumulate blocks of type ThinkingBlock
        // 仅累积 ThinkingBlock 类型的内容块
        if (block is ThinkingBlock tb)
            _parts.Add(tb.Thinking ?? "");
    }

    /// <inheritdoc />
    public ContentBlock? GetAccumulated()
    {
        if (_parts.Count == 0) return null;
        return new ThinkingBlock { Thinking = string.Concat(_parts) };
    }

    /// <inheritdoc />
    public void Reset() => _parts.Clear();

    /// <summary>
    /// Gets the concatenated thinking content from all accumulated parts.
    /// 获取所有已累积思考片段的拼接内容。
    /// </summary>
    public string GetThinking() => string.Concat(_parts);
}
