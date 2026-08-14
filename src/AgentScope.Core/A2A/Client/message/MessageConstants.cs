namespace AgentScope.Core.A2A.Client.Message;

/// <summary>
/// A2A 消息转换常量。对标 Java MessageConstants。
/// </summary>
public static class MessageConstants
{
    public const string BlockTypeText = "text";
    public const string BlockTypeThinking = "thinking";
    public const string BlockTypeImage = "image";
    public const string BlockTypeAudio = "audio";
    public const string BlockTypeVideo = "video";
    public const string BlockTypeToolUse = "tool_use";
    public const string BlockTypeToolResult = "tool_result";

    public const string MetaMsgSource = "_agentscope_msg_source";
    public const string MetaMsgId = "_agentscope_msg_id";
    public const string MetaBlockType = "_agentscope_block_type";
    public const string MetaToolName = "_agentscope_tool_name";
    public const string MetaToolCallId = "_agentscope_tool_call_id";

    // A2A Task states
    public const string TaskStateSubmitted = "submitted";
    public const string TaskStateWorking = "working";
    public const string TaskStateInputRequired = "input-required";
    public const string TaskStateCompleted = "completed";
    public const string TaskStateCanceled = "canceled";
    public const string TaskStateFailed = "failed";
    public const string TaskStateUnknown = "unknown";
}
