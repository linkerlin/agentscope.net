// Copyright 2024-2026 the original author or authors.
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

namespace AgentScope.Harness.Filesystem.Remote.Store;

/// <summary>
/// 存储条目：键值及其元数据（创建/更新时间、内容类型）。
/// 对应 Java: io.agentscope.harness.agent.filesystem.remote.store.StoreItem
/// </summary>
public sealed class StoreItem
{
    public required string Key { get; init; }
    public required string Value { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ContentType { get; set; }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
