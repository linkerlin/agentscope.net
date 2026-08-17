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
/// Document reader interface. Maps to Java Reader.
/// Sub-projects (PDF/Word/Tika) integrate through this interface.
/// 文档读取器接口。对标 Java Reader。
/// 子工程（PDF/Word/Tika）通过此接口接入。
/// </summary>
public interface IReader
{
    /// <summary>
    /// Gets the list of supported file formats (e.g. "txt", "md", "pdf").
    /// 获取支持的文件格式列表（如 "txt"、"md"、"pdf"）。
    /// </summary>
    IAsyncEnumerable<string> SupportedFormats { get; }

    /// <summary>
    /// Reads and chunks a document from the given input.
    /// 从给定输入读取并分块文档。
    /// </summary>
    /// <param name="input">The reader input specifying the document source. 指定文档源的读取器输入。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>A list of document chunks. 文档块列表。</returns>
    Task<IReadOnlyList<DocumentChunk>> ReadAsync(ReaderInput input, CancellationToken ct = default);
}

/// <summary>
/// A single chunk of a document with optional metadata.
/// 文档分块结果，附带可选的元数据。
/// </summary>
/// <param name="Text">The text content of the chunk. 块文本内容。</param>
/// <param name="Metadata">Optional metadata associated with the chunk. 可选的块关联元数据。</param>
public readonly record struct DocumentChunk(string Text, IReadOnlyDictionary<string, object>? Metadata = null);
