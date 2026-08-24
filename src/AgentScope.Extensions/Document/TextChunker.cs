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
/// Text chunking utility. Maps to Java TextChunker.
/// 文本分块工具。对标 Java TextChunker。
/// </summary>
public static class TextChunker
{
    /// <summary>
    /// Splits the input text into chunks using the specified strategy.
    /// 使用指定的策略将输入文本分割成块。
    /// </summary>
    /// <param name="text">The input text to split. 要分割的输入文本。</param>
    /// <param name="chunkSize">Target size per chunk (default 1000). 每个块的目标大小（默认 1000）。</param>
    /// <param name="overlap">Overlap size between chunks (default 200). 块间重叠大小（默认 200）。</param>
    /// <param name="strategy">The split strategy (default Paragraph). 分割策略（默认 Paragraph）。</param>
    /// <returns>List of chunk strings. 块字符串列表。</returns>
    public static IReadOnlyList<string> Split(string text, int chunkSize = 1000,
        int overlap = 200, SplitStrategy strategy = SplitStrategy.Paragraph)
    {
        var segments = strategy switch
        {
            SplitStrategy.Line => text.Split(['\n'], StringSplitOptions.RemoveEmptyEntries),
            SplitStrategy.Character => ChunkByCharacter(text, chunkSize, overlap),
            SplitStrategy.Token => ChunkByToken(text, chunkSize, overlap),
            _ => SplitByParagraph(text, chunkSize, overlap)
        };
        return segments;
    }

    /// <summary>
    /// Splits text by paragraphs (double newlines) and merges into chunks of the target size.
    /// 按段落（双换行）分割文本并合并为目标大小的块。
    /// </summary>
    private static IReadOnlyList<string> SplitByParagraph(string text, int size, int overlap)
    {
        var paragraphs = text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
        return MergeChunks(paragraphs, size, overlap);
    }

    /// <summary>
    /// Splits text by fixed character count with overlap.
    /// 按固定字符数（带重叠）分割文本。
    /// </summary>
    private static IReadOnlyList<string> ChunkByCharacter(string text, int size, int overlap)
    {
        var result = new List<string>();
        for (int i = 0; i < text.Length; i += size - overlap)
            result.Add(text.Substring(i, Math.Min(size, text.Length - i)));
        return result;
    }

    /// <summary>
    /// Splits text by approximate token count. Approximation: 1 token ≈ 4 characters.
    /// 按近似 token 数分割文本。近似值：1 token ≈ 4 字符。
    /// </summary>
    private static IReadOnlyList<string> ChunkByToken(string text, int size, int overlap)
    {
        var charSize = size * 4;
        var charOverlap = overlap * 4;
        return ChunkByCharacter(text, charSize, charOverlap);
    }

    /// <summary>
    /// Merges text parts into chunks of the target size, maintaining overlap between consecutive chunks.
    /// 将文本片段合并为目标大小的块，保持连续块之间的重叠。
    /// </summary>
    private static IReadOnlyList<string> MergeChunks(string[] parts, int size, int overlap)
    {
        var result = new List<string>();
        var current = new List<string>();
        var len = 0;

        foreach (var part in parts)
        {
            // Add the current part to the working buffer
            // 将当前片段添加到工作缓冲区
            current.Add(part);
            len += part.Length;

            // Trim from the front if the buffer exceeds the target size
            // 如果缓冲区超出目标大小，从前面裁剪
            while (len > size && current.Count > 1)
            {
                len -= current[0].Length;
                current.RemoveAt(0);
            }

            // When the buffer reaches the target size, emit a chunk
            // 当缓冲区达到目标大小时，输出一个块
            if (len >= size)
            {
                result.Add(string.Concat(current));

                // Keep the overlapping tail for the next chunk
                // 保留重叠的尾部用于下一个块
                var keep = Math.Max(0, current.Count - overlap);
                if (keep < current.Count)
                {
                    var removed = current.Take(current.Count - keep).ToList();
                    len -= removed.Sum(x => x.Length);
                    current.RemoveRange(0, current.Count - keep);
                }
            }
        }

        // Emit the remaining buffer as the final chunk
        // 输出剩余缓冲区作为最后一个块
        if (current.Count > 0)
            result.Add(string.Concat(current));

        return result;
    }
}
