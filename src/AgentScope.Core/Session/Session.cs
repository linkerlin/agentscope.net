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

namespace AgentScope.Core.Session;

/// <summary>
/// Represents an independent conversation session with its own context and metadata.
/// 表示一个独立的对话会话，拥有自己的上下文和元数据。
/// 
/// Sessions provide isolation between different conversations, allowing agents to maintain
/// separate state, context, and metadata for each interaction.
/// 会话在不同对话之间提供隔离，允许 Agent 为每次交互维护独立的状态、上下文和元数据。
/// 
/// Corresponds to Java: io.agentscope.core.session.Session
/// 对应 Java: io.agentscope.core.session.Session
/// </summary>
public class Session
{
    /// <summary>
    /// Gets the unique session identifier (UUID).
    /// 获取会话唯一标识符（UUID）。
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the session name for display purposes.
    /// 获取或设置会话名称，用于显示。
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the session creation timestamp (UTC).
    /// 获取会话创建时间戳（UTC）。
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets or sets the last update timestamp (UTC).
    /// 获取或设置最后更新时间戳（UTC）。
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets the session metadata dictionary for storing arbitrary key-value pairs.
    /// 获取会话元数据字典，用于存储任意键值对。
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets or sets the session status (Active, Paused, Closed).
    /// 获取或设置会话状态（Active, Paused, Closed）。
    /// </summary>
    public SessionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the name of the agent associated with this session.
    /// 获取或设置与此会话关联的 Agent 名称。
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// Gets the session context dictionary for storing runtime data.
    /// 获取会话上下文字典，用于存储运行时数据。
    /// </summary>
    public Dictionary<string, object> Context { get; }

    /// <summary>
    /// Initializes a new instance of the Session class.
    /// 初始化 Session 类的新实例。
    /// </summary>
    /// <param name="id">Optional session ID. Auto-generated as UUID if not provided.
    /// 可选的会话 ID。如果未提供，则自动生成为 UUID。</param>
    /// <param name="name">Optional session name. Auto-generated with timestamp if not provided.
    /// 可选的会话名称。如果未提供，则自动生成带时间戳的名称。</param>
    public Session(string? id = null, string? name = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        Name = name ?? $"Session-{DateTime.Now:yyyyMMdd-HHmmss}";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
        Context = new Dictionary<string, object>();
        Status = SessionStatus.Active;
    }

    /// <summary>
    /// Updates the session timestamp to the current UTC time.
    /// 将会话时间戳更新为当前 UTC 时间。
    /// Called internally when context or metadata changes.
    /// 当上下文或元数据更改时内部调用。
    /// </summary>
    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets a context value for the specified key.
    /// 为指定键设置上下文值。
    /// </summary>
    /// <param name="key">The context key. 上下文键。</param>
    /// <param name="value">The context value. 上下文值。</param>
    public void SetContext(string key, object value)
    {
        Context[key] = value;
        Touch();
    }

    /// <summary>
    /// Gets a typed context value for the specified key.
    /// 获取指定键的类型化上下文值。
    /// </summary>
    /// <typeparam name="T">The expected type of the value. 值的预期类型。</typeparam>
    /// <param name="key">The context key. 上下文键。</param>
    /// <returns>The typed value if found; otherwise default(T). 如果找到则返回类型化值；否则返回 default(T)。</returns>
    public T? GetContext<T>(string key)
    {
        if (Context.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Sets a metadata value for the specified key.
    /// 为指定键设置元数据值。
    /// </summary>
    /// <param name="key">The metadata key. 元数据键。</param>
    /// <param name="value">The metadata value. 元数据值。</param>
    public void SetMetadata(string key, object value)
    {
        Metadata[key] = value;
        Touch();
    }

    /// <summary>
    /// Gets a typed metadata value for the specified key.
    /// 获取指定键的类型化元数据值。
    /// </summary>
    /// <typeparam name="T">The expected type of the value. 值的预期类型。</typeparam>
    /// <param name="key">The metadata key. 元数据键。</param>
    /// <returns>The typed value if found; otherwise default(T). 如果找到则返回类型化值；否则返回 default(T)。</returns>
    public T? GetMetadata<T>(string key)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}

/// <summary>
/// Session status enumeration defining the lifecycle states of a session.
/// 会话状态枚举，定义会话的生命周期状态。
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// Session is active and ready for use.
    /// 会话处于活跃状态，可供使用。
    /// </summary>
    Active,
    
    /// <summary>
    /// Session is paused and not currently accepting new messages.
    /// 会话已暂停，当前不接受新消息。
    /// </summary>
    Paused,
    
    /// <summary>
    /// Session is closed and no longer available.
    /// 会话已关闭，不再可用。
    /// </summary>
    Closed
}
