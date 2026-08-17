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

using AgentScope.Extensions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AgentScope.Extensions.Document.Word;

/// <summary>
/// Word document reader. Counterpart to the Java WordReader.
/// Word 文档读取器。对标 Java WordReader。
/// Uses DocumentFormat.OpenXml (official OpenXML SDK) instead of Apache POI.
/// 使用 DocumentFormat.OpenXml（官方 OpenXML SDK）替代 Apache POI。
/// </summary>
public sealed class WordReader : AbstractChunkingReader
{
    /// <summary>
    /// Supported file formats (.docx and .docm).
    /// 支持的文件格式（.docx 和 .docm）。
    /// </summary>
    private static readonly string[] _formats = ["docx", "docm"];

    /// <summary>
    /// Initializes a new instance of <see cref="WordReader"/>.
    /// 初始化 <see cref="WordReader"/> 类的新实例。
    /// </summary>
    /// <param name="chunkSize">Maximum characters per chunk / 每个块的最大字符数。</param>
    /// <param name="strategy">Text splitting strategy / 文本分割策略。</param>
    /// <param name="overlap">Overlap characters between consecutive chunks / 相邻块之间的重叠字符数。</param>
    public WordReader(int chunkSize = 1000, SplitStrategy strategy = SplitStrategy.Paragraph, int overlap = 200)
        : base(chunkSize, strategy, overlap) { }

    /// <summary>
    /// Gets the list of file formats supported by this reader.
    /// 获取此读取器支持的文件格式列表。
    /// </summary>
    public override IAsyncEnumerable<string> SupportedFormats => _formats.ToAsyncEnumerable();

    /// <summary>
    /// Reads a Word document and splits it into chunks.
    /// 读取 Word 文档并将其分割为块。
    /// </summary>
    /// <param name="input">The reader input containing the file path / 包含文件路径的读取器输入。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>A list of document chunks / 文档块列表。</returns>
    /// <exception cref="NotSupportedException">Thrown when the input type is not a file / 当输入类型不是文件时抛出。</exception>
    public override Task<IReadOnlyList<DocumentChunk>> ReadAsync(ReaderInput input, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            // Word reader only supports file input; streams are not supported
            // Word 读取器仅支持文件输入，不支持流输入
            var path = input.Type == ReaderInput.InputType.File
                ? input.FilePath
                : throw new NotSupportedException("Word 只支持文件输入");

            // Open the Word document using OpenXML SDK and extract text from paragraphs
            // 使用 OpenXML SDK 打开 Word 文档并提取段落文本
            using var doc = WordprocessingDocument.Open(path!, false);
            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null) return Array.Empty<DocumentChunk>();

            var text = string.Join("\n\n", body.Elements<Paragraph>().Select(p => p.InnerText));
            return Chunk(text);
        }, ct);
    }
}
