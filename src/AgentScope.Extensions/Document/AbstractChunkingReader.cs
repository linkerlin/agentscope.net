namespace AgentScope.Extensions.Document;

/// <summary>
/// 抽象分块阅读器基类。对标 Java AbstractChunkingReader。
/// 提供分块逻辑，子类只需实现 ReadAsync 并调用 Chunk()。
/// </summary>
public abstract class AbstractChunkingReader : IReader
{
    protected int ChunkSize { get; }
    protected SplitStrategy Strategy { get; }
    protected int OverlapSize { get; }

    protected AbstractChunkingReader(int chunkSize = 1000, SplitStrategy strategy = SplitStrategy.Paragraph, int overlap = 200)
    {
        ChunkSize = chunkSize;
        Strategy = strategy;
        OverlapSize = overlap;
    }

    public abstract IAsyncEnumerable<string> SupportedFormats { get; }
    public abstract Task<IReadOnlyList<DocumentChunk>> ReadAsync(ReaderInput input, CancellationToken ct = default);

    protected IReadOnlyList<DocumentChunk> Chunk(string text)
    {
        var segments = TextChunker.Split(text, ChunkSize, OverlapSize, Strategy);
        return segments.Select(s => new DocumentChunk(s)).ToList();
    }
}
