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

using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Client.Message;

/// <summary>
/// Parses TextBlock to A2A text Part.
/// 将 TextBlock 解析为 A2A 文本 Part。
/// </summary>
public sealed class TextBlockParser : IContentBlockParser<TextBlock>
{
    public object Parse(TextBlock block) => new { type = "text", text = block.Text ?? "" };
}

/// <summary>
/// Parses ThinkingBlock to A2A text Part with thinking metadata.
/// 将 ThinkingBlock 解析为带思考元数据的 A2A 文本 Part。
/// </summary>
public sealed class ThinkingBlockParser : IContentBlockParser<ThinkingBlock>
{
    public object Parse(ThinkingBlock block) => new { type = "text", text = block.Thinking ?? "", metadata = new { _agentscope_block_type = "thinking" } };
}

/// <summary>
/// Parses ImageBlock to A2A file Part.
/// 将 ImageBlock 解析为 A2A 文件 Part。
/// </summary>
public sealed class ImageBlockParser : IContentBlockParser<ImageBlock>
{
    public object Parse(ImageBlock block) => new { type = "file", mimeType = "image/*", file = new { bytes = block.Data, uri = block.Url } };
}

/// <summary>
/// Parses AudioBlock to A2A file Part.
/// 将 AudioBlock 解析为 A2A 文件 Part。
/// </summary>
public sealed class AudioBlockParser : IContentBlockParser<AudioBlock>
{
    public object Parse(AudioBlock block) => new { type = "file", mimeType = "audio/*", file = new { bytes = block.Data, uri = block.Url } };
}

/// <summary>
/// Parses VideoBlock to A2A file Part.
/// 将 VideoBlock 解析为 A2A 文件 Part。
/// </summary>
public sealed class VideoBlockParser : IContentBlockParser<VideoBlock>
{
    public object Parse(VideoBlock block) => new { type = "file", mimeType = "video/*", file = new { bytes = block.Data, uri = block.Url } };
}

/// <summary>
/// Parses ToolUseBlock to A2A data Part with tool metadata.
/// 将 ToolUseBlock 解析为带工具元数据的 A2A 数据 Part。
/// </summary>
public sealed class ToolUseBlockParser : IContentBlockParser<ToolUseBlock>
{
    public object Parse(ToolUseBlock block) => new
    {
        type = "data",
        data = block.Input,
        metadata = new Dictionary<string, object>
        {
            [MessageConstants.MetaBlockType] = "tool_use",
            [MessageConstants.MetaToolName] = block.Name,
            [MessageConstants.MetaToolCallId] = block.Id
        }
    };
}

/// <summary>
/// Parses ToolResultBlock to A2A data Part with tool result metadata.
/// 将 ToolResultBlock 解析为带工具结果元数据的 A2A 数据 Part。
/// </summary>
public sealed class ToolResultBlockParser : IContentBlockParser<ToolResultBlock>
{
    public object Parse(ToolResultBlock block) => new
    {
        type = "data",
        data = block.Output,
        metadata = new Dictionary<string, object>
        {
            [MessageConstants.MetaBlockType] = "tool_result",
            [MessageConstants.MetaToolCallId] = block.Id ?? ""
        }
    };
}
