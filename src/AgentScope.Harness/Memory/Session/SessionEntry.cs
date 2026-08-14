using System.Text.Json;
using System.Text.Json.Serialization;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Memory.Session;

/// <summary>JSONL 会话条目基类，使用 JsonDerivedType 多态序列化</summary>
[JsonDerivedType(typeof(MessageEntry), typeDiscriminator: "message")]
[JsonDerivedType(typeof(ToolUseEntry), typeDiscriminator: "tool_use")]
[JsonDerivedType(typeof(ToolResultEntry), typeDiscriminator: "tool_result")]
[JsonDerivedType(typeof(CompactionEntry), typeDiscriminator: "compaction")]
[JsonDerivedType(typeof(SummaryEntry), typeDiscriminator: "summary")]
public abstract record SessionEntry
{
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public abstract string Type { get; }
}

/// <summary>消息条目</summary>
public sealed record MessageEntry : SessionEntry
{
    public override string Type => "message";
    public string Role { get; init; } = "";
    public string? Name { get; init; }
    public string? Content { get; init; }
    public string? MessageId { get; init; }

    public static MessageEntry FromMsg(Msg msg) => new()
    {
        Role = msg.Role,
        Name = msg.Name,
        Content = msg.GetTextContent(),
        MessageId = msg.Id
    };

    public Msg ToMsg() => Msg.Builder()
        .Role(Role)
        .Name(Name ?? "")
        .TextContent(Content ?? "")
        .Build();
}

/// <summary>工具调用条目</summary>
public sealed record ToolUseEntry : SessionEntry
{
    public override string Type => "tool_use";
    public string ToolName { get; init; } = "";
    public string? ToolCallId { get; init; }
    public string? Arguments { get; init; }
}

/// <summary>工具结果条目</summary>
public sealed record ToolResultEntry : SessionEntry
{
    public override string Type => "tool_result";
    public string? ToolCallId { get; init; }
    public string? Result { get; init; }
    public bool IsError { get; init; }
}

/// <summary>压缩标记条目</summary>
public sealed record CompactionEntry : SessionEntry
{
    public override string Type => "compaction";
    public string? Summary { get; init; }
    public int OriginalMessageCount { get; init; }
    public int CompressedMessageCount { get; init; }
}

/// <summary>对话摘要条目</summary>
public sealed record SummaryEntry : SessionEntry
{
    public override string Type => "summary";
    public string? Summary { get; init; }
    public int SourceMessageCount { get; init; }
}
