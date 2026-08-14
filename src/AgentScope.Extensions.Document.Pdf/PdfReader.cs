using AgentScope.Extensions;
using UglyToad.PdfPig;

namespace AgentScope.Extensions.Document.Pdf;

/// <summary>
/// PDF 文档读取器。对标 Java PDFReader。
/// 使用 PdfPig（纯 .NET PDF 解析库）替代 Apache PDFBox。
/// </summary>
public sealed class PdfReader : AbstractChunkingReader
{
    private static readonly string[] _formats = ["pdf"];

    public PdfReader(int chunkSize = 1000, SplitStrategy strategy = SplitStrategy.Paragraph, int overlap = 200)
        : base(chunkSize, strategy, overlap) { }

    public override IAsyncEnumerable<string> SupportedFormats => _formats.ToAsyncEnumerable();

    public override Task<IReadOnlyList<DocumentChunk>> ReadAsync(ReaderInput input, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var path = input.Type == ReaderInput.InputType.File
                ? input.FilePath
                : throw new NotSupportedException("PDF 只支持文件输入");

            using var pdf = PdfDocument.Open(path!);
            var text = string.Join("\n\n", pdf.GetPages().Select(p => p.Text));
            return Chunk(text);
        }, ct);
    }
}
