using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Client.Message;

public sealed class TextBlockParser : IContentBlockParser<TextBlock>
{
    public object Parse(TextBlock block) => new { type = "text", text = block.Text ?? "" };
}

public sealed class ThinkingBlockParser : IContentBlockParser<ThinkingBlock>
{
    public object Parse(ThinkingBlock block) => new { type = "text", text = block.Thinking ?? "", metadata = new { _agentscope_block_type = "thinking" } };
}

public sealed class ImageBlockParser : IContentBlockParser<ImageBlock>
{
    public object Parse(ImageBlock block) => new { type = "file", mimeType = "image/*", file = new { bytes = block.Data, uri = block.Url } };
}

public sealed class AudioBlockParser : IContentBlockParser<AudioBlock>
{
    public object Parse(AudioBlock block) => new { type = "file", mimeType = "audio/*", file = new { bytes = block.Data, uri = block.Url } };
}

public sealed class VideoBlockParser : IContentBlockParser<VideoBlock>
{
    public object Parse(VideoBlock block) => new { type = "file", mimeType = "video/*", file = new { bytes = block.Data, uri = block.Url } };
}

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
