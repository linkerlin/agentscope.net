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

namespace AgentScope.Extensions.Store.Cos;

/// <summary>
/// Tencent Cloud COS-based Agent state store.
/// Persists and retrieves agent runtime state (conversation context, memory snapshots, etc.)
/// using Tencent Cloud Object Storage (COS) as the backend.
/// 基于腾讯云 COS 的 Agent 状态存储。
/// 使用腾讯云对象存储（COS）作为后端，持久化和检索 Agent 运行时状态
/// （会话上下文、内存快照等）。
/// Corresponds to Java class: io.agentscope.extensions.cos.CosAgentStateStore
/// 对应 Java 类: io.agentscope.extensions.cos.CosAgentStateStore
/// </summary>
/// <remarks>
/// This store inherits from <see cref="DistributedAgentStateStore"/> and adds COS-specific
/// behavior on top of the generic distributed state store. It uses the COS REST API
/// (via <see cref="CosStore"/>) for all storage operations rather than the COS SDK,
/// avoiding SDK version compatibility issues.
/// 该存储继承自 <see cref="DistributedAgentStateStore"/>，在通用分布式状态存储之上
/// 增添了 COS 特定的行为。它通过 <see cref="CosStore"/> 使用 COS REST API
/// 进行所有存储操作，而非依赖 COS SDK，从而避免 SDK 版本兼容性问题。
/// </remarks>
public sealed class CosAgentStateStore : DistributedAgentStateStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosAgentStateStore"/> class.
    /// Initializes the COS-based agent state store with the given COS store and key prefix.
    /// 使用指定的 COS 存储和键前缀初始化基于 COS 的 Agent 状态存储实例。
    /// </summary>
    /// <param name="store">
    /// The underlying COS object store adapter used for all read/write operations.
    /// 底层 COS 对象存储适配器，用于所有读写操作。
    /// </param>
    /// <param name="keyPrefix">
    /// Optional prefix prepended to all keys to namespace agent states within the bucket.
    /// Defaults to "agentstate".
    /// 可选前缀，附加到所有键之前，用于在存储桶中隔离 Agent 状态的命名空间。
    /// 默认值为 "agentstate"。
    /// </param>
    public CosAgentStateStore(CosStore store, string keyPrefix = "agentstate")
        : base(store, keyPrefix)
    {
    }
}
