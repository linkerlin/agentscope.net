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
/// Session 类，表示一个独立的对话会话
/// Represents an independent conversation session
/// </summary>
public class Session
{
    /// <summary>
    /// Session 唯一标识符
    /// Unique session identifier
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Session 名称
    /// Session name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Session 创建时间
    /// Session creation time
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Session 最后更新时间
    /// Last update time
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Session 元数据
    /// Session metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Session 状态（active, paused, closed）
    /// Session status
    /// </summary>
    public SessionStatus Status { get; set; }

    /// <summary>
    /// Session 相关的 Agent 名称
    /// Associated agent name
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// Session 上下文数据
    /// Session context data
    /// </summary>
    public Dictionary<string, object> Context { get; }

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
    /// 更新 Session 时间戳
    /// Update session timestamp
    /// </summary>
    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置上下文值
    /// Set context value
    /// </summary>
    public void SetContext(string key, object value)
    {
        Context[key] = value;
        Touch();
    }

    /// <summary>
    /// 获取上下文值
    /// Get context value
    /// </summary>
    public T? GetContext<T>(string key)
    {
        if (Context.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// 设置元数据
    /// Set metadata
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        Metadata[key] = value;
        Touch();
    }

    /// <summary>
    /// 获取元数据
    /// Get metadata
    /// </summary>
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
/// Session 状态枚举
/// Session status enumeration
/// </summary>
public enum SessionStatus
{
    /// <summary>活跃 Active</summary>
    Active,
    
    /// <summary>暂停 Paused</summary>
    Paused,
    
    /// <summary>已关闭 Closed</summary>
    Closed
}
