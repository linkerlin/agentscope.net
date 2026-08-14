namespace AgentScope.Core.AgUI.Model;

/// <summary>
/// AG-UI 运行 Agent 请求。对标 Java RunAgentInput。
/// </summary>
public sealed record RunAgentInput(
    string ThreadId,
    string RunId,
    IReadOnlyList<AguiMessage> Messages,
    IReadOnlyList<AguiTool>? Tools = null,
    IReadOnlyList<AguiContext>? Context = null,
    IReadOnlyDictionary<string, object>? State = null,
    IReadOnlyDictionary<string, string>? ForwardedProps = null,
    IReadOnlyList<AguiResume>? Resume = null);

/// <summary>
/// AG-UI 消息。对标 Java AguiMessage。
/// </summary>
public sealed record AguiMessage(
    string Id,
    string Role,
    string? Text = null,
    IReadOnlyList<InputContent>? Blocks = null,
    IReadOnlyList<AguiToolCall>? ToolCalls = null,
    string? ToolCallId = null)
{
    public static AguiMessage UserMessage(string text) => new(Guid.NewGuid().ToString(), "user", text);
    public static AguiMessage AssistantMessage(string text) => new(Guid.NewGuid().ToString(), "assistant", text);
    public static AguiMessage SystemMessage(string text) => new(Guid.NewGuid().ToString(), "system", text);
    public static AguiMessage ToolMessage(string text, string toolCallId) => new(Guid.NewGuid().ToString(), "tool", text, ToolCallId: toolCallId);
}

/// <summary>
/// AG-UI 工具。对标 Java AguiTool。
/// </summary>
public sealed record AguiTool(string Name, string Description, object? Parameters = null);

/// <summary>
/// AG-UI 工具调用。对标 Java AguiToolCall。
/// </summary>
public sealed record AguiToolCall(string Id, string Type = "function", AguiFunctionCall? Function = null);
public sealed record AguiFunctionCall(string Name, string Arguments);

/// <summary>
/// AG-UI 上下文项。对标 Java AguiContext。
/// </summary>
public sealed record AguiContext(string Key, string Value);

/// <summary>
/// AG-UI 恢复响应。对标 Java AguiResume。
/// </summary>
public sealed record AguiResume(string Type, string? Id = null, string? Output = null);

/// <summary>
/// 输入内容块（多模态）。对标 Java InputContent。
/// </summary>
public abstract record InputContent(string Type);
public sealed record TextInputContent(string Text) : InputContent("text");
public sealed record ImageInputContent(InputContentSource Source) : InputContent("image");
public sealed record AudioInputContent(InputContentSource Source) : InputContent("audio");
public sealed record VideoInputContent(InputContentSource Source) : InputContent("video");

/// <summary>
/// 输入内容来源。对标 Java InputContentSource。
/// </summary>
public abstract record InputContentSource(string Type);
public sealed record UrlInputSource(string Url, string MimeType) : InputContentSource("url");
public sealed record DataInputSource(string Base64, string MimeType) : InputContentSource("data");

/// <summary>
/// 工具合并模式枚举。对标 Java ToolMergeMode。
/// </summary>
public enum ToolMergeMode { FrontendOnly, AgentOnly, MergeFrontendPriority }
