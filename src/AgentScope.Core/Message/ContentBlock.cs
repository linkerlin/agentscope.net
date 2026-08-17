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

namespace AgentScope.Core.Message;

/// <summary>
/// Abstract base record for multimodal content blocks.
/// Each block has a type identifier and represents a piece of multimodal content
/// (text, image, audio, video, tool call, tool result, thinking, etc.).
/// Corresponds to Java: io.agentscope.core.message.ContentBlock
/// 多模态内容块的抽象基类记录。
/// 每个块具有类型标识符，表示一段多模态内容
///（文本、图片、音频、视频、工具调用、工具结果、思考过程等）。
/// 对应 Java: io.agentscope.core.message.ContentBlock
/// </summary>
public abstract record ContentBlock
{
    /// <summary>
    /// Gets the type identifier of this content block (e.g., "text", "image", "tool_use").
    /// 获取此内容块的类型标识符（例如 "text"、"image"、"tool_use"）。
    /// </summary>
    public abstract string Type { get; }
}

/// <summary>
/// Text content block — represents a plain text segment.
/// 文本内容块——表示纯文本片段。
/// </summary>
public record TextBlock : ContentBlock
{
    /// <summary>Content type identifier, always "text" / 内容类型标识，固定为 "text"。</summary>
    public override string Type => "text";

    /// <summary>The text content / 文本内容。</summary>
    public required string Text { get; set; }
}

/// <summary>
/// Image content block — represents an image referenced by URL or raw data.
/// 图片内容块——表示由 URL 或原始数据引用的图片。
/// </summary>
public record ImageBlock : ContentBlock
{
    /// <summary>Content type identifier, always "image" / 内容类型标识，固定为 "image"。</summary>
    public override string Type => "image";

    /// <summary>URL of the image / 图片的 URL。</summary>
    public required string Url { get; set; }

    /// <summary>Optional MIME type (e.g., "image/png") / 可选的 MIME 类型（例如 "image/png"）。</summary>
    public string? MimeType { get; set; }

    /// <summary>Optional raw image data / 可选的原始图片数据。</summary>
    public byte[]? Data { get; set; }
}

/// <summary>
/// Tool use block — represents a tool/function call made by the assistant.
/// 工具使用块——表示助手发起的工具/函数调用。
/// </summary>
public record ToolUseBlock : ContentBlock
{
    /// <summary>Content type identifier, always "tool_use" / 内容类型标识，固定为 "tool_use"。</summary>
    public override string Type => "tool_use";

    /// <summary>Unique identifier for this tool call / 此工具调用的唯一标识符。</summary>
    public required string Id { get; set; }

    /// <summary>Name of the tool being called / 被调用的工具名称。</summary>
    public required string Name { get; set; }

    /// <summary>Input parameters for the tool call / 工具调用的输入参数。</summary>
    public Dictionary<string, object>? Input { get; set; }

    /// <summary>Optional text content associated with the tool call / 与工具调用关联的可选文本内容。</summary>
    public string? Content { get; set; }
}

/// <summary>
/// Tool result block — represents the result of a tool execution.
/// Corresponds to Java: io.agentscope.core.message.ToolResultBlock
/// 工具结果块——表示工具执行的结果。
/// 对应 Java: io.agentscope.core.message.ToolResultBlock
/// </summary>
public record ToolResultBlock : ContentBlock
{
    /// <summary>Content type identifier, always "tool_result" / 内容类型标识，固定为 "tool_result"。</summary>
    public override string Type => "tool_result";

    /// <summary>Unique identifier matching the corresponding ToolUseBlock / 与对应 ToolUseBlock 匹配的唯一标识符。</summary>
    public required string Id { get; set; }

    /// <summary>Output result of the tool execution / 工具执行的输出结果。</summary>
    public object? Output { get; set; }

    /// <summary>Indicates whether the tool execution resulted in an error / 指示工具执行是否产生错误。</summary>
    public bool IsError { get; set; }

