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

using AgentScope.Extensions.Store;

namespace AgentScope.Extensions.Store.Oss;

/// <summary>
/// Agent state store backed by Alibaba Cloud OSS (Object Storage Service).
/// 基于阿里云 OSS（对象存储服务）的 Agent 状态存储实现。
/// </summary>
/// <remarks>
/// Corresponds to Java: io.agentscope.extensions.oss.OssAgentStateStore
/// 对应 Java 实现: io.agentscope.extensions.oss.OssAgentStateStore
/// </remarks>
public sealed class OssAgentStateStore : DistributedAgentStateStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OssAgentStateStore"/> class.
    /// 初始化 <see cref="OssAgentStateStore"/> 类的新实例。
    /// </summary>
    /// <param name="store">The underlying OSS distributed store / 底层 OSS 分布式存储</param>
    /// <param name="keyPrefix">
    /// The key prefix for grouping agent state entries (default "agentstate").
    /// 用于分组 Agent 状态条目的键前缀（默认为 "agentstate"）。
    /// </param>
    public OssAgentStateStore(OssDistributedStore store, string keyPrefix = "agentstate")
        : base(store, keyPrefix)
    {
    }
}
