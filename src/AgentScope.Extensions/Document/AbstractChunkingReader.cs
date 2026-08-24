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
/// Abstract base class for chunking readers. Maps to Java AbstractChunkingReader.
/// Provides chunking logic; subclasses only need to implement ReadAsync and call Chunk().
/// 抽象分块阅读器基类。对标 Java AbstractChunkingReader。
/// 提供分块逻辑，子类只需实现 ReadAsync 并调用 Chunk()。
/// </summary>
public abstract class AbstractChunkingReader : IReader
{
    /// <summary>Maximum number of characters per chunk. 每个块的最大字符数。</summary>
    protected int ChunkSize { get; }

    /// <summary>The split strategy to use (paragraph, character, etc.). 使用的分块策略（段落、字符等）。</summary>
    protected SplitStrategy Strategy { get; }

    /// <summary>Number of overlapping characters between consecutive chunks. 连续块之间的重叠字符数。</summary>
    protected int OverlapSize { get; }

    /// <summary>
    /// Creates an AbstractChunkingReader with the specified chunking parameters.
    /// 使用指定的分块参数创建 AbstractChunkingReader。
    /// </summary>
    /// <param name="chunkSize">Maximum characters per chunk (default 1000). 每个块的最大字符数（默认 1000）。</param>
    /// <param name="strategy">Text split strategy (default Paragraph). 文本分割策略（默认 Paragraph）。</param>
    /// <param name="overlap">Overlap size between chunks (default 200). 块间重叠大小（默认 200）。</param>
    protected AbstractChunkingReader(int chunkSize = 1000, SplitStrategy strategy = SplitStrategy.Paragraph, int overlap = 200)
    {
        ChunkSize = chunkSize;
        Strategy = strategy;
        OverlapSize = overlap;
    }

    /// <inheritdoc />
    public abstract IAsyncEnumerable<string> SupportedFormats { get; }

    /// <inheritdoc />
    public abstract Task<IReadOnlyList<DocumentChunk>> ReadAsync(ReaderInput input, CancellationToken ct = default);

    /// <summary>
    /// Splits the given text into chunks using the configured strategy.
    /// 使用配置的策略将给定文本分割成块。
    /// </summary>
    /// <param name="text">The input text to chunk. 要分块的输入文本。</param>
    /// <returns>A list of document chunks. 文档块列表。</returns>
    protected IReadOnlyList<DocumentChunk> Chunk(string text)
    {
        var segments = TextChunker.Split(text, ChunkSize, OverlapSize, Strategy);
        return segments.Select(s => new DocumentChunk(s)).ToList();
    }
}
