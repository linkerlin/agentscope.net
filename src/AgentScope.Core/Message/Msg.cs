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

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentScope.Core.Message;

/// <summary>
/// Core message class for inter-agent communication in the AgentScope framework.
/// Supports JSON serialization, text content extraction, and builder pattern construction.
/// Corresponds to Java: io.agentscope.core.message.Msg
/// AgentScope 框架中 Agent 间通信的核心消息类。
/// 支持 JSON 序列化、文本内容提取和构建器模式构造。
/// 对应 Java: io.agentscope.core.message.Msg
/// </summary>
public class Msg
{
    /// <summary>
    /// Unique message identifier, auto-generated as a GUID.
    /// 消息唯一标识符，自动生成为 GUID。
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Optional sender name (e.g., agent name or user name).
    /// 可选的发送者名称（例如 Agent 名称或用户名）。
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Message role: "system", "user", "assistant", or "tool".
    /// 消息角色："system"（系统）、"user"（用户）、"assistant"（助手）或 "tool"（工具）。
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    /// <summary>
    /// Message content — can be a plain string, a structured object, or a dictionary.
    /// 消息内容——可以是纯文本字符串、结构化对象或字典。
    /// </summary>
    [JsonPropertyName("content")]
    public object? Content { get; set; }

    /// <summary>
    /// Optional list of URLs associated with the message (e.g., image URLs, file links).
    /// 可选的与消息关联的 URL 列表（例如图片 URL、文件链接）。
    /// </summary>
    [JsonPropertyName("url")]
    public List<string>? Url { get; set; }

    /// <summary>
    /// Message creation timestamp in UTC.
    /// 消息创建时间戳（UTC 时间）。
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional dictionary for custom metadata key-value pairs.
    /// 可选的元数据字典，用于存储自定义键值对。
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Initializes a new instance of the Msg class with default values.
    /// 使用默认值初始化 Msg 类的新实例。
    /// </summary>
    public Msg()
    {
    }

    /// <summary>
    /// Initializes a new instance of the Msg class with the specified name, content, and role.
    /// 使用指定的名称、内容和角色初始化 Msg 类的新实例。
    /// </summary>
    /// <param name="name">Optional sender name / 可选的发送者名称。</param>
    /// <param name="content">Message content / 消息内容。</param>
    /// <param name="role">Message role (default: "user") / 消息角色（默认："user"）。</param>
    public Msg(string? name, object? content, string role = "user")
    {
        Name = name;
        Content = content;
        Role = role;
    }

    /// <summary>
    /// Extracts the text content from the message.
    /// If Content is a string, returns it directly.
    /// If Content is a dictionary with a "text" key, returns that value.
    /// Otherwise, returns Content.ToString().
    /// 从消息中提取文本内容。
    /// 如果 Content 是字符串，直接返回。
    /// 如果 Content 是包含 "text" 键的字典，返回该值。
    /// 否则返回 Content.ToString()。
    /// </summary>
    /// <returns>Extracted text content, or null if Content is null / 提取的文本内容，如果 Content 为 null 则返回 null。</returns>
    public string? GetTextContent()
    {
        if (Content is string text)
        {
            return text;
        }
        
        if (Content is Dictionary<string, object> dict && dict.ContainsKey("text"))
        {
            return dict["text"]?.ToString();
        }

        return Content?.ToString();
    }

    /// <summary>
    /// Sets the message content to a plain text string.
    /// 将消息内容设置为纯文本字符串。
    /// </summary>
    /// <param name="text">The text content to set / 要设置的文本内容。</param>
    public void SetTextContent(string text)
    {
        Content = text;
    }

    /// <summary>
    /// Creates a new MsgBuilder for fluent message construction.
    /// 创建一个新的 MsgBuilder 用于流畅的消息构造。
    /// </summary>
    /// <returns>A new MsgBuilder instance / 一个新的 MsgBuilder 实例。</returns>
    public static MsgBuilder Builder()
    {
        return new MsgBuilder();
    }

    /// <summary>
    /// Serializes the message to a pretty-printed JSON string.
    /// Null properties are omitted from the output.
    /// 将消息序列化为格式化的 JSON 字符串，忽略 null 属性。
    /// </summary>
    /// <returns>JSON string representation of the message / 消息的 JSON 字符串表示。</returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}

/// <summary>
/// Fluent builder for constructing Msg instances with a chainable API.
/// Corresponds to Java: io.agentscope.core.message.MsgBuilder
/// 用于流畅构造 Msg 实例的构建器，提供链式调用 API。
/// 对应 Java: io.agentscope.core.message.MsgBuilder
/// </summary>
public class MsgBuilder
{
    private readonly Msg _msg = new();

    /// <summary>
    /// Sets the message ID.
    /// 设置消息 ID。
    /// </summary>
    public MsgBuilder Id(string id)
    {
        _msg.Id = id;
        return this;
    }

    /// <summary>
    /// Sets the sender name.
    /// 设置发送者名称。
    /// </summary>
    public MsgBuilder Name(string name)
    {
        _msg.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the message role.
    /// 设置消息角色。
    /// </summary>
    public MsgBuilder Role(string role)
    {
        _msg.Role = role;
        return this;
    }

    /// <summary>
    /// Sets the message content (any object type).
    /// 设置消息内容（任意对象类型）。
    /// </summary>
    public MsgBuilder Content(object content)
    {
        _msg.Content = content;
        return this;
    }

    /// <summary>
    /// Sets the message content as a plain text string.
    /// 将消息内容设置为纯文本字符串。
    /// </summary>
    public MsgBuilder TextContent(string text)
    {
        _msg.Content = text;
        return this;
    }

    /// <summary>
    /// Sets the list of associated URLs.
    /// 设置关联的 URL 列表。
    /// </summary>
    public MsgBuilder Url(List<string> urls)
    {
        _msg.Url = urls;
        return this;
    }

    /// <summary>
    /// Sets the message timestamp.
    /// 设置消息时间戳。
    /// </summary>
    public MsgBuilder Timestamp(DateTime timestamp)
    {
        _msg.Timestamp = timestamp;
        return this;
    }

    /// <summary>
    /// Sets the entire metadata dictionary.
    /// 设置完整的元数据字典。
    /// </summary>
    public MsgBuilder Metadata(Dictionary<string, object> metadata)
    {
        _msg.Metadata = metadata;
        return this;
    }

    /// <summary>
    /// Adds or updates a single metadata key-value pair.
    /// 添加或更新单个元数据键值对。
    /// </summary>
    public MsgBuilder AddMetadata(string key, object value)
    {
        _msg.Metadata ??= new Dictionary<string, object>();
        _msg.Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured Msg instance.
    /// 构建并返回配置好的 Msg 实例。
    /// </summary>
    public Msg Build()
    {
        return _msg;
    }
}
