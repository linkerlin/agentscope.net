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
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Extensions.Document;

/// <summary>
/// 图片阅读器：登记图片来源（路径 / URL）并返回文档块。
/// 对应 Java: io.agentscope.core.rag.reader.ImageReader
/// 注：Java 侧 ImageReader 的 OCR 文本抽取同样为占位桩；此处仅登记图片元数据，
/// 真实 OCR 应由外部视觉模型 / 服务接力消费。
/// </summary>
public sealed class ImageReader : AbstractChunkingReader
{
    private static readonly string[] _formats = ["png", "jpg", "jpeg", "gif", "bmp", "webp"];

    public ImageReader(int chunkSize = 1000, SplitStrategy strategy = SplitStrategy.Paragraph, int overlap = 200)
        : base(chunkSize, strategy, overlap) { }

    public override IAsyncEnumerable<string> SupportedFormats => ToAsync();

    private static async IAsyncEnumerable<string> ToAsync()
    {
        foreach (var f in _formats) yield return f;
    }

    public override Task<IReadOnlyList<DocumentChunk>> ReadAsync(ReaderInput input, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var source = input.Type switch
            {
                ReaderInput.InputType.Url => input.Content,
                ReaderInput.InputType.File => input.FilePath ?? input.Content,
                _ => input.Content
            };

            var metadata = new Dictionary<string, object> { ["image_source"] = source };
            var chunk = new DocumentChunk($"[image] {source}", metadata);
            return (IReadOnlyList<DocumentChunk>)new List<DocumentChunk> { chunk };
        }, ct);
    }
}
