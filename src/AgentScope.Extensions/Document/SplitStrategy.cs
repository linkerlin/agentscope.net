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

namespace AgentScope.Extensions.Document;

/// <summary>
/// Text splitting strategies. Maps to Java SplitStrategy.
/// 文本分块策略。对标 Java SplitStrategy。
/// </summary>
public enum SplitStrategy
{
    /// <summary>Split by fixed character count. 按固定字符数分割。</summary>
    Character,

    /// <summary>Split by paragraphs (double newlines). 按段落分割（双换行）。</summary>
    Paragraph,

    /// <summary>Split by lines (single newlines). 按行分割（单换行）。</summary>
    Line,

    /// <summary>Split by approximate token count (1 token ≈ 4 chars). 按近似 token 数分割（1 token ≈ 4 字符）。</summary>
    Token,

    /// <summary>Split by semantic boundaries (placeholder for future ML-based splitting). 按语义边界分割（未来基于 ML 的分割占位）。</summary>
    Semantic
}
