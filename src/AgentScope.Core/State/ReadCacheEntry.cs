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

namespace AgentScope.Core.State;

/// <summary>
/// 读取缓存条目：记录某次读取结果的缓存值、哈希与过期时间，用于避免重复读取/校验变更。
/// 对应 Java: io.agentscope.core.state.ReadCacheEntry
/// </summary>
public class ReadCacheEntry
{
    /// <summary>缓存内容</summary>
    public string? Content { get; set; }

    /// <summary>内容哈希（用于脏检查）</summary>
    public long Hash { get; set; }

    /// <summary>缓存写入时间</summary>
    public DateTimeOffset CachedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>读取快照版本号</summary>
    public long ReadVersion { get; set; }

    public ReadCacheEntry() { }

    public ReadCacheEntry(string? content, long hash)
    {
        Content = content;
        Hash = hash;
    }

    /// <summary>是否已过期</summary>
    public bool IsExpired(TimeSpan ttl) => DateTimeOffset.UtcNow - CachedAt > ttl;
}
