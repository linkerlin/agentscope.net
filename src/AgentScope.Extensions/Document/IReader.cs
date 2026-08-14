namespace AgentScope.Extensions.Document;

/// <summary>
/// 文档读取器接口。对标 Java Reader。
/// 子工程（PDF/Word/Tika）通过此接口接入。
/// </summary>
public interface IReader
{
    IAsyncEnumerable<string> SupportedFormats { get; }
    Task<IReadOnlyList<DocumentChunk>> ReadAsync(ReaderInput input, CancellationToken ct = default);
}

/// <summary>
/// 文档分块结果
/// </summary>
public readonly record struct DocumentChunk(string Text, IReadOnlyDictionary<string, object>? Metadata = null);