    /// <summary>
    /// Name of the tool that produced this result.
    /// Used for eviction filtering by tool name, telemetry categorization, etc.
    /// Corresponds to Java: ToolResultBlock.getName()
    /// 产生该结果的工具名。用于按工具名做驱逐排除、遥测归类等。
    /// 对标 Java: ToolResultBlock.getName()
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Result metadata dictionary.
    /// For example, eviction middleware uses the key "agentscope.tool_result_evicted"
    /// to mark results as already evicted, preventing duplicate eviction.
    /// Corresponds to Java: ToolResultBlock.getMetadata()
    /// 结果元数据字典。
    /// 例如驱逐中间件使用 "agentscope.tool_result_evicted" 键标记已驱逐，避免重复驱逐。
    /// 对标 Java: ToolResultBlock.getMetadata()
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Extracts plain text output from the result.
    /// Handles string output, collections of ContentBlock, and other object types.
    /// 从结果中提取纯文本输出。
    /// 支持字符串输出、ContentBlock 集合以及其他对象类型。
    /// </summary>
    /// <returns>Extracted text content / 提取的文本内容。</returns>
    public string ExtractText()
    {
        switch (Output)
        {
            case null:
                return string.Empty;
            case string s:
                return s;
            case IEnumerable<ContentBlock> blocks:
            {
                var sb = new System.Text.StringBuilder();
                foreach (var b in blocks)
                    if (b is TextBlock tb && tb.Text != null)
                        sb.Append(tb.Text);
                return sb.ToString();
            }
            default:
                return Output.ToString() ?? string.Empty;
        }
    }
}

/// <summary>
/// Thinking block — represents the model's internal reasoning/thinking process.
/// Used by models with extended thinking capability (e.g., Anthropic Claude).
/// 思考块——表示模型的内部推理/思考过程。
/// 用于具有扩展思考能力的模型（例如 Anthropic Claude）。
/// </summary>
public record ThinkingBlock : ContentBlock
{
    /// <summary>Content type identifier, always "thinking" / 内容类型标识，固定为 "thinking"。</summary>
    public override string Type => "thinking";

    /// <summary>The thinking/reasoning content / 思考/推理内容。</summary>
    public required string Thinking { get; set; }

    /// <summary>Optional digital signature for verification / 可选的数字签名，用于验证。</summary>
    public string? Signature { get; set; }
}

/// <summary>
/// Audio content block — represents audio content referenced by URL or raw data.
/// 音频内容块——表示由 URL 或原始数据引用的音频内容。
/// </summary>
public record AudioBlock : ContentBlock
{
    /// <summary>Content type identifier, always "audio" / 内容类型标识，固定为 "audio"。</summary>
    public override string Type => "audio";

    /// <summary>URL of the audio file / 音频文件的 URL。</summary>
    public required string Url { get; set; }

    /// <summary>Optional MIME type (e.g., "audio/mpeg") / 可选的 MIME 类型（例如 "audio/mpeg"）。</summary>
    public string? MimeType { get; set; }

    /// <summary>Optional raw audio data / 可选的原始音频数据。</summary>
    public byte[]? Data { get; set; }

    /// <summary>Optional duration of the audio in seconds / 可选的音频时长（秒）。</summary>
    public float? DurationSec { get; set; }
}

/// <summary>
/// Video content block — represents video content referenced by URL or raw data.
/// 视频内容块——表示由 URL 或原始数据引用的视频内容。
/// </summary>
public record VideoBlock : ContentBlock
{
    /// <summary>Content type identifier, always "video" / 内容类型标识，固定为 "video"。</summary>
    public override string Type => "video";

    /// <summary>URL of the video file / 视频文件的 URL。</summary>
    public required string Url { get; set; }

    /// <summary>Optional MIME type (e.g., "video/mp4") / 可选的 MIME 类型（例如 "video/mp4"）。</summary>
    public string? MimeType { get; set; }

    /// <summary>Optional raw video data / 可选的原始视频数据。</summary>
    public byte[]? Data { get; set; }

    /// <summary>Optional URL of the poster/thumbnail image / 可选的封面/缩略图 URL。</summary>
    public string? PosterUrl { get; set; }
}
