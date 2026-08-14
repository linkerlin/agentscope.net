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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Extensions.Document;

/// <summary>
/// 通用文本抽取阅读器（.NET 版 Apache Tika 等价物）。
/// 处理纯文本可抽取格式：txt / md / html / htm / csv / json / xml / log / 源码等。
/// 对应 Java: io.agentscope.core.rag.reader.TikaReader（基于 Apache Tika 的通用格式解析）。
/// 注：PDF / Word 等二进制格式请使用专属 PdfReader / WordReader。
/// </summary>
public sealed class TikaReader : AbstractChunkingReader
{
    private static readonly HashSet<string> _textFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "text", "md", "markdown", "rst", "html", "htm", "csv", "tsv",
        "json", "xml", "log", "cs", "java", "py", "js", "ts", "go", "rs", "yaml", "yml"
    };

    public TikaReader(int chunkSize = 1000, SplitStrategy strategy = SplitStrategy.Paragraph, int overlap = 200)
        : base(chunkSize, strategy, overlap) { }

    public override IAsyncEnumerable<string> SupportedFormats => ToAsync();

    private static async IAsyncEnumerable<string> ToAsync()
    {
        foreach (var f in _textFormats) yield return f;
    }

    public override Task<IReadOnlyList<DocumentChunk>> ReadAsync(ReaderInput input, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var text = input.Type switch
            {
                ReaderInput.InputType.String => input.Content,
                ReaderInput.InputType.File => input.FilePath != null ? File.ReadAllText(input.FilePath) : input.Content,
                _ => input.Content
            };
            return Chunk(text);
        }, ct);
    }
}
