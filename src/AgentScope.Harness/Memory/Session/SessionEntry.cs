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

using System.Text.Json;
using System.Text.Json.Serialization;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Memory.Session;

/// <summary>
/// Base class for JSONL session entries using JsonDerivedType polymorphic serialization.<br />
/// JSONL 会话条目基类，使用 <see cref="JsonDerivedTypeAttribute"/> 多态序列化。
/// </summary>
[JsonDerivedType(typeof(MessageEntry), typeDiscriminator: "message")]
[JsonDerivedType(typeof(ToolUseEntry), typeDiscriminator: "tool_use")]
[JsonDerivedType(typeof(ToolResultEntry), typeDiscriminator: "tool_result")]
[JsonDerivedType(typeof(CompactionEntry), typeDiscriminator: "compaction")]
[JsonDerivedType(typeof(SummaryEntry), typeDiscriminator: "summary")]
public abstract record SessionEntry
{
    /// <summary>Unix timestamp in milliseconds of when this entry was created / 条目创建时的 Unix 毫秒时间戳</summary>
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Discriminator type string for polymorphic deserialization / 多态反序列化的鉴别器类型字符串</summary>
    public abstract string Type { get; }
}

/// <summary>
/// Represents a conversation message entry in the session log.<br />
/// 消息条目：表示会话日志中的一条对话消息。
/// </summary>
public sealed record MessageEntry : SessionEntry
{
    /// <inheritdoc />
    public override string Type => "message";

    /// <summary>Message role (e.g. "user", "assistant", "system") / 消息角色（如 "user"、"assistant"、"system"）</summary>
    public string Role { get; init; } = "";

    /// <summary>Optional sender name / 可选的发送者名称</summary>
    public string? Name { get; init; }

    /// <summary>Message text content / 消息文本内容</summary>
    public string? Content { get; init; }

    /// <summary>Unique message identifier / 消息唯一标识</summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Creates a <see cref="MessageEntry"/> from an <see cref="Msg"/>.<br />
    /// 从 <see cref="Msg"/> 创建 <see cref="MessageEntry"/>。
    /// </summary>
    /// <param name="msg">Source message / 源消息</param>
    /// <returns>A new MessageEntry / 新的 MessageEntry 实例</returns>
    public static MessageEntry FromMsg(Msg msg) => new()
    {
        Role = msg.Role,
        Name = msg.Name,
        Content = msg.GetTextContent(),
        MessageId = msg.Id
    };

    /// <summary>
    /// Converts this entry back into an <see cref="Msg"/>.<br />
    /// 将此条目转换回 <see cref="Msg"/>。
    /// </summary>
    /// <returns>A new Msg instance / 新的 Msg 实例</returns>
    public Msg ToMsg() => Msg.Builder()
        .Role(Role)
        .Name(Name ?? "")
        .TextContent(Content ?? "")
        .Build();
}

/// <summary>
/// Represents a tool invocation entry in the session log.<br />
/// 工具调用条目：表示会话日志中的一次工具调用。
/// </summary>
public sealed record ToolUseEntry : SessionEntry
{
    /// <inheritdoc />
    public override string Type => "tool_use";

    /// <summary>Name of the invoked tool / 被调用工具的名称</summary>
    public string ToolName { get; init; } = "";

    /// <summary>Identifier for this tool call / 本次工具调用的标识</summary>
    public string? ToolCallId { get; init; }

    /// <summary>Arguments passed to the tool / 传递给工具的参数</summary>
    public string? Arguments { get; init; }
}

/// <summary>
/// Represents a tool execution result entry in the session log.<br />
/// 工具结果条目：表示会话日志中的一次工具执行结果。
/// </summary>
public sealed record ToolResultEntry : SessionEntry
{
    /// <inheritdoc />
    public override string Type => "tool_result";

    /// <summary>Identifier linking to the corresponding tool call / 对应工具调用的标识</summary>
    public string? ToolCallId { get; init; }

    /// <summary>Tool execution result content / 工具执行结果内容</summary>
    public string? Result { get; init; }

    /// <summary>Whether the tool execution resulted in an error / 工具执行是否返回错误</summary>
    public bool IsError { get; init; }
}

/// <summary>
/// Represents a compaction marker entry recording when and how many entries were compressed.<br />
/// 压缩标记条目：记录压缩操作的时间及条目数量变化。
/// </summary>
public sealed record CompactionEntry : SessionEntry
{
    /// <inheritdoc />
    public override string Type => "compaction";

    /// <summary>Summary description of the compaction / 压缩操作的摘要描述</summary>
    public string? Summary { get; init; }

    /// <summary>Number of original messages before compaction / 压缩前的原始消息数</summary>
    public int OriginalMessageCount { get; init; }

    /// <summary>Number of messages after compaction / 压缩后的消息数</summary>
    public int CompressedMessageCount { get; init; }
}

/// <summary>
/// Represents a conversation summary entry generated by LLM summarization.<br />
/// 对话摘要条目：由 LLM 汇总生成的对话摘要。
/// </summary>
public sealed record SummaryEntry : SessionEntry
{
    /// <inheritdoc />
    public override string Type => "summary";

    /// <summary>Summary text content / 摘要文本内容</summary>
    public string? Summary { get; init; }

    /// <summary>Number of source messages this summary was generated from / 生成此摘要的源消息数</summary>
    public int SourceMessageCount { get; init; }
}
