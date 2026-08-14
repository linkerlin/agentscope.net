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

namespace AgentScope.Core.Shutdown;

/// <summary>
/// 优雅关闭配置：超时、是否接受新请求、关闭策略等。
/// 对应 Java: io.agentscope.core.shutdown.GracefulShutdownConfig
/// </summary>
public class GracefulShutdownConfig
{
    /// <summary>关闭等待活跃请求完成的最长时间。</summary>
    public System.TimeSpan DrainTimeout { get; set; } = System.TimeSpan.FromSeconds(30);

    /// <summary>进入关闭后是否拒绝新请求（默认拒绝）。</summary>
    public bool RejectNewRequests { get; set; } = true;

    /// <summary>是否在关闭时持久化 Agent 状态。</summary>
    public bool PersistStateOnShutdown { get; set; } = true;

    /// <summary>中断发生后对已产出部分推理结果的处理策略。</summary>
    public PartialReasoningPolicy PartialReasoningPolicy { get; set; } = PartialReasoningPolicy.Discard;

    /// <summary>是否注册进程级关闭钩子（进程退出时触发优雅关闭）。</summary>
    public bool RegisterProcessShutdownHook { get; set; } = true;

    /// <summary>默认配置。</summary>
    public static GracefulShutdownConfig Default => new();
}
