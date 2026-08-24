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

namespace AgentScope.Core.Message;

/// <summary>
/// Enum representing the reason why model generation stopped.
/// Corresponds to Java: io.agentscope.core.message.GenerateReason
/// 生成停止原因枚举，表示模型生成结束的原因。
/// 对应 Java: io.agentscope.core.message.GenerateReason
/// </summary>
public enum GenerateReason
{
    /// <summary>Model stopped naturally (e.g., stop sequence) / 模型自然停止（例如遇到停止序列）。</summary>
    ModelStop,

    /// <summary>Generation stopped because tool calls were requested / 生成因工具调用请求而停止。</summary>
    ToolCalls,

    /// <summary>Generation stopped due to structured output constraints / 生成因结构化输出约束而停止。</summary>
    StructuredOutput,

    /// <summary>Tool execution was suspended / 工具执行被暂停。</summary>
    ToolSuspended,

    /// <summary>Reasoning phase was stopped by request / 推理阶段被请求停止。</summary>
    ReasoningStopRequested,

    /// <summary>Acting phase was stopped by request / 行动阶段被请求停止。</summary>
    ActingStopRequested,

    /// <summary>Generation stopped to ask for user permission / 生成因请求用户权限而停止。</summary>
    PermissionAsking,

    /// <summary>Generation stopped by middleware request / 生成被中间件请求停止。</summary>
    MiddlewareStopRequested,

    /// <summary>All tool calls were denied / 所有工具调用均被拒绝。</summary>
    AllToolsDenied,

    /// <summary>Generation was interrupted / 生成被中断。</summary>
    Interrupted,

    /// <summary>Maximum iteration limit was reached / 达到最大迭代次数限制。</summary>
    MaxIterations
}
