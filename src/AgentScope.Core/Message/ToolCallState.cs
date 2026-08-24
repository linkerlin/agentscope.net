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
/// Tool call state enum, tracking the lifecycle of a tool call within the agent.
/// Corresponds to Java: io.agentscope.core.message.ToolCallState
/// 工具调用状态枚举，跟踪代理内工具调用的生命周期。
/// 对应 Java: io.agentscope.core.message.ToolCallState
/// </summary>
public enum ToolCallState
{
    /// <summary>
    /// Tool call is pending and has not been processed yet.
    /// 工具调用待处理，尚未被处理。
    /// </summary>
    Pending,

    /// <summary>
    /// Awaiting user confirmation to execute the tool.
    /// 等待用户确认执行工具。
    /// </summary>
    Asking,

    /// <summary>
    /// Tool call has been allowed / approved.
    /// 工具调用已被允许/批准。
    /// </summary>
    Allowed,

    /// <summary>
    /// Tool call has been submitted for execution.
    /// 工具调用已提交执行。
    /// </summary>
    Submitted,

    /// <summary>
    /// Tool call has finished execution.
    /// 工具调用已完成执行。
    /// </summary>
    Finished
}

/// <summary>
/// Tool result state enum, indicating the outcome of a tool execution.
/// Corresponds to Java: io.agentscope.core.message.ToolResultState
/// 工具执行结果状态枚举，表示工具执行的结果。
/// 对应 Java: io.agentscope.core.message.ToolResultState
/// </summary>
public enum ToolResultState
{
    /// <summary>
    /// Execution completed successfully.
    /// 执行成功完成。
    /// </summary>
    Success,

    /// <summary>
    /// Execution encountered an error.
    /// 执行遇到错误。
    /// </summary>
    Error,

    /// <summary>
    /// Execution was interrupted before completion.
    /// 执行在完成前被中断。
    /// </summary>
    Interrupted,

    /// <summary>
    /// Execution was denied / rejected.
    /// 执行被拒绝。
    /// </summary>
    Denied,

    /// <summary>
    /// Execution is currently running.
    /// 执行正在进行中。
    /// </summary>
    Running
}
