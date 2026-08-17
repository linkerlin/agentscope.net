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
/// Text content accumulator: merges multiple TextBlocks into a single text string.
/// 文本内容累加器：合并多个 TextBlock 为一段文本。
/// </summary>
public class TextAccumulator : IContentAccumulator
{
    private readonly List<string> _parts = new();

    /// <inheritdoc />
    public void Accumulate(ContentBlock block)
    {
        // Only accumulate blocks of type TextBlock
        // 仅累积 TextBlock 类型的内容块
        if (block is TextBlock tb)
            _parts.Add(tb.Text ?? "");
    }

    /// <inheritdoc />
    public ContentBlock? GetAccumulated()
    {
        if (_parts.Count == 0) return null;
        return new TextBlock { Text = string.Concat(_parts) };
    }

    /// <inheritdoc />
    public void Reset() => _parts.Clear();

    /// <summary>
    /// Gets the concatenated text from all accumulated parts.
    /// 获取所有已累积片段的拼接文本。
    /// </summary>
    public string GetText() => string.Concat(_parts);
}
