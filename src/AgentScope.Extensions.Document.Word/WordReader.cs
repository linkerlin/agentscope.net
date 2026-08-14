using AgentScope.Extensions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AgentScope.Extensions.Document.Word;

/// <summary>
/// Word 文档读取器。对标 Java WordReader。
/// 使用 DocumentFormat.OpenXml（官方 OpenXML SDK）替代 Apache POI。
/// </summary>
public sealed class WordReader : AbstractChunkingReader
{
    private static readonly string[] _formats = ["docx", "docm"];

    public WordReader(int chunkSize = 1000, SplitStrategy strategy = SplitStrategy.Paragraph, int overlap = 200)
        : base(chunkSize, strategy, overlap) { }

    public override IAsyncEnumerable<string> SupportedFormats => _formats.ToAsyncEnumerable();

    public override Task<IReadOnlyList<DocumentChunk>> ReadAsync(ReaderInput input, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var path = input.Type == ReaderInput.InputType.File
                ? input.FilePath
                : throw new NotSupportedException("Word 只支持文件输入");

            using var doc = WordprocessingDocument.Open(path!, false);
            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null) return Array.Empty<DocumentChunk>();

            var text = string.Join("\n\n", body.Elements<Paragraph>().Select(p => p.InnerText));
            return Chunk(text);
        }, ct);
    }
}
