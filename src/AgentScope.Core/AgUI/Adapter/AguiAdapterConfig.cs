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

using AgentScope.Core.AgUI.Model;

namespace AgentScope.Core.AgUI.Adapter;

/// <summary>
/// AG-UI adapter configuration, controls event emission and behavior.
/// AG-UI 适配器配置，控制事件发射和行为。
/// Corresponds to Java: AguiAdapterConfig
/// </summary>
public sealed record AguiAdapterConfig
{
    /// <summary>
    /// Tool merge mode; determines how frontend and agent tool definitions are merged.
    /// 工具合并模式；决定前端和 Agent 工具定义如何合并。
    /// </summary>
    public ToolMergeMode ToolMergeMode { get; init; } = ToolMergeMode.FrontendOnly;

    /// <summary>
    /// Whether to emit state snapshot/delta events.
    /// 是否发射状态快照/增量事件。
    /// </summary>
    public bool EmitStateEvents { get; init; }

    /// <summary>
    /// Whether to emit tool call argument events.
    /// 是否发射工具调用参数事件。
    /// </summary>
    public bool EmitToolCallArgs { get; init; } = true;

    /// <summary>
    /// Whether to emit token usage information.
    /// 是否发射 Token 用量信息。
    /// </summary>
    public bool EmitTokenUsage { get; init; }

    /// <summary>
    /// Whether to enable reasoning/thinking events.
    /// 是否启用推理/思考事件。
    /// </summary>
    public bool EnableReasoning { get; init; } = true;

    /// <summary>
    /// Whether to emit a RunFinished event after an error occurs.
    /// 是否在错误发生后仍然发射 RunFinished 事件。
    /// </summary>
    public bool EmitRunFinishedAfterError { get; init; } = true;

    /// <summary>
    /// Optional timeout for a single run execution.
    /// 单次运行的可选超时时间。
    /// </summary>
    public TimeSpan? RunTimeout { get; init; }

    /// <summary>
    /// Default agent identifier used when no specific agent is targeted.
    /// 默认 Agent 标识符，当未指定特定 Agent 时使用。
    /// </summary>
    public string DefaultAgentId { get; init; } = "default";

    /// <summary>
    /// Whether to emit sub-agent events as native AG-UI events rather than wrapping them.
    /// 是否将子 Agent 事件作为原生 AG-UI 事件发射（而非包装）。
    /// </summary>
    public bool EmitSubagentEventsAsNative { get; init; }
}
